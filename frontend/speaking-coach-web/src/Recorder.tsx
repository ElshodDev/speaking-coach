import { useEffect, useRef, useState } from 'react';
import { STABILITY_RUNS, StabilityTable, computeStats, runSequentially, type DimensionStats } from './Stability';
import { apiJson, loadHistory as fetchHistory, type HistoryItem } from './api';
import { HistoryHint } from './AuthPanel';

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
  saved: boolean;
}

export function Recorder({ loggedIn }: { loggedIn: boolean }) {
  const [status, setStatus] = useState<Status>('idle');
  const [message, setMessage] = useState('');
  const [topic] = useState(TOPICS[0]);
  const [result, setResult] = useState<SubmitResponse | null>(null);
  const [history, setHistory] = useState<HistoryItem[]>([]);
  // Oxirgi yozuv xotirada saqlanadi — barqarorlik testida AYNAN SHU audio
  // qayta-qayta yuboriladi (serverda audio doimiy saqlanmaydi, shuning uchun
  // uni brauzerda ushlab turamiz).
  const [lastBlob, setLastBlob] = useState<Blob | null>(null);
  const [stability, setStability] = useState<{ stats: DimensionStats[]; failed: number } | null>(null);
  const [testProgress, setTestProgress] = useState('');
  const [isTesting, setIsTesting] = useState(false);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);

  useEffect(() => {
    loadHistory();
  }, []);

  async function loadHistory() {
    setHistory(await fetchHistory('speaking'));
  }

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
        setLastBlob(blob);
        await uploadAudio(blob);
      };

      recorder.start();
      mediaRecorderRef.current = recorder;
      setStatus('recording');
      setMessage('Yozilmoqda...');
      setResult(null);
      setStability(null);
      setTestProgress('');
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

  // Bitta yuborish — oddiy oqim (save=true) ham, barqarorlik testi
  // (save=false) ham shu funksiyani ishlatadi.
  async function postAudio(blob: Blob, save: boolean): Promise<SubmitResponse> {
    const formData = new FormData();
    formData.append('audio', blob, 'recording.webm');
    formData.append('topic', topic);

    const path = save ? '/api/speaking/submit' : '/api/speaking/submit?save=false';
    return apiJson<SubmitResponse>(path, { method: 'POST', body: formData });
  }

  async function runStabilityTest() {
    if (!lastBlob) return;
    setIsTesting(true);
    setStability(null);
    setTestProgress(`0/${STABILITY_RUNS} bajarildi...`);

    const { results, failed } = await runSequentially(
      STABILITY_RUNS,
      () => postAudio(lastBlob, false),
      (done, failedSoFar) =>
        setTestProgress(`${done}/${STABILITY_RUNS} bajarildi${failedSoFar ? ` (${failedSoFar} ta xato)` : ''}...`),
    );

    setIsTesting(false);
    if (results.length === 0) {
      setTestProgress("Birorta ham urinish muvaffaqiyatli bo'lmadi — keyinroq qayta urinib ko'ring.");
      return;
    }

    const evals = results.map((r) => r.evaluation);
    setStability({
      stats: [
        computeStats('Ravonlik', evals.map((e) => e.fluency.score)),
        computeStats('Grammatika', evals.map((e) => e.grammar.score)),
        computeStats("Lug'at", evals.map((e) => e.vocabulary.score)),
      ],
      failed,
    });
    setTestProgress('');
  }

  async function uploadAudio(blob: Blob) {
    try {
      const data = await postAudio(blob, true);
      setResult(data);
      setStatus('done');
      setMessage('Tayyor');
      loadHistory(); // yangi urinish ro'yxatga qo'shilishi uchun tarixni qayta yuklaymiz
    } catch (err) {
      setStatus('error');
      setMessage(err instanceof Error ? err.message : "Yuklab bo'lmadi");
    }
  }

  const isRecording = status === 'recording';
  const isBusy = status === 'uploading' || isTesting;

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

          <button
            onClick={runStabilityTest}
            disabled={isBusy || !lastBlob}
            style={{
              marginTop: '0.5rem',
              padding: '0.5rem 1rem',
              fontSize: '0.9rem',
              backgroundColor: 'white',
              color: '#2563eb',
              border: '1px solid #2563eb',
              borderRadius: '0.5rem',
              cursor: isBusy ? 'not-allowed' : 'pointer',
            }}
          >
            🔁 Barqarorlikni tekshirish ({STABILITY_RUNS}x)
          </button>
          <p style={{ color: '#6b7280', fontSize: '0.8rem', marginBottom: 0 }}>
            Aynan shu yozuvni yana {STABILITY_RUNS} marta baholatib, ballar qanchalik o'zgarishini ko'rsatadi.
          </p>
          {testProgress && <p style={{ fontSize: '0.9rem' }}>{testProgress}</p>}
        </div>
      )}

      {stability && <StabilityTable stats={stability.stats} failed={stability.failed} />}

      <HistoryHint loggedIn={loggedIn} />
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
  // shuning uchun bu yerda JSON.parse bilan ochamiz. Agar format kutilganidan
  // farq qilsa (masalan kelajakda Writing turi qo'shilsa, shakli boshqacha
  // bo'lishi mumkin), try/catch bilan xato butun ro'yxatni buzmasligini
  // ta'minlaymiz.
  try {
    const prompt = JSON.parse(item.promptData) as { topic: string };
    const response = JSON.parse(item.responseData) as EvaluationResult;
    const date = new Date(item.createdAtUtc).toLocaleString();

    return (
      <li style={{ padding: '0.75rem 0', borderBottom: '1px solid #e5e7eb' }}>
        <div style={{ fontSize: '0.85rem', color: '#6b7280' }}>{date}</div>
        <div style={{ fontStyle: 'italic' }}>{prompt.topic}</div>
        <div style={{ fontSize: '0.9rem' }}>
          Ravonlik: {response.fluency.score} · Grammatika: {response.grammar.score} · Lug'at:{' '}
          {response.vocabulary.score}
        </div>
      </li>
    );
  } catch {
    return null; // bitta yaroqsiz yozuv butun ro'yxatni yiqitmasin
  }
}
