// Diktant: gap brauzer ovozida o'qiladi, foydalanuvchi yozadi, so'zma-so'z tekshiriladi (AI'siz).
import { useEffect, useRef, useState, type FormEvent } from 'react';
import { getLevel, postJson } from './api';
import { DICTATION } from './content/dictation';
import { LEVELS, type Level } from './content/types';
import { checkDictation, pickSentences, type DictationCheck } from './dictationLogic';
import { useT } from './i18n';
import { dictationMsg } from './locales/dictation';
import { isSpeechSupported, speakAsync, stopSpeaking } from './speech';
import { PageHeader } from './ui';

const ROUND = 5;
const SEEN_KEY = 'speakingCoach.dictation.seen';

function readSeen(): string[] {
  try {
    const v = JSON.parse(localStorage.getItem(SEEN_KEY) ?? '[]');
    return Array.isArray(v) ? v.filter((x) => typeof x === 'string') : [];
  } catch {
    return [];
  }
}
function addSeen(list: string[]) {
  try {
    localStorage.setItem(SEEN_KEY, JSON.stringify([...list, ...readSeen().filter((x) => !list.includes(x))].slice(0, 120)));
  } catch {
    /* saqlanmasa ham ishlaydi */
  }
}

export function Dictation({ loggedIn, go }: { loggedIn: boolean; go: (route: string) => void }) {
  const t = useT(dictationMsg);
  const initial = getLevel();
  const [level, setLevel] = useState<Level>(LEVELS.includes(initial as Level) ? (initial as Level) : 'B1');
  const [items, setItems] = useState<string[]>([]);
  const [index, setIndex] = useState(0);
  const [typed, setTyped] = useState('');
  const [checks, setChecks] = useState<DictationCheck[]>([]);
  const [saved, setSaved] = useState(false);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const speech = isSpeechSupported();

  useEffect(() => () => stopSpeaking(), []);

  const current = items[index];
  const checked = checks[index];
  const done = items.length > 0 && checks.length === items.length && index === items.length;

  function play(rate = 0.9) {
    if (!current) return;
    stopSpeaking();
    void speakAsync(current, rate);
    inputRef.current?.focus();
  }

  function start() {
    const picked = pickSentences(DICTATION[level], readSeen(), ROUND);
    addSeen(picked);
    setItems(picked);
    setIndex(0);
    setChecks([]);
    setTyped('');
    setSaved(false);
    stopSpeaking();
    if (speech) void speakAsync(picked[0], 0.9);
  }

  function check(e?: FormEvent) {
    e?.preventDefault();
    if (!current || checked) return;
    setChecks((c) => [...c, checkDictation(current, typed)]);
  }

  async function next() {
    const nextIndex = index + 1;
    setIndex(nextIndex);
    setTyped('');
    if (nextIndex < items.length) {
      if (speech) void speakAsync(items[nextIndex], 0.9);
      setTimeout(() => inputRef.current?.focus(), 0);
      return;
    }
    // Tugadi: natija tarixga (kirgan bo'lsa).
    const correct = checks.reduce((n, c) => n + c.correct, 0);
    const total = checks.reduce((n, c) => n + c.total, 0);
    if (loggedIn && total > 0) {
      try {
        const r = await postJson<{ saved: boolean }>('/api/dictation/result', { level, sentences: checks.length, correctWords: correct, totalWords: total });
        setSaved(r.saved);
      } catch {
        // saqlanmasa ham natija ekranda ko'rinadi
      }
    }
  }

  const header = <PageHeader title={t.title} subtitle={t.subtitle} onBack={() => go('practice')} />;

  if (!speech) {
    return (
      <>
        {header}
        <div className="card"><p className="error" style={{ margin: 0 }}>{t.noSpeech}</p></div>
      </>
    );
  }

  if (items.length === 0 || done) {
    const correct = checks.reduce((n, c) => n + c.correct, 0);
    const total = checks.reduce((n, c) => n + c.total, 0);
    return (
      <>
        {header}
        {done && (
          <div className="card stack center" data-testid="dictation-done">
            <h3 style={{ margin: 0 }}>{t.doneTitle}</h3>
            <div className="dict-score">{t.accuracy(total ? Math.round((correct / total) * 100) : 0)}</div>
            <p className="muted" style={{ margin: 0 }}>{t.words(correct, total)}</p>
            <p className="small" style={{ margin: 0 }}>{loggedIn ? (saved ? t.saved : '') : t.guestNote}</p>
          </div>
        )}
        <div className="card stack">
          <div>
            <div className="small muted" style={{ marginBottom: 6 }}>{t.level}</div>
            <div className="chips" role="group" aria-label={t.level}>
              {LEVELS.map((l) => (
                <button key={l} aria-pressed={level === l} onClick={() => setLevel(l)}>
                  {l}
                </button>
              ))}
            </div>
          </div>
          <button className="btn btn-primary" onClick={start} data-testid="dictation-start">
            {done ? t.again : `${t.start} · ${t.round(ROUND)}`}
          </button>
        </div>
      </>
    );
  }

  return (
    <>
      {header}
      <form className="card stack" onSubmit={check} data-testid="dictation">
        <div className="spread">
          <strong>{t.progress(index + 1, items.length)}</strong>
          <span className="badge">{level}</span>
        </div>
        <div className="progress"><div style={{ width: `${Math.round((index / items.length) * 100)}%` }} /></div>
        <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
          <button type="button" className="btn btn-outline" onClick={() => play(0.9)} data-testid="dictation-play">{t.play}</button>
          <button type="button" className="btn btn-outline" onClick={() => play(0.65)}>{t.slow}</button>
        </div>
        <textarea
          ref={inputRef}
          className="input dict-input"
          rows={3}
          value={typed}
          onChange={(e) => setTyped(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
              e.preventDefault();
              if (checked) void next();
              else check();
            }
          }}
          placeholder={t.placeholder}
          aria-label={t.placeholder}
          autoCapitalize="none"
          autoCorrect="off"
          autoComplete="off"
          spellCheck={false}
          readOnly={!!checked}
          data-testid="dictation-input"
        />
        {!checked && (
          <>
            <button className="btn btn-primary block" disabled={!typed.trim()}>{t.check}</button>
            <span className="muted tiny">{t.enterHint}</span>
          </>
        )}
        {checked && (
          <div className="stack" data-testid="dictation-result">
            <div className="dict-marks" aria-label={t.correct(checked.correct, checked.total)}>
              {checked.marks.map((m, i) => (
                <span key={i} className={`dict-w ${m.status}`} title={m.status === 'wrong' ? m.expected : undefined}>
                  {m.status === 'missing' ? m.expected : m.typed}
                  {m.status === 'wrong' && <small> → {m.expected}</small>}
                </span>
              ))}
            </div>
            <strong className="small">{t.correct(checked.correct, checked.total)}</strong>
            {checked.correct < checked.total && (
              <p className="small" style={{ margin: 0 }}><span className="muted">{t.answer}:</span> {current}</p>
            )}
            <p className="muted tiny" style={{ margin: 0 }}>{t.legend}</p>
            <button type="button" className="btn btn-primary block" onClick={() => void next()} data-testid="dictation-next">
              {index + 1 < items.length ? t.next : t.finish}
            </button>
          </div>
        )}
      </form>
    </>
  );
}
