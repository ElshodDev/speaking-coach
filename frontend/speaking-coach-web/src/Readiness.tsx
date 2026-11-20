import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { planMsg } from './locales/plan';
import { readinessMsg } from './locales/readiness';
import { focusRoute, formatRange, formatScore, pct, statusTone, type Readiness } from './readinessLogic';

/**
 * Imtihonga tayyorlik kartasi: umumiy taxminiy natija (oraliq bilan) va maqsad, imtihongacha kunlar,
 * 4 ko'nikma bo'yicha baho (maqsad chizig'i bilan) va bugungi fokus.
 */
export function ReadinessCard({ go }: { go: (route: string) => void }) {
  const t = useT(readinessMsg);
  const tp = useT(planMsg);
  const [r, setR] = useState<Readiness | null>(null);

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

  if (!r.exam) {
    return (
      <div className="card" data-testid="readiness">
        <h3 style={{ marginTop: 0 }}>{t.title}</h3>
        <p className="small">{t.noGoalText}</p>
        <button className="btn btn-outline" onClick={() => go('welcome')}>{t.setGoal}</button>
      </div>
    );
  }

  const examName = r.exam === 'cefr' ? 'CEFR' : 'IELTS';
  const tone = statusTone(r.status);
  const name = (s: string) => tp.moduleNames[s] ?? s;
  const route = focusRoute(r);
  const targetPct = r.targetValue !== null ? pct(r.targetValue, r.max) : null;
  const targetLabel = r.target ?? '';

  return (
    <div className="card" data-testid="readiness">
      <div className="spread" style={{ alignItems: 'baseline' }}>
        <h3 style={{ margin: 0 }}>{t.title}</h3>
        {r.daysToExam !== null && <span className="small"><strong>{t.days(r.daysToExam)}</strong></span>}
      </div>

      <div className="stats" style={{ gridTemplateColumns: '1fr 1fr', marginTop: 12 }}>
        <div className="stat">
          <div className={`value${tone ? ` txt-${tone}` : ''}`} data-testid="readiness-overall">
            {formatScore(r.exam, r.overall)}
            {r.overallLevel && <span className="small"> · {r.overallLevel}</span>}
          </div>
          <div className="label">{t.estimate(examName)}</div>
          {r.overall !== null && <div className="label">{t.range(formatRange(r.exam, r.overallLow, r.overallHigh))}</div>}
        </div>
        <div className="stat">
          <div className="value">{targetLabel || '—'}</div>
          <div className="label">{t.target(targetLabel || '—')}</div>
          {r.gap !== null && r.gap > 0 && <div className="label">{t.gap(formatScore(r.exam, r.gap))}</div>}
        </div>
      </div>
      {t.status[r.status] && <p className="small" style={{ marginBottom: 4 }}>{t.status[r.status]}</p>}

      <ul style={{ listStyle: 'none', padding: 0, margin: '10px 0 0' }} data-testid="readiness-skills">
        {r.skills.map((s) => (
          <li key={s.skill} style={{ marginBottom: 10 }}>
            <div className="spread small">
              <strong>{name(s.skill)}</strong>
              {s.estimate === null ? (
                <button className="btn-link small" onClick={() => go(focusRoute({ ...r, focusSkill: s.skill, focusReason: 'missing' }) ?? 'mock')}>
                  {t.noData}
                </button>
              ) : (
                <span>
                  <strong>{formatScore(r.exam, s.estimate)}</strong>{' '}
                  <span className="muted tiny">({formatRange(r.exam, s.low, s.high)} · {t.confidence[s.confidence]})</span>
                </span>
              )}
            </div>
            <div className="progress" style={{ position: 'relative', marginTop: 4 }} aria-hidden>
              {s.estimate !== null && <div style={{ width: `${pct(s.estimate, r.max)}%` }} />}
              {targetPct !== null && (
                <span style={{ position: 'absolute', left: `${targetPct}%`, top: -2, bottom: -2, width: 2, background: 'var(--text)' }} title={t.target(targetLabel)} />
              )}
            </div>
            {s.estimate !== null && <div className="muted tiny">{t.sources(s.mocks, s.practice)}</div>}
          </li>
        ))}
      </ul>

      {r.focusSkill && route && (
        <div className="cta" style={{ marginTop: 6 }} data-testid="readiness-focus">
          <p className="small" style={{ margin: 0 }}>
            {r.focusReason === 'missing' ? t.focusMissing(name(r.focusSkill)) : t.focusWeakest(name(r.focusSkill))}
          </p>
          <button className="btn btn-primary" onClick={() => go(route)}>{t.go}</button>
        </div>
      )}
      <p className="muted tiny" style={{ marginBottom: 0 }}>{t.note}</p>
    </div>
  );
}
