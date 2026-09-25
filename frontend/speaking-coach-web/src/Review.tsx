import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { apiFetch, apiJson, postJson } from './api';
import { isSpeechSupported, sleep, speakAsync, stopSpeaking } from './speech';
import { PageHeader } from './ui';

export interface ReviewCard {
  id: string;
  kind: 'Correction' | 'Question' | 'Manual';
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

const GRADES = [
  { value: 0, label: 'Yana', hint: '10 daqiqadan keyin', cls: 'g-again' },
  { value: 1, label: 'Qiyin', hint: 'tezroq qaytadi', cls: 'g-hard' },
  { value: 2, label: 'Yaxshi', hint: 'oraliq uzayadi', cls: 'g-good' },
  { value: 3, label: 'Oson', hint: 'ancha keyin', cls: 'g-easy' },
];

const SOURCE_LABEL: Record<string, string> = {
  Speaking: 'Gapirish',
  Writing: 'Yozish',
  Reading: "O'qish",
  Listening: 'Tinglash',
};

/** Kartaning savol qismi uchun sarlavha — karta turiga qarab. */
export function promptFor(kind: ReviewCard['kind']): string {
  switch (kind) {
    case 'Correction':
      return "Bu iborani qanday to'g'ri aytasiz?";
    case 'Question':
      return 'Savolga javob bering:';
    default:
      return "Ma'nosini eslang:";
  }
}

/** "Yo'lda" rejimida ovoz chiqarib o'qiladigan matnlar (inglizcha, chunki ovoz inglizcha). */
export function spokenParts(card: ReviewCard): { question: string; answer: string } {
  switch (card.kind) {
    case 'Correction':
      return { question: `How would you correct this? ${card.front}`, answer: `Better: ${card.back}. ${card.note}` };
    case 'Question':
      return { question: card.front, answer: `Answer: ${card.back}. ${card.note}` };
    default:
      return { question: card.front, answer: `${card.back}. ${card.note}` };
  }
}

export function Review({
  loggedIn,
  onChanged,
  onLogin,
}: {
  loggedIn: boolean;
  onChanged: () => void;
  onLogin?: () => void;
}) {
  const [cards, setCards] = useState<ReviewCard[]>([]);
  const [stats, setStats] = useState<ReviewStats | null>(null);
  const [revealed, setRevealed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    if (!loggedIn) return;
    try {
      const [due, s] = await Promise.all([
        apiJson<ReviewCard[]>('/api/review/due?limit=50'),
        apiJson<ReviewStats>(statsPath()),
      ]);
      setCards(due);
      setStats(s);
      setError('');
    } catch (err) {
      setError(err instanceof Error ? err.message : "Kartalarni yuklab bo'lmadi");
    }
  }, [loggedIn]);

  useEffect(() => {
    load();
  }, [load]);

