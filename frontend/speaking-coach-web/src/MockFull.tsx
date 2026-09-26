import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { localeOf, useLang, useT } from './i18n';
import { CefrWriting } from './CefrWriting';
import { cefrMsg } from './locales/cefr';
import { mockMsg } from './locales/mock';
import { mockObjMsg } from './locales/mockObjective';
import { MockListening } from './MockListening';
import { MockReading } from './MockReading';
import { MockSpeaking } from './MockSpeaking';
import { MockWriting } from './MockWriting';
import { formatBand } from './mockLogic';
import { PageHeader } from './ui';

export const FULL_ORDER = ['listening', 'reading', 'writing', 'speaking'] as const;
export type Variant = 'academic' | 'general';
/** To'liq imtihon turi: IELTS Academic/General yoki CEFR (Multilevel). */
export type FullKind = Variant | 'cefr';

function newSessionId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) return crypto.randomUUID().replace(/-/g, '');
  return Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join('');
}

/** To'liq imtihonni boshlash: turini tanlash va qoidalar. */
export function MockFullStart({ go, exam = 'ielts' }: { go: (route: string) => void; exam?: 'ielts' | 'cefr' }) {
  const t = useT(mockObjMsg);
  const tm = useT(mockMsg);
  const tc = useT(cefrMsg);
  if (exam === 'cefr') {
    return (
      <>
        <PageHeader title={`${tc.title} · ${tc.full.replace(/^🏁\s*/, '')}`} subtitle={tc.fullText} onBack={() => go('mock')} backLabel={tm.title} />
        <div className="card stack">
          <ul className="small" style={{ margin: 0, paddingLeft: 20 }}>
            {tc.fullRules.map((r) => <li key={r}>{r}</li>)}
          </ul>
          <button className="btn btn-primary block" style={{ minHeight: 52 }} onClick={() => go(`mock/full/cefr/${newSessionId()}/0`)}>{tc.fullStartCefr}</button>
          <p className="muted tiny" style={{ margin: 0 }}>{tc.disclaimer}</p>
        </div>
      </>
    );
  }
  return (
    <>
      <PageHeader title={t.fullTitle} subtitle={t.fullText} onBack={() => go('mock')} backLabel={tm.title} />
      <div className="card stack">
        <ul className="small" style={{ margin: 0, paddingLeft: 20 }}>
          {t.fullRules.map((r) => <li key={r}>{r}</li>)}
        </ul>
        <strong>{t.chooseVariant}</strong>
        <div className="row">
          <button className="btn btn-primary" onClick={() => go(`mock/full/academic/${newSessionId()}/0`)}>{tm.academic}</button>
          <button className="btn btn-outline" onClick={() => go(`mock/full/general/${newSessionId()}/0`)}>{tm.general}</button>
        </div>
        <p className="muted tiny" style={{ margin: 0 }}>{tm.disclaimer}</p>
      </div>
    </>
  );
}

/**
 * To'liq imtihonning bitta bosqichi. Har bo'lim oldidan qisqa oraliq sahifa
 * (dam olish imkoniyati), keyin bo'limning o'zi — natija sessiyaga yoziladi.
 */
export function MockFullStep({ variant, sessionId, step, go }: { variant: FullKind; sessionId: string; step: number; go: (route: string) => void }) {
  const t = useT(mockObjMsg);
  const [started, setStarted] = useState(false);
  const module = FULL_ORDER[step];

  // Bosqich raqami chegaradan tashqarida — natijaga o'tamiz (render'dan keyin).
  useEffect(() => {
    if (!module) go(`mock/session/${sessionId}`);
  }, [module, sessionId, go]);
  if (!module) return null;

  const next = () => (step + 1 < FULL_ORDER.length ? go(`mock/full/${variant}/${sessionId}/${step + 1}`) : go(`mock/session/${sessionId}`));
  const name = t.moduleNames[module];

  if (!started) {
    return (
      <>
        <PageHeader title={t.fullTitle} subtitle={t.step(step + 1, FULL_ORDER.length, name)} />
        <div className="card stack">
          {step > 0 && <p className="success" style={{ margin: 0 }}>{t.sectionDone}</p>}
          <strong>{t.nextSection(name)}</strong>
          <ol className="small" style={{ margin: 0, paddingLeft: 20 }}>
            {FULL_ORDER.map((m, i) => (
              <li key={m} style={{ fontWeight: i === step ? 700 : 400, opacity: i < step ? 0.6 : 1 }}>
                {t.moduleNames[m]} {i < step ? '✅' : ''}
              </li>
            ))}
          </ol>
          <button className="btn btn-primary block" style={{ minHeight: 52 }} onClick={() => setStarted(true)}>
            {t.startSection}
          </button>
        </div>
      </>
    );
  }

  const common = { go, sessionId, onSubmitted: next };
  if (variant === 'cefr') {
    if (module === 'listening') return <MockListening key={`${sessionId}-l`} exam="cefr" {...common} />;
    if (module === 'reading') return <MockReading key={`${sessionId}-r`} exam="cefr" variant="academic" {...common} />;
    if (module === 'writing') return <CefrWriting key={`${sessionId}-w`} {...common} />;
    return <MockSpeaking key={`${sessionId}-s`} exam="cefr" {...common} />;
  }
  if (module === 'listening') return <MockListening key={`${sessionId}-l`} {...common} />;
  if (module === 'reading') return <MockReading key={`${sessionId}-r`} variant={variant} {...common} />;
  if (module === 'writing') return <MockWriting key={`${sessionId}-w`} variant={variant} {...common} />;
  return <MockSpeaking key={`${sessionId}-s`} {...common} />;
}

