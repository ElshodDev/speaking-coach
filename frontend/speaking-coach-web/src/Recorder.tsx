import { useEffect, useRef, useState } from 'react';
import { STABILITY_RUNS, StabilityTable, computeStats, runSequentially, type DimensionStats } from './Stability';
import { apiJson, getLevel, loadHistory as fetchHistory, type HistoryItem } from './api';
import { cardsMessage } from './cards';
import { useT } from './i18n';
import { speakingMsg } from './locales/speaking';
import { stabilityMsg } from './locales/stability';
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
  const t = useT(speakingMsg);
  const ts = useT(stabilityMsg);
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
      setMessage(t.micDenied);
    }
  }

  function stopRecording() {
    mediaRecorderRef.current?.stop();
    setStatus('uploading');
    setMessage(t.evaluating);
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
    setTestProgress(ts.progress(0, STABILITY_RUNS, 0));

    const { results, failed } = await runSequentially(
      STABILITY_RUNS,
      () => postAudio(lastBlob, false),
      (done, failedSoFar) =>
        setTestProgress(ts.progress(done, STABILITY_RUNS, failedSoFar)),
    );

    setIsTesting(false);
    if (results.length === 0) {
      setTestProgress(ts.allFailed);
      return;
    }

    const evals = results.map((r) => r.evaluation);
    setStability({
      stats: [
        // Yorliq sifatida kalit saqlanadi — matn chizishda joriy tilda olinadi.
        computeStats('fluency', evals.map((e) => e.fluency.score)),
        computeStats('grammar', evals.map((e) => e.grammar.score)),
        computeStats('vocabulary', evals.map((e) => e.vocabulary.score)),
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
      setMessage(err instanceof Error ? err.message : t.uploadFailed);
    }
  }

  const isRecording = status === 'recording';
  const isBusy = status === 'uploading' || isTesting;

  return (
    <>
      <div className="card">
        <div className="spread">
          <span className="muted small">{t.topic}</span>
          <button
            className="btn-link small"
            disabled={isRecording || isBusy}
            onClick={() => setTopicIndex((i) => (i + 1) % SPEAKING_TOPICS.length)}
          >
            {t.otherTopic}
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
              <span className="recording-dot" /> {t.stop} · {Math.floor(seconds / 60)}:{String(seconds % 60).padStart(2, '0')}
            </>
          ) : (
            t.start
          )}
        </button>
        <p className="muted tiny" style={{ marginTop: 8, marginBottom: 0 }}>
          {t.tip}
        </p>
        {message && (
          <p className={status === 'error' ? 'error small' : 'small'} style={{ marginTop: 10, marginBottom: 0 }}>
            {message}
          </p>
        )}
      </div>

      {result && (
        <div className="card">
          <h3>{t.youSaid}</h3>
          <p className="quote">{result.evaluation.transcript}</p>

          <h3 style={{ marginTop: 18 }}>{t.evaluation}</h3>
          <ScoreBar label={t.criteria.fluency} data={result.evaluation.fluency} />
          <ScoreBar label={t.criteria.grammar} data={result.evaluation.grammar} />
          <ScoreBar label={t.criteria.vocabulary} data={result.evaluation.vocabulary} />

          <Corrections items={result.evaluation.topCorrections} />
          <Feedback encouragement={result.evaluation.encouragement} nextFocus={result.evaluation.nextFocus} />

          <div style={{ borderTop: '1px solid var(--border)', marginTop: 14, paddingTop: 12 }}>
            <button className="btn btn-outline" onClick={runStabilityTest} disabled={isBusy || !lastBlob}>
              {ts.checkButton(STABILITY_RUNS)}
            </button>
            <p className="muted tiny" style={{ marginTop: 6, marginBottom: 0 }}>
              {t.stabilityHint(STABILITY_RUNS)}
            </p>
            {testProgress && <p className="small">{testProgress}</p>}
          </div>
        </div>
      )}

      {stability && <StabilityTable
          stats={stability.stats.map((s) => ({ ...s, label: t.short[s.label as keyof typeof t.short] ?? s.label }))}
          failed={stability.failed}
        />}

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
                {t.short.fluency} {r.fluency.score} · {t.short.grammar} {r.grammar.score} · {t.short.vocabulary} {r.vocabulary.score}
              </div>
            </>
          );
        }}
      />
    </>
  );
}