  if (!loggedIn) {
    return (
      <>
        <PageHeader title="Takrorlash" subtitle="Xatolaringizni esda qolguncha takrorlang." />
        <div className="card">
          <div className="steps">
            <div className="step">
              <div>Mashqlardagi xatolaringiz <strong>avtomatik ravishda kartalarga</strong> aylanadi.</div>
            </div>
            <div className="step">
              <div>
                Har bir karta <strong>unutish arafasida</strong> qaytadi: 1 kun → 3 kun → 1 hafta... (oraliqli
                takrorlash).
              </div>
            </div>
            <div className="step">
              <div>
                <strong>Yo'lda rejimi</strong> — avtobusda quloqchin bilan, qo'l tegizmasdan takrorlang.
              </div>
            </div>
          </div>
          <p className="muted small" style={{ marginTop: 14 }}>
            Kartalar hisobingizda saqlanadi — foydalanish uchun yuqorida tizimga kiring.
          </p>
          {onLogin && (
            <button className="btn btn-primary block" onClick={onLogin}>
              Kirish yoki hisob ochish
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
      setError(err instanceof Error ? err.message : 'Xato yuz berdi');
    } finally {
      setBusy(false);
    }
  }

  async function remove() {
    if (!current || !confirm("Bu kartani butunlay o'chirasizmi?")) return;
    await apiFetch(`/api/review/cards/${current.id}`, { method: 'DELETE' });
    setRevealed(false);
    setCards((prev) => prev.slice(1));
    setStats(await apiJson<ReviewStats>(statsPath()));
    onChanged();
  }

  return (
    <>
      <PageHeader title="Takrorlash" />
      {stats && <StatsBar stats={stats} />}
      {error && <p className="error small">{error}</p>}

      {current ? (
        <div className="card">
          <div className="spread muted tiny">
            <span>
              {promptFor(current.kind)}
              {current.source && SOURCE_LABEL[current.source] && (
                <span className="badge" style={{ marginLeft: 6 }}>
                  {SOURCE_LABEL[current.source]}
                </span>
              )}
            </span>
            <span>{cards.length} ta qoldi</span>
          </div>
          <p className="flash-front">
            {current.kind === 'Correction' ? <s style={{ textDecorationColor: 'var(--danger)' }}>{current.front}</s> : current.front}
          </p>
          {isSpeechSupported() && (
            <button className="btn-link small" onClick={() => { stopSpeaking(); speakAsync(spokenParts(current).question); }}>
              🔊 Eshitish
            </button>
          )}

          {!revealed ? (
            <button className="btn btn-primary block" style={{ marginTop: 16 }} onClick={() => setRevealed(true)}>
              Javobni ko'rsatish
            </button>
          ) : (
            <>
              <div style={{ borderTop: '1px solid var(--border)', marginTop: 16, paddingTop: 14 }}>
                <p className="flash-back">{current.back}</p>
                {current.note && <p className="muted small">{current.note}</p>}
                {isSpeechSupported() && (
                  <button className="btn-link small" onClick={() => { stopSpeaking(); speakAsync(spokenParts(current).answer); }}>
                    🔊 Eshitish
                  </button>
                )}
              </div>
              <p className="muted tiny" style={{ margin: '12px 0 6px' }}>
                Qanchalik esladingiz?
              </p>
              <div className="grades">
                {GRADES.map((g) => (
                  <button key={g.value} className={`btn ${g.cls}`} onClick={() => grade(g.value)} disabled={busy}>
                    {g.label}
                    <small>{g.hint}</small>
                  </button>
                ))}
              </div>
            </>
          )}
          <button className="btn-link quiet tiny" style={{ marginTop: 14 }} onClick={remove}>
            Kartani o'chirish
          </button>
        </div>
      ) : (
        stats && (
          <div className="card center">
            <div style={{ fontSize: '2.2rem' }}>{stats.total === 0 ? '🗂' : '✅'}</div>
            <p style={{ fontSize: '1.1rem', fontWeight: 600, margin: '4px 0' }}>
              {stats.total === 0 ? "Hali kartalar yo'q" : "Barakalla! Hozircha takrorlanadigan karta yo'q"}
            </p>
            <p className="muted small" style={{ margin: 0 }}>
              {stats.total === 0
                ? "Gapirish, Yozish, O'qish yoki Tinglash mashqini bajaring — xatolaringiz shu yerga tushadi. Yoki pastda o'zingiz qo'shing."
                : "Kartalar eslash vaqti kelganda shu yerda paydo bo'ladi."}
            </p>
          </div>
        )
      )}

      <CommuteMode dueCards={cards} />
      <AddCardForm onAdded={() => { load(); onChanged(); }} />
    </>
  );
}

function StatsBar({ stats }: { stats: ReviewStats }) {
  const pct = Math.min(100, Math.round((stats.reviewedToday / stats.dailyGoal) * 100));
  return (
    <div className="card">
      <div className="stats">
        <div className="stat">
          <div className="value">🔥 {stats.streakDays}</div>
          <div className="label">kun ketma-ket</div>
        </div>
        <div className="stat">
          <div className="value">{stats.due}</div>
          <div className="label">navbatda</div>
        </div>
        <div className="stat">
          <div className="value">{stats.total}</div>
          <div className="label">jami karta</div>
        </div>
      </div>
      <div className="spread tiny muted" style={{ marginTop: 12 }}>
        <span>
          Bugungi maqsad: {Math.min(stats.reviewedToday, stats.dailyGoal)}/{stats.dailyGoal}
          {stats.reviewedToday >= stats.dailyGoal ? ' — bajarildi 🎉' : ''}
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
function CommuteMode({ dueCards }: { dueCards: ReviewCard[] }) {
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
    setStatus('Kartalar tayyorlanmoqda...');

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
        list = await apiJson<ReviewCard[]>('/api/review/cards?limit=30');
      } catch {
        list = [];
      }
    }
    if (list.length === 0) {
      setStatus("Tinglash uchun hali kartalar yo'q.");
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
      setStatus(`Tugadi — ${list.length} ta karta tinglandi. 👏`);
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
    setStatus("To'xtatildi.");
    wakeLockRef.current?.release().catch(() => undefined);
    wakeLockRef.current = null;
  }

  return (
    <div className="card soft">
      <h3>🎧 Yo'lda rejimi</h3>
      <p className="muted small">
        Quloqchinni taqing: savol o'qiladi, o'ylash uchun pauza, keyin javob. Qo'l tegizish shart emas. Telefon
        ekrani yoniq tursin — ba'zi telefonlarda ekran o'chsa, ovoz ham to'xtaydi.
      </p>
      <div className="row">
        <button className={`btn ${running ? 'btn-danger' : 'btn-teal'}`} onClick={running ? stop : start}>
          {running ? "⏹ To'xtatish" : '▶️ Boshlash'}
        </button>
        <label className="small">
          O'ylash vaqti{' '}
          <select className="input" value={pauseSec} onChange={(e) => setPauseSec(Number(e.target.value))} disabled={running}>
            <option value={2}>2 soniya</option>
            <option value={4}>4 soniya</option>
            <option value={7}>7 soniya</option>
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
      setMsg("Qo'shildi ✅");
      onAdded();
    } catch (err) {
      setMsg(err instanceof Error ? err.message : 'Xato yuz berdi');
    }
  }

  if (!open) {
    return (
      <button className="btn-link" style={{ marginTop: 16 }} onClick={() => setOpen(true)}>
        ➕ O'zim karta qo'shaman
      </button>
    );
  }

  return (
    <form className="card stack" onSubmit={submit}>
      <h3>Yangi karta</h3>
      <input className="input" required maxLength={500} placeholder="So'z yoki ibora (masalan: take for granted)" value={front} onChange={(e) => setFront(e.target.value)} />
      <input className="input" required maxLength={500} placeholder="Ma'nosi (masalan: qadriga yetmaslik)" value={back} onChange={(e) => setBack(e.target.value)} />
      <input className="input" maxLength={1000} placeholder="Misol gap (ixtiyoriy)" value={note} onChange={(e) => setNote(e.target.value)} />
      <div className="row">
        <button type="submit" className="btn btn-primary">
          Qo'shish
        </button>
        <button type="button" className="btn-link" onClick={() => setOpen(false)}>
          Yopish
        </button>
        {msg && <span className="small">{msg}</span>}
      </div>
    </form>
  );
}
