import { useEffect, useState } from 'react';

// Recorder.tsx bilan bir xil manzil — backend bitta, faqat yo'l (path) farq
// qiladi (/api/writing/... vs /api/speaking/...).
const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000';
const API_URL = `${API_BASE}/api/writing/submit`;

const TOPICS = [
  'Do you think social media has a positive or negative effect on society? Explain your view.',
  'Describe a piece of technology that has changed your daily life.',
  'Should university education be free for everyone? Give your opinion with reasons.',
];

const MIN_CHARS = 20;
const MAX_CHARS = 5000;

type Status = 'idle' | 'uploading' | 'done' | 'error';

interface ScoreWithReasoning {
  score: number;
  reasoning: string;
}

interface CorrectionItem {
  original: string;
  corrected: string;
  explanation: string;
}

// Speaking'dan farqli rubrika: Fluency o'rniga Task Achievement va
// Coherence & Cohesion — bular yozma matn uchun mazmunliroq mezonlar
// (backend'dagi WritingEvaluationResult bilan bir xil shakl).
interface WritingEvaluationResult {
  taskAchievement: ScoreWithReasoning;
  coherenceCohesion: ScoreWithReasoning;
  grammar: ScoreWithReasoning;
  vocabulary: ScoreWithReasoning;
  topCorrections: CorrectionItem[];
  encouragement: string;
  nextFocus: string;
}

interface SubmitResponse {
  submissionId: string;
  evaluation: WritingEvaluationResult;
}

interface HistoryItem {
  id: string;
  createdAtUtc: string;
  promptData: string;
  responseData: string;
}

