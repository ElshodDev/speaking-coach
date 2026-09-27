// "Darslar": ilovaning o'z materiallari — grammatika darslari (qoida, misollar,
// tipik xatolar, mashq) va mavzuli lug'at. Hammasi brauzerda, Gemini'siz;
// foydalanuvchi xohlasa, AI qo'shimcha savollar tuzadi.
import { useMemo, useState } from 'react';
import { ApiError, getLevel, postJson } from './api';
import { GRAMMAR } from './content/grammar';
import { LEVELS, type GrammarLesson, type L3, type Level, type TopicWord } from './content/types';
import { VOCAB_TOPICS } from './content/vocabTopics';
import { useLang, useT } from './i18n';
import { learnMsg } from './locales/learn';
import { isSpeechSupported, speakAsync, stopSpeaking } from './speech';
import { PageHeader } from './ui';

type Lang = 'uz' | 'ru' | 'en';
const pick = (t: L3, lang: Lang) => t[lang] || t.en;

function Speak({ text }: { text: string }) {
  if (!isSpeechSupported()) return null;
  return (
    <button
      className="btn-link small"
      aria-label={`🔊 ${text}`}
      onClick={() => {
        stopSpeaking();
        void speakAsync(text);
      }}
    >
      🔊
    </button>
  );
}

function LevelChips({ value, onChange }: { value: Level | null; onChange: (l: Level | null) => void }) {
  const t = useT(learnMsg);
  return (
    <div className="chips" role="group" style={{ margin: '12px 0 0' }}>
      <button aria-pressed={value === null} onClick={() => onChange(null)}>{t.all}</button>
      {LEVELS.map((l) => (
        <button key={l} aria-pressed={value === l} onClick={() => onChange(l)}>{l}</button>
      ))}
    </div>
  );
}

// ---------------- Ro'yxat ----------------

export function LearnHub({ tab, go }: { tab: 'grammar' | 'words'; go: (route: string) => void }) {
  const t = useT(learnMsg);
  const { lang } = useLang();
  const userLevel = getLevel() as Level;
  const [level, setLevel] = useState<Level | null>(LEVELS.includes(userLevel) ? userLevel : null);
  const lessons = GRAMMAR.filter((g) => !level || g.level === level);

  return (
    <>
      <PageHeader title={t.title} subtitle={t.subtitle} onBack={() => go('practice')} />
      <div className="segmented" role="group" style={{ marginTop: 8 }}>
        <button aria-pressed={tab === 'grammar'} onClick={() => go('learn')}>{t.grammar}</button>
        <button aria-pressed={tab === 'words'} onClick={() => go('learn/words')}>{t.words}</button>
      </div>

      {tab === 'grammar' ? (
        <>
          <LevelChips value={level} onChange={setLevel} />
          <p className="muted small" style={{ margin: '8px 0 0' }}>{t.lessons(lessons.length)}</p>
          <div className="card-grid learn-grid" data-testid="grammar-list">
            {lessons.map((g) => (
              <button key={g.id} className="card learn-card" onClick={() => go(`learn/grammar/${g.id}`)}>
                <span className="badge">{g.level}</span>
                <strong>{pick(g.title, lang)}</strong>
                <span className="muted small">{pick(g.summary, lang)}</span>
              </button>
            ))}
          </div>
        </>
      ) : (
        <div className="tiles learn-topics" data-testid="topic-list">
          {VOCAB_TOPICS.map((v) => (
            <button key={v.id} className="tile" onClick={() => go(`learn/words/${v.id}`)}>
              <span className="emoji" aria-hidden>{v.emoji}</span>
              <strong>{pick(v.title, lang)}</strong>
              <span className="muted">{t.wordsCount(v.words.length)}</span>
            </button>
          ))}
        </div>
      )}
    </>
  );
}

// ---------------- Grammatika darsi ----------------

interface QuizItem {
  prompt: string;
  options: string[];
  answer: number;
  why: string;
}

