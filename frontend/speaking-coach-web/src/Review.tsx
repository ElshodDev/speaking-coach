import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { apiFetch, apiJson, postJson } from './api';
import { isSpeechSupported, sleep, speakAsync, stopSpeaking } from './speech';
import { PageHeader } from './ui';
import { common, msg, useT } from './i18n';
import { reviewMsg } from './locales/review';
import { parseNote } from './Vocab';

export interface ReviewCard {
  id: string;
  kind: 'Correction' | 'Question' | 'Manual' | 'Word';
  source: string | null;
  front: string;
  back: string;
  note: string;
}

export interface ReviewStats {
  due: number;
  total: number;
  reviewedToday: number;
  dailyGoal: number;
  streakDays: number;
}

// JavaScript'ning getTimezoneOffset() qiymati (Toshkent uchun -300) —
// server "bugun" va "ketma-ket kunlar"ni mahalliy vaqt bo'yicha hisoblaydi.
export const statsPath = () => `/api/review/stats?tzOffsetMinutes=${new Date().getTimezoneOffset()}`;

// Baho tugmalari: matnlari (label/hint) reviewMsg.grades'da, shu tartibda.
const GRADES = [
  { value: 0, cls: 'g-again' },
  { value: 1, cls: 'g-hard' },
  { value: 2, cls: 'g-good' },
  { value: 3, cls: 'g-easy' },
];

type ReviewText = (typeof reviewMsg)['uz'];

/** Kartaning savol qismi uchun sarlavha — karta turiga qarab (joriy tilda). */
export function promptFor(kind: ReviewCard['kind'], t: ReviewText = msg(reviewMsg)): string {
  return t.prompt[kind] ?? t.prompt.Manual;
}

/** "Yo'lda" rejimida ovoz chiqarib o'qiladigan matnlar (inglizcha, chunki ovoz inglizcha). */
export function spokenParts(card: ReviewCard): { question: string; answer: string } {
  switch (card.kind) {
    case 'Correction':
      return { question: `How would you correct this? ${card.front}`, answer: `Better: ${card.back}. ${card.note}` };
    case 'Question':
      return { question: card.front, answer: `Answer: ${card.back}. ${card.note}` };
    case 'Word': {
      // Ovoz inglizcha — o'zbekcha tarjima o'rniga inglizcha ta'rif va misol o'qiladi.
      const { rest } = parseNote(card.note);
      return { question: card.front, answer: rest || card.back };
    }
    default:
      return { question: card.front, answer: `${card.back}. ${card.note}` };
  }
}

