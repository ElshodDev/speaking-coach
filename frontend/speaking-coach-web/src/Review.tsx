import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { apiFetch, apiJson, postJson } from './api';
import { isSpeechSupported, sleep, speakAsync, stopSpeaking } from './speech';

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
  { value: 0, label: 'Yana', hint: '10 daqiqadan keyin', color: '#dc2626' },
  { value: 1, label: 'Qiyin', hint: 'tezroq qaytadi', color: '#ca8a04' },
  { value: 2, label: 'Yaxshi', hint: 'oraliq uzayadi', color: '#16a34a' },
  { value: 3, label: 'Oson', hint: 'ancha keyin', color: '#2563eb' },
];

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

export function Review({ loggedIn, onChanged }: { loggedIn: boolean; onChanged: () => void }) {
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
      <div style={{ marginTop: '1.5rem', color: '#374151' }}>
        <p>
          <strong>Takrorlash</strong> — mashqlardagi xatolaringiz avtomatik ravishda kartalarga aylanadi va
          "unutish arafasida" qayta ko'rsatiladi (oraliqli takrorlash). O'zingiz ham so'z va iboralar qo'sha olasiz.
        </p>
        <p>Kartalar hisobingizda saqlanadi — foydalanish uchun yuqorida tizimga kiring.</p>
      </div>
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
    <div style={{ marginTop: '1.5rem' }}>
      {stats && <StatsBar stats={stats} />}
      {error && <p style={{ color: '#dc2626' }}>{error}</p>}

      {current ? (
        <div style={card}>
          <div style={{ fontSize: '0.8rem', color: '#6b7280', display: 'flex', justifyContent: 'space-between' }}>
            <span>{promptFor(current.kind)}</span>
            <span>{cards.length} ta qoldi</span>
          </div>
          <p style={{ fontSize: '1.25rem', margin: '0.75rem 0', lineHeight: 1.4 }}>
            {current.kind === 'Correction' ? <s style={{ textDecorationColor: '#dc2626' }}>{current.front}</s> : current.front}
          </p>
          {isSpeechSupported() && (
            <button onClick={() => { stopSpeaking(); speakAsync(spokenParts(current).question); }} style={smallButton}>
              🔊 Eshitish
            </button>
          )}

          {!revealed ? (
            <button onClick={() => setRevealed(true)} style={{ ...bigButton('#2563eb'), width: '100%', marginTop: '1rem' }}>
              Javobni ko'rsatish
            </button>
          ) : (
            <>
              <div style={{ borderTop: '1px solid #e5e7eb', marginTop: '1rem', paddingTop: '1rem' }}>
                <p style={{ fontSize: '1.2rem', fontWeight: 600, color: '#15803d', margin: 0 }}>{current.back}</p>
                {current.note && <p style={{ color: '#4b5563', fontSize: '0.9rem' }}>{current.note}</p>}
                {isSpeechSupported() && (
                  <button onClick={() => { stopSpeaking(); speakAsync(spokenParts(current).answer); }} style={smallButton}>
                    🔊 Eshitish
                  </button>
                )}
              </div>
              <p style={{ fontSize: '0.8rem', color: '#6b7280', marginBottom: '0.4rem' }}>Qanchalik esladingiz?</p>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '0.4rem' }}>
                {GRADES.map((g) => (
                  <button key={g.value} onClick={() => grade(g.value)} disabled={busy} style={bigButton(g.color)}>
                    {g.label}
                    <span style={{ display: 'block', fontSize: '0.65rem', opacity: 0.85, fontWeight: 400 }}>{g.hint}</span>
                  </button>
                ))}
              </div>
            </>
          )}
          <button onClick={remove} style={{ ...smallButton, color: '#9ca3af', marginTop: '0.75rem' }}>
            Kartani o'chirish
          </button>
        </div>
      ) : (
        stats && (
          <div style={{ ...card, textAlign: 'center' }}>
            <p style={{ fontSize: '1.1rem', margin: '0.25rem 0' }}>
              {stats.total === 0 ? "Hali kartalar yo'q" : "Barakalla! Hozircha takrorlanadigan karta yo'q ✅"}
            </p>
            <p style={{ color: '#6b7280', fontSize: '0.9rem', margin: 0 }}>
              {stats.total === 0
                ? "Gapirish, Yozish, O'qish yoki Tinglash mashqini bajaring — xatolaringiz shu yerga tushadi. Yoki pastda o'zingiz qo'shing."
                : 'Kartalar eslash vaqti kelganda shu yerda paydo bo\'ladi.'}
            </p>
          </div>
        )
      )}

      <CommuteMode dueCards={cards} />
      <AddCardForm onAdded={() => { load(); onChanged(); }} />
    </div>
  );
}

