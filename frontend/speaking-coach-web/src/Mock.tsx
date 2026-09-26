import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { localeOf, useLang, useT } from './i18n';
import { mockMsg } from './locales/mock';
import { mockObjMsg } from './locales/mockObjective';
import { cefrMsg } from './locales/cefr';
import type { ClientGroup } from './ObjectiveQuestions';
import { bandKey, cefrLevel, formatBand } from './mockLogic';
import { Corrections, PageHeader, type CorrectionItem } from './ui';

interface MockStatus {
  perDay: number;
  remaining: number | null;
  retryAtUtc: string | null;
}

interface HistoryRow {
  id: string;
  createdAtUtc: string;
  exam: string;
  module: string;
  variant: string | null;
  setId: string;
  overall: number | null;
  sessionId?: string | null;
}

interface Band {
  band: number;
  reasoning: string;
}

interface SpeakingResult {
  module: 'speaking';
  overall: number;
  fluencyCoherence: Band;
  lexicalResource: Band;
  grammaticalRange: Band;
  pronunciation: Band;
  answers: { index: number; part: number; question: string; transcript: string; seconds: number }[];
  topCorrections: CorrectionItem[];
  strengths: string;
  nextSteps: string[];
}

interface WritingTask {
  band: number;
  words: number;
  minWords: number;
  text: string;
  criteria: { task: Band; coherenceCohesion: Band; lexicalResource: Band; grammaticalRange: Band };
}

interface WritingResult {
  module: 'writing';
  variant: 'academic' | 'general';
  overall: number;
  secondsUsed: number;
  task1: WritingTask;
  task2: WritingTask;
  topCorrections: CorrectionItem[];
  strengths: string;
  nextSteps: string[];
}

interface ObjectiveReview {
  number: number;
  given: string;
  accepted: string[];
  correct: boolean;
  explanation: string;
}

interface ObjectiveResult {
  module: 'listening' | 'reading';
  variant: string;
  overall: number;
  score: number;
  total: number;
  secondsUsed: number;
  questions: ObjectiveReview[];
}

interface TestContent {
  variant?: string;
  passages?: { title: string; text: string; groups: (ClientGroup & { questions: { number: number; prompt: string; options: string[] | null }[] })[] }[];
  parts?: { part: number; context: string; script: { speaker: string; text: string }[]; groups: ClientGroup[] }[];
}

interface PartScore {
  score: number;
  reasoning: string;
}

interface CefrSpeakingResult {
  exam: 'cefr';
  module: 'speaking';
  overall: number;
  level: string;
  parts: { part: string; score: number; max: number; reasoning: string }[];
  answers: { index: number; part: string; question: string; transcript: string; seconds: number }[];
  topCorrections: CorrectionItem[];
  strengths: string;
  nextSteps: string[];
}

interface CefrWritingResult {
  exam: 'cefr';
  module: 'writing';
  overall: number;
  level: string;
  secondsUsed: number;
  tasks: {
    task: string;
    raw: number;
    points: number;
    maxPoints: number;
    words: number;
    minWords: number;
    maxWords: number;
    text: string;
    criteria: { task: PartScore; organisation: PartScore; vocabulary: PartScore; grammar: PartScore };
  }[];
  topCorrections: CorrectionItem[];
  strengths: string;
  nextSteps: string[];
}

type CefrResult = CefrSpeakingResult | CefrWritingResult;

type MockResult = SpeakingResult | WritingResult | ObjectiveResult;

