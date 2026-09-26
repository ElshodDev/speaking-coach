import { useCallback, useEffect, useRef, useState } from 'react';
import { apiJson } from './api';
import { msg, useT } from './i18n';
import { mockMsg } from './locales/mock';
import { formatClock, speakingSteps, type SpeakingSet, type SpeakingStep, type SpeakingTiming } from './mockLogic';
import { isSpeechSupported, sleep, speakAsync, stopSpeaking } from './speech';
import { PageHeader } from './ui';

interface Recorded {
  blob: Blob;
  seconds: number;
}

type Phase = 'loading' | 'intro' | 'running' | 'submitting' | 'error';
type StepState = 'asking' | 'recording' | 'prep';

/**
 * IELTS Speaking mock: savol ovoz bilan o'qiladi → javob avtomatik yoziladi
 * (vaqt chegarasi bilan) → keyingi savol. Orqaga qaytish yo'q, xuddi
 * haqiqiy imtihondagidek. Oxirida barcha javoblar bitta so'rovda baholanadi.
 */
export function MockSpeaking({ go }: { go: (route: string) => void }) {
  const t = useT(mockMsg);
  const [phase, setPhase] = useState<Phase>('loading');
  const [error, setError] = useState('');
  const [set, setSet] = useState<SpeakingSet | null>(null);
  const [steps, setSteps] = useState<SpeakingStep[]>([]);
  const [stepIndex, setStepIndex] = useState(0);
  const [stepState, setStepState] = useState<StepState>('asking');
  const [elapsed, setElapsed] = useState(0);
  const [notes, setNotes] = useState('');

  const streamRef = useRef<MediaStream | null>(null);
  const recorderRef = useRef<MediaRecorder | null>(null);
  const answersRef = useRef<Map<number, Recorded>>(new Map());
  const startedAtRef = useRef(0);
  const cancelledRef = useRef(false);

  useEffect(() => {
    apiJson<{ set: SpeakingSet; timing: SpeakingTiming }>('/api/mock/ielts/speaking/new')
      .then((data) => {
        setSet(data.set);
        setSteps(speakingSteps(data.set, data.timing));
        setPhase('intro');
      })
      .catch((e) => {
        setError(e instanceof Error ? e.message : String(e));
        setPhase('error');
      });
    return () => {
      cancelledRef.current = true;
      stopSpeaking();
      if (recorderRef.current?.state === 'recording') recorderRef.current.stop();
      streamRef.current?.getTracks().forEach((tr) => tr.stop());
    };
  }, []);

  // Imtihon davomida sahifani yopmoqchi bo'lsa — ogohlantiramiz.
  useEffect(() => {
    if (phase !== 'running') return;
    const onBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      e.returnValue = msg(mockMsg).leaveWarning;
    };
    window.addEventListener('beforeunload', onBeforeUnload);
    return () => window.removeEventListener('beforeunload', onBeforeUnload);
  }, [phase]);

  async function begin() {
    try {
      streamRef.current = await navigator.mediaDevices.getUserMedia({ audio: true });
      stepIndexRef.current = 0;
      setStepIndex(0);
      setPhase('running');
    } catch {
      setError(t.micDenied);
    }
  }

  const submittedRef = useRef(false);
  const submit = useCallback(async () => {
    if (!set || submittedRef.current) return;
    submittedRef.current = true;
    setPhase('submitting');
    streamRef.current?.getTracks().forEach((tr) => tr.stop());
    const form = new FormData();
    form.append('setId', set.id);
    answersRef.current.forEach((a, index) => {
      const ext = a.blob.type.includes('mp4') ? 'mp4' : a.blob.type.includes('ogg') ? 'ogg' : 'webm';
      form.append(`a${index}`, a.blob, `a${index}.${ext}`);
      form.append(`s${index}`, String(a.seconds));
    });
    try {
      const res = await apiJson<{ id: string }>('/api/mock/ielts/speaking', { method: 'POST', body: form });
      go(`mock/result/${res.id}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
      setPhase('error');
    }
  }, [set, go]);

  // Joriy qadam ref'da ham — MediaRecorder.onstop kabi eski closure'lar ham
  // to'g'ri qadamni ko'rishi uchun (setState updater ichida yon ta'sir qilmaymiz).
  const stepIndexRef = useRef(0);
  const advance = useCallback(() => {
    const next = stepIndexRef.current + 1;
    if (next >= steps.length) {
      void submit();
      return;
    }
    stepIndexRef.current = next;
    setStepIndex(next);
  }, [steps.length, submit]);

  // Yozishni to'xtatib, javobni saqlaydi, keyin keyingi qadamga o'tadi.
  const finishAnswer = useCallback(() => {
    const rec = recorderRef.current;
    if (rec && rec.state === 'recording') rec.stop(); // onstop → saqlaydi va advance()
  }, []);

  // Har bir qadamga kirganda: savolni o'qish → yozishni boshlash; yoki tayyorgarlik.
  useEffect(() => {
    if (phase !== 'running') return;
    const step = steps[stepIndex];
    if (!step) return;
    let alive = true;
    setElapsed(0);

    if (step.kind === 'prep') {
      setStepState('prep');
      startedAtRef.current = Date.now();
      return () => {
        alive = false;
      };
    }

    (async () => {
      setStepState('asking');
      stopSpeaking();
      // 2-qismda imtihonchi kartochkani beradi va darhol gapirishni kutadi — qayta o'qimaymiz.
      // Ba'zi brauzerlarda ovoz sintezi "tugadi" hodisasini bermaydi — imtihon
      // qotib qolmasligi uchun savol uzunligiga qarab vaqt chegarasi qo'yamiz.
      if (step.part !== 2 && isSpeechSupported()) {
        const words = step.text.split(/\s+/).length;
        await Promise.race([speakAsync(step.text, 0.95), sleep(3000 + words * 450)]);
        stopSpeaking();
      }
      if (!alive || cancelledRef.current || !streamRef.current) return;

      const rec = new MediaRecorder(streamRef.current);
      const chunks: Blob[] = [];
      rec.ondataavailable = (e) => {
        if (e.data.size > 0) chunks.push(e.data);
      };
      rec.onstop = () => {
        const seconds = Math.round((Date.now() - startedAtRef.current) / 1000);
        answersRef.current.set(step.index, { blob: new Blob(chunks, { type: rec.mimeType || 'audio/webm' }), seconds });
        if (!cancelledRef.current) advance();
      };
      recorderRef.current = rec;
      startedAtRef.current = Date.now();
      rec.start();
      setStepState('recording');
    })();

    return () => {
      alive = false;
    };
  }, [phase, stepIndex, steps, advance]);

  // Taymer: tayyorgarlik yoki javob vaqti tugasa — avtomatik keyingisiga.
  useEffect(() => {
    if (phase !== 'running' || stepState === 'asking') return;
    const step = steps[stepIndex];
    if (!step) return;
    const limit = step.kind === 'prep' ? step.seconds : step.maxSeconds;
    const id = setInterval(() => {
      const s = (Date.now() - startedAtRef.current) / 1000;
      setElapsed(s);
      if (s >= limit) {
        clearInterval(id);
        if (step.kind === 'prep') advance();
        else finishAnswer();
      }
    }, 250);
    return () => clearInterval(id);
  }, [phase, stepState, stepIndex, steps, advance, finishAnswer]);

  const step = steps[stepIndex];
  const header = <PageHeader title={t.speakingTitle} onBack={phase === 'running' || phase === 'submitting' ? undefined : () => go('mock')} backLabel={t.title} />;

  if (phase === 'loading') return <>{header}<p className="muted">…</p></>;

  if (phase === 'error') {
    return (
      <>
        {header}
        <div className="card">
          <p className="error" role="alert">{error}</p>
          {answersRef.current.size > 0 && (
            <>
              <p className="small">{t.answersKept}</p>
              <button
                className="btn btn-primary"
                onClick={() => {
                  submittedRef.current = false;
                  void submit();
                }}
              >
                {t.retrySubmit}
              </button>{' '}
            </>
          )}
          <button className="btn btn-outline" onClick={() => go('mock')}>{t.backToMock}</button>
        </div>
      </>
    );
  }

  if (phase === 'intro') {
    return (
      <>
        {header}
        <div className="card stack">
          <h3 style={{ margin: 0 }}>{t.micCheckTitle}</h3>
          <p style={{ margin: 0 }}>{t.micCheckText}</p>
          <ul className="small" style={{ margin: 0, paddingLeft: 20 }}>
            {t.rules.map((r) => <li key={r}>{r}</li>)}
          </ul>
          {error && <p className="error small" role="alert">{error}</p>}
          <button className="btn btn-primary block" onClick={begin} style={{ minHeight: 52 }}>
            🎙 {t.allowMic}
          </button>
          <p className="muted tiny" style={{ margin: 0 }}>{t.disclaimer}</p>
        </div>
      </>
    );
  }

  if (phase === 'submitting') {
    return (
      <>
        {header}
        <div className="card" role="status">
          <p style={{ margin: 0 }}>⏳ {t.submitting}</p>
        </div>
      </>
    );
  }

  if (!step || !set) return null;

  // ---- Imtihon jarayoni ----
  const part = step.kind === 'prep' ? 2 : step.part;
  const limit = step.kind === 'prep' ? step.seconds : step.maxSeconds;
  const left = limit - elapsed;
  const progress = Math.min(100, (elapsed / limit) * 100);
  const answeredCount = steps.slice(0, stepIndex).filter((s) => s.kind === 'answer').length;
  const totalAnswers = steps.filter((s) => s.kind === 'answer').length;

  return (
    <>
      {header}
      <div className="card stack" data-testid="mock-speaking">
        <div className="spread small">
          <strong>{t.part(part)}</strong>
          <span className="muted">
            {[step.kind === 'answer' && step.part !== 2 ? t.questionOf(step.numberInPart, step.ofInPart) : null, `${answeredCount}/${totalAnswers}`]
              .filter(Boolean)
              .join(' · ')}
          </span>
        </div>

        {(step.kind === 'prep' || step.part === 2) && (
          <div className="quote" style={{ margin: 0 }}>
            <strong>{set.part2.topic}</strong>
            <div className="small" style={{ marginTop: 6 }}>{t.youShouldSay}</div>
            <ul className="small" style={{ margin: '4px 0', paddingLeft: 20 }}>
              {set.part2.points.map((p) => <li key={p}>{p}</li>)}
            </ul>
            <div className="small">{set.part2.explain}</div>
          </div>
        )}

        {step.kind === 'answer' && step.part !== 2 && (
          <p style={{ fontSize: '1.15rem', fontWeight: 600, margin: 0 }}>{step.text}</p>
        )}

        {step.kind === 'prep' ? (
          <>
            <div className="spread">
              <strong>{t.prepTitle}</strong>
              <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 700 }}>{formatClock(left)}</span>
            </div>
            <div className="bar" aria-hidden><div className="fill-good" style={{ width: `${progress}%` }} /></div>
            <p className="muted small" style={{ margin: 0 }}>{t.prepText}</p>
            <textarea
              className="input"
              rows={4}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={t.notesPlaceholder}
              aria-label={t.notesPlaceholder}
            />
            <button className="btn btn-primary block" onClick={advance}>{t.readyNow}</button>
          </>
        ) : stepState === 'asking' ? (
          <p className="muted" role="status" style={{ margin: 0 }}>{t.listening}</p>
        ) : (
          <>
            {step.part === 2 && notes && <p className="muted small" style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{notes}</p>}
            <div className="spread" role="timer" aria-live="off">
              <span><span className="recording-dot" /> {t.recording}</span>
              <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 700 }}>
                {formatClock(elapsed)} / {formatClock(limit)}
              </span>
            </div>
            <div className="bar" aria-hidden><div className={left < 10 ? 'fill-low' : 'fill-good'} style={{ width: `${progress}%` }} /></div>
            {step.part === 2 && <p className="muted small" style={{ margin: 0 }}>{t.speakTwoMinutes}</p>}
            <button className="btn btn-primary block" onClick={finishAnswer} style={{ minHeight: 52 }}>
              {step.part === 2 ? t.finishPart2 : t.next}
            </button>
          </>
        )}
      </div>
    </>
  );
}
