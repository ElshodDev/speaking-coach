import { useCallback, useEffect, useRef, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { funMsg } from './locales/fun';
import { loadBest, quizUrl, saveBest, telegramShareUrl, type QuizQuestion } from './funLogic';
import { PageHeader } from './ui';

interface QuizData {
  questions: QuizQuestion[];
  ownWords: number;
  fromVocab: boolean;
}

/**
 * Tezkor viktorina: 10 ta savol (so'z → ma'no va ma'no → so'z navbat bilan),
 * har javobdan keyin to'g'risi ko'rsatiladi, oxirida natija va do'stga
 * yuborish (Telegram yoki telefonning "Ulashish" oynasi).
 */
export function Quiz({ go, loggedIn }: { go: (route: string) => void; loggedIn: boolean }) {
  const t = useT(funMsg);
  const [data, setData] = useState<QuizData | null>(null);
  const [error, setError] = useState('');
  const [i, setI] = useState(0);
  const [picked, setPicked] = useState<number | null>(null);
  const [score, setScore] = useState(0);
  const [done, setDone] = useState(false);
  const [record, setRecord] = useState(false);
  const [copied, setCopied] = useState(false);

  // Birinchi javob berilganmi — kirish holati (mehmon → foydalanuvchi) keyin
  // aniqlansa ham, boshlangan o'yin qayta yuklanmasin.
  const started = useRef(false);

  const load = useCallback(() => {
    started.current = false;
    setData(null);
    setError('');
    setI(0);
    setPicked(null);
    setScore(0);
    setDone(false);
    setRecord(false);
    setCopied(false);
    apiJson<QuizData>('/api/quiz')
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
  }, []);

  useEffect(() => {
    if (!started.current) load();
  }, [load, loggedIn]);

  const header = <PageHeader title={t.quizTitle} subtitle={t.quizSubtitle} onBack={() => go('practice')} />;
  if (error) return <>{header}<p className="error" role="alert">{error}</p></>;
  if (!data) return <>{header}<p className="muted">…</p></>;
  if (data.questions.length === 0) return <>{header}<div className="card"><p>{t.notEnough}</p></div></>;

  const n = data.questions.length;

  if (done) {
    const best = loadBest();
    const text = t.shareText(score, n);
    const url = quizUrl(window.location.origin + window.location.pathname);
    async function share() {
      const nav = navigator as Navigator & { share?: (d: ShareData) => Promise<void> };
      if (nav.share) {
        try {
          await nav.share({ text, url });
          return;
        } catch {
          // bekor qilindi — Telegram havolasiga o'tamiz
        }
      }
      window.open(telegramShareUrl(url, text), '_blank', 'noopener');
    }
    return (
      <>
        {header}
        <div className="card" style={{ textAlign: 'center' }} data-testid="quiz-result">
          <div style={{ fontSize: '3rem', fontWeight: 800 }}>{t.result(score, n)}</div>
          <p style={{ margin: '4px 0' }}><strong>{t.praise(score, n)}</strong></p>
          {record ? (
            <p className="success small" style={{ margin: 0 }}>{t.newBest}</p>
          ) : (
            best !== null && <p className="muted small" style={{ margin: 0 }}>{t.best(Math.round((best * n) / 100), n)}</p>
          )}
        </div>
        <div className="row" style={{ flexWrap: 'wrap', gap: 8 }}>
          <button className="btn btn-primary" onClick={load}>{t.again}</button>
          <button className="btn btn-outline" onClick={() => void share()}>{t.share}</button>
          <button
            className="btn-link small"
            onClick={() => {
              void navigator.clipboard?.writeText(`${text} ${url}`).then(() => setCopied(true)).catch(() => undefined);
            }}
          >
            {copied ? t.copied : '📋'}
          </button>
        </div>
        {loggedIn && <button className="btn-link small" style={{ marginTop: 12 }} onClick={() => go('review')}>{t.review}</button>}
      </>
    );
  }

  const q = data.questions[i];
  const answered = picked !== null;

  function choose(k: number) {
    if (answered) return;
    started.current = true;
    setPicked(k);
    if (k === q.answer) setScore((s) => s + 1);
  }

  function next() {
    if (i + 1 < n) {
      setI(i + 1);
      setPicked(null);
    } else {
      setRecord(saveBest(score, n));
      setDone(true);
    }
  }

  return (
    <>
      {header}
      <p className="muted small">{data.fromVocab ? t.fromVocab(data.ownWords) : t.fromDaily}</p>
      <div className="card stack" data-testid="quiz-question">
        <div className="spread">
          <span className="muted small" data-testid="quiz-progress">{t.question(i + 1, n)}</span>
          <div className="progress" style={{ flex: 1, marginLeft: 12, maxWidth: 200 }} aria-hidden="true">
            <div style={{ width: `${(i / n) * 100}%` }} />
          </div>
        </div>
        <div className="muted small">{q.direction === 'word' ? t.askWord : t.askMeaning}</div>
        <div style={{ fontSize: '1.5rem', fontWeight: 800 }} lang={q.direction === 'word' ? 'en' : undefined}>{q.prompt}</div>
        <div className="stack">
          {q.options.map((o, k) => {
            const state = !answered ? '' : k === q.answer ? ' correct' : k === picked ? ' wrong' : '';
            return (
              <button key={k} type="button" className={`choice${state}`} aria-pressed={picked === k} onClick={() => choose(k)} disabled={answered && k !== picked && k !== q.answer}>
                <span lang={q.direction === 'meaning' ? 'en' : undefined}>{o}</span>
              </button>
            );
          })}
        </div>
        {answered && (
          <p className={picked === q.answer ? 'success small' : 'error small'} role="status" style={{ margin: 0 }}>
            {picked === q.answer ? t.correct : t.wrong(q.options[q.answer])}
          </p>
        )}
        <button className="btn btn-primary block" disabled={!answered} onClick={next}>{i + 1 < n ? t.next : t.finish}</button>
      </div>
    </>
  );
}