/** CEFR natijasi: 0–75 ball, daraja, qism/vazifa ballari va maslahatlar. */
function CefrResultView({ r }: { r: CefrResult }) {
  const t = useT(mockMsg);
  const c = useT(cefrMsg);
  return (
    <>
      <div className="card" style={{ textAlign: 'center' }}>
        <div className="muted small">{c.score}</div>
        <div style={{ fontSize: '3rem', fontWeight: 800, lineHeight: 1.1 }} data-testid="mock-overall">{c.of75(r.overall)}</div>
        <div className="small"><strong>{c.level(r.level)}</strong></div>
        <div className="muted small" style={{ marginTop: 6 }}>
          CEFR {r.module === 'speaking' ? 'Speaking' : 'Writing'}
          {r.module === 'writing' ? ` · ${t.timeUsed(Math.round(r.secondsUsed / 60))}` : ''}
        </div>
      </div>

      {r.module === 'speaking' ? (
        <>
          <div className="card">
            <h3 style={{ marginBottom: 0 }}>{c.parts}</h3>
            {r.parts.map((p) => (
              <div key={p.part} style={{ padding: '10px 0', borderTop: '1px solid var(--border)' }}>
                <div className="spread">
                  <strong className="small">{c.partScore(p.part)}</strong>
                  <strong>{c.points(p.score, p.max)}</strong>
                </div>
                <div className="muted small" style={{ marginTop: 4 }}>{p.reasoning}</div>
              </div>
            ))}
          </div>
          <CefrAdvice r={r} />
          <div className="card">
            <h3>{t.yourAnswers}</h3>
            {r.answers.map((a) => (
              <details key={a.index} style={{ padding: '6px 0', borderTop: '1px solid var(--border)' }}>
                <summary className="small">
                  <strong>{t.part(a.part)}</strong> · {a.question} <span className="muted">({t.seconds(a.seconds)})</span>
                </summary>
                <p className="quote small">{a.transcript || t.noAnswer}</p>
              </details>
            ))}
          </div>
        </>
      ) : (
        <>
          {r.tasks.map((task) => (
            <div className="card" key={task.task}>
              <div className="spread">
                <h3 style={{ margin: 0 }}>{c.task(task.task)}</h3>
                <strong>{c.points(task.points, task.maxPoints)}</strong>
              </div>
              <div className={task.words < task.minWords || task.words > task.maxWords ? 'txt-low small' : 'muted small'}>
                {c.words(task.words, task.minWords, task.maxWords)}
              </div>
              {(['task', 'organisation', 'vocabulary', 'grammar'] as const).map((k) => (
                <div key={k} style={{ padding: '8px 0', borderTop: '1px solid var(--border)' }}>
                  <div className="spread">
                    <strong className="small">{c.criteria[k]}</strong>
                    <strong>{task.criteria[k].score}/5</strong>
                  </div>
                  <div className="muted small" style={{ marginTop: 4 }}>{task.criteria[k].reasoning}</div>
                </div>
              ))}
              <details style={{ marginTop: 8 }}>
                <summary className="small">{t.yourText}</summary>
                <p className="quote small" style={{ whiteSpace: 'pre-wrap' }}>{task.text || t.noAnswer}</p>
              </details>
            </div>
          ))}
          <CefrAdvice r={r} />
        </>
      )}
      <p className="muted tiny">{c.scaleNote}</p>
      <p className="muted tiny">{c.disclaimer}</p>
    </>
  );
}

function CefrAdvice({ r }: { r: CefrResult }) {
  const t = useT(mockMsg);
  return (
    <div className="card">
      <h3>{t.strengths}</h3>
      <p className="small">{r.strengths}</p>
      <h3>{t.nextSteps}</h3>
      <ol className="small" style={{ paddingLeft: 20 }}>
        {r.nextSteps.map((s, i) => <li key={i}>{s}</li>)}
      </ol>
      <Corrections items={r.topCorrections} />
      {r.topCorrections.length > 0 && <p className="muted tiny">{t.cardsNote}</p>}
    </div>
  );
}

