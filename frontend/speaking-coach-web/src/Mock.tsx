import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { localeOf, useLang, useT } from './i18n';
import { mockMsg } from './locales/mock';
import { bandKey, formatBand } from './mockLogic';
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

type MockResult = SpeakingResult | WritingResult;

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

      <div className="card stack" aria-disabled="true" style={{ opacity: 0.75 }}>
        <div className="spread">
          <h3 style={{ margin: 0 }}>{t.cefrTitle}</h3>
          <span className="muted small">{t.soon}</span>
        </div>
        <p className="muted small" style={{ margin: 0 }}>{t.cefrText}</p>
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
                    <strong>IELTS {t.moduleName[h.module] ?? h.module}</strong>
                    {h.variant ? ` · ${h.variant === 'general' ? t.general : t.academic}` : ''}
                    <div className="muted tiny">{new Date(h.createdAtUtc).toLocaleString(locale)}</div>
                  </span>
                  <span className="row" style={{ alignItems: 'center', gap: 10 }}>
                    <strong style={{ fontSize: '1.2rem' }}>{formatBand(h.overall)}</strong>
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

function Advice({ result }: { result: MockResult }) {
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
  const [data, setData] = useState<{ createdAtUtc: string; result: MockResult } | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    apiJson<{ createdAtUtc: string; result: MockResult }>(`/api/mock/${id}`)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
  }, [id]);

  const header = <PageHeader title={t.resultTitle} subtitle={data ? new Date(data.createdAtUtc).toLocaleString(locale) : undefined} onBack={() => go('mock')} backLabel={t.title} />;
  if (error) return <>{header}<p className="error" role="alert">{error}</p></>;
  if (!data) return <>{header}<p className="muted">…</p></>;

  const r = data.result;
  const againRoute = r.module === 'speaking' ? 'mock/speaking' : `mock/writing-${r.variant}`;

  return (
    <>
      {header}
      {r.module === 'speaking' ? (
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
            band={r.overall}
            extra={`IELTS Writing · ${r.variant === 'general' ? t.general : t.academic} · ${t.timeUsed(Math.round(r.secondsUsed / 60))}`}
          />
          {([['task1', r.task1], ['task2', r.task2]] as const).map(([key, task]) => (
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
          <Advice result={r} />
        </>
      )}
      <p className="muted tiny">{t.disclaimer}</p>
      <div className="row">
        <button className="btn btn-primary" onClick={() => go(againRoute)}>{t.again}</button>
        <button className="btn btn-outline" onClick={() => go('mock')}>{t.backToMock}</button>
      </div>
    </>
  );
}
