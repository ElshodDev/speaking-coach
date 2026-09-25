import { useEffect, useRef, useState } from 'react';
import { STABILITY_RUNS, StabilityTable, computeStats, runSequentially, type DimensionStats } from './Stability';
import { apiJson, getLevel, loadHistory as fetchHistory, type HistoryItem } from './api';
import { cardsMessage } from './cards';
import { Corrections, Feedback, GuestNote, HistoryList, ScoreBar, type CorrectionItem, type ScoreWithReasoning } from './ui';

export const SPEAKING_TOPICS = [
  'Describe your favorite city and why you like it.',
  'Talk about a skill you would like to learn.',
  'Describe a memorable trip you took.',
  'What do you usually do at the weekend?',
  'Talk about a person who inspires you.',
  'Describe your ideal job.',
];

type Status = 'idle' | 'recording' | 'uploading' | 'done' | 'error';

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
  newCards: number;
}

export function Recorder({
  loggedIn,
  onCardsAdded,
  onLogin,
}: {
  loggedIn: boolean;
  onCardsAdded?: () => void;
  onLogin?: () => void;
}) {
  const [status, setStatus] = useState<Status>('idle');
  const [message, setMessage] = useState('');
  const [topicIndex, setTopicIndex] = useState(0);
  const topic = SPEAKING_TOPICS[topicIndex];
  const [result, setResult] = useState<SubmitResponse | null>(null);
  const [history, setHistory] = useState<HistoryItem[]>([]);
  const [seconds, setSeconds] = useState(0);
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

  // Yozish davomida soniyalarni sanaymiz — 15-20 soniya tavsiya etiladi.
  useEffect(() => {
    if (status !== 'recording') return;
    setSeconds(0);
    const id = setInterval(() => setSeconds((s) => s + 1), 1000);
    return () => clearInterval(id);
  }, [status]);

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
      setMessage('');
      setResult(null);
      setStability(null);
      setTestProgress('');
    } catch {
      setStatus('error');
      setMessage('Mikrofonga ruxsat berilmadi. Brauzer manzil satridagi 🔒 belgisidan mikrofonga ruxsat bering.');
    }
  }

  function stopRecording() {
    mediaRecorderRef.current?.stop();
    setStatus('uploading');
    setMessage('Tinglanmoqda va baholanmoqda... (5-15 soniya)');
  }

  // Bitta yuborish — oddiy oqim (save=true) ham, barqarorlik testi
  // (save=false) ham shu funksiyani ishlatadi.
  async function postAudio(blob: Blob, save: boolean): Promise<SubmitResponse> {
    const formData = new FormData();
    formData.append('audio', blob, 'recording.webm');
    formData.append('topic', topic);
    formData.append('level', getLevel());

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
      setMessage(cardsMessage(data.newCards));
      if (data.newCards > 0) onCardsAdded?.();
      loadHistory(); // yangi urinish ro'yxatga qo'shilishi uchun tarixni qayta yuklaymiz
    } catch (err) {
      setStatus('error');
      setMessage(err instanceof Error ? err.message : "Yuklab bo'lmadi");
    }
  }

  const isRecording = status === 'recording';
  const isBusy = status === 'uploading' || isTesting;

  return (
    <>
      <div className="card">
        <div className="spread">
          <span className="muted small">Mavzu</span>
          <button
            className="btn-link small"
            disabled={isRecording || isBusy}
            onClick={() => setTopicIndex((i) => (i + 1) % SPEAKING_TOPICS.length)}
          >
            🔀 Boshqa mavzu
          </button>
        </div>
        <p style={{ fontSize: '1.15rem', fontWeight: 600, margin: '6px 0 14px' }}>{topic}</p>

        <button
          className={`btn block ${isRecording ? 'btn-danger' : 'btn-primary'}`}
          onClick={isRecording ? stopRecording : startRecording}
          disabled={isBusy}
          style={{ minHeight: 56, fontSize: '1.05rem' }}
        >
          {isRecording ? (
            <>
              <span className="recording-dot" /> To'xtatish · {Math.floor(seconds / 60)}:{String(seconds % 60).padStart(2, '0')}
            </>
          ) : (
            '🎙 Yozishni boshlash'
          )}
        </button>
        <p className="muted tiny" style={{ marginTop: 8, marginBottom: 0 }}>
          Maslahat: 15-30 soniya gapiring. Xato qilishdan qo'rqmang — aynan ular takrorlash kartalariga aylanadi.
        </p>
        {message && (
          <p className={status === 'error' ? 'error small' : 'small'} style={{ marginTop: 10, marginBottom: 0 }}>
            {message}
          </p>
        )}
      </div>

      {result && (
        <div className="card">
          <h3>Siz aytdingiz</h3>
          <p className="quote">{result.evaluation.transcript}</p>

          <h3 style={{ marginTop: 18 }}>Baholash</h3>
          <ScoreBar label="Ravonlik" data={result.evaluation.fluency} />
          <ScoreBar label="Grammatika" data={result.evaluation.grammar} />
          <ScoreBar label="Lug'at boyligi" data={result.evaluation.vocabulary} />

          <Corrections items={result.evaluation.topCorrections} />
          <Feedback encouragement={result.evaluation.encouragement} nextFocus={result.evaluation.nextFocus} />

          <div style={{ borderTop: '1px solid var(--border)', marginTop: 14, paddingTop: 12 }}>
            <button className="btn btn-outline" onClick={runStabilityTest} disabled={isBusy || !lastBlob}>
              🔁 Barqarorlikni tekshirish ({STABILITY_RUNS}x)
            </button>
            <p className="muted tiny" style={{ marginTop: 6, marginBottom: 0 }}>
              Aynan shu yozuvni yana {STABILITY_RUNS} marta baholatib, AI bahosi qanchalik o'zgarishini ko'rsatadi.
            </p>
            {testProgress && <p className="small">{testProgress}</p>}
          </div>
        </div>
      )}

      {stability && <StabilityTable stats={stability.stats} failed={stability.failed} />}

      <GuestNote loggedIn={loggedIn} onLogin={onLogin} />
      <HistoryList
        items={history}
        renderRow={(item) => {
          const prompt = JSON.parse(item.promptData) as { topic: string };
          const r = JSON.parse(item.responseData) as EvaluationResult;
          return (
            <>
              <div className="small">{prompt.topic}</div>
              <div className="small muted">
                Ravonlik {r.fluency.score} · Grammatika {r.grammar.score} · Lug'at {r.vocabulary.score}
              </div>
            </>
          );
        }}
      />
    </>
  );
}
