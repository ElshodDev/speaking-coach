import { useEffect, useRef, useState } from 'react';
import { loadHistory as fetchHistory, postJson, type HistoryItem } from './api';
import { cardsMessage } from './cards';
import { isSpeechSupported, speakAsync, stopSpeaking } from './speech';
import { GuestNote, HistoryList } from './ui';

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
  onLogin,
}: {
  mode: Mode;
  loggedIn: boolean;
  onCardsAdded?: () => void;
  onLogin?: () => void;
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
    setMessage('Siz uchun yangi mashq tayyorlanmoqda... (5-15 soniya)');
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

  const answeredCount = answers.filter((a) => a !== null).length;
  const allAnswered = answers.length > 0 && answeredCount === answers.length;
  // Listening'da matn javob berilgunicha yashirin — aks holda bu o'qish
  // mashqiga aylanib qoladi.
  const showPassage = exercise && (mode === 'reading' || result);

  return (
    <>
      {!exercise && (
        <div className="card">
          <p>
            {mode === 'reading'
              ? "Sun'iy intellekt siz uchun yangi matn va 4 ta savol tayyorlaydi. Matnni o'qing va javob bering."
              : "Qisqa nutq tayyorlanadi va ovoz chiqarib o'qiladi. Tinglang, 4 ta savolga javob bering — matn javobdan keyin ko'rinadi."}
          </p>
          <button className="btn btn-primary block" onClick={generate} disabled={busy}>
            {mode === 'reading' ? '📖 Mashqni boshlash' : '🎧 Mashqni boshlash'}
          </button>
        </div>
      )}

      {message && <p className={isError ? 'error small' : 'small'} style={{ marginTop: 12 }}>{message}</p>}

      {exercise && (
        <div className="card">
          <div className="spread">
            <h2 style={{ margin: 0 }}>{exercise.title}</h2>
            <span className="badge">{mode === 'reading' ? "O'qish" : 'Tinglash'}</span>
          </div>

          {mode === 'listening' && <Speaker text={exercise.passage} />}

          {showPassage && (
            <div style={{ marginTop: 12 }}>
              {exercise.passage.split(/\n\s*\n/).map((para, i) => (
                <p key={i} style={{ lineHeight: 1.7 }}>
                  {para}
                </p>
              ))}
            </div>
          )}

          <div className="spread" style={{ margin: '18px 0 10px' }}>
            <h3 style={{ margin: 0 }}>Savollar</h3>
            {!result && (
              <span className="muted small">
                {answeredCount}/{answers.length}
              </span>
            )}
          </div>

          {exercise.questions.map((q, qi) => {
            const r = result?.results[qi];
            return (
              <fieldset key={qi}>
                <legend>
                  {qi + 1}. {q.question}
                </legend>
                {q.options.map((opt, oi) => {
                  const cls = r ? (oi === r.correctIndex ? 'correct' : oi === r.chosenIndex && !r.isCorrect ? 'wrong' : '') : '';
                  return (
                    <label key={oi} className={`option ${cls}`}>
                      <input
                        type="radio"
                        name={`q${qi}`}
                        checked={answers[qi] === oi}
                        disabled={!!result || busy}
                        onChange={() => setAnswers((prev) => prev.map((a, i) => (i === qi ? oi : a)))}
                      />
                      <span>
                        <strong>{LETTERS[oi]}.</strong> {opt}
                      </span>
                    </label>
                  );
                })}
                {r && (
                  <p className={`small ${r.isCorrect ? 'success' : 'error'}`} style={{ margin: '4px 0 0' }}>
                    {r.isCorrect ? "✓ To'g'ri." : `✗ To'g'ri javob: ${LETTERS[r.correctIndex]}.`} {r.explanation}
                  </p>
                )}
              </fieldset>
            );
          })}

          {!result ? (
            <>
              <button className="btn btn-primary block" onClick={submit} disabled={busy || !allAnswered}>
                ✅ Javoblarni tekshirish
              </button>
              {!allAnswered && <p className="muted tiny" style={{ marginTop: 6 }}>Barcha savollarga javob bering.</p>}
            </>
          ) : (
            <div className="card soft center" style={{ marginTop: 8 }}>
              <div className="muted small">Natija</div>
              <div style={{ fontSize: '2rem', fontWeight: 800 }}>
                {result.score}/{result.total}
                {result.score === result.total ? ' 🎉' : ''}
              </div>
              <button className="btn btn-primary" onClick={generate} disabled={busy} style={{ marginTop: 8 }}>
                🔄 Keyingi mashq
              </button>
            </div>
          )}
        </div>
      )}

      <GuestNote loggedIn={loggedIn} onLogin={onLogin} />
      <HistoryList
        items={history}
        renderRow={(item) => {
          const prompt = JSON.parse(item.promptData) as { title: string };
          const r = JSON.parse(item.responseData) as { score: number; total: number };
          return (
            <div className="spread small">
              <span>{prompt.title}</span>
              <strong>
                {r.score}/{r.total}
              </strong>
            </div>
          );
        }}
      />
    </>
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
    // Boshqa ekranga o'tilsa yoki yangi mashq olinsa — ovozni to'xtatamiz.
    return () => window.speechSynthesis.cancel();
  }, [supported, text]);

  if (!supported) {
    return <p className="error small">Brauzeringiz ovoz chiqarib o'qishni qo'llab-quvvatlamaydi. Chrome yoki Edge'da oching.</p>;
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
    <div className="card soft row" style={{ marginTop: 12 }}>
      <button className={`btn ${speaking ? 'btn-danger' : 'btn-primary'}`} onClick={speaking ? stop : play}>
        {speaking ? "⏹ To'xtatish" : plays === 0 ? '▶️ Tinglash' : '🔁 Qayta tinglash'}
      </button>
      <label className="small">
        Tezlik{' '}
        <select className="input" value={rate} onChange={(e) => setRate(Number(e.target.value))} disabled={speaking}>
          <option value={0.75}>Sekin</option>
          <option value={0.9}>O'rtacha</option>
          <option value={1}>Oddiy</option>
        </select>
      </label>
      {plays > 0 && <span className="muted tiny">{plays} marta tinglandi</span>}
    </div>
  );
}
