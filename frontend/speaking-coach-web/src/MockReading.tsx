import { useCallback, useEffect, useRef, useState } from 'react';
import { apiJson, postJson } from './api';
import { msg, useT } from './i18n';
import { mockMsg } from './locales/mock';
import { mockObjMsg } from './locales/mockObjective';
import { formatClock } from './mockLogic';
import { answeredCount, ObjectiveQuestions, QuestionMap, questionNumbers, type Answers, type ClientGroup } from './ObjectiveQuestions';
import { PageHeader } from './ui';

interface ReadingClient {
  id: string;
  variant: 'academic' | 'general';
  passages: { title: string; text: string; groups: ClientGroup[] }[];
}

const MINUTES = 60;
const DRAFT = 'speakingCoach.mockReading';

interface Draft {
  testId: string;
  startedAt: number;
  answers: Answers;
}

function loadDraft(): Draft | null {
  try {
    const d = JSON.parse(localStorage.getItem(DRAFT) ?? 'null') as Draft | null;
    return d && typeof d.testId === 'string' ? d : null;
  } catch {
    return null;
  }
}
function saveDraft(d: Draft | null) {
  try {
    if (d) localStorage.setItem(DRAFT, JSON.stringify(d));
    else localStorage.removeItem(DRAFT);
  } catch {
    // qoralamasiz ham ishlaydi
  }
}

/**
 * IELTS Reading mock: 3 matn, 40 savol, 60 daqiqa. Matn va savollar
 * o'rtasida bitta tugma bilan o'tiladi (telefonda ikkalasi sig'maydi).
 * Javoblar shu qurilmada saqlanadi — sahifa yangilansa ham yo'qolmaydi.
 */
