// AI ustoz: foydalanuvchi mavzu yozadi ("Modal verbs") — AI qisqa dars tayyorlaydi:
// tushuntirish → mashqlar → o'z gaplari → mini-test. Javoblar serverda tekshiriladi; xatolar takrorlash kartalariga tushadi.
import { useEffect, useRef, useState, type FormEvent } from 'react';
import { apiJson, getLevel, postJson } from './api';
import { cardsMessage } from './cards';
import { common, useT } from './i18n';
import { tutorMsg } from './locales/tutor';
import { lessonTopic } from './groupLogic';
import { PageHeader } from './ui';
import { TutorSuggest } from './TutorSuggest';
import {
  answered,
  applyAnswer,
  firstOpen,
  savedLesson,
  saveLesson,
  stepOf,
  type AnswerReply,
  type Correction,
  type ExerciseView,
  type LessonView,
  type Step,
  type WritingView,
} from './tutorLogic';

interface Config {
  enabled: boolean;
  topics: string[];
}

interface Result {
  practiceCorrect: number;
  practiceTotal: number;
  testCorrect: number;
  testTotal: number;
  mastery: string;
  mistakes: Correction[];
  wroteOwn: boolean;
}

const LEVELS = ['A2', 'B1', 'B2', 'C1'];

interface AssignedTask {
  id: string;
  groupName: string;
  kind: string;
  instructions: string;
  status: { done: boolean };
}

