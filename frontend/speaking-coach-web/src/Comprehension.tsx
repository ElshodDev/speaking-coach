import { useEffect, useRef, useState } from 'react';
import { loadHistory as fetchHistory, postJson, type HistoryItem } from './api';
import { HistoryHint } from './AuthPanel';
import { cardsMessage } from './cards';
import { isSpeechSupported, speakAsync, stopSpeaking } from './speech';

// Reading va Listening — bitta komponent, ikki rejim. Backend ham bitta
// mantiq (ComprehensionEndpoints.cs), faqat yo'l farq qiladi.
type Mode = 'reading' | 'listening';

interface Question {
  question: string;
  options: string[];
}

interface Exercise {
  exerciseId: string;
  title: string;
  passage: string;
  questions: Question[];
}

interface QuestionResult {
  chosenIndex: number;
  correctIndex: number;
  isCorrect: boolean;
  explanation: string;
}

interface SubmitResponse {
  score: number;
  total: number;
  results: QuestionResult[];
  passage: string;
  saved: boolean;
  newCards: number;
}

const LETTERS = ['A', 'B', 'C', 'D', 'E', 'F'];

export function Comprehension({
  mode,
  loggedIn,
  onCardsAdded,
}: {
  mode: Mode;
  loggedIn: boolean;
  onCardsAdded?: () => void;
}) {
  const [exercise, setExercise] = useState<Exercise | null>(null);
  const [answers, setAnswers] = useState<(number | null)[]>([]);
  const [result, setResult] = useState<SubmitResponse | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [isError, setIsError] = useState(false);
  const [history, setHistory] = useState<HistoryItem[]>([]);

  useEffect(() => {
    fetchHistory(mode).then(setHistory);
  }, [mode]);

  async function generate() {
    setBusy(true);
    setIsError(false);
    setMessage('Yangi mashq tayyorlanmoqda... (5-15 soniya)');
    setExercise(null);
    setResult(null);
    try {
      const data = await postJson<Exercise>(`/api/${mode}/generate`, {});
      setExercise(data);
      setAnswers(data.questions.map(() => null));
      setMessage('');
    } catch (err) {
      setIsError(true);
      setMessage(err instanceof Error ? err.message : "Mashqni yaratib bo'lmadi");
    } finally {
      setBusy(false);
    }
  }

  async function submit() {
    if (!exercise || answers.some((a) => a === null)) return;
    setBusy(true);
    setIsError(false);
    try {
      const data = await postJson<SubmitResponse>(`/api/${mode}/submit`, {
        exerciseId: exercise.exerciseId,
        answers,
      });
      setResult(data);
      setMessage(data.newCards > 0 ? cardsMessage(data.newCards) : '');
      if (data.newCards > 0) onCardsAdded?.();
      setHistory(await fetchHistory(mode));
    } catch (err) {
      setIsError(true);
      setMessage(err instanceof Error ? err.message : "Javoblarni yuborib bo'lmadi");
    } finally {
      setBusy(false);
    }
  }

  const allAnswered = answers.length > 0 && answers.every((a) => a !== null);
  // Listening'da matn javob berilgunicha yashirin — aks holda bu o'qish
  // mashqiga aylanib qoladi.
  const showPassage = exercise && (mode === 'reading' || result);

  return (
    <div style={{ marginTop: '1.5rem' }}>
      <p style={{ color: '#4b5563', fontSize: '0.95rem' }}>
        {mode === 'reading'
          ? "Sun'iy intellekt siz uchun yangi matn va 4 ta savol tayyorlaydi. Matnni o'qing va javob bering."
          : "Sun'iy intellekt qisqa nutq tayyorlaydi, brauzer uni ovoz chiqarib o'qiydi. Tinglang va 4 ta savolga javob bering — matn javobdan keyin ko'rinadi."}
      </p>

      <button onClick={generate} disabled={busy} style={primaryButton(busy)}>
        {exercise ? '🔄 Boshqa mashq' : mode === 'reading' ? '📖 Mashqni boshlash' : '🎧 Mashqni boshlash'}
      </button>

      {message && <p style={{ marginTop: '1rem', color: isError ? '#dc2626' : '#374151' }}>{message}</p>}

      {exercise && (
        <div style={{ marginTop: '1.5rem', padding: '1rem', border: '1px solid #e5e7eb', borderRadius: '0.5rem' }}>
          <h3 style={{ marginTop: 0 }}>{exercise.title}</h3>

          {mode === 'listening' && <Speaker text={exercise.passage} />}

          {showPassage &&
            exercise.passage.split(/\n\s*\n/).map((para, i) => (
              <p key={i} style={{ lineHeight: 1.6, color: '#1f2937' }}>
                {para}
              </p>
            ))}

          <h3>Savollar</h3>
          {exercise.questions.map((q, qi) => {
            const r = result?.results[qi];
            return (
              <fieldset key={qi} style={{ border: 'none', padding: 0, margin: '0 0 1rem' }}>
                <legend style={{ fontWeight: 600, marginBottom: '0.4rem' }}>
                  {qi + 1}. {q.question}
                </legend>
                {q.options.map((opt, oi) => {
                  const isCorrect = r && oi === r.correctIndex;
                  const isWrongChoice = r && oi === r.chosenIndex && !r.isCorrect;
                  return (
                    <label
                      key={oi}
                      style={{
                        display: 'flex',
                        gap: '0.5rem',
                        alignItems: 'flex-start',
                        padding: '0.35rem 0.5rem',
                        borderRadius: '0.375rem',
                        cursor: result ? 'default' : 'pointer',
                        backgroundColor: isCorrect ? '#dcfce7' : isWrongChoice ? '#fee2e2' : 'transparent',
                      }}
                    >
                      <input
                        type="radio"
                        name={`q${qi}`}
                        checked={answers[qi] === oi}
                        disabled={!!result || busy}
                        onChange={() => setAnswers((prev) => prev.map((a, i) => (i === qi ? oi : a)))}
                        style={{ marginTop: '0.2rem' }}
                      />
                      <span>
                        <strong>{LETTERS[oi]}.</strong> {opt}
                      </span>
                    </label>
                  );
                })}
                {r && (
                  <p style={{ fontSize: '0.85rem', color: r.isCorrect ? '#16a34a' : '#b91c1c', margin: '0.3rem 0 0' }}>
                    {r.isCorrect ? "✓ To'g'ri." : `✗ To'g'ri javob: ${LETTERS[r.correctIndex]}.`} {r.explanation}
                  </p>
                )}
              </fieldset>
            );
          })}

          {!result && (
            <button onClick={submit} disabled={busy || !allAnswered} style={primaryButton(busy || !allAnswered)}>
              ✅ Javoblarni tekshirish
            </button>
          )}
          {!result && !allAnswered && (
            <p style={{ fontSize: '0.8rem', color: '#6b7280' }}>Barcha savollarga javob bering.</p>
          )}

          {result && (
            <p style={{ fontSize: '1.1rem', marginBottom: 0 }}>
              Natija: <strong>{result.score}/{result.total}</strong>
              {result.score === result.total ? ' 🎉' : ''}
            </p>
          )}
        </div>
      )}

      <HistoryHint loggedIn={loggedIn} />
      {history.length > 0 && (
        <div style={{ marginTop: '2rem' }}>
          <h3>Oldingi urinishlar ({history.length})</h3>
          <ul style={{ listStyle: 'none', padding: 0 }}>
            {history.map((item) => (
              <HistoryRow key={item.id} item={item} />
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

/**
 * Brauzerning o'rnatilgan ovoz sintezi (Web Speech API) — tashqi xizmat
 * yoki API kaliti kerak emas, bepul. Kamchiligi: ovoz sifati brauzer va
 * operatsion tizimga bog'liq (Chrome'da odatda eng yaxshi).
 */
function Speaker({ text }: { text: string }) {
  const supported = isSpeechSupported();
  const [speaking, setSpeaking] = useState(false);
  const [plays, setPlays] = useState(0);
  const [rate, setRate] = useState(0.9);
  const runRef = useRef(0);

  useEffect(() => {
    if (!supported) return;
    // Chrome ovozlar ro'yxatini kechikib yuklaydi — oldindan "uyg'otib" qo'yamiz.
    window.speechSynthesis.getVoices();
    // Boshqa tab'ga o'tilsa yoki yangi mashq olinsa — ovozni to'xtatamiz.
    return () => window.speechSynthesis.cancel();
  }, [supported, text]);

  if (!supported) {
    return (
      <p style={{ color: '#dc2626' }}>
        Brauzeringiz ovoz chiqarib o'qishni qo'llab-quvvatlamaydi. Chrome yoki Edge'da oching.
      </p>
    );
  }

  async function play() {
    stopSpeaking();
    // To'xtatilgan oldingi o'qish ham "tugadi" deb xabar beradi — u yangi
    // o'qishning holatini buzmasligi uchun har bir o'qishga raqam beramiz.
    const run = ++runRef.current;
    setSpeaking(true);
    setPlays((p) => p + 1);
    await speakAsync(text, rate);
    if (runRef.current === run) setSpeaking(false);
  }

  function stop() {
    runRef.current++;
    stopSpeaking();
    setSpeaking(false);
  }

  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '0.5rem', margin: '0.5rem 0 1rem' }}>
      <button onClick={speaking ? stop : play} style={primaryButton(false)}>
        {speaking ? '⏹ To\'xtatish' : plays === 0 ? '▶️ Tinglash' : '🔁 Qayta tinglash'}
      </button>
      <label style={{ fontSize: '0.85rem', color: '#374151' }}>
        Tezlik:{' '}
        <select value={rate} onChange={(e) => setRate(Number(e.target.value))} disabled={speaking}>
          <option value={0.75}>Sekin</option>
          <option value={0.9}>O'rtacha</option>
          <option value={1}>Oddiy</option>
        </select>
      </label>
      {plays > 0 && <span style={{ fontSize: '0.8rem', color: '#6b7280' }}>{plays} marta tinglandi</span>}
    </div>
  );
}

function HistoryRow({ item }: { item: HistoryItem }) {
  try {
    const prompt = JSON.parse(item.promptData) as { title: string };
    const response = JSON.parse(item.responseData) as { score: number; total: number };
    return (
      <li style={{ padding: '0.75rem 0', borderBottom: '1px solid #e5e7eb' }}>
        <div style={{ fontSize: '0.85rem', color: '#6b7280' }}>{new Date(item.createdAtUtc).toLocaleString()}</div>
        <div>
          {prompt.title} — <strong>{response.score}/{response.total}</strong>
        </div>
      </li>
    );
  } catch {
    return null; // bitta yaroqsiz yozuv butun ro'yxatni yiqitmasin
  }
}

function primaryButton(disabled: boolean) {
  return {
    padding: '0.6rem 1.25rem',
    fontSize: '1rem',
    backgroundColor: disabled ? '#93c5fd' : '#2563eb',
    color: 'white',
    border: 'none',
    borderRadius: '0.5rem',
    cursor: disabled ? 'not-allowed' : 'pointer',
  };
}