export function MockReading({
  variant,
  go,
  sessionId,
  onSubmitted,
}: {
  variant: 'academic' | 'general';
  go: (route: string) => void;
  sessionId?: string;
  onSubmitted?: (id: string) => void;
}) {
  const t = useT(mockObjMsg);
  const tm = useT(mockMsg);
  const [test, setTest] = useState<ReadingClient | null>(null);
  const [repeated, setRepeated] = useState(false);
  const [answers, setAnswers] = useState<Answers>({});
  const [startedAt, setStartedAt] = useState(0);
  const [now, setNow] = useState(Date.now());
  const [passage, setPassage] = useState(0);
  const [view, setView] = useState<'text' | 'questions'>('text');
  const [status, setStatus] = useState<'loading' | 'exam' | 'submitting' | 'error'>('loading');
  const [error, setError] = useState('');
  const submittedRef = useRef(false);
  const autoRef = useRef(false);

  useEffect(() => {
    apiJson<{ test: ReadingClient; repeated: boolean }>(`/api/mock/ielts/reading/new?variant=${variant}`)
      .then(({ test, repeated }) => {
        setTest(test);
        setRepeated(repeated);
        const d = loadDraft();
        if (d && d.testId === test.id && Date.now() < d.startedAt + (MINUTES + 5) * 60_000) {
          setAnswers(d.answers);
          setStartedAt(d.startedAt);
        } else {
          setStartedAt(Date.now());
        }
        setStatus('exam');
      })
      .catch((e) => {
        setError(e instanceof Error ? e.message : String(e));
        setStatus('error');
      });
  }, [variant]);

  useEffect(() => {
    if (status === 'exam' && test) saveDraft({ testId: test.id, startedAt, answers });
  }, [status, test, startedAt, answers]);

  useEffect(() => {
    if (status !== 'exam') return;
    const id = setInterval(() => setNow(Date.now()), 1000);
    const warn = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      e.returnValue = msg(mockMsg).leaveWarning;
    };
    window.addEventListener('beforeunload', warn);
    return () => {
      clearInterval(id);
      window.removeEventListener('beforeunload', warn);
    };
  }, [status]);

  const submit = useCallback(async () => {
    if (!test || submittedRef.current) return;
    submittedRef.current = true;
    setStatus('submitting');
    try {
      const res = await postJson<{ id: string }>('/api/mock/ielts/reading', {
        testId: test.id,
        answers,
        secondsUsed: Math.round((Date.now() - startedAt) / 1000),
        sessionId,
      });
      saveDraft(null);
      if (onSubmitted) onSubmitted(res.id);
      else go(`mock/result/${res.id}`);
    } catch (e) {
      submittedRef.current = false;
      setError(e instanceof Error ? e.message : String(e));
      setStatus('exam');
    }
  }, [test, answers, startedAt, sessionId, onSubmitted, go]);

  const left = startedAt + MINUTES * 60_000 - now;
  useEffect(() => {
    if (status === 'exam' && test && left <= 0 && !autoRef.current) {
      autoRef.current = true;
      void submit();
    }
  }, [status, test, left, submit]);

  const title = `${t.readingTitle} · ${variant === 'general' ? tm.general : tm.academic}`;
  const back = sessionId ? undefined : () => go('mock');

  if (status === 'loading') {
    return (
      <>
        <PageHeader title={title} onBack={back} backLabel={tm.title} />
        <p className="muted" role="status">⏳ {t.preparing}</p>
      </>
    );
  }
  if (!test) {
    return (
      <>
        <PageHeader title={title} onBack={back} backLabel={tm.title} />
        <div className="card"><p className="error" role="alert">{error}</p></div>
      </>
    );
  }

  const all = test.passages.flatMap((p) => questionNumbers(p.groups));
  const answered = answeredCount(answers, all);
  const p = test.passages[passage];
  const nums = questionNumbers(p.groups);

  function jump(n: number) {
    const idx = test!.passages.findIndex((ps) => questionNumbers(ps.groups).includes(n));
    setPassage(idx);
    setView('questions');
    setTimeout(() => document.getElementById(`q-${n}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 50);
  }

  function confirmSubmit() {
    if (window.confirm(t.confirmSubmit(all.length - answered))) void submit();
  }

  return (
    <>
      <PageHeader title={title} onBack={back} backLabel={tm.title} />
      {repeated && <p className="muted small">{t.repeated}</p>}

      <div className="card" style={{ position: 'sticky', top: 8, zIndex: 5, padding: '10px 14px' }}>
        <div className="spread" style={{ alignItems: 'center' }}>
          <span role="timer" aria-live="off">
            <span className="muted small">{t.timeLeft}: </span>
            <strong className={left < 5 * 60_000 ? 'txt-low' : undefined} style={{ fontVariantNumeric: 'tabular-nums' }}>
              {formatClock(left / 1000)}
            </strong>
            <span className="muted tiny" style={{ display: 'block' }}>{t.answered(answered, all.length)}</span>
          </span>
          <button className="btn btn-primary" onClick={confirmSubmit} disabled={status === 'submitting'}>
            {status === 'submitting' ? '⏳' : t.submit}
          </button>
        </div>
        <div className="chips" style={{ marginTop: 8 }}>
          {test.passages.map((_, i) => (
            <button key={i} aria-pressed={passage === i} onClick={() => setPassage(i)}>
              {t.passage(i + 1)}
            </button>
          ))}
        </div>
        <div className="segmented" role="group" style={{ marginTop: 8 }}>
          <button aria-pressed={view === 'text'} onClick={() => setView('text')}>{t.showText}</button>
          <button aria-pressed={view === 'questions'} onClick={() => setView('questions')}>
            {t.showQuestions} ({nums[0]}–{nums[nums.length - 1]})
          </button>
        </div>
      </div>

      {error && <p className="error small" role="alert">{error}</p>}

      <div className="card" data-testid={`reading-${view}`}>
        {view === 'text' ? (
          <article>
            <h3 style={{ marginTop: 0 }}>{p.title}</h3>
            {p.text.split(/\n\s*\n/).map((para, i) => (
              <p key={i} style={{ lineHeight: 1.65 }}>{para}</p>
            ))}
          </article>
        ) : (
          <ObjectiveQuestions
            groups={p.groups}
            answers={answers}
            onChange={(n, v) => setAnswers((a) => ({ ...a, [n]: v }))}
            disabled={status === 'submitting'}
          />
        )}
      </div>

      <div className="card">
        <QuestionMap numbers={all} answers={answers} onJump={jump} />
      </div>
    </>
  );
}
