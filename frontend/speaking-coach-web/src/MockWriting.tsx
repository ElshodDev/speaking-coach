import { useCallback, useEffect, useRef, useState } from 'react';
import { apiJson, postJson } from './api';
import { msg, useT } from './i18n';
import { mockMsg } from './locales/mock';
import { MockChart, type ChartData } from './MockChart';
import { countWords, draftUsable, formatClock, readDraft, writeDraft, type WritingDraft } from './mockLogic';
import { PageHeader } from './ui';

interface WritingSet {
  id: string;
  variant: 'academic' | 'general';
  task1: { prompt: string; chart: ChartData | null };
  task2: string;
}

interface Timing {
  minutes: number;
  task1MinWords: number;
  task2MinWords: number;
}

/**
 * IELTS Writing mock: 60 daqiqalik umumiy taymer, ikki vazifa (tab),
 * so'z hisoblagichi. Qoralama shu qurilmada saqlanadi — sahifa yangilansa
 * yoki telefon uxlab qolsa ham matn va vaqt yo'qolmaydi. Vaqt tugasa —
 * avtomatik topshiriladi.
 */
export function MockWriting({ variant, go }: { variant: 'academic' | 'general'; go: (route: string) => void }) {
  const t = useT(mockMsg);
  const [set, setSet] = useState<WritingSet | null>(null);
  const [timing, setTiming] = useState<Timing | null>(null);
  const [tab, setTab] = useState<1 | 2>(1);
  const [task1, setTask1] = useState('');
  const [task2, setTask2] = useState('');
  const [startedAt, setStartedAt] = useState(0);
  const [now, setNow] = useState(Date.now());
  const [status, setStatus] = useState<'loading' | 'exam' | 'submitting' | 'error'>('loading');
  const [error, setError] = useState('');
  const [note, setNote] = useState('');
  const submittedRef = useRef(false);

  useEffect(() => {
    const saved = readDraft();
    const resume = saved && saved.variant === variant ? saved : null;
    const q = new URLSearchParams({ variant });
    if (resume) q.set('setId', resume.setId);
    apiJson<{ set: WritingSet; timing: Timing }>(`/api/mock/ielts/writing/new?${q}`)
      .then((data) => {
        setSet(data.set);
        setTiming(data.timing);
        if (resume && resume.setId === data.set.id && draftUsable(resume, variant, data.timing.minutes, Date.now())) {
          setTask1(resume.task1);
          setTask2(resume.task2);
          setStartedAt(resume.startedAt);
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

  // Qoralamani saqlash (har o'zgarishda — matn kichik, localStorage tez).
  useEffect(() => {
    if (status !== 'exam' || !set) return;
    const d: WritingDraft = { setId: set.id, variant, startedAt, task1, task2 };
    writeDraft(d);
  }, [status, set, variant, startedAt, task1, task2]);

  useEffect(() => {
    if (status !== 'exam') return;
    const id = setInterval(() => setNow(Date.now()), 1000);
    const onBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      e.returnValue = msg(mockMsg).leaveWarning;
    };
    window.addEventListener('beforeunload', onBeforeUnload);
    return () => {
      clearInterval(id);
      window.removeEventListener('beforeunload', onBeforeUnload);
    };
  }, [status]);

  const submit = useCallback(
    async (auto: boolean) => {
      if (!set || submittedRef.current) return;
      submittedRef.current = true;
      setStatus('submitting');
      if (auto) setNote(msg(mockMsg).timeUp);
      try {
        const res = await postJson<{ id: string }>('/api/mock/ielts/writing', {
          setId: set.id,
          task1,
          task2,
          secondsUsed: Math.round((Date.now() - startedAt) / 1000),
        });
        writeDraft(null);
        go(`mock/result/${res.id}`);
      } catch (e) {
        submittedRef.current = false;
        setError(e instanceof Error ? e.message : String(e));
        setStatus('exam'); // matn joyida qoladi — qayta topshirish mumkin
      }
    },
    [set, task1, task2, startedAt, go],
  );

  const left = timing ? startedAt + timing.minutes * 60_000 - now : 1;
  // Vaqt tugadi — bir marta avtomatik topshiramiz (xato bo'lsa, qayta-qayta
  // urinmaymiz: foydalanuvchi "Topshirish"ni o'zi bosadi).
  const autoTriedRef = useRef(false);
  useEffect(() => {
    if (status === 'exam' && timing && left <= 0 && !autoTriedRef.current) {
      autoTriedRef.current = true;
      void submit(true);
    }
  }, [status, timing, left, submit]);

  function confirmAndSubmit() {
    if (!timing) return;
    const w1 = countWords(task1);
    const w2 = countWords(task2);
    const checks: [string, number, number][] = [
      [t.task1, w1, timing.task1MinWords],
      [t.task2, w2, timing.task2MinWords],
    ];
    for (const [name, n, min] of checks) {
      if (n < min && !window.confirm(t.shortWarning(name, n, min))) return;
    }
    if (window.confirm(t.confirmSubmit)) void submit(false);
  }

  const title = `${t.writingTitle} · ${variant === 'academic' ? t.academic : t.general}`;

  if (status === 'loading') return <><PageHeader title={title} onBack={() => go('mock')} backLabel={t.title} /><p className="muted">…</p></>;
  if (!set || !timing) {
    return (
      <>
        <PageHeader title={title} onBack={() => go('mock')} backLabel={t.title} />
        <div className="card"><p className="error" role="alert">{error}</p></div>
      </>
    );
  }

  const words = tab === 1 ? countWords(task1) : countWords(task2);
  const min = tab === 1 ? timing.task1MinWords : timing.task2MinWords;
  const lowTime = left < 5 * 60_000;

  return (
    <>
      <PageHeader title={title} onBack={() => go('mock')} backLabel={t.title} />

      <div className="card mock-sticky" style={{ position: 'sticky', top: 8, zIndex: 5, padding: '10px 14px' }}>
        <div className="spread" style={{ alignItems: 'center' }}>
          <span role="timer" aria-live="off">
            <span className="muted small">{t.timeLeft}: </span>
            <strong className={lowTime ? 'txt-low' : undefined} style={{ fontVariantNumeric: 'tabular-nums' }}>
              {formatClock(left / 1000)}
            </strong>
          </span>
          <button className="btn btn-primary" onClick={confirmAndSubmit} disabled={status === 'submitting'}>
            {status === 'submitting' ? '⏳' : t.submit}
          </button>
        </div>
        <div className="chips" role="tablist" style={{ marginTop: 8 }}>
          {([1, 2] as const).map((n) => (
            <button key={n} role="tab" aria-pressed={tab === n} aria-selected={tab === n} onClick={() => setTab(n)}>
              {n === 1 ? t.task1 : t.task2} <span className="n">{countWords(n === 1 ? task1 : task2)}</span>
            </button>
          ))}
        </div>
      </div>

      {note && <p className="small" role="status">{note}</p>}
      {error && <p className="error small" role="alert">{error}</p>}
      {status === 'submitting' && <p className="small" role="status">⏳ {t.submitting}</p>}

      <div className="card stack" data-testid={`mock-writing-task${tab}`}>
        <div className="spread small">
          <strong>{tab === 1 ? t.task1 : t.task2}</strong>
          <span className="muted">{t.recommended(tab === 1 ? 20 : 40)}</span>
        </div>
        <p style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{tab === 1 ? set.task1.prompt : set.task2}</p>
        {tab === 1 && set.task1.chart && <MockChart data={set.task1.chart} />}
        <textarea
          className="input"
          rows={14}
          value={tab === 1 ? task1 : task2}
          onChange={(e) => (tab === 1 ? setTask1 : setTask2)(e.target.value)}
          placeholder={t.placeholder}
          aria-label={tab === 1 ? t.task1 : t.task2}
          disabled={status === 'submitting'}
          spellCheck={false}
          autoCorrect="off"
          autoCapitalize="sentences"
        />
        <div className="spread small">
          <span className={words >= min ? 'txt-great' : 'muted'}>{t.words(words, min)}</span>
          <span className="muted tiny">{t.draftSaved}</span>
        </div>
      </div>
      <p className="muted tiny">{t.disclaimer}</p>
    </>
  );
}