function Quiz({ items, onDone }: { items: QuizItem[]; onDone?: (score: number) => void }) {
  const t = useT(learnMsg);
  const [i, setI] = useState(0);
  const [chosen, setChosen] = useState<number | null>(null);
  const [score, setScore] = useState(0);
  const finished = i >= items.length;

  if (finished) {
    return (
      <div className="card soft center" data-testid="quiz-score">
        <div style={{ fontSize: '1.6rem', fontWeight: 800 }}>{t.score(score, items.length)}</div>
        <button
          className="btn btn-outline"
          style={{ marginTop: 8 }}
          onClick={() => {
            setI(0);
            setChosen(null);
            setScore(0);
          }}
        >
          {t.again}
        </button>
      </div>
    );
  }
  const q = items[i];
  return (
    <div data-testid="grammar-quiz">
      <p className="muted tiny" style={{ margin: 0 }}>{t.questionOf(i + 1, items.length)}</p>
      <p className="learn-prompt">{q.prompt}</p>
      <div className="stack" style={{ gap: 6 }}>
        {q.options.map((o, k) => {
          const cls = chosen === null ? '' : k === q.answer ? 'correct' : k === chosen ? 'wrong' : '';
          return (
            <button
              key={o}
              className={`option learn-option ${cls}`}
              disabled={chosen !== null}
              onClick={() => {
                setChosen(k);
                if (k === q.answer) setScore((s) => s + 1);
              }}
            >
              <strong>{String.fromCharCode(65 + k)}.</strong> {o}
            </button>
          );
        })}
      </div>
      {chosen !== null && (
        <div style={{ marginTop: 10 }}>
          <p className={`small ${chosen === q.answer ? 'success' : 'error'}`} style={{ margin: '0 0 8px' }} role="status">
            {chosen === q.answer ? t.correct : t.wrong} {q.why}
          </p>
          <button
            className="btn btn-primary"
            onClick={() => {
              const nextI = i + 1;
              setI(nextI);
              setChosen(null);
              if (nextI >= items.length) onDone?.(score);
            }}
          >
            {t.next}
          </button>
        </div>
      )}
    </div>
  );
}

export function GrammarLessonPage({ id, go }: { id: string; go: (route: string) => void }) {
  const t = useT(learnMsg);
  const { lang } = useLang();
  const lesson: GrammarLesson | undefined = GRAMMAR.find((g) => g.id === id);
  const [aiQuiz, setAiQuiz] = useState<QuizItem[] | null>(null);
  const [aiBusy, setAiBusy] = useState(false);
  const [aiError, setAiError] = useState('');
  const [quizKey, setQuizKey] = useState(0);

  const builtIn = useMemo<QuizItem[]>(
    () => (lesson ? lesson.quiz.map((q) => ({ prompt: q.prompt, options: q.options, answer: q.answer, why: pick(q.why, lang) })) : []),
    [lesson, lang],
  );

  if (!lesson) {
    return (
      <>
        <PageHeader title={t.title} onBack={() => go('learn')} backLabel={t.back} />
        <p className="error">{t.notFound}</p>
      </>
    );
  }

  const idx = GRAMMAR.indexOf(lesson);
  const next = GRAMMAR[idx + 1];

  async function moreFromAi() {
    setAiBusy(true);
    setAiError('');
    try {
      const r = await postJson<{ questions: QuizItem[] }>('/api/grammar/quiz', { topic: lesson!.title.en, level: lesson!.level });
      setAiQuiz(r.questions);
      setQuizKey((k) => k + 1);
    } catch (err) {
      setAiError(err instanceof ApiError || err instanceof Error ? err.message : '');
    } finally {
      setAiBusy(false);
    }
  }

  return (
    <>
      <PageHeader title={pick(lesson.title, lang)} subtitle={pick(lesson.summary, lang)} onBack={() => go('learn')} backLabel={t.back} />
      <span className="badge">{lesson.level}</span>

      <div className="learn-lesson">
        <div>
          {lesson.rules.map((r, k) => (
            <div className="card" key={k} data-testid="grammar-rule">
              <p className="muted tiny" style={{ margin: 0 }}>{t.rules} {k + 1}</p>
              <p style={{ margin: '4px 0 10px' }}>{pick(r.text, lang)}</p>
              <ul className="learn-examples">
                {r.examples.map((e) => (
                  <li key={e}>
                    <span>{e}</span> <Speak text={e} />
                  </li>
                ))}
              </ul>
            </div>
          ))}

          <div className="card">
            <h3 style={{ marginTop: 0 }}>{t.mistakes}</h3>
            <ul className="learn-mistakes">
              {lesson.mistakes.map((m) => (
                <li key={m.wrong}>
                  <div>
                    <s className="error">{m.wrong}</s> → <strong className="success">{m.right}</strong>
                  </div>
                  <div className="muted small">{pick(m.note, lang)}</div>
                </li>
              ))}
            </ul>
          </div>
        </div>

        <aside>
          <div className="card">
            <div className="spread">
              <h3 style={{ margin: 0 }}>{t.practice}</h3>
              <span className="badge">{aiQuiz ? t.aiQuiz : t.builtInQuiz}</span>
            </div>
            <div style={{ marginTop: 10 }}>
              <Quiz key={quizKey} items={aiQuiz ?? builtIn} />
            </div>
            <div style={{ borderTop: '1px solid var(--border)', marginTop: 14, paddingTop: 10 }}>
              <button className="btn-link small" onClick={moreFromAi} disabled={aiBusy} data-testid="grammar-ai">
                {aiBusy ? t.aiBusy : t.aiMore}
              </button>
              <p className="muted tiny" style={{ margin: '4px 0 0' }}>{t.aiNote}</p>
              {aiError && <p className="error small" role="alert">{aiError}</p>}
            </div>
          </div>
          {next && (
            <button className="card cta" style={{ width: '100%', font: 'inherit', color: 'inherit', textAlign: 'left', cursor: 'pointer' }} onClick={() => go(`learn/grammar/${next.id}`)}>
              <div>
                <strong>{t.nextLesson}</strong>
                <div className="muted small">{pick(next.title, lang)} · {next.level}</div>
              </div>
              <span aria-hidden>→</span>
            </button>
          )}
        </aside>
      </div>
    </>
  );
}