export function Review({
  loggedIn,
  onChanged,
  onLogin,
  scope,
  onBack,
}: {
  loggedIn: boolean;
  onChanged: () => void;
  onLogin?: () => void;
  /** 'vocab' — faqat Lug'at so'zlari (Lug'at bo'limidan ochilganda). */
  scope?: 'vocab';
  onBack?: () => void;
}) {
  const t = useT(reviewMsg);
  const c = useT(common);
  const scopeQuery = scope ? `&scope=${scope}` : '';
  const [cards, setCards] = useState<ReviewCard[]>([]);
  const [stats, setStats] = useState<ReviewStats | null>(null);
  const [revealed, setRevealed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    if (!loggedIn) return;
    try {
      const [due, s] = await Promise.all([
        apiJson<ReviewCard[]>(`/api/review/due?limit=50${scopeQuery}`),
        apiJson<ReviewStats>(statsPath()),
      ]);
      setCards(due);
      setStats(s);
      setError('');
    } catch (err) {
      setError(err instanceof Error ? err.message : msg(reviewMsg).loadError);
    }
  }, [loggedIn, scopeQuery]);

  useEffect(() => {
    load();
  }, [load]);

  if (!loggedIn) {
    return (
      <>
        <PageHeader title={t.title} subtitle={t.guestSubtitle} />
        <div className="card">
          <div className="steps">
            <div className="step">
              <div>
                {t.guestStep1Before}
                <strong>{t.guestStep1Strong}</strong>
                {t.guestStep1After}
              </div>
            </div>
            <div className="step">
              <div>
                {t.guestStep2Before}
                <strong>{t.guestStep2Strong}</strong>
                {t.guestStep2After}
              </div>
            </div>
            <div className="step">
              <div>
                <strong>{t.guestStep3Strong}</strong>
                {t.guestStep3After}
              </div>
            </div>
          </div>
          <p className="muted small" style={{ marginTop: 14 }}>
            {t.guestNote}
          </p>
          {onLogin && (
            <button className="btn btn-primary block" onClick={onLogin}>
              {t.loginOrSignup}
            </button>
          )}
        </div>
      </>
    );
  }

  const current = cards[0];

  async function grade(value: number) {
    if (!current) return;
    setBusy(true);
    try {
      await postJson(`/api/review/cards/${current.id}/grade`, { grade: value });
      setRevealed(false);
      setCards((prev) => prev.slice(1));
      setStats(await apiJson<ReviewStats>(statsPath()));
      onChanged();
    } catch (err) {
      setError(err instanceof Error ? err.message : c.error);
    } finally {
      setBusy(false);
    }
  }

  async function remove() {
    if (!current || !confirm(t.confirmDelete)) return;
    await apiFetch(`/api/review/cards/${current.id}`, { method: 'DELETE' });
    setRevealed(false);
    setCards((prev) => prev.slice(1));
    setStats(await apiJson<ReviewStats>(statsPath()));
    onChanged();
  }

  const vocab = scope === 'vocab';

  return (
    <>
      {vocab ? (
        <PageHeader title={t.vocabTitle} subtitle={t.vocabSubtitle} onBack={onBack} backLabel={t.vocabBack} />
      ) : (
        <PageHeader title={t.title} />
      )}
      {stats && !vocab && <StatsBar stats={stats} />}
      {error && <p className="error small">{error}</p>}

      {current ? (
        <div className="card">
          <div className="spread muted tiny">
            <span>
              {promptFor(current.kind, t)}
              {current.source && t.source[current.source] && (
                <span className="badge" style={{ marginLeft: 6 }}>
                  {t.source[current.source]}
                </span>
              )}
            </span>
            <span>{t.left(cards.length)}</span>
          </div>
          <p className="flash-front">
            {current.kind === 'Correction' ? <s style={{ textDecorationColor: 'var(--danger)' }}>{current.front}</s> : current.front}
          </p>
          {isSpeechSupported() && (
            <button className="btn-link small" onClick={() => { stopSpeaking(); speakAsync(spokenParts(current).question); }}>
              {c.listen}
            </button>
          )}

          {!revealed ? (
            <button className="btn btn-primary block" style={{ marginTop: 16 }} onClick={() => setRevealed(true)}>
              {t.showAnswer}
            </button>
          ) : (
            <>
              <div style={{ borderTop: '1px solid var(--border)', marginTop: 16, paddingTop: 14 }}>
                <p className="flash-back">{current.back}</p>
                {current.note && <p className="muted small">{current.note}</p>}
                {isSpeechSupported() && (
                  <button className="btn-link small" onClick={() => { stopSpeaking(); speakAsync(spokenParts(current).answer); }}>
                    {c.listen}
                  </button>
                )}
              </div>
              <p className="muted tiny" style={{ margin: '12px 0 6px' }}>
                {t.howWell}
              </p>
              <div className="grades">
                {GRADES.map((g) => (
                  <button key={g.value} className={`btn ${g.cls}`} onClick={() => grade(g.value)} disabled={busy}>
                    {t.grades[g.value].label}
                    <small>{t.grades[g.value].hint}</small>
                  </button>
                ))}
              </div>
            </>
          )}
          <button className="btn-link quiet tiny" style={{ marginTop: 14 }} onClick={remove}>
            {t.deleteCard}
          </button>
        </div>
      ) : vocab ? (
        stats && (
          <div className="card center">
            <div style={{ fontSize: '2.2rem' }}>✅</div>
            <p style={{ fontSize: '1.1rem', fontWeight: 600, margin: '4px 0' }}>{t.vocabDoneTitle}</p>
            <p className="muted small" style={{ margin: '0 0 12px' }}>
              {t.vocabDoneText}
            </p>
            {onBack && (
              <button className="btn btn-outline" onClick={onBack}>
                {t.backToVocab}
              </button>
            )}
          </div>
        )
      ) : (
        stats && (
          <div className="card center">
            <div style={{ fontSize: '2.2rem' }}>{stats.total === 0 ? '🗂' : '✅'}</div>
            <p style={{ fontSize: '1.1rem', fontWeight: 600, margin: '4px 0' }}>
              {stats.total === 0 ? t.noCardsTitle : t.allDoneTitle}
            </p>
            <p className="muted small" style={{ margin: 0 }}>
              {stats.total === 0 ? t.noCardsText : t.allDoneText}
            </p>
          </div>
        )
      )}

      <CommuteMode dueCards={cards} scopeQuery={scopeQuery} />
      {!vocab && <AddCardForm onAdded={() => { load(); onChanged(); }} />}
    </>
  );
}

