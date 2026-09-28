import { useEffect, useRef, useState } from 'react';
import { STABILITY_RUNS, StabilityTable, computeStats, runSequentially, type DimensionStats } from './Stability';
import { apiJson, getLevel, loadHistory as fetchHistory, type HistoryItem } from './api';
import { cardsMessage } from './cards';
import { useIsAdmin } from './adminFlag';
import { useT } from './i18n';
import { speakingMsg } from './locales/speaking';
import { currentMicEnv, micPrecheck, micProblemOf, type MicProblem } from './micErrors';
import { MicProblemNote } from './MicProblem';
import { audioExt } from './shadowLogic';
import { initialTopic, TopicPicker, type PickedTopic } from './TopicPicker';
import { stabilityMsg } from './locales/stability';
import { Corrections, Feedback, GuestNote, HistoryList, ScoreBar, type CorrectionItem, type ScoreWithReasoning } from './ui';

// Mavzular endi ilovaning tayyor bankida (content/topics.ts) — TopicPicker orqali.

type Status = 'idle' | 'recording' | 'uploading' | 'done' | 'error';

/**
 * Yozuvning eng uzun davomiyligi (soniya). Server 10 MB gacha qabul qiladi
 * (3 daqiqa webm/opus ≈ 1 MB), lekin uzun nutqni AI sifatsiz baholaydi va
 * so'rov sekinlashadi — 3 daqiqada avtomatik to'xtaymiz.
 */