export function WritingCoach() {
  const [status, setStatus] = useState<Status>('idle');
  const [message, setMessage] = useState('');
  const [topic] = useState(TOPICS[0]);
  const [text, setText] = useState('');
  const [result, setResult] = useState<SubmitResponse | null>(null);
  const [history, setHistory] = useState<HistoryItem[]>([]);

  useEffect(() => {
    loadHistory();
  }, []);

  async function loadHistory() {
    try {
      const response = await fetch(`${API_BASE}/api/writing/history`);
      if (!response.ok) return; // tarix ixtiyoriy — o'chib qolsa ham asosiy funksiya ishlayveradi
      const data: HistoryItem[] = await response.json();
      setHistory(data);
    } catch {
      // Tarmoq xatosi bo'lsa ham jim o'tkazamiz.
    }
  }

  async function submitEssay() {
    if (text.trim().length < MIN_CHARS) {
      setStatus('error');
      setMessage(`Matn juda qisqa (kamida ${MIN_CHARS} belgi kerak)`);
      return;
    }

    setStatus('uploading');
    setMessage('Yuborilmoqda va baholanmoqda... (bu 5-15 soniya davom etishi mumkin)');
    setResult(null);

    try {
      const response = await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ topic, text }),
      });

      if (!response.ok) {
        const errorBody = await response.json().catch(() => ({}));
        throw new Error(errorBody.error ?? errorBody.detail ?? `Server xatosi: ${response.status}`);
      }

      const data: SubmitResponse = await response.json();
      setResult(data);
      setStatus('done');
      setMessage('Tayyor');
      loadHistory(); // yangi urinish ro'yxatga qo'shilishi uchun tarixni qayta yuklaymiz
    } catch (err) {
      setStatus('error');
      setMessage(err instanceof Error ? err.message : "Yuborib bo'lmadi");
    }
  }

  const isBusy = status === 'uploading';
  const charsLeft = MAX_CHARS - text.length;

  return (
    <div style={{ marginTop: '1.5rem' }}>
      <p style={{ fontStyle: 'italic', marginBottom: '1rem' }}>Mavzu: {topic}</p>

      <textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        disabled={isBusy}
        maxLength={MAX_CHARS}
        rows={8}
        placeholder="Shu mavzuda inglizcha insho yozing..."
        style={{
          width: '100%',
          padding: '0.75rem',
          fontSize: '1rem',
          fontFamily: 'inherit',
          border: '1px solid #d1d5db',
          borderRadius: '0.5rem',
          boxSizing: 'border-box',
          resize: 'vertical',
        }}
      />
      <p style={{ fontSize: '0.8rem', color: charsLeft < 0 ? '#dc2626' : '#6b7280', marginTop: '0.25rem' }}>
        {text.length}/{MAX_CHARS} belgi
      </p>

      <button
        onClick={submitEssay}
        disabled={isBusy}
        style={{
          padding: '0.75rem 1.5rem',
          fontSize: '1rem',
          backgroundColor: '#2563eb',
          color: 'white',
          border: 'none',
          borderRadius: '0.5rem',
          cursor: isBusy ? 'not-allowed' : 'pointer',
        }}
      >
        ✍️ Yuborish
      </button>

      <p style={{ marginTop: '1rem', color: status === 'error' ? '#dc2626' : '#374151' }}>
        {message}
      </p>

      {result && (
        <div style={{ marginTop: '1.5rem', padding: '1rem', border: '1px solid #e5e7eb', borderRadius: '0.5rem' }}>
          <h3>Baholash</h3>
          <ScoreRow label="Vazifani bajarish (Task Achievement)" data={result.evaluation.taskAchievement} />
          <ScoreRow label="Mantiqiy bog'lanish (Coherence & Cohesion)" data={result.evaluation.coherenceCohesion} />
          <ScoreRow label="Grammatika" data={result.evaluation.grammar} />
          <ScoreRow label="Lug'at boyligi" data={result.evaluation.vocabulary} />

          {result.evaluation.topCorrections.length > 0 && (
            <>
              <h3>Tuzatishlar</h3>
              <ul>
                {result.evaluation.topCorrections.map((c, i) => (
                  <li key={i} style={{ marginBottom: '0.5rem' }}>
                    <s>{c.original}</s> → <strong>{c.corrected}</strong>
                    <br />
                    <span style={{ color: '#6b7280', fontSize: '0.9rem' }}>{c.explanation}</span>
                  </li>
                ))}
              </ul>
            </>
          )}

          <p style={{ marginTop: '1rem' }}>
            <strong>💬 {result.evaluation.encouragement}</strong>
          </p>
          <p style={{ color: '#2563eb' }}>Keyingi fokus: {result.evaluation.nextFocus}</p>
        </div>
      )}

      {history.length > 0 && (
        <div style={{ marginTop: '2rem' }}>
          <h3>Oldingi urinishlar ({history.length})</h3>
          <p style={{ color: '#6b7280', fontSize: '0.85rem', marginTop: '-0.5rem' }}>
            Bu ro'yxat ma'lumotlar bazasidan (Postgres) o'qilyapti — sahifani yangilasangiz ham yo'qolmaydi.
          </p>
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

function ScoreRow({ label, data }: { label: string; data: ScoreWithReasoning }) {
  return (
    <div style={{ marginBottom: '0.75rem' }}>
      <strong>{label}: {data.score}/100</strong>
      <p style={{ margin: '0.25rem 0', color: '#6b7280', fontSize: '0.9rem' }}>{data.reasoning}</p>
    </div>
  );
}

function HistoryRow({ item }: { item: HistoryItem }) {
  // promptData/responseData bazada jsonb (matn) sifatida saqlangan —
  // shuning uchun bu yerda JSON.parse bilan ochamiz. Bitta yaroqsiz yozuv
  // butun ro'yxatni buzmasin deb try/catch bilan o'raymiz.
  try {
    const prompt = JSON.parse(item.promptData) as { topic: string; text: string };
    const response = JSON.parse(item.responseData) as WritingEvaluationResult;
    const date = new Date(item.createdAtUtc).toLocaleString();

    return (
      <li style={{ padding: '0.75rem 0', borderBottom: '1px solid #e5e7eb' }}>
        <div style={{ fontSize: '0.85rem', color: '#6b7280' }}>{date}</div>
        <div style={{ fontStyle: 'italic' }}>{prompt.topic}</div>
        <div style={{ fontSize: '0.9rem' }}>
          Vazifa: {response.taskAchievement.score} · Bog'lanish: {response.coherenceCohesion.score} · Grammatika:{' '}
          {response.grammar.score} · Lug'at: {response.vocabulary.score}
        </div>
      </li>
    );
  } catch {
    return null; // bitta yaroqsiz yozuv butun ro'yxatni yiqitmasin
  }
}