function StatsBar({ stats }: { stats: ReviewStats }) {
  const t = useT(reviewMsg);
  const pct = Math.min(100, Math.round((stats.reviewedToday / stats.dailyGoal) * 100));
  return (
    <div className="card">
      <div className="stats">
        <div className="stat">
          <div className="value">🔥 {stats.streakDays}</div>
          <div className="label">{t.streak}</div>
        </div>
        <div className="stat">
          <div className="value">{stats.due}</div>
          <div className="label">{t.due}</div>
        </div>
        <div className="stat">
          <div className="value">{stats.total}</div>
          <div className="label">{t.total}</div>
        </div>
      </div>
      <div className="spread tiny muted" style={{ marginTop: 12 }}>
        <span>
          {t.dailyGoal} {Math.min(stats.reviewedToday, stats.dailyGoal)}/{stats.dailyGoal}
          {stats.reviewedToday >= stats.dailyGoal ? t.goalDone : ''}
        </span>
      </div>
      <div className="progress" style={{ marginTop: 6 }}>
        <div style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}

/**
 * "Yo'lda" rejimi — qo'l tegizmasdan, quloqchin bilan takrorlash
 * (avtobusda, yurganda). Har bir karta uchun: savol o'qiladi → o'ylash
 * uchun pauza → javob o'qiladi → keyingisi.
 *
 * Bu rejim kartalarni BAHOLAMAYDI: tinglash "eslay oldimmi?" degan savolga
 * javob bermaydi, shuning uchun jadvalni buzmaslik uchun faqat takrorlash.
 */
function CommuteMode({ dueCards, scopeQuery }: { dueCards: ReviewCard[]; scopeQuery: string }) {
  const t = useT(reviewMsg);
  const [running, setRunning] = useState(false);
  const [status, setStatus] = useState('');
  const [pauseSec, setPauseSec] = useState(4);
  const runRef = useRef(0);
  const wakeLockRef = useRef<{ release: () => Promise<void> } | null>(null);

  useEffect(
    () => () => {
      runRef.current++;
      stopSpeaking();
      wakeLockRef.current?.release().catch(() => undefined);
    },
    [],
  );

  if (!isSpeechSupported()) return null;

  async function start() {
    const run = ++runRef.current;
    setRunning(true);
    setStatus(t.preparing);

    // Ekran o'chib qolsa, ko'p telefonlarda ovoz ham to'xtaydi — imkon
    // bo'lsa ekranni yoniq ushlab turamiz (Screen Wake Lock API).
    try {
      const nav = navigator as Navigator & {
        wakeLock?: { request: (t: 'screen') => Promise<{ release: () => Promise<void> }> };
      };
      wakeLockRef.current = (await nav.wakeLock?.request('screen')) ?? null;
    } catch {
      wakeLockRef.current = null;
    }

    let list = dueCards;
    if (list.length === 0) {
      try {
        list = await apiJson<ReviewCard[]>(`/api/review/cards?limit=30${scopeQuery}`);
      } catch {
        list = [];
      }
    }
    if (list.length === 0) {
      setStatus(t.noCardsToListen);
      finish(run);
      return;
    }

    for (let i = 0; i < list.length; i++) {
      if (runRef.current !== run) return;
      const parts = spokenParts(list[i]);
      setStatus(`${i + 1}/${list.length}: ${list[i].front}`);
      await speakAsync(parts.question);
      if (runRef.current !== run) return;
      await sleep(pauseSec * 1000);
      if (runRef.current !== run) return;
      await speakAsync(parts.answer);
      await sleep(1500);
    }
    if (runRef.current === run) {
      setStatus(t.finished(list.length));
      finish(run);
    }
  }

  function finish(run: number) {
    if (runRef.current !== run) return;
    setRunning(false);
    wakeLockRef.current?.release().catch(() => undefined);
    wakeLockRef.current = null;
  }

  function stop() {
    runRef.current++;
    stopSpeaking();
    setRunning(false);
    setStatus(t.stopped);
    wakeLockRef.current?.release().catch(() => undefined);
    wakeLockRef.current = null;
  }

  return (
    <div className="card soft">
      <h3>{t.commuteTitle}</h3>
      <p className="muted small">{t.commuteText}</p>
      <div className="row">
        <button className={`btn ${running ? 'btn-danger' : 'btn-teal'}`} onClick={running ? stop : start}>
          {running ? t.stop : t.start}
        </button>
        <label className="small">
          {t.thinkTime}{' '}
          <select className="input" value={pauseSec} onChange={(e) => setPauseSec(Number(e.target.value))} disabled={running}>
            {[2, 4, 7].map((n) => (
              <option key={n} value={n}>
                {t.seconds(n)}
              </option>
            ))}
          </select>
        </label>
      </div>
      {status && (
        <p className="small break" style={{ marginTop: 10, marginBottom: 0 }}>
          {status}
        </p>
      )}
    </div>
  );
}

function AddCardForm({ onAdded }: { onAdded: () => void }) {
  const t = useT(reviewMsg);
  const c = useT(common);
  const [open, setOpen] = useState(false);
  const [front, setFront] = useState('');
  const [back, setBack] = useState('');
  const [note, setNote] = useState('');
  const [msg, setMsg] = useState('');

  async function submit(e: FormEvent) {
    e.preventDefault();
    try {
      await postJson('/api/review/cards', { front, back, note });
      setFront('');
      setBack('');
      setNote('');
      setMsg(t.added);
      onAdded();
    } catch (err) {
      setMsg(err instanceof Error ? err.message : c.error);
    }
  }

  if (!open) {
    return (
      <button className="btn-link" style={{ marginTop: 16 }} onClick={() => setOpen(true)}>
        {t.addOwn}
      </button>
    );
  }

  return (
    <form className="card stack" onSubmit={submit}>
      <h3>{t.newCard}</h3>
      <input className="input" required maxLength={500} placeholder={t.frontPh} value={front} onChange={(e) => setFront(e.target.value)} />
      <input className="input" required maxLength={500} placeholder={t.backPh} value={back} onChange={(e) => setBack(e.target.value)} />
      <input className="input" maxLength={1000} placeholder={t.notePh} value={note} onChange={(e) => setNote(e.target.value)} />
      <div className="row">
        <button type="submit" className="btn btn-primary">
          {t.add}
        </button>
        <button type="button" className="btn-link" onClick={() => setOpen(false)}>
          {c.close}
        </button>
        {msg && <span className="small">{msg}</span>}
      </div>
    </form>
  );
}