interface SessionData {
  sessionId: string;
  exam?: 'ielts' | 'cefr';
  modules: Record<string, { id: string; createdAtUtc: string; overall: number | null; variant: string | null }>;
  overall: number | null;
  level?: string | null;
}

/** To'liq imtihon natijasi: 4 bo'lim va umumiy band (rasmiy yaxlitlash — serverda). */
export function MockSessionView({ sessionId, go }: { sessionId: string; go: (route: string) => void }) {
  const t = useT(mockObjMsg);
  const tm = useT(mockMsg);
  const tc = useT(cefrMsg);
  const locale = localeOf(useLang().lang);
  const [data, setData] = useState<SessionData | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    apiJson<SessionData>(`/api/mock/session/${sessionId}`)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
  }, [sessionId]);

  const header = <PageHeader title={t.sessionTitle} onBack={() => go('mock')} backLabel={tm.title} />;
  if (error) return <>{header}<p className="error" role="alert">{error}</p></>;
  if (!data) return <>{header}<p className="muted">…</p></>;

  const firstMissing = FULL_ORDER.findIndex((m) => !data.modules[m]);
  const cefr = data.exam === 'cefr';
  const variant: FullKind = cefr ? 'cefr' : ((Object.values(data.modules).find((m) => m.variant)?.variant as Variant | undefined) ?? 'academic');
  const show = (v: number | null) => (v === null ? '—' : cefr ? tc.of75(v) : formatBand(v));

  return (
    <>
      {header}
      <div className="card" style={{ textAlign: 'center' }}>
        <div className="muted small">{tm.overall}</div>
        <div style={{ fontSize: '3rem', fontWeight: 800, lineHeight: 1.1 }} data-testid="session-overall">{show(data.overall)}</div>
        {data.overall !== null ? (
          <div className="small">{cefr ? <strong>{tc.level(data.level ?? '')}</strong> : tm.bandNames[Math.floor(data.overall)]}</div>
        ) : (
          <div className="muted small">{cefr ? tc.overallPendingCefr : t.overallPending}</div>
        )}
        {cefr && <div className="muted small" style={{ marginTop: 6 }}>{tc.title}</div>}
      </div>
      <div className="card">
        <table className="data">
          <tbody>
            {FULL_ORDER.map((m) => {
              const r = data.modules[m];
              return (
                <tr key={m}>
                  <td><strong>{t.moduleNames[m]}</strong>{r && <div className="muted tiny">{new Date(r.createdAtUtc).toLocaleString(locale)}</div>}</td>
                  <td style={{ fontSize: '1.2rem', fontWeight: 700 }}>{r ? show(r.overall) : <span className="muted small">{t.notTaken}</span>}</td>
                  <td>{r && <button className="btn-link small" onClick={() => go(`mock/result/${r.id}`)}>{tm.open}</button>}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {firstMissing >= 0 && (
        <button className="btn btn-primary" onClick={() => go(`mock/full/${variant}/${data.sessionId}/${firstMissing}`)}>
          {t.continueExam}
        </button>
      )}
      <p className="muted tiny">{cefr ? tc.disclaimer : tm.disclaimer}</p>
    </>
  );
}