export function TutorPage({
  loggedIn,
  onLogin,
  onCardsAdded,
  go,
  assignmentId,
  presetTopic,
}: {
  /** Ustoz vazifasidan ochilgan bo'lsa — vazifa identifikatori (mavzu vazifadan olinadi). */
  assignmentId?: string;
  /** Tavsiyadan ochilgan bo'lsa — oldindan yozilgan mavzu. */
  presetTopic?: string;
  loggedIn: boolean;
  onLogin: () => void;
  onCardsAdded?: () => void;
  go: (route: string) => void;
}) {
  const t = useT(tutorMsg);
  const cm = useT(common);
  const [config, setConfig] = useState<Config | null>(null);
  const [lessonId, setLessonId] = useState('');
  const [lesson, setLesson] = useState<LessonView | null>(null);
  const [step, setStep] = useState<Step>('explain');
  const [explainIdx, setExplainIdx] = useState(0);
  const [cursor, setCursor] = useState(0);
  const [topic, setTopic] = useState(presetTopic ?? '');
  const [level, setLevel] = useState(getLevel());
  const [resume, setResume] = useState<{ id: string; topic: string } | null>(null);
  // undefined — yuklanmoqda, null — topilmadi.
  const [task, setTask] = useState<AssignedTask | null | undefined>(assignmentId ? undefined : null);
  const [busy, setBusy] = useState<'' | 'start' | 'write' | 'finish'>('');
  const [error, setError] = useState('');
  const [text, setText] = useState('');
  const [result, setResult] = useState<{ result: Result; writing: WritingView | null; newCards: number } | null>(null);
  const alive = useRef(true);

  useEffect(() => {
    alive.current = true;
    return () => {
      alive.current = false;
    };
  }, []);

  useEffect(() => {
    apiJson<Config>('/api/tutor/config')
      .then((c) => alive.current && setConfig(c))
      .catch(() => alive.current && setConfig({ enabled: true, topics: [] }));
  }, []);

  // Ustoz vazifasi: mavzu va izoh vazifalar ro'yxatidan olinadi (server mavzuni baribir o'zi aniqlaydi).
  useEffect(() => {
    if (!assignmentId || !loggedIn) return;
    apiJson<{ assignments: AssignedTask[] }>('/api/student/assignments')
      .then((d) => alive.current && setTask(d.assignments.find((a) => a.id === assignmentId && lessonTopic(a.kind)) ?? null))
      .catch(() => alive.current && setTask(null));
  }, [assignmentId, loggedIn]);

  // Oxirgi dars bo'lsa — "Davom ettirish".
  useEffect(() => {
    const id = savedLesson();
    if (!loggedIn || !id) return;
    apiJson<{ lesson: LessonView }>(`/api/tutor/${id}`)
      .then((d) => alive.current && setResume({ id, topic: d.lesson.topic }))
      .catch(() => saveLesson(null));
  }, [loggedIn]);

  function open(id: string, view: LessonView) {
    setLessonId(id);
    setLesson(view);
    setExplainIdx(0);
    const s = stepOf(view);
    setStep(s);
    setCursor(s === 'practice' ? firstOpen(view.practice) : s === 'test' && view.test ? firstOpen(view.test) : 0);
    setText('');
    setResult(null);
    saveLesson(id);
  }

  async function resumeLesson() {
    if (!resume) return;
    setError('');
    try {
      const d = await apiJson<{ lessonId: string; lesson: LessonView }>(`/api/tutor/${resume.id}`);
      if (alive.current) open(d.lessonId, d.lesson);
    } catch (e) {
      saveLesson(null);
      setResume(null);
      setError(e instanceof Error ? e.message : cm.error);
    }
  }

  async function start(e?: FormEvent, forTopic?: string) {
    e?.preventDefault();
    const value = (forTopic ?? (task ? (lessonTopic(task.kind) ?? '') : topic)).trim();
    if (value.length < 2 || busy) return;
    setBusy('start');
    setError('');
    try {
      const fromTask = !forTopic && task ? task.id : undefined;
      const d = await postJson<{ lessonId: string; lesson: LessonView }>('/api/tutor/start', fromTask ? { assignmentId: fromTask, level } : { topic: value, level });
      if (alive.current) open(d.lessonId, d.lesson);
    } catch (err) {
      if (alive.current) setError(err instanceof Error ? err.message : cm.error);
    } finally {
      if (alive.current) setBusy('');
    }
  }

  function reset() {
    saveLesson(null);
    setLesson(null);
    setLessonId('');
    setResult(null);
    setResume(null);
    setError('');
  }

  async function answer(section: 'practice' | 'test', index: number, option: number) {
    if (!lesson) return;
    setError('');
    try {
      const r = await postJson<AnswerReply>(`/api/tutor/${lessonId}/answer`, { section, index, option });
      if (!alive.current) return;
      setLesson((l) => {
        if (!l) return l;
        return section === 'practice'
          ? { ...l, practice: applyAnswer(l.practice, index, option, r), practiceDone: r.practiceDone }
          : { ...l, test: applyAnswer(l.test ?? [], index, option, r), testDone: r.testDone };
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : cm.error);
    }
  }

  // Mashqlar tugagach yozma topshiriq uchun test va topshiriq matnini serverdan olamiz.
  async function afterPractice() {
    setError('');
    try {
      const d = await apiJson<{ lesson: LessonView }>(`/api/tutor/${lessonId}`);
      if (!alive.current) return;
      setLesson(d.lesson);
      setStep('write');
    } catch (err) {
      setError(err instanceof Error ? err.message : cm.error);
    }
  }

  async function submitWriting(e: FormEvent) {
    e.preventDefault();
    const v = text.trim();
    if (v.length < 3 || busy) return;
    setBusy('write');
    setError('');
    try {
      const d = await postJson<{ writing: WritingView }>(`/api/tutor/${lessonId}/write`, { text: v });
      if (alive.current) setLesson((l) => (l ? { ...l, writing: d.writing } : l));
    } catch (err) {
      if (alive.current) setError(err instanceof Error ? err.message : cm.error);
    } finally {
      if (alive.current) setBusy('');
    }
  }

  function toTest() {
    setStep('test');
    setCursor(lesson?.test ? firstOpen(lesson.test) : 0);
  }

  async function finish() {
    if (busy) return;
    setBusy('finish');
    setError('');
    try {
      const d = await postJson<{ result: Result; writing: WritingView | null; newCards: number }>(`/api/tutor/${lessonId}/finish`, {});
      if (!alive.current) return;
      setResult(d);
      saveLesson(null);
      if (d.newCards > 0) onCardsAdded?.();
    } catch (err) {
      if (alive.current) setError(err instanceof Error ? err.message : cm.error);
    } finally {
      if (alive.current) setBusy('');
    }
  }

  const header = <PageHeader title={t.title} subtitle={t.subtitle} onBack={() => go('practice')} backLabel={t.back} />;

  if (!loggedIn) {
    return (
      <>
        {header}
        <div className="card center">
          <p>{t.loginText}</p>
          <button className="btn btn-primary" onClick={onLogin}>{t.login}</button>
        </div>
      </>
    );
  }
  if (config && !config.enabled) {
    return (
      <>
        {header}
        <div className="card center"><p>{t.disabled}</p></div>
      </>
    );
  }

  // ---- 1. Mavzu tanlash ----
  if (!lesson) {
    return (
      <>
        {header}
        {task && (
          <form className="card stack" onSubmit={(e) => void start(e)} data-testid="tutor-assignment">
            <div className="muted tiny">{t.assignmentTitle} · {t.assignmentFrom(task.groupName)}</div>
            <h3 style={{ margin: 0 }} data-testid="tutor-assignment-topic">{lessonTopic(task.kind)}</h3>
            {task.instructions && <p style={{ margin: 0 }}>{task.instructions}</p>}
            {task.status.done && <p className="muted small" style={{ margin: 0 }}>{t.assignmentDone}</p>}
            <label className="small" htmlFor="tutor-level-a">{t.level}</label>
            <select id="tutor-level-a" className="input" value={level} onChange={(e) => setLevel(e.target.value)} style={{ maxWidth: 120 }}>
              {LEVELS.map((l) => <option key={l} value={l}>{l}</option>)}
            </select>
            {error && <p className="error small" role="alert">{error}</p>}
            <button className="btn btn-primary" type="submit" disabled={busy === 'start'} data-testid="tutor-start-assigned">
              {busy === 'start' ? t.starting : t.start}
            </button>
          </form>
        )}
        {task === null && assignmentId && <p className="muted small">{t.assignmentMissing}</p>}
        {task === undefined && <p className="muted small">…</p>}
        {!task && task !== undefined && !presetTopic && <TutorSuggest loggedIn={loggedIn} go={go} limit={3} />}
        {!task && task !== undefined && (<form className="card stack" onSubmit={(e) => void start(e)} data-testid="tutor-topic-form">
          {resume && (
            <button type="button" className="btn btn-outline" onClick={() => void resumeLesson()} data-testid="tutor-resume">
              {t.resume(resume.topic)}
            </button>
          )}
          <label htmlFor="tutor-topic"><strong>{t.topicLabel}</strong></label>
          <input
            id="tutor-topic"
            className="input"
            value={topic}
            maxLength={80}
            placeholder={t.topicPlaceholder}
            onChange={(e) => setTopic(e.target.value)}
            data-testid="tutor-topic"
          />
          <div className="muted tiny">{t.topicHint}</div>
          {(config?.topics.length ?? 0) > 0 && (
            <>
              <div className="muted small">{t.orPick}</div>
              <div className="chips-row">
                {config!.topics.map((s) => (
                  <button key={s} type="button" className="chip-btn" onClick={() => setTopic(s)} data-testid="tutor-suggest">{s}</button>
                ))}
              </div>
            </>
          )}
          <label className="small" htmlFor="tutor-level">{t.level}</label>
          <select id="tutor-level" className="input" value={level} onChange={(e) => setLevel(e.target.value)} style={{ maxWidth: 120 }}>
            {LEVELS.map((l) => <option key={l} value={l}>{l}</option>)}
          </select>
          {error && <p className="error small" role="alert">{error}</p>}
          <button className="btn btn-primary" type="submit" disabled={busy === 'start' || topic.trim().length < 2} data-testid="tutor-start">
            {busy === 'start' ? t.starting : t.start}
          </button>
        </form>)}
      </>
    );
  }

  const lessonHeader = <PageHeader title={lesson.topic} subtitle={t.title} onBack={reset} backLabel={t.newTopic} />;

  // ---- 6. Natija ----
  if (result) {
    const r = result.result;
    return (
      <>
        {lessonHeader}
        <div className="card stack" data-testid="tutor-result">
          <h3 style={{ margin: 0 }}>{t.resultTitle}</h3>
          <div className="spread">
            <span>{t.practiceScore}</span><strong>{r.practiceCorrect}/{r.practiceTotal}</strong>
          </div>
          <div className="spread">
            <span>{t.testScore}</span><strong data-testid="tutor-test-score">{r.testCorrect}/{r.testTotal}</strong>
          </div>
          <p className="success" style={{ margin: 0 }} data-testid="tutor-mastery">{t.mastery[r.mastery] ?? ''}</p>
          <div>
            <strong className="small">{t.mistakes}</strong>
            {r.mistakes.length === 0 ? (
              <p className="muted small" style={{ margin: '4px 0 0' }}>{t.noMistakes}</p>
            ) : (
              <ul className="small" style={{ margin: '4px 0 0', paddingLeft: 18 }}>
                {r.mistakes.map((m, i) => (
                  <li key={i}><s>{m.original}</s> → <strong>{m.corrected}</strong>{m.explanation && <div className="muted tiny">{m.explanation}</div>}</li>
                ))}
              </ul>
            )}
          </div>
          {result.newCards > 0 && <p className="muted small" style={{ margin: 0 }}>{cardsMessage(result.newCards)}</p>}
          <div className="rate-row">
            {result.newCards > 0 && <button className="btn btn-primary" onClick={() => go('review')}>{t.review}</button>}
            {r.mastery === 'weak' && <button className="btn btn-outline" onClick={() => void start(undefined, lesson.topic)} disabled={busy === 'start'}>{t.again}</button>}
            <button className="btn btn-outline" onClick={reset}>{t.newTopic}</button>
          </div>
        </div>
      </>
    );
  }

  const askBox = <AskBox lessonId={lessonId} asksLeft={lesson.asksLeft} onAsked={(left) => setLesson((l) => (l ? { ...l, asksLeft: left } : l))} />;

  // ---- 2. Tushuntirish ----
  if (step === 'explain') {
    const sec = lesson.explain[explainIdx];
    const last = explainIdx >= lesson.explain.length - 1;
    return (
      <>
        {lessonHeader}
        <div className="card stack" data-testid="tutor-explain">
          <div className="muted tiny">{t.explainStep(explainIdx + 1, lesson.explain.length)}</div>
          <h3 style={{ margin: 0 }}>{sec.title}</h3>
          <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{sec.body}</p>
          <button className="btn btn-primary" onClick={() => (last ? (setStep('practice'), setCursor(firstOpen(lesson.practice))) : setExplainIdx(explainIdx + 1))} data-testid="tutor-next">
            {last ? t.toPractice : t.next}
          </button>
        </div>
        {askBox}
      </>
    );
  }

  // ---- 3 va 5. Mashq / mini-test ----
  if (step === 'practice' || step === 'test') {
    const list: ExerciseView[] = step === 'test' ? (lesson.test ?? []) : lesson.practice;
    const q = list[cursor];
    const isDone = answered(list) === list.length;
    const label = step === 'test' ? t.testStep(cursor + 1, list.length) : t.practiceStep(cursor + 1, list.length);
    if (!q) return null;
    return (
      <>
        {lessonHeader}
        <div className="card stack" data-testid={`tutor-${step}`}>
          <div className="muted tiny">{label}</div>
          <div><strong data-testid="tutor-prompt">{q.prompt}</strong></div>
          <div role="group" aria-label={label}>
            {q.options.map((o, oi) => {
              const state = q.picked === null ? '' : oi === q.correctIndex ? 'correct' : oi === q.picked ? 'wrong' : '';
              return (
                <button
                  key={oi}
                  type="button"
                  className={`option tutor-opt ${state}`}
                  disabled={q.picked !== null}
                  onClick={() => void answer(step, cursor, oi)}
                  data-testid="tutor-option"
                >
                  {o}
                </button>
              );
            })}
          </div>
          {q.picked !== null && (
            <div className="small" role="status" data-testid="tutor-feedback">
              <strong className={q.correct ? 'success' : 'error'}>{q.correct ? t.correct : t.wrong}</strong>{' '}
              {!q.correct && q.correctIndex !== null && <>{t.rightAnswer} <strong>{q.options[q.correctIndex]}</strong>. </>}
              <span className="muted">{q.explanation}</span>
            </div>
          )}
          {error && <p className="error small" role="alert">{error}</p>}
          {q.picked !== null && cursor < list.length - 1 && (
            <button className="btn btn-primary" onClick={() => setCursor(cursor + 1)} data-testid="tutor-next">{t.nextQ}</button>
          )}
          {isDone && cursor === list.length - 1 && step === 'practice' && (
            <button className="btn btn-primary" onClick={() => void afterPractice()} data-testid="tutor-after-practice">{t.practiceDone} {t.next}</button>
          )}
          {isDone && cursor === list.length - 1 && step === 'test' && (
            <button className="btn btn-primary" onClick={() => void finish()} disabled={busy === 'finish'} data-testid="tutor-finish">
              {busy === 'finish' ? t.finishing : t.finish}
            </button>
          )}
        </div>
        {askBox}
      </>
    );
  }

  // ---- 4. O'z gaplari ----
  const w = lesson.writing;
  return (
    <>
      {lessonHeader}
      <div className="card stack" data-testid="tutor-write">
        <h3 style={{ margin: 0 }}>{t.writeTitle}</h3>
        <p style={{ margin: 0 }}><strong>{lesson.writingTask}</strong></p>
        <div className="muted tiny">{t.writeHint}</div>
        {!w ? (
          <form className="stack" onSubmit={(e) => void submitWriting(e)}>
            <textarea className="input" rows={4} maxLength={800} value={text} placeholder={t.writePlaceholder} onChange={(e) => setText(e.target.value)} data-testid="tutor-write-text" />
            {error && <p className="error small" role="alert">{error}</p>}
            <div className="rate-row">
              <button className="btn btn-primary" type="submit" disabled={busy === 'write' || text.trim().length < 3} data-testid="tutor-write-send">
                {busy === 'write' ? t.checking : t.check}
              </button>
              <button className="btn btn-outline" type="button" onClick={toTest} disabled={busy === 'write'} data-testid="tutor-skip-write">{t.skipWrite}</button>
            </div>
          </form>
        ) : (
          <div className="stack" data-testid="tutor-write-feedback">
            <p style={{ margin: 0 }}>{w.feedback}</p>
            {w.corrections.length > 0 && (
              <div>
                <strong className="small">{t.corrections}</strong>
                <ul className="small" style={{ margin: '4px 0 0', paddingLeft: 18 }}>
                  {w.corrections.map((c, i) => (
                    <li key={i}><s>{c.original}</s> → <strong>{c.corrected}</strong>{c.explanation && <div className="muted tiny">{c.explanation}</div>}</li>
                  ))}
                </ul>
              </div>
            )}
            {w.improved && <p className="small" style={{ margin: 0 }}><strong>{t.improved}:</strong> {w.improved}</p>}
            <button className="btn btn-primary" onClick={toTest} data-testid="tutor-to-test">{t.toTest}</button>
          </div>
        )}
      </div>
      {askBox}
    </>
  );
}

/** "Tushunmadingizmi?" — mavzu bo'yicha savol (darsga ko'pi bilan 5 ta). */
function AskBox({ lessonId, asksLeft, onAsked }: { lessonId: string; asksLeft: number; onAsked: (left: number) => void }) {
  const t = useT(tutorMsg);
  const cm = useT(common);
  const [open, setOpen] = useState(false);
  const [q, setQ] = useState('');
  const [answers, setAnswers] = useState<{ q: string; a: string }[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function send(e: FormEvent) {
    e.preventDefault();
    const question = q.trim();
    if (question.length < 2 || busy) return;
    setBusy(true);
    setError('');
    try {
      const d = await postJson<{ answer: string; asksLeft: number }>(`/api/tutor/${lessonId}/ask`, { question });
      setAnswers((a) => [...a, { q: question, a: d.answer }]);
      setQ('');
      onAsked(d.asksLeft);
    } catch (err) {
      setError(err instanceof Error ? err.message : cm.error);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="card stack" style={{ marginTop: 12 }} data-testid="tutor-ask">
      <button type="button" className="btn-link small" style={{ alignSelf: 'flex-start' }} onClick={() => setOpen((v) => !v)} aria-expanded={open} data-testid="tutor-ask-toggle">
        {t.askTitle}
      </button>
      {open && (
        <>
          {answers.map((x, i) => (
            <div key={i} className="small" data-testid="tutor-ask-answer">
              <div className="muted">{x.q}</div>
              <p style={{ margin: '2px 0 0', whiteSpace: 'pre-wrap' }}>{x.a}</p>
            </div>
          ))}
          {asksLeft > 0 ? (
            <form className="stack" onSubmit={(e) => void send(e)}>
              <input className="input" value={q} maxLength={300} placeholder={t.askPlaceholder} onChange={(e) => setQ(e.target.value)} data-testid="tutor-ask-input" />
              {error && <p className="error small" role="alert">{error}</p>}
              <div className="spread">
                <button className="btn btn-outline" type="submit" disabled={busy || q.trim().length < 2} data-testid="tutor-ask-send">{busy ? t.asking : t.ask}</button>
                <span className="muted tiny">{t.asksLeft(asksLeft)}</span>
              </div>
            </form>
          ) : (
            <p className="muted small" style={{ margin: 0 }}>{t.askNone}</p>
          )}
        </>
      )}
    </div>
  );
}