export const DEFAULT_MAX_SECONDS = 180;
/** Oxirgi shuncha soniyada "qolgan vaqt" ko'rsatiladi. */
const WARN_SECONDS = 30;

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
  maxSeconds = DEFAULT_MAX_SECONDS,
}: {
  loggedIn: boolean;
  onCardsAdded?: () => void;
  onLogin?: () => void;
  /** Yozuv shuncha soniyada avtomatik to'xtaydi (standart — 3 daqiqa). */
  maxSeconds?: number;
}) {
  const t = useT(speakingMsg);
  const ts = useT(stabilityMsg);
  const isAdmin = useIsAdmin();
  const [micProblem, setMicProblem] = useState<MicProblem | null>(null);
  const [status, setStatus] = useState<Status>('idle');
  const [message, setMessage] = useState('');
  const [picked, setPicked] = useState<PickedTopic>(() => initialTopic('speaking', getLevel()));
  const topic = picked.text;
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
  const streamRef = useRef<MediaStream | null>(null);
  const aliveRef = useRef(true);

  useEffect(() => {
    loadHistory();
  }, []);

  // Sahifadan chiqilganda mikrofon o'chadi (yozuv yuborilmaydi) —
  // aks holda brauzer "mikrofon yoniq" belgisini ko'rsatib turadi.
  useEffect(() => {
    aliveRef.current = true;
    return () => {
      aliveRef.current = false;
      const rec = mediaRecorderRef.current;
      if (rec && rec.state !== 'inactive') {
        rec.onstop = null;
        try {
          rec.stop();
        } catch {
          /* allaqachon to'xtagan */
        }
      }
      streamRef.current?.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
    };
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
    setMicProblem(null);
    const env = currentMicEnv();
    const pre = micPrecheck(env);
    if (pre) {
      setStatus('error');
      setMessage('');
      setMicProblem(pre);
      return;
    }
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      if (!aliveRef.current) {
        stream.getTracks().forEach((track) => track.stop());
        return;
      }
      streamRef.current = stream;
      const recorder = new MediaRecorder(stream);
      chunksRef.current = [];

      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) chunksRef.current.push(event.data);
      };

      recorder.onstop = async () => {
        // Safari — audio/mp4, Firefox — ogg: brauzer yozgan haqiqiy format.
        const blob = new Blob(chunksRef.current, { type: recorder.mimeType || 'audio/webm' });
        stream.getTracks().forEach((track) => track.stop());
        streamRef.current = null;
        if (!aliveRef.current) return;
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
    } catch (err) {
      streamRef.current?.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
      setStatus('error');
      setMessage('');
      setMicProblem(micProblemOf(err, env));
    }
  }

  function stopRecording(auto = false) {
    const rec = mediaRecorderRef.current;
    if (!rec || rec.state === 'inactive') return;
    rec.stop();
    setStatus('uploading');
    setMessage(auto ? `${t.autoStopped(maxSeconds)} ${t.evaluating}` : t.evaluating);
  }

  // Eng uzun davomiylikka yetdi — o'zimiz to'xtatib, baholashga yuboramiz.
  useEffect(() => {
    if (status === 'recording' && seconds >= maxSeconds) stopRecording(true);
    // stopRecording har chizishda yangi — faqat soniya/holat o'zgarganda tekshiramiz.
  }, [seconds, status, maxSeconds]);

  // Yuborish muvaffaqiyatsiz bo'lsa (internet uzildi, AI band) — o'sha yozuvni qayta yuborish.
  function retryUpload() {
    if (!lastBlob) return;
    setStatus('uploading');
    setMessage(t.evaluating);
    uploadAudio(lastBlob);
  }

  // Bitta yuborish — oddiy oqim (save=true) ham, barqarorlik testi
  // (save=false) ham shu funksiyani ishlatadi.
  async function postAudio(blob: Blob, save: boolean): Promise<SubmitResponse> {
    const formData = new FormData();
    formData.append('audio', blob, `recording.${audioExt(blob.type)}`);
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
  const left = Math.max(0, maxSeconds - seconds);
  const clock = (s: number) => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
  const uploadFailed = status === 'error' && !micProblem && !!lastBlob && !result;

  return (
    <div className="practice-split">
      <div className="card">
        <span className="muted small">{t.topic}</span>
        <TopicPicker kind="speaking" level={getLevel()} value={picked} onChange={setPicked} disabled={isRecording || isBusy} />

        <button
          className={`btn block ${isRecording ? 'btn-danger' : 'btn-primary'}`}
          onClick={isRecording ? () => stopRecording() : startRecording}
          disabled={isBusy}
          style={{ minHeight: 56, fontSize: '1.05rem' }}
        >
          {isRecording ? (
            <>
              <span className="recording-dot" /> {t.stop} · {clock(seconds)}
            </>
          ) : (
            t.start
          )}
        </button>
        {isRecording && left <= WARN_SECONDS ? (
          <p className="small" style={{ marginTop: 8, marginBottom: 0, color: 'var(--warning)', fontWeight: 600 }} data-testid="rec-left">
            {t.timeLeft(left)}
          </p>
        ) : (
          <p className="muted tiny" style={{ marginTop: 8, marginBottom: 0 }}>
            {t.tip} {t.maxLength(Math.round(maxSeconds / 60))}
          </p>
        )}
        {micProblem && <MicProblemNote problem={micProblem} onRetry={startRecording} />}
        {message && (
          <p className={status === 'error' ? 'error small' : 'small'} style={{ marginTop: 10, marginBottom: 0 }}>
            {message}
          </p>
        )}
        {uploadFailed && (
          <button className="btn btn-outline btn-sm" style={{ marginTop: 8 }} onClick={retryUpload}>
            {t.retry}
          </button>
        )}
      </div>

      <div>
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

          {/* Barqarorlik testi — ichki tekshiruv vositasi (5× AI so'rovi), faqat admin uchun. */}
          {isAdmin && (
            <div style={{ borderTop: '1px solid var(--border)', marginTop: 14, paddingTop: 12 }}>
              <button className="btn btn-outline" onClick={runStabilityTest} disabled={isBusy || !lastBlob}>
                {ts.checkButton(STABILITY_RUNS)}
              </button>
              <p className="muted tiny" style={{ marginTop: 6, marginBottom: 0 }}>
                {t.stabilityHint(STABILITY_RUNS)}
              </p>
              {testProgress && <p className="small">{testProgress}</p>}
            </div>
          )}
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
      </div>
    </div>
  );
}
