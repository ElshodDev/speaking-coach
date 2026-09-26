import { useCallback, useEffect, useRef, useState } from 'react';
import { apiJson, postJson } from './api';
import { msg, useT } from './i18n';
import { cefrMsg } from './locales/cefr';
import { mockMsg } from './locales/mock';
import { countWords, formatClock, wordRangeStatus } from './mockLogic';
import { PageHeader } from './ui';

interface CefrWritingSet {
  id: string;
  role: string;
  emailFrom: string;
  email: string;
  task11: string;
  task12: string;
  task2: string;
}

interface Range {
  min: number;
  max: number;
}

interface Timing {
  minutes: number;
  words11: Range;
  words12: Range;
  words2: Range;
}

type TaskKey = '11' | '12' | '2';
const TASKS: { key: TaskKey; label: string }[] = [
  { key: '11', label: '1.1' },
  { key: '12', label: '1.2' },
  { key: '2', label: '2' },
];

const DRAFT = 'speakingCoach.cefrWriting';

interface Draft {
  setId: string;
  startedAt: number;
  texts: Record<TaskKey, string>;
}

function loadDraft(): Draft | null {
  try {
    const d = JSON.parse(localStorage.getItem(DRAFT) ?? 'null') as Draft | null;
    return d && typeof d.setId === 'string' ? d : null;
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
 * CEFR (Multilevel) Writing: bitta vaziyat (email) asosida 1.1 — do'stga
 * xat, 1.2 — rasmiy xat, 2 — onlayn muhokama posti. 60 daqiqa, so'z
 * oraliqlari, qoralama shu qurilmada, vaqt tugasa avtomatik topshiriladi.
 */
export function CefrWriting({ go, sessionId, onSubmitted }: { go: (route: string) => void; sessionId?: string; onSubmitted?: (id: string) => void }) {
  const t = useT(cefrMsg);
  const tm = useT(mockMsg);
  const [set, setSet] = useState<CefrWritingSet | null>(null);
  const [timing, setTiming] = useState<Timing | null>(null);
  const [texts, setTexts] = useState<Record<TaskKey, string>>({ '11': '', '12': '', '2': '' });
  const [tab, setTab] = useState<TaskKey>('11');
  const [startedAt, setStartedAt] = useState(0);
  const [now, setNow] = useState(Date.now());
  const [status, setStatus] = useState<'loading' | 'exam' | 'submitting' | 'error'>('loading');
  const [error, setError] = useState('');
  const submittedRef = useRef(false);
  const autoRef = useRef(false);

  useEffect(() => {
    const d = loadDraft();
    const q = d ? `?setId=${encodeURIComponent(d.setId)}` : '';
    apiJson<{ set: CefrWritingSet; timing: Timing }>(`/api/mock/cefr/writing/new${q}`)
      .then((data) => {
        setSet(data.set);
        setTiming(data.timing);
        if (d && d.setId === data.set.id && Date.now() < d.startedAt + (data.timing.minutes + 5) * 60_000) {
          setTexts(d.texts);
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
  }, []);

  useEffect(() => {
    if (status === 'exam' && set) saveDraft({ setId: set.id, startedAt, texts });
  }, [status, set, startedAt, texts]);

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
    if (!set || submittedRef.current) return;
    submittedRef.current = true;
    setStatus('submitting');
    try {
      const res = await postJson<{ id: string }>('/api/mock/cefr/writing', {
        setId: set.id,
        task11: texts['11'],
        task12: texts['12'],
        task2: texts['2'],
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
  }, [set, texts, startedAt, sessionId, onSubmitted, go]);

  const left = timing ? startedAt + timing.minutes * 60_000 - now : 1;
  useEffect(() => {
    if (status === 'exam' && timing && left <= 0 && !autoRef.current) {
      autoRef.current = true;
      void submit();
    }
  }, [status, timing, left, submit]);

  const back = sessionId ? undefined : () => go('mock');
  if (status === 'loading') return <><PageHeader title={t.writingTitle} onBack={back} backLabel={tm.title} /><p className="muted">…</p></>;
  if (!set || !timing) {
    return (
      <>
        <PageHeader title={t.writingTitle} onBack={back} backLabel={tm.title} />
        <div className="card"><p className="error" role="alert">{error}</p></div>
      </>
    );
  }

  const ranges: Record<TaskKey, Range> = { '11': timing.words11, '12': timing.words12, '2': timing.words2 };
  const prompts: Record<TaskKey, string> = { '11': set.task11, '12': set.task12, '2': set.task2 };

  function confirmSubmit() {
    for (const { key, label } of TASKS) {
      const n = countWords(texts[key]);
      const r = ranges[key];
      if (wordRangeStatus(n, r.min, r.max) !== 'ok' && !window.confirm(t.confirmRange(t.task(label), n, r.min, r.max))) return;
    }
    if (window.confirm(tm.confirmSubmit)) void submit();
  }

  const n = countWords(texts[tab]);
  const r = ranges[tab];
  const st = wordRangeStatus(n, r.min, r.max);
  const label = TASKS.find((x) => x.key === tab)!.label;

  return (
    <>
      <PageHeader title={t.writingTitle} onBack={back} backLabel={tm.title} />

      <div className="card" style={{ position: 'sticky', top: 8, zIndex: 5, padding: '10px 14px' }}>
        <div className="spread" style={{ alignItems: 'center' }}>
          <span role="timer" aria-live="off">
            <span className="muted small">{tm.timeLeft}: </span>
            <strong className={left < 5 * 60_000 ? 'txt-low' : undefined} style={{ fontVariantNumeric: 'tabular-nums' }}>
              {formatClock(left / 1000)}
            </strong>
          </span>
          <button className="btn btn-primary" onClick={confirmSubmit} disabled={status === 'submitting'}>
            {status === 'submitting' ? '⏳' : tm.submit}
          </button>
        </div>
        <div className="chips" role="tablist" style={{ marginTop: 8 }}>
          {TASKS.map((x) => (
            <button key={x.key} role="tab" aria-selected={tab === x.key} aria-pressed={tab === x.key} onClick={() => setTab(x.key)}>
              {x.label} <span className="n">{countWords(texts[x.key])}</span>
            </button>
          ))}
        </div>
      </div>

      {error && <p className="error small" role="alert">{error}</p>}
      {status === 'submitting' && <p className="small" role="status">⏳ {tm.submitting}</p>}

      {tab !== '2' && (
        <div className="card stack">
          <strong className="small">{t.situation}</strong>
          <p className="small" style={{ margin: 0 }}>{set.role}</p>
          <div className="quote small" style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{set.email}</div>
        </div>
      )}

      <div className="card stack" data-testid={`cefr-writing-${tab}`}>
        <strong>{t.task(label)}</strong>
        <p style={{ margin: 0 }}>{prompts[tab]}</p>
        <textarea
          className="input"
          rows={tab === '11' ? 6 : 12}
          value={texts[tab]}
          onChange={(e) => setTexts((x) => ({ ...x, [tab]: e.target.value }))}
          placeholder={tm.placeholder}
          aria-label={t.task(label)}
          disabled={status === 'submitting'}
          spellCheck={false}
          autoCorrect="off"
        />
        <div className="spread small">
          <span className={st === 'ok' ? 'txt-great' : 'muted'}>
            {t.words(n, r.min, r.max)}
            {st !== 'ok' && n > 0 ? ` · ${st === 'low' ? t.tooShort : t.tooLong}` : ''}
          </span>
          <span className="muted tiny">{tm.draftSaved}</span>
        </div>
      </div>
      <p className="muted tiny">{t.disclaimer}</p>
    </>
  );
}