// ---------------- Mavzuli lug'at ----------------

const meaning = (w: TopicWord, lang: Lang) => (lang === 'ru' ? w.ru : lang === 'uz' ? w.uz : w.definition);

function shuffle<T>(xs: T[]): T[] {
  const a = [...xs];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

export function VocabTopicPage({ id, go, loggedIn, onLogin }: { id: string; go: (route: string) => void; loggedIn: boolean; onLogin: () => void }) {
  const t = useT(learnMsg);
  const { lang } = useLang();
  const topic = VOCAB_TOPICS.find((v) => v.id === id);
  const [added, setAdded] = useState<Set<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState('');
  const [quiz, setQuiz] = useState<QuizItem[] | null>(null);

  if (!topic) {
    return (
      <>
        <PageHeader title={t.title} onBack={() => go('learn/words')} backLabel={t.back} />
        <p className="error">{t.notFound}</p>
      </>
    );
  }

  async function add(w: TopicWord) {
    try {
      await postJson('/api/vocab', { word: w.word, translation: lang === 'ru' ? w.ru : w.uz, partOfSpeech: w.pos, definitionEn: w.definition, example: w.example });
    } catch (err) {
      // Allaqachon lug'atda bo'lsa (409) — qo'shilgan deb hisoblaymiz.
      if (!(err instanceof ApiError && err.status === 409)) throw err;
    }
    setAdded((s) => new Set(s).add(w.word));
  }

  async function addAll() {
    setBusy(true);
    setNote('');
    try {
      const todo = topic!.words.filter((w) => !added.has(w.word));
      for (const w of todo) await add(w);
      setNote(t.addedAll(todo.length));
    } catch (err) {
      setNote(err instanceof Error ? err.message : '');
    } finally {
      setBusy(false);
    }
  }

  function startQuiz() {
    const words = shuffle(topic!.words).slice(0, 8);
    setQuiz(
      words.map((w) => {
        const wrong = shuffle(topic!.words.filter((x) => x.word !== w.word)).slice(0, 3);
        const options = shuffle([w, ...wrong]);
        return { prompt: `${w.word} — ${t.quizPrompt}`, options: options.map((o) => meaning(o, lang)), answer: options.indexOf(w), why: `${w.word}: ${meaning(w, lang)}. ${w.example}` };
      }),
    );
  }

  return (
    <>
      <PageHeader title={`${topic.emoji} ${pick(topic.title, lang)}`} subtitle={t.wordsCount(topic.words.length)} onBack={() => go('learn/words')} backLabel={t.back} />

      <div className="row" style={{ marginTop: 8 }}>
        <button className="btn btn-primary" onClick={startQuiz} data-testid="words-quiz">{t.quiz}</button>
        {loggedIn ? (
          <button className="btn btn-outline" onClick={addAll} disabled={busy} data-testid="add-all">{t.addAll}</button>
        ) : (
          <button className="btn-link small" onClick={onLogin}>{t.loginToAdd}</button>
        )}
      </div>
      {note && <p className="small" role="status">{note}</p>}

      {quiz ? (
        <div className="card">
          <Quiz items={quiz} />
          <button className="btn-link small" style={{ marginTop: 10 }} onClick={() => setQuiz(null)}>{t.closeQuiz}</button>
        </div>
      ) : (
        <div className="card-grid" data-testid="topic-words">
          {topic.words.map((w) => (
            <div key={w.word} className="card learn-word">
              <div className="spread">
                <span>
                  <strong style={{ fontSize: '1.1rem' }}>{w.word}</strong> <span className="muted small">{w.pos}</span> <Speak text={w.word} />
                </span>
                <span className="badge">{w.level}</span>
              </div>
              <div style={{ fontWeight: 600 }}>{meaning(w, lang)}</div>
              {lang !== 'en' && <div className="muted small">{w.definition}</div>}
              <div className="quote small" style={{ margin: '6px 0' }}>{w.example}</div>
              {loggedIn && (
                <button className="btn-link small" disabled={added.has(w.word)} onClick={() => void add(w).catch((e) => setNote(e instanceof Error ? e.message : ''))}>
                  {added.has(w.word) ? t.added : t.add}
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </>
  );
}