function StatsBar({ stats }: { stats: ReviewStats }) {
  const pct = Math.min(100, Math.round((stats.reviewedToday / stats.dailyGoal) * 100));
  return (
    <div style={{ ...card, padding: '0.75rem 1rem' }}>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem 1.25rem', fontSize: '0.9rem' }}>
        <span>🔥 <strong>{stats.streakDays}</strong> kun ketma-ket</span>
        <span>📌 Navbatda: <strong>{stats.due}</strong></span>
        <span>🗂 Jami: <strong>{stats.total}</strong></span>
      </div>
      <div style={{ marginTop: '0.6rem', fontSize: '0.8rem', color: '#4b5563' }}>
        Bugungi maqsad: {Math.min(stats.reviewedToday, stats.dailyGoal)}/{stats.dailyGoal}
        {stats.reviewedToday >= stats.dailyGoal ? ' — bajarildi 🎉' : ''}
      </div>
      <div style={{ height: 6, background: '#e5e7eb', borderRadius: 3, marginTop: '0.3rem', overflow: 'hidden' }}>
        <div style={{ width: `${pct}%`, height: '100%', background: '#16a34a' }} />
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

  useEffect(() => () => {
    runRef.current++;
    stopSpeaking();
    wakeLockRef.current?.release().catch(() => undefined);
  }, []);

  if (!isSpeechSupported()) return null;

  async function start() {
    const run = ++runRef.current;
    setRunning(true);
    setStatus('Kartalar tayyorlanmoqda...');

    // Ekran o'chib qolsa, ko'p telefonlarda ovoz ham to'xtaydi — imkon
    // bo'lsa ekranni yoniq ushlab turamiz (Screen Wake Lock API).
    try {
      const nav = navigator as Navigator & { wakeLock?: { request: (t: 'screen') => Promise<{ release: () => Promise<void> }> } };
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
    <div style={{ ...card, background: '#f8fafc' }}>
      <h3 style={{ margin: '0 0 0.4rem' }}>🎧 Yo'lda rejimi</h3>
      <p style={{ fontSize: '0.85rem', color: '#4b5563', marginTop: 0 }}>
        Quloqchinni taqing: savol o'qiladi, o'ylash uchun pauza, keyin javob. Qo'l tegizish shart emas.
        Telefon ekrani yoniq tursin — ba'zi telefonlarda ekran o'chsa, ovoz ham to'xtaydi.
      </p>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', alignItems: 'center' }}>
        <button onClick={running ? stop : start} style={bigButton(running ? '#dc2626' : '#0f766e')}>
          {running ? "⏹ To'xtatish" : '▶️ Boshlash'}
        </button>
        <label style={{ fontSize: '0.85rem' }}>
          O'ylash vaqti:{' '}
          <select value={pauseSec} onChange={(e) => setPauseSec(Number(e.target.value))} disabled={running}>
            <option value={2}>2 soniya</option>
            <option value={4}>4 soniya</option>
            <option value={7}>7 soniya</option>
          </select>
        </label>
      </div>
      {status && <p style={{ fontSize: '0.85rem', color: '#374151', marginBottom: 0, overflowWrap: 'anywhere' }}>{status}</p>}
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
      <button onClick={() => setOpen(true)} style={{ ...smallButton, marginTop: '1rem' }}>
        ➕ O'zim karta qo'shaman
      </button>
    );
  }

  const input = {
    width: '100%',
    padding: '0.5rem',
    fontSize: '0.95rem',
    border: '1px solid #d1d5db',
    borderRadius: '0.375rem',
    boxSizing: 'border-box' as const,
    marginBottom: '0.5rem',
  };

  return (
    <form onSubmit={submit} style={card}>
      <h3 style={{ margin: '0 0 0.5rem' }}>Yangi karta</h3>
      <input required maxLength={500} placeholder="So'z yoki ibora (masalan: take for granted)" value={front} onChange={(e) => setFront(e.target.value)} style={input} />
      <input required maxLength={500} placeholder="Ma'nosi (masalan: qadriga yetmaslik)" value={back} onChange={(e) => setBack(e.target.value)} style={input} />
      <input maxLength={1000} placeholder="Misol gap (ixtiyoriy)" value={note} onChange={(e) => setNote(e.target.value)} style={input} />
      <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
        <button type="submit" style={bigButton('#2563eb')}>Qo'shish</button>
        <button type="button" onClick={() => setOpen(false)} style={smallButton}>Yopish</button>
        {msg && <span style={{ fontSize: '0.85rem' }}>{msg}</span>}
      </div>
    </form>
  );
}

const card = {
  marginTop: '1rem',
  padding: '1rem',
  border: '1px solid #e5e7eb',
  borderRadius: '0.75rem',
};

const smallButton = {
  background: 'none',
  border: 'none',
  color: '#2563eb',
  cursor: 'pointer',
  fontSize: '0.9rem',
  padding: 0,
};

function bigButton(color: string) {
  return {
    padding: '0.7rem 0.5rem',
    fontSize: '1rem',
    fontWeight: 600,
    backgroundColor: color,
    color: 'white',
    border: 'none',
    borderRadius: '0.5rem',
    cursor: 'pointer',
    minHeight: 48, // telefonda barmoq bilan bosishga qulay o'lcham
  };
}
