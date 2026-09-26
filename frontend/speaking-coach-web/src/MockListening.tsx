import { useCallback, useEffect, useRef, useState } from 'react';
import { apiJson, postJson } from './api';
import { msg, useT } from './i18n';
import { cefrMsg } from './locales/cefr';
import { mockMsg } from './locales/mock';
import { mockObjMsg } from './locales/mockObjective';
import { formatClock } from './mockLogic';
import { answeredCount, ObjectiveQuestions, QuestionMap, questionNumbers, type Answers, type ClientGroup } from './ObjectiveQuestions';
import { isSpeechSupported, sleep, splitSentences, stopSpeaking } from './speech';
import { PageHeader } from './ui';

interface ScriptLine {
  speaker: string;
  voice: 'male' | 'female';
  text: string;
}

interface ListeningClient {
  id: string;
  parts: { part: number; context: string; script: ScriptLine[]; groups: ClientGroup[] }[];
}

const READ_SECONDS = 30;
const REVIEW_SECONDS = 120;

const MALE = /male\b|daniel|david|fred|alex|george|james|guy|arthur|ryan|mark|thomas|oliver|aaron/i;
const FEMALE = /female|samantha|karen|zira|susan|victoria|moira|tessa|serena|kate|libby|sonia|jenny|aria|emma|google us english|google uk english female/i;

/** Ikki xil ovoz: erkak va ayol (bo'lmasa — bitta ovoz, balandligi farqli). */
export function pickVoices(voices: SpeechSynthesisVoice[]): { male?: SpeechSynthesisVoice; female?: SpeechSynthesisVoice } {
  const en = voices.filter((v) => v.lang.toLowerCase().startsWith('en'));
  const female = en.find((v) => FEMALE.test(v.name)) ?? en[0];
  const male = en.find((v) => MALE.test(v.name) && !/female/i.test(v.name)) ?? en.find((v) => v !== female) ?? female;
  return { male, female };
}

function speakOne(text: string, voice: SpeechSynthesisVoice | undefined, pitch: number): Promise<void> {
  return new Promise((resolve) => {
    const u = new SpeechSynthesisUtterance(text);
    u.lang = voice?.lang ?? 'en-GB';
    if (voice) u.voice = voice;
    u.rate = 0.95;
    u.pitch = pitch;
    u.onend = () => resolve();
    u.onerror = () => resolve();
    window.speechSynthesis.speak(u);
    // Ba'zi brauzerlar "end" bermaydi — qotib qolmaslik uchun vaqt chegarasi.
    setTimeout(resolve, 2500 + text.split(/\s+/).length * 480);
  });
}

type Phase = 'loading' | 'intro' | 'read' | 'play' | 'review' | 'submitting' | 'error';

/**
 * Tinglash tartibi. IELTS: har yozuv BIR marta. CEFR (Multilevel): har yozuv
 * IKKI marta; 1-qismda har gap ketma-ket ikki marta ("You will hear each
 * sentence twice"), boshqa qismlarda butun yozuv qaytadan.
 */
export function playbackPlan(exam: 'ielts' | 'cefr', part: number, lines: number): number[][] {
  const all = Array.from({ length: lines }, (_, i) => i);
  if (exam === 'ielts') return [all];
  if (part === 1) return [all.flatMap((i) => [i, i])];
  return [all, all];
}

/**
 * Listening mock: har qism oldidan 30 s savollarni o'qish, keyin yozuv
 * eshittiriladi (brauzer ovozi, suhbatda ikki xil ovoz), oxirida 2 daqiqa
 * tekshirish. Skript imtihon tugaguncha ko'rsatilmaydi.
 */
