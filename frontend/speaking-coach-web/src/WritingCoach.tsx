import { useEffect, useRef, useState } from 'react';
import { STABILITY_RUNS, StabilityTable, computeStats, runSequentially, type DimensionStats } from './Stability';
import { getLevel, loadHistory as fetchHistory, postJson, type HistoryItem } from './api';
import { initialTopic, TopicPicker, type PickedTopic } from './TopicPicker';
import { cardsMessage } from './cards';
import { useIsAdmin } from './adminFlag';
import { clearDraft, draftKey, latestDraft, loadDraft, saveDraft } from './writingDraft';
import { useT } from './i18n';
import { stabilityMsg } from './locales/stability';
import { writingMsg } from './locales/writing';
import { Corrections, Feedback, GuestNote, HistoryList, ScoreBar, type CorrectionItem, type ScoreWithReasoning } from './ui';

// Mavzular endi ilovaning tayyor bankida (content/topics.ts) — TopicPicker orqali.

const MIN_CHARS = 20;
const MAX_CHARS = 5000;
/** Qoralama yozish to'xtagandan shuncha ms keyin saqlanadi. */
const DRAFT_DEBOUNCE_MS = 700;

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
  const t = useT(writingMsg);
  const ts = useT(stabilityMsg);
  const isAdmin = useIsAdmin();
  const [status, setStatus] = useState<Status>('idle');
  const [message, setMessage] = useState('');
  // Oldingi (yuborilmagan) qoralama bo'lsa — mavzu ham, matn ham tiklanadi.
  const [restored] = useState(() => latestDraft());
  const [picked, setPicked] = useState<PickedTopic>(() => restored?.topic ?? initialTopic('writing', getLevel()));
  const topic = picked.text;
  const [text, setText] = useState(() => restored?.text ?? '');
  const [draftNote, setDraftNote] = useState<'restored' | 'saved' | null>(restored ? 'restored' : null);
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

  // ---- Qoralama: yozish to'xtagach saqlanadi, sahifa yashirilganda — darhol ----
  const latest = useRef({ picked, text, lastSubmittedText });
  latest.current = { picked, text, lastSubmittedText };

  // Oxirgi saqlangan holat — o'zgarmagan matnni qayta yozmaslik uchun.
  const savedRef = useRef(restored ? `${draftKey(restored.topic)}\n${restored.text}` : '');

  useEffect(() => {
    // Baholangan matnni qayta saqlamaymiz (yuborilgach qoralama o'chirilgan).
    if (text === lastSubmittedText) return;
    const snapshot = `${draftKey(picked)}\n${text}`;
    if (snapshot === savedRef.current) return;
    const id = setTimeout(() => {
      saveDraft(picked, text);
      savedRef.current = snapshot;
      setDraftNote(text.trim() ? 'saved' : null);
    }, DRAFT_DEBOUNCE_MS);
    return () => clearTimeout(id);
  }, [picked, text, lastSubmittedText]);

  useEffect(() => {
    const flush = () => {
      const c = latest.current;
      if (document.visibilityState === 'hidden' && c.text !== c.lastSubmittedText) saveDraft(c.picked, c.text);
    };
    document.addEventListener('visibilitychange', flush);
    window.addEventListener('pagehide', flush);
    return () => {
      document.removeEventListener('visibilitychange', flush);
      window.removeEventListener('pagehide', flush);
    };
  }, []);

  /** Boshqa mavzu tanlandi: uning qoralamasi bo'lsa — tiklanadi, bo'lmasa matn qoladi. */
  function pickTopic(next: PickedTopic) {
    setPicked(next);
    const d = loadDraft(next);
    if (d) {
      savedRef.current = `${draftKey(next)}\n${d.text}`;
      setText(d.text);
      setDraftNote('restored');
    }
  }

  async function loadHistory() {
    setHistory(await fetchHistory('writing'));
  }

  // Bitta yuborish — oddiy oqim (save=true) ham, barqarorlik testi
  // (save=false) ham shu funksiyani ishlatadi.
  async function postEssay(essay: string, save: boolean): Promise<SubmitResponse> {
    const path = save ? '/api/writing/submit' : '/api/writing/submit?save=false';
    return postJson<SubmitResponse>(path, { topic, text: essay, level: getLevel() });
  }

  async function runStabilityTest() {
    if (!lastSubmittedText) return;
    const essay = lastSubmittedText;
    setIsTesting(true);
    setStability(null);
    setTestProgress(ts.progress(0, STABILITY_RUNS, 0));

    const { results, failed } = await runSequentially(
      STABILITY_RUNS,
      () => postEssay(essay, false),
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
        computeStats('taskAchievement', evals.map((e) => e.taskAchievement.score)),
        computeStats('coherence', evals.map((e) => e.coherenceCohesion.score)),
        computeStats('grammar', evals.map((e) => e.grammar.score)),
        computeStats('vocabulary', evals.map((e) => e.vocabulary.score)),
      ],
      failed,
    });
    setTestProgress('');
  }

  async function submitEssay() {
    if (text.trim().length < MIN_CHARS) {
      setStatus('error');
      setMessage(t.tooShort(MIN_CHARS));
      return;
    }

    setStatus('uploading');
    setMessage(t.evaluating);
    setResult(null);
    setStability(null);
    setTestProgress('');

    try {
      const data = await postEssay(text, true);
      setLastSubmittedText(text);
      clearDraft(picked);
      setDraftNote(null);
      setResult(data);
      setStatus('done');
      setMessage(cardsMessage(data.newCards));
      if (data.newCards > 0) onCardsAdded?.();
      loadHistory(); // yangi urinish ro'yxatga qo'shilishi uchun tarixni qayta yuklaymiz
    } catch (err) {
      setStatus('error');
      setMessage(err instanceof Error ? err.message : t.sendFailed);
    }
  }

  const isBusy = status === 'uploading' || isTesting;
  const words = countWords(text);

  return (
    <div className="practice-split">
      <div className="card">
        <span className="muted small">{t.topic}</span>
        <TopicPicker kind="writing" level={getLevel()} value={picked} onChange={pickTopic} disabled={isBusy} />

        <textarea
          className="input"
          value={text}
          onChange={(e) => setText(e.target.value)}
          disabled={isBusy}
          maxLength={MAX_CHARS}
          rows={8}
          placeholder={t.placeholder}
          aria-label={t.essayAria}
        />
        <div className="spread muted tiny" style={{ margin: '6px 0 12px' }}>
          <span>{t.words(words)}</span>
          <span>{t.chars(text.length, MAX_CHARS)}</span>
        </div>
        {draftNote && text.trim() && (
          <p className="muted tiny" style={{ margin: '-6px 0 10px' }} data-testid="draft-note">
            {draftNote === 'restored' ? t.draftRestored : t.draftSaved}
          </p>
        )}

        <button className="btn btn-primary block" onClick={submitEssay} disabled={isBusy}>
          {t.submit}
        </button>
        {message && (
          <p className={status === 'error' ? 'error small' : 'small'} style={{ marginTop: 10, marginBottom: 0 }}>
            {message}
          </p>
        )}
      </div>

      <div>
      {result && (
        <div className="card">
          <h3>{t.evaluation}</h3>
          <ScoreBar label={t.criteria.taskAchievement} data={result.evaluation.taskAchievement} />
          <ScoreBar label={t.criteria.coherence} data={result.evaluation.coherenceCohesion} />
          <ScoreBar label={t.criteria.grammar} data={result.evaluation.grammar} />
          <ScoreBar label={t.criteria.vocabulary} data={result.evaluation.vocabulary} />

          <Corrections items={result.evaluation.topCorrections} />
          <Feedback encouragement={result.evaluation.encouragement} nextFocus={result.evaluation.nextFocus} />

          {/* Barqarorlik testi — ichki tekshiruv vositasi (5× AI so'rovi), faqat admin uchun. */}
          {isAdmin && (
            <div style={{ borderTop: '1px solid var(--border)', marginTop: 14, paddingTop: 12 }}>
              <button className="btn btn-outline" onClick={runStabilityTest} disabled={isBusy || !lastSubmittedText}>
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
          const r = JSON.parse(item.responseData) as WritingEvaluationResult;
          return (
            <>
              <div className="small">{prompt.topic}</div>
              <div className="small muted">
                {t.short.taskAchievement} {r.taskAchievement.score} · {t.short.coherence} {r.coherenceCohesion.score} ·{' '}
                {t.short.grammar} {r.grammar.score} · {t.short.vocabulary} {r.vocabulary.score}
              </div>
            </>
          );
        }}
      />
      </div>
    </div>
  );
}