/** Listening/Reading natijasi: ball, band, har savol tahlili va matn/skript. */
function ObjectiveResultView({ r, test }: { r: ObjectiveResult; test: TestContent | null }) {
  const t = useT(mockMsg);
  const to = useT(mockObjMsg);
  const [onlyWrong, setOnlyWrong] = useState(true);
  const prompts = new Map<number, string>();
  for (const g of [...(test?.passages ?? []).flatMap((p) => p.groups), ...(test?.parts ?? []).flatMap((p) => p.groups)]) {
    for (const q of g.questions) prompts.set(q.number, q.prompt);
  }
  const shown = r.questions.filter((q) => !onlyWrong || !q.correct);
  const title = r.module === 'listening' ? 'IELTS Listening' : `IELTS Reading · ${r.variant === 'general' ? t.general : t.academic}`;

  return (
    <>
      <Overall band={r.overall} extra={`${title} · ${to.score(r.score, r.total)}`} />
      <p className="muted tiny">{to.bandNote}</p>
      <div className="card">
        <div className="spread">
          <h3 style={{ margin: 0 }}>{to.reviewTitle}</h3>
          <div className="segmented" role="group">
            <button aria-pressed={onlyWrong} onClick={() => setOnlyWrong(true)}>{to.onlyWrong}</button>
            <button aria-pressed={!onlyWrong} onClick={() => setOnlyWrong(false)}>{to.all}</button>
          </div>
        </div>
        <ul style={{ listStyle: 'none', padding: 0, margin: '10px 0 0' }}>
          {shown.map((q) => (
            <li key={q.number} style={{ padding: '8px 0', borderTop: '1px solid var(--border)' }} className="small">
              <div>
                {q.correct ? '✅' : '❌'} <strong>{q.number}.</strong> {prompts.get(q.number) ?? ''}
              </div>
              <div>
                {to.your}: <span className={q.correct ? 'txt-great' : 'txt-low'}>{q.given || to.empty}</span>
                {!q.correct && (
                  <>
                    {' · '}
                    {to.correctAnswer}: <strong>{q.accepted.join(' / ')}</strong>
                  </>
                )}
              </div>
              <div className="muted tiny">{q.explanation}</div>
            </li>
          ))}
        </ul>
      </div>
      {test?.parts && (
        <div className="card">
          <details>
            <summary><strong>{to.script}</strong></summary>
            {test.parts.map((p) => (
              <div key={p.part}>
                <h4>{to.part(p.part)}</h4>
                {p.script.map((l, i) => (
                  <p key={i} className="small"><strong>{l.speaker}:</strong> {l.text}</p>
                ))}
              </div>
            ))}
          </details>
        </div>
      )}
      {test?.passages && (
        <div className="card">
          <details>
            <summary><strong>{to.texts}</strong></summary>
            {test.passages.map((p, i) => (
              <div key={i}>
                <h4>{to.passage(i + 1)}: {p.title}</h4>
                {p.text.split(/\n\s*\n/).map((para, j) => <p key={j} className="small">{para}</p>)}
              </div>
            ))}
          </details>
        </div>
      )}
    </>
  );
}

