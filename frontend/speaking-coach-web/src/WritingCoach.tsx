import { useEffect, useState } from 'react';
import { STABILITY_RUNS, StabilityTable, computeStats, runSequentially, type DimensionStats } from './Stability';
import { loadHistory as fetchHistory, postJson, type HistoryItem } from './api';
import { cardsMessage } from './cards';
import { Corrections, Feedback, GuestNote, HistoryList, ScoreBar, type CorrectionItem, type ScoreWithReasoning } from './ui';

export const WRITING_TOPICS = [
  'Do you think social media has a positive or negative effect on society? Explain your view.',
  'Describe a piece of technology that has changed your daily life.',
  'Should university education be free for everyone? Give your opinion with reasons.',
  'Is it better to live in a big city or in the countryside?',
  'Describe a problem in your town and suggest a solution.',
];

const MIN_CHARS = 20;
const MAX_CHARS = 5000;

type Status = 'idle' | 'uploading' | 'done' | 'error';

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
  saved: boolean;
  newCards: number;
}

const countWords = (t: string) => (t.trim() ? t.trim().split(/\s+/).length : 0);

export function WritingCoach({
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
  const topic = WRITING_TOPICS[topicIndex];
  const [text, setText] = useState('');
  const [result, setResult] = useState<SubmitResponse | null>(null);
  const [history, setHistory] = useState<HistoryItem[]>([]);
  // Barqarorlik testi oxirgi MUVAFFAQIYATLI yuborilgan matnni ishlatadi —
  // foydalanuvchi keyin textarea'ni tahrirlasa ham, test aynan baholangan
  // matn ustida o'tadi (aks holda boshqa matnni solishtirgan bo'lamiz).
  const [lastSubmittedText, setLastSubmittedText] = useState<string | null>(null);
  const [stability, setStability] = useState<{ stats: DimensionStats[]; failed: number } | null>(null);
  const [testProgress, setTestProgress] = useState('');
  const [isTesting, setIsTesting] = useState(false);

  useEffect(() => {
    loadHistory();
  }, []);

  async function loadHistory() {
    setHistory(await fetchHistory('writing'));
  }

  // Bitta yuborish — oddiy oqim (save=true) ham, barqarorlik testi
  // (save=false) ham shu funksiyani ishlatadi.
  async function postEssay(essay: string, save: boolean): Promise<SubmitResponse> {
    const path = save ? '/api/writing/submit' : '/api/writing/submit?save=false';
    return postJson<SubmitResponse>(path, { topic, text: essay });
  }

  async function runStabilityTest() {
    if (!lastSubmittedText) return;
    const essay = lastSubmittedText;
    setIsTesting(true);
    setStability(null);
    setTestProgress(`0/${STABILITY_RUNS} bajarildi...`);

    const { results, failed } = await runSequentially(
      STABILITY_RUNS,
      () => postEssay(essay, false),
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
        computeStats('Vazifa', evals.map((e) => e.taskAchievement.score)),
        computeStats("Bog'lanish", evals.map((e) => e.coherenceCohesion.score)),
        computeStats('Grammatika', evals.map((e) => e.grammar.score)),
        computeStats("Lug'at", evals.map((e) => e.vocabulary.score)),
      ],
      failed,
    });
    setTestProgress('');
  }

  async function submitEssay() {
    if (text.trim().length < MIN_CHARS) {
      setStatus('error');
      setMessage(`Matn juda qisqa (kamida ${MIN_CHARS} belgi kerak)`);
      return;
    }

    setStatus('uploading');
    setMessage("O'qilmoqda va baholanmoqda... (5-15 soniya)");
    setResult(null);
    setStability(null);
    setTestProgress('');

    try {
      const data = await postEssay(text, true);
      setLastSubmittedText(text);
      setResult(data);
      setStatus('done');
      setMessage(cardsMessage(data.newCards));
      if (data.newCards > 0) onCardsAdded?.();
      loadHistory(); // yangi urinish ro'yxatga qo'shilishi uchun tarixni qayta yuklaymiz
    } catch (err) {
      setStatus('error');
      setMessage(err instanceof Error ? err.message : "Yuborib bo'lmadi");
    }
  }

  const isBusy = status === 'uploading' || isTesting;
  const words = countWords(text);

  return (
    <>
      <div className="card">
        <div className="spread">
          <span className="muted small">Mavzu</span>
          <button className="btn-link small" disabled={isBusy} onClick={() => setTopicIndex((i) => (i + 1) % WRITING_TOPICS.length)}>
            🔀 Boshqa mavzu
          </button>
        </div>
        <p style={{ fontSize: '1.1rem', fontWeight: 600, margin: '6px 0 14px' }}>{topic}</p>

        <textarea
          className="input"
          value={text}
          onChange={(e) => setText(e.target.value)}
          disabled={isBusy}
          maxLength={MAX_CHARS}
          rows={8}
          placeholder="Shu mavzuda inglizcha yozing... (tavsiya: 80-200 so'z)"
          aria-label="Insho matni"
        />
        <div className="spread muted tiny" style={{ margin: '6px 0 12px' }}>
          <span>{words} so'z</span>
          <span>
            {text.length}/{MAX_CHARS} belgi
          </span>
        </div>

        <button className="btn btn-primary block" onClick={submitEssay} disabled={isBusy}>
          ✍️ Yuborish
        </button>
        {message && (
          <p className={status === 'error' ? 'error small' : 'small'} style={{ marginTop: 10, marginBottom: 0 }}>
            {message}
          </p>
        )}
      </div>

      {result && (
        <div className="card">
          <h3>Baholash</h3>
          <ScoreBar label="Vazifani bajarish" data={result.evaluation.taskAchievement} />
          <ScoreBar label="Mantiqiy bog'lanish" data={result.evaluation.coherenceCohesion} />
          <ScoreBar label="Grammatika" data={result.evaluation.grammar} />
          <ScoreBar label="Lug'at boyligi" data={result.evaluation.vocabulary} />

          <Corrections items={result.evaluation.topCorrections} />
          <Feedback encouragement={result.evaluation.encouragement} nextFocus={result.evaluation.nextFocus} />

          <div style={{ borderTop: '1px solid var(--border)', marginTop: 14, paddingTop: 12 }}>
            <button className="btn btn-outline" onClick={runStabilityTest} disabled={isBusy || !lastSubmittedText}>
              🔁 Barqarorlikni tekshirish ({STABILITY_RUNS}x)
            </button>
            <p className="muted tiny" style={{ marginTop: 6, marginBottom: 0 }}>
              Aynan shu matnni yana {STABILITY_RUNS} marta baholatib, AI bahosi qanchalik o'zgarishini ko'rsatadi.
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
          const r = JSON.parse(item.responseData) as WritingEvaluationResult;
          return (
            <>
              <div className="small">{prompt.topic}</div>
              <div className="small muted">
                Vazifa {r.taskAchievement.score} · Bog'lanish {r.coherenceCohesion.score} · Grammatika {r.grammar.score} ·
                Lug'at {r.vocabulary.score}
              </div>
            </>
          );
        }}
      />
    </>
  );
}
