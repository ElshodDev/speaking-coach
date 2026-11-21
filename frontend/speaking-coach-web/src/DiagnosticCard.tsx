import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { Icon } from './Icon';
import { diagnosticMsg } from './locales/diagnostic';
import { diagnosticState, diagnosticSteps, formatRange, formatScore, nextDiagnosticStep, type Readiness } from './readinessLogic';

const DISMISS_KEY = 'sc.diag.dismissedAt';
const READY_SEEN_KEY = 'sc.diag.readySeen';
const DISMISS_DAYS = 3;

function read(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}
function write(key: string, value: string) {
  try {
    localStorage.setItem(key, value);
  } catch {
    // saqlab bo'lmadi — keyingi safar yana ko'rinadi; muhim emas
  }
}

/** "Keyinroq" bosilganmi (shuncha kun ko'rsatilmaydi). */
export function dismissedRecently(now = Date.now()): boolean {
  const t = Number(read(DISMISS_KEY));
  return Number.isFinite(t) && t > 0 && now - t < DISMISS_DAYS * 86_400_000;
}

/**
 * Bosh sahifada: imtihon maqsadi bor foydalanuvchiga birinchi kunoq tayyorlik bahosi uchun 4 ta qisqa mashq.
 * Qadamlar — oddiy mashq sahifalari; "bajarilgan" holat serverdagi tayyorlik hisobotidan olinadi
 * (alohida ma'lumot saqlanmaydi). Baho tayyor bo'lganda — bir marta qisqa natija kartasi.
 * Server flag'i (Features:DayOneDiagnostic) o'chiq bo'lsa — hech narsa ko'rinmaydi.
 */
export function DiagnosticCard({ go }: { go: (route: string) => void }) {
  const t = useT(diagnosticMsg);
  const [r, setR] = useState<Readiness | null>(null);
  const [hidden, setHidden] = useState(() => dismissedRecently());
  const [readySeen, setReadySeen] = useState(() => read(READY_SEEN_KEY) === '1');

  useEffect(() => {
    let alive = true;
    apiJson<Readiness>(`/api/readiness?tzOffsetMinutes=${new Date().getTimezoneOffset()}`)
      .then((d) => alive && setR(d))
      .catch(() => undefined);
    return () => {
      alive = false;
    };
  }, []);

  if (!r) return null;
  const state = diagnosticState(r);
  if (state === 'off') return null;
  const examName = r.exam === 'cefr' ? 'CEFR' : 'IELTS';

  if (state === 'done') {
    if (readySeen) return null;
    return (
      <div className="card diag" data-testid="diagnostic-ready">
        <h3 style={{ margin: 0 }}>{t.readyTitle}</h3>
        <p style={{ margin: '6px 0' }}>{t.ready(examName, formatScore(r.exam, r.overall), formatRange(r.exam, r.overallLow, r.overallHigh))}</p>
        <p className="muted tiny" style={{ margin: '0 0 10px' }}>{t.note}</p>
        <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
          <button className="btn btn-primary" onClick={() => { write(READY_SEEN_KEY, '1'); setReadySeen(true); go('progress'); }}>{t.details}</button>
          <button className="btn btn-outline" onClick={() => { write(READY_SEEN_KEY, '1'); setReadySeen(true); }}>{t.close}</button>
        </div>
      </div>
    );
  }

  if (hidden) return null;
  const steps = diagnosticSteps(r);
  const doneCount = steps.filter((s) => s.done).length;
  const next = nextDiagnosticStep(r);

  return (
    <div className="card diag" data-testid="diagnostic">
      <div className="spread" style={{ alignItems: 'baseline' }}>
        <h3 style={{ margin: 0 }}>{t.title}</h3>
        <span className="muted small" data-testid="diagnostic-progress">{t.progress(doneCount)}</span>
      </div>
      <p className="muted small" style={{ margin: '6px 0 10px' }}>{t.text(examName)}</p>
      <ul className="diag-steps">
        {steps.map((s) => (
          <li key={s.skill} className={s.done ? 'is-done' : ''} data-testid={`diag-step-${s.skill}`}>
            <span className="diag-mark" aria-hidden>{s.done ? <Icon name="check" size={16} /> : null}</span>
            <span className="diag-label">{t.steps[s.skill]}</span>
            {s.done ? (
              <span className="muted small">{t.done}</span>
            ) : (
              <button className={`btn btn-sm ${s.skill === next?.skill ? 'btn-primary' : 'btn-outline'}`} onClick={() => go(s.route)}>{t.start}</button>
            )}
          </li>
        ))}
      </ul>
      <div className="spread" style={{ marginTop: 10, alignItems: 'center' }}>
        <p className="muted tiny" style={{ margin: 0, flex: 1 }}>{t.note}</p>
        <button className="btn-link small quiet" onClick={() => { write(DISMISS_KEY, String(Date.now())); setHidden(true); }}>{t.later}</button>
      </div>
    </div>
  );
}