/** Mock imtihonlar markazi: turini tanlash, sutkalik limit, oldingi natijalar. */
export function MockHub({ loggedIn, go, onLogin }: { loggedIn: boolean; go: (route: string) => void; onLogin: () => void }) {
  const t = useT(mockMsg);
  const locale = localeOf(useLang().lang);
  const [status, setStatus] = useState<MockStatus | null>(null);
  const [history, setHistory] = useState<HistoryRow[]>([]);

  useEffect(() => {
    if (!loggedIn) return;
    apiJson<MockStatus>('/api/mock/status').then(setStatus).catch(() => undefined);
    apiJson<HistoryRow[]>('/api/mock/history').then(setHistory).catch(() => undefined);
  }, [loggedIn]);

  const to = useT(mockObjMsg);
  const tc = useT(cefrMsg);
  const blocked = !loggedIn || status?.remaining === 0;

  return (
    <>
      <PageHeader title={t.title} subtitle={t.subtitle} onBack={() => go('practice')} />

      {!loggedIn && (
        <div className="card cta">
          <div className="small">{t.loginNeeded}</div>
          <button className="btn btn-primary" onClick={onLogin}>{t.login}</button>
        </div>
      )}
      {status && status.remaining !== null && (
        <p className="small muted" role="status">
          {status.remaining > 0
            ? t.remaining(status.remaining, status.perDay)
            : t.noneLeft(status.retryAtUtc ? new Date(status.retryAtUtc).toLocaleString(locale) : '—')}
        </p>
      )}

      <div className="card stack" style={{ borderColor: 'var(--primary)' }}>
        <h3 style={{ margin: 0 }}>{to.fullTitle}</h3>
        <p className="muted small" style={{ margin: 0 }}>{to.fullText}</p>
        <button className="btn btn-primary" disabled={blocked} onClick={() => go('mock/full')}>
          {to.fullStart}
        </button>
      </div>

      <div className="card stack">
        <h3 style={{ margin: 0 }}>{to.listeningTitle}</h3>
        <p className="muted small" style={{ margin: 0 }}>{to.listeningText}</p>
        <button className="btn btn-primary" disabled={!loggedIn} onClick={() => go('mock/listening')}>
          {t.start}
        </button>
      </div>

      <div className="card stack">
        <h3 style={{ margin: 0 }}>{to.readingTitle}</h3>
        <p className="muted small" style={{ margin: 0 }}>{to.readingText}</p>
        <div className="row">
          <button className="btn btn-primary" disabled={!loggedIn} onClick={() => go('mock/reading-academic')}>
            {t.academic}
          </button>
          <button className="btn btn-outline" disabled={!loggedIn} onClick={() => go('mock/reading-general')}>
            {t.general}
          </button>
        </div>
      </div>

      <div className="card stack">
        <h3 style={{ margin: 0 }}>{t.speakingTitle}</h3>
        <p className="muted small" style={{ margin: 0 }}>{t.speakingText}</p>
        <button className="btn btn-primary" disabled={blocked} onClick={() => go('mock/speaking')}>
          {t.start}
        </button>
      </div>

      <div className="card stack">
        <h3 style={{ margin: 0 }}>{t.writingTitle}</h3>
        <p className="muted small" style={{ margin: 0 }}>{t.writingText}</p>
        <div className="row">
          <button className="btn btn-primary" disabled={blocked} onClick={() => go('mock/writing-academic')}>
            {t.academic}
            <span className="tiny" style={{ display: 'block', fontWeight: 400, opacity: 0.85 }}>{t.academicHint}</span>
          </button>
          <button className="btn btn-outline" disabled={blocked} onClick={() => go('mock/writing-general')}>
            {t.general}
            <span className="tiny" style={{ display: 'block', fontWeight: 400, opacity: 0.85 }}>{t.generalHint}</span>
          </button>
        </div>
      </div>

      <div className="card stack">
        <h3 style={{ margin: 0 }}>{tc.title}</h3>
        <p className="muted small" style={{ margin: 0 }}>{tc.text}</p>
        <div className="stack">
          <div>
            <button className="btn btn-primary" disabled={blocked} onClick={() => go('mock/cefr-speaking')}>{tc.speaking}</button>
            <div className="muted tiny" style={{ marginTop: 4 }}>{tc.speakingText}</div>
          </div>
          <div>
            <button className="btn btn-primary" disabled={blocked} onClick={() => go('mock/cefr-writing')}>{tc.writing}</button>
            <div className="muted tiny" style={{ marginTop: 4 }}>{tc.writingText}</div>
          </div>
        </div>
        <p className="muted tiny" style={{ margin: 0 }}>{tc.lrSoon}</p>
      </div>

      {loggedIn && (
        <div className="card">
          <h3>{t.history}</h3>
          {history.length === 0 ? (
            <p className="muted small">{t.historyEmpty}</p>
          ) : (
            <ul className="history" style={{ listStyle: 'none', padding: 0, margin: 0 }}>
              {history.map((h) => (
                <li key={h.id} className="spread" style={{ padding: '8px 0', borderTop: '1px solid var(--border)' }}>
                  <span className="small">
                    <strong>{h.exam === 'cefr' ? 'CEFR' : 'IELTS'} {t.moduleName[h.module] ?? h.module}</strong>
                    {h.variant ? ` · ${h.variant === 'general' ? t.general : t.academic}` : ''}
                    {h.sessionId && (
                      <button className="btn-link tiny" style={{ marginLeft: 6 }} onClick={() => go(`mock/session/${h.sessionId}`)}>
                        🏁 {to.fullTitle.replace(/^🏁\s*/, '')}
                      </button>
                    )}
                    <div className="muted tiny">{new Date(h.createdAtUtc).toLocaleString(locale)}</div>
                  </span>
                  <span className="row" style={{ alignItems: 'center', gap: 10 }}>
                    <strong style={{ fontSize: '1.2rem' }}>
                      {h.exam === 'cefr' ? (h.overall === null ? '—' : `${h.overall}/75 · ${cefrLevel(h.overall)}`) : formatBand(h.overall)}
                    </strong>
                    <button className="btn-link small" onClick={() => go(`mock/result/${h.id}`)}>{t.open}</button>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </>
  );
}

function BandRow({ label, data }: { label: string; data: Band }) {
  return (
    <div style={{ padding: '10px 0', borderTop: '1px solid var(--border)' }}>
      <div className="spread">
        <strong className="small">{label}</strong>
        <strong>{data.band}</strong>
      </div>
      <div className="muted small" style={{ marginTop: 4 }}>{data.reasoning}</div>
    </div>
  );
}

function Overall({ band, extra }: { band: number; extra?: string }) {
  const t = useT(mockMsg);
  return (
    <div className="card" style={{ textAlign: 'center' }}>
      <div className="muted small">{t.overall}</div>
      <div style={{ fontSize: '3rem', fontWeight: 800, lineHeight: 1.1 }} data-testid="mock-overall">{formatBand(band)}</div>
      <div className="small">{t.bandNames[bandKey(band)]}</div>
      {extra && <div className="muted small" style={{ marginTop: 6 }}>{extra}</div>}
    </div>
  );
}

function Advice({ result }: { result: SpeakingResult | WritingResult }) {
  const t = useT(mockMsg);
  return (
    <div className="card">
      <h3>{t.strengths}</h3>
      <p className="small">{result.strengths}</p>
      <h3>{t.nextSteps}</h3>
      <ol className="small" style={{ paddingLeft: 20 }}>
        {result.nextSteps.map((s, i) => <li key={i}>{s}</li>)}
      </ol>
      <Corrections items={result.topCorrections} />
      {result.topCorrections.length > 0 && <p className="muted tiny">{t.cardsNote}</p>}
    </div>
  );
}

/** Bitta mock natijasi (Speaking yoki Writing) — server hisoblagan band bilan. */
export function MockResultView({ id, go }: { id: string; go: (route: string) => void }) {
  const t = useT(mockMsg);
  const locale = localeOf(useLang().lang);
  const to = useT(mockObjMsg);
  const [data, setData] = useState<{ createdAtUtc: string; result: MockResult; test: TestContent | null; sessionId: string | null } | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    apiJson<{ createdAtUtc: string; result: MockResult; test: TestContent | null; sessionId: string | null }>(`/api/mock/${id}`)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
  }, [id]);

  const header = <PageHeader title={t.resultTitle} subtitle={data ? new Date(data.createdAtUtc).toLocaleString(locale) : undefined} onBack={() => go('mock')} backLabel={t.title} />;
  if (error) return <>{header}<p className="error" role="alert">{error}</p></>;
  if (!data) return <>{header}<p className="muted">…</p></>;

  if ((data.result as unknown as { exam?: string }).exam === 'cefr') {
    const cr = data.result as unknown as CefrResult;
    return (
      <>
        {header}
        <CefrResultView r={cr} />
        <div className="row">
          <button className="btn btn-primary" onClick={() => go(cr.module === 'speaking' ? 'mock/cefr-speaking' : 'mock/cefr-writing')}>{t.again}</button>
          <button className="btn btn-outline" onClick={() => go('mock')}>{t.backToMock}</button>
        </div>
      </>
    );
  }

  const r = data.result;
  const w = r as WritingResult;
  const againRoute =
    r.module === 'speaking' ? 'mock/speaking'
    : r.module === 'writing' ? `mock/writing-${r.variant}`
    : r.module === 'listening' ? 'mock/listening'
    : `mock/reading-${r.variant === 'general' ? 'general' : 'academic'}`;

  return (
    <>
      {header}
      {r.module === 'listening' || r.module === 'reading' ? (
        <ObjectiveResultView r={r} test={data.test} />
      ) : r.module === 'speaking' ? (
        <>
          <Overall band={r.overall} extra="IELTS Speaking" />
          <div className="card">
            <h3 style={{ marginBottom: 0 }}>{t.criteria}</h3>
            <BandRow label={t.speakingCriteria.fluencyCoherence} data={r.fluencyCoherence} />
            <BandRow label={t.speakingCriteria.lexicalResource} data={r.lexicalResource} />
            <BandRow label={t.speakingCriteria.grammaticalRange} data={r.grammaticalRange} />
            <BandRow label={t.speakingCriteria.pronunciation} data={r.pronunciation} />
          </div>
          <Advice result={r} />
          <div className="card">
            <h3>{t.yourAnswers}</h3>
            {r.answers.map((a) => (
              <details key={a.index} style={{ padding: '6px 0', borderTop: '1px solid var(--border)' }}>
                <summary className="small">
                  <strong>{t.part(a.part)}</strong> · {a.question} <span className="muted">({t.seconds(a.seconds)})</span>
                </summary>
                <p className="quote small">{a.transcript || t.noAnswer}</p>
              </details>
            ))}
          </div>
        </>
      ) : (
        <>
          <Overall
            band={w.overall}
            extra={`IELTS Writing · ${w.variant === 'general' ? t.general : t.academic} · ${t.timeUsed(Math.round(w.secondsUsed / 60))}`}
          />
          {([['task1', w.task1], ['task2', w.task2]] as const).map(([key, task]) => (
            <div className="card" key={key}>
              <div className="spread">
                <h3 style={{ margin: 0 }}>{key === 'task1' ? t.task1 : t.task2}</h3>
                <strong style={{ fontSize: '1.3rem' }}>{formatBand(task.band)}</strong>
              </div>
              <div className={task.words < task.minWords ? 'txt-low small' : 'muted small'}>{t.wordsUsed(task.words, task.minWords)}</div>
              <BandRow label={t.writingCriteria[key]} data={task.criteria.task} />
              <BandRow label={t.writingCriteria.coherenceCohesion} data={task.criteria.coherenceCohesion} />
              <BandRow label={t.writingCriteria.lexicalResource} data={task.criteria.lexicalResource} />
              <BandRow label={t.writingCriteria.grammaticalRange} data={task.criteria.grammaticalRange} />
              <details style={{ marginTop: 8 }}>
                <summary className="small">{t.yourText}</summary>
                <p className="quote small" style={{ whiteSpace: 'pre-wrap' }}>{task.text || t.noAnswer}</p>
              </details>
            </div>
          ))}
          <Advice result={w} />
        </>
      )}
      <p className="muted tiny">{t.disclaimer}</p>
      <div className="row">
        {data.sessionId && (
          <button className="btn btn-primary" onClick={() => go(`mock/session/${data.sessionId}`)}>🏁 {to.sessionTitle}</button>
        )}
        <button className={data.sessionId ? 'btn btn-outline' : 'btn btn-primary'} onClick={() => go(againRoute)}>{t.again}</button>
        <button className="btn btn-outline" onClick={() => go('mock')}>{t.backToMock}</button>
      </div>
    </>
  );
}