export function MockListening({
  go,
  sessionId,
  onSubmitted,
  exam = 'ielts',
}: {
  go: (route: string) => void;
  sessionId?: string;
  onSubmitted?: (id: string) => void;
  exam?: 'ielts' | 'cefr';
}) {
  const t = useT(mockObjMsg);
  const tm = useT(mockMsg);
  const tc = useT(cefrMsg);
  const [test, setTest] = useState<ListeningClient | null>(null);
  const [repeated, setRepeated] = useState(false);
  const [phase, setPhase] = useState<Phase>('loading');
  const [part, setPart] = useState(0);
  const [answers, setAnswers] = useState<Answers>({});
  const [countdown, setCountdown] = useState(0);
  const [error, setError] = useState('');
  const [listenAgain, setListenAgain] = useState(false);
  const cancelled = useRef(false);
  const startedAt = useRef(0);
  const submittedRef = useRef(false);
  const answersRef = useRef<Answers>({});
  answersRef.current = answers;

  useEffect(() => {
    apiJson<{ test: ListeningClient; repeated: boolean }>(`/api/mock/${exam}/listening/new`)
      .then(({ test, repeated }) => {
        setTest(test);
        setRepeated(repeated);
        setPhase('intro');
      })
      .catch((e) => {
        setError(e instanceof Error ? e.message : String(e));
        setPhase('error');
      });
    return () => {
      cancelled.current = true;
      stopSpeaking();
    };
  }, [exam]);

  useEffect(() => {
    if (phase === 'loading' || phase === 'intro' || phase === 'error') return;
    const warn = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      e.returnValue = msg(mockMsg).leaveWarning;
    };
    window.addEventListener('beforeunload', warn);
    return () => window.removeEventListener('beforeunload', warn);
  }, [phase]);

  const submit = useCallback(async () => {
    if (!test || submittedRef.current) return;
    submittedRef.current = true;
    stopSpeaking();
    setPhase('submitting');
    try {
      const res = await postJson<{ id: string }>(`/api/mock/${exam}/listening`, {
        testId: test.id,
        answers: answersRef.current,
        secondsUsed: Math.round((Date.now() - startedAt.current) / 1000),
        sessionId,
      });
      if (onSubmitted) onSubmitted(res.id);
      else go(`mock/result/${res.id}`);
    } catch (e) {
      submittedRef.current = false;
      setError(e instanceof Error ? e.message : String(e));
      setPhase('review');
    }
  }, [test, sessionId, onSubmitted, go, exam]);

  // Ortga sanash: savollarni o'qish (read) yoki tekshirish (review) vaqti.
  useEffect(() => {
    if (phase !== 'read' && phase !== 'review') return;
    if (countdown <= 0) {
      if (phase === 'read') setPhase('play');
      else void submit();
      return;
    }
    const id = setTimeout(() => setCountdown((c) => c - 1), 1000);
    return () => clearTimeout(id);
  }, [phase, countdown, submit]);

  // Yozuvni o'ynatish — bir marta; tugagach keyingi qism yoki tekshirish.
  useEffect(() => {
    if (phase !== 'play' || !test) return;
    let alive = true;
    (async () => {
      const { male, female } = pickVoices(window.speechSynthesis.getVoices());
      const sameVoice = male === female;
      const script = test.parts[part].script;
      const plan = playbackPlan(exam, test.parts[part].part, script.length);
      for (let round = 0; round < plan.length; round++) {
        if (round > 0) {
          await sleep(1500);
          if (!alive || cancelled.current) return;
          setListenAgain(true);
        }
        for (const idx of plan[round]) {
          const line = script[idx];
          for (const sentence of splitSentences(line.text)) {
            if (!alive || cancelled.current) return;
            const isMale = line.voice === 'male';
            await speakOne(sentence, isMale ? male : female, sameVoice ? (isMale ? 0.8 : 1.2) : 1);
          }
          await sleep(350);
        }
      }
      setListenAgain(false);
      if (!alive || cancelled.current) return;
      if (part + 1 < test.parts.length) {
        await sleep(1500);
        setPart(part + 1);
        setCountdown(READ_SECONDS);
        setPhase('read');
      } else {
        setCountdown(REVIEW_SECONDS);
        setPhase('review');
      }
    })();
    return () => {
      alive = false;
      stopSpeaking();
    };
  }, [phase, part, test, exam]);

  const back = sessionId ? undefined : () => go('mock');
  const title = exam === 'cefr' ? tc.listeningTitle : t.listeningTitle;
  const header = <PageHeader title={title} onBack={phase === 'intro' || phase === 'error' || phase === 'loading' ? back : undefined} backLabel={tm.title} />;

  if (phase === 'loading') return <>{header}<p className="muted" role="status">⏳ {t.preparing}</p></>;
  if (phase === 'error' || !test) return <>{header}<div className="card"><p className="error" role="alert">{error}</p></div></>;

  if (!isSpeechSupported()) {
    return <>{header}<div className="card"><p className="error">{t.ttsMissing}</p></div></>;
  }

  if (phase === 'intro') {
    return (
      <>
        {header}
        {repeated && <p className="muted small">{t.repeated}</p>}
        <div className="card stack">
          <h3 style={{ margin: 0 }}>{t.soundCheck}</h3>
          <p className="small" style={{ margin: 0 }}>{t.soundCheckText}</p>
          <button
            className="btn btn-outline"
            onClick={() => {
              stopSpeaking();
              const { female } = pickVoices(window.speechSynthesis.getVoices());
              void speakOne(t.sample, female, 1);
            }}
          >
            {t.playSample}
          </button>
          <ul className="small" style={{ margin: 0, paddingLeft: 20 }}>
            {(exam === 'cefr' ? tc.listeningRules : t.listeningRules).map((r) => <li key={r}>{r}</li>)}
          </ul>
          <button
            className="btn btn-primary block"
            style={{ minHeight: 52 }}
            onClick={() => {
              stopSpeaking();
              startedAt.current = Date.now();
              setPart(0);
              setCountdown(READ_SECONDS);
              setPhase('read');
            }}
          >
            {t.begin}
          </button>
          <p className="muted tiny" style={{ margin: 0 }}>{exam === 'cefr' ? tc.disclaimer : tm.disclaimer}</p>
        </div>
      </>
    );
  }

  const all = test.parts.flatMap((p) => questionNumbers(p.groups));
  const answered = answeredCount(answers, all);
  const current = test.parts[part];
  const showAll = phase === 'review' || phase === 'submitting';

  return (
    <>
      {header}
      <div className="card" style={{ position: 'sticky', top: 8, zIndex: 5, padding: '10px 14px' }} data-testid="listening-status">
        <div className="spread" style={{ alignItems: 'center' }}>
          <span>
            <strong>{showAll ? t.answered(answered, all.length) : t.part(current.part)}</strong>
            <span className="muted small" style={{ display: 'block' }} role="status">
              {phase === 'read' && t.readQuestions(countdown)}
              {phase === 'play' && (listenAgain ? tc.listenAgain : t.playing)}
              {phase === 'review' && t.review(formatClock(countdown))}
              {phase === 'submitting' && '⏳'}
            </span>
          </span>
          {phase === 'read' && (
            <button className="btn btn-outline" onClick={() => setCountdown(0)}>{t.startNow}</button>
          )}
          {phase === 'review' && (
            <button className="btn btn-primary" onClick={() => window.confirm(t.confirmSubmit(all.length - answered)) && void submit()}>
              {t.submit}
            </button>
          )}
        </div>
      </div>

      {error && <p className="error small" role="alert">{error}</p>}

      {(showAll ? test.parts : [current]).map((p) => (
        <div className="card stack" key={p.part}>
          <div className="small">
            <strong>{t.part(p.part)}</strong> <span className="muted">— {p.context}</span>
          </div>
          <ObjectiveQuestions
            groups={p.groups}
            answers={answers}
            onChange={(n, v) => setAnswers((a) => ({ ...a, [n]: v }))}
            disabled={phase === 'submitting'}
          />
        </div>
      ))}

      {showAll && (
        <div className="card">
          <QuestionMap numbers={all} answers={answers} onJump={(n) => document.getElementById(`q-${n}`)?.scrollIntoView({ behavior: 'smooth' })} />
        </div>
      )}
    </>
  );
}
