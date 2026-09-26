import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { planMsg } from './locales/plan';
import { doneCount, mockParts, orderItems, type PlanItem, type TodayPlan as Plan } from './planLogic';

export const planPath = () => `/api/plan/today?tzOffsetMinutes=${new Date().getTimezoneOffset()}`;

function useItemLabel() {
  const t = useT(planMsg);
  return (item: PlanItem): string => {
    if (item.kind === 'review') return t.items.review(item.progress ?? 0, item.target ?? 0);
    if (item.kind === 'mock') {
      const [exam, module] = mockParts(item.key);
      return t.items.mock(exam, t.moduleNames[module] ?? module);
    }
    const f = (t.items as Record<string, (...a: never[]) => string>)[item.key];
    return f ? f() : item.key;
  };
}

/**
 * Bosh sahifadagi "Bugungi reja": takrorlash, kunning ko'nikmasi va
 * (imtihonga tayyorlanayotganlarga) mock bo'limi. Bajarilganlari o'zi
 * belgilanadi — mashq tugagach server shuni ko'radi.
 */
export function TodayPlan({ go }: { go: (route: string) => void }) {
  const t = useT(planMsg);
  const label = useItemLabel();
  const [plan, setPlan] = useState<Plan | null>(null);

  useEffect(() => {
    apiJson<Plan>(planPath()).then(setPlan).catch(() => setPlan(null));
  }, []);

  if (!plan) return null;
  const done = doneCount(plan.items);
  const all = plan.items.length;
  const exam = plan.goal === 'ielts' ? 'IELTS' : plan.goal === 'cefr' ? 'CEFR' : null;

  return (
    <div className="card" data-testid="today-plan">
      <div className="spread" style={{ alignItems: 'baseline' }}>
        <h3 style={{ margin: 0 }}>{t.planTitle}</h3>
        {all > 0 && <span className="muted small" data-testid="plan-count">{t.planDone(done, all)}</span>}
      </div>
      {exam && (plan.daysToExam !== null || plan.targetScore) && (
        <p className="small" style={{ margin: '6px 0 0' }} data-testid="plan-countdown">
          {plan.daysToExam !== null ? <strong>⏳ {t.countdown(plan.daysToExam, exam)}</strong> : <strong>{exam}</strong>}
          {plan.targetScore && <span className="muted"> · {t.target(plan.targetScore)}</span>}
        </p>
      )}
      {!plan.onboarded || !plan.goal ? (
        <p className="small" style={{ margin: '6px 0 0' }}>
          <button className="btn-link small" onClick={() => go('welcome')}>{t.setGoal} →</button>
        </p>
      ) : null}
      {all > 0 && done === all && <p className="success small" style={{ margin: '8px 0 0' }}>{t.allDone}</p>}
      <div style={{ marginTop: 6 }}>
        {orderItems(plan.items).map((item) => (
          <div key={item.key} className={`plan-item${item.done ? ' done' : ''}`} data-testid="plan-item">
            <span className="small" style={{ minWidth: 0 }}>
              <span className="plan-title">{label(item)}</span>
              <span className="muted tiny" style={{ display: 'block' }}>{t.aboutMinutes(item.minutes)}</span>
            </span>
            {item.done ? (
              <span className="txt-great small" style={{ whiteSpace: 'nowrap' }}>✅ {t.doneMark}</span>
            ) : (
              <button className="btn btn-primary" onClick={() => go(item.route)}>{t.start}</button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
