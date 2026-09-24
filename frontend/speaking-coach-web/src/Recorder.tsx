import { useRef, useState } from 'react';

// Lokalda .env fayl bo'lmasa, localhost:5000'ga tushadi.
// Vercel'ga deploy qilinganda VITE_API_URL environment variable orqali
// haqiqiy backend manziliga (masalan Railway URL'iga) yo'naltiriladi.
const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000';
const API_URL = `${API_BASE}/api/speaking/submit`;

const TOPICS = [
  'Describe your favorite city and why you like it.',
  'Talk about a skill you would like to learn.',
  'Describe a memorable trip you took.',
];

type Status = 'idle' | 'recording' | 'uploading' | 'done' | 'error';

interface ScoreWithReasoning {
  score: number;
  reasoning: string;
}

interface CorrectionItem {
  original: string;
  corrected: string;
  explanation: string;
}

interface EvaluationResult {
  transcript: string;
  fluency: ScoreWithReasoning;
  grammar: ScoreWithReasoning;
  vocabulary: ScoreWithReasoning;
  topCorrections: CorrectionItem[];
  encouragement: string;
  nextFocus: string;
}

interface SubmitResponse {
  submissionId: string;
  evaluation: EvaluationResult;
}

export function Recorder() {
  const [status, setStatus] = useState<Status>('idle');
  const [message, setMessage] = useState('');
  const [topic] = useState(TOPICS[0]);
  const [result, setResult] = useState<SubmitResponse | null>(null);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);

  async function startRecording() {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const recorder = new MediaRecorder(stream);
      chunksRef.current = [];

      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) chunksRef.current.push(event.data);
      };

      recorder.onstop = async () => {
        const blob = new Blob(chunksRef.current, { type: 'audio/webm' });
        stream.getTracks().forEach((track) => track.stop());
        await uploadAudio(blob);
      };

      recorder.start();
      mediaRecorderRef.current = recorder;
      setStatus('recording');
      setMessage('Yozilmoqda...');
      setResult(null);
    } catch {
      setStatus('error');
      setMessage('Mikrofonga ruxsat berilmadi yoki xato yuz berdi');
    }
  }

  function stopRecording() {
    mediaRecorderRef.current?.stop();
    setStatus('uploading');
    setMessage('Yuklanmoqda va baholanmoqda... (bu 5-15 soniya davom etishi mumkin)');
  }

  async function uploadAudio(blob: Blob) {
    const formData = new FormData();
    formData.append('audio', blob, 'recording.webm');
    formData.append('topic', topic);

    try {
      const response = await fetch(API_URL, { method: 'POST', body: formData });

      if (!response.ok) {
        const errorBody = await response.json().catch(() => ({}));
        throw new Error(errorBody.error ?? errorBody.detail ?? `Server xatosi: ${response.status}`);
      }

      const data: SubmitResponse = await response.json();
      setResult(data);
      setStatus('done');
      setMessage('Tayyor');
    } catch (err) {
      setStatus('error');
      setMessage(err instanceof Error ? err.message : "Yuklab bo'lmadi");
    }
  }

  const isRecording = status === 'recording';
  const isBusy = status === 'uploading';

  return (
    <div style={{ marginTop: '1.5rem' }}>
      <p style={{ fontStyle: 'italic', marginBottom: '1rem' }}>Mavzu: {topic}</p>

      <button
        onClick={isRecording ? stopRecording : startRecording}
        disabled={isBusy}
        style={{
          padding: '0.75rem 1.5rem',
          fontSize: '1rem',
          backgroundColor: isRecording ? '#dc2626' : '#2563eb',
          color: 'white',
          border: 'none',
          borderRadius: '0.5rem',
          cursor: isBusy ? 'not-allowed' : 'pointer',
        }}
      >
        {isRecording ? "⏹ To'xtatish" : '🎙 Yozishni boshlash'}
      </button>

      <p style={{ marginTop: '1rem', color: status === 'error' ? '#dc2626' : '#374151' }}>
        {message}
      </p>

      {result && (
        <div style={{ marginTop: '1.5rem', padding: '1rem', border: '1px solid #e5e7eb', borderRadius: '0.5rem' }}>
          <h3>Transkript</h3>
          <p style={{ color: '#4b5563' }}>{result.evaluation.transcript}</p>

          <h3>Baholash</h3>
          <ScoreRow label="Ravonlik (Fluency)" data={result.evaluation.fluency} />
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
