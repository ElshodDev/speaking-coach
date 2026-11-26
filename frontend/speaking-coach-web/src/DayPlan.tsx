import { useT } from './i18n';
import { hubMsg } from './locales/hub';
import { planMsg } from './locales/plan';
import { doneCount, nowKey, orderItems } from './planLogic';
import { PlanStatusNotice, useItemLabel, useTodayPlan } from './TodayPlan';
import type { ReviewStats } from './reviewStats';

/**
 * Yangi bosh sahifa ("Kundalik"): sariq eslatma (maqsad) va bugungi reja chizig'i.
 * Ma'lumot — o'sha /api/plan/today; chiziqdagi birinchi bajarilmagan qadam "Hozir" deb ajratiladi.
 */
export function DayPlan({ go, stats }: { go: (route: string) => void; stats: ReviewStats | null }) {
  const t = useT(planMsg);
  const h = useT(hubMsg);
  const label = useItemLabel();
  const { plan, loading, failed, reload } = useTodayPlan();

  if (!plan) return <PlanStatusNotice loading={loading} failed={failed} onRetry={() => void reload()} go={go} />;
  const items = orderItems(plan.items);
  const now = nowKey(plan.items);
  const exam = plan.goal === 'ielts' ? 'IELTS' : plan.goal === 'cefr' ? 'CEFR' : null;
  const hasGoal = plan.onboarded && !!plan.goal;

  return (
    <section data-testid="day-plan">
      <div className="nn-note" data-testid="day-note">
        {hasGoal ? (
          <>
            <b>
              {h.goalNote}: {exam ?? t.planTitle}
              {plan.targetScore ? ` ${plan.targetScore}` : ''}
            </b>
            {exam && plan.daysToExam !== null && <div>{t.countdown(plan.daysToExam, exam)}</div>}
            {exam && plan.focusSkill && (
              <div>
                <span className="nn-mark">{plan.focusReason === 'missing' ? t.focusMissing(t.moduleNames[plan.focusSkill]) : t.focusWeakest(t.moduleNames[plan.focusSkill])}</span>
              </div>
            )}
          </>
        ) : (
          <button className="btn-link" onClick={() => go('welcome')}>{t.setGoal} →</button>
        )}
        {stats && stats.dailyGoal > 0 && <div className="nn-note-sub">{h.reviewDone(Math.min(stats.reviewedToday, stats.dailyGoal), stats.dailyGoal)}</div>}
      </div>

      {items.length > 0 && (
        <>
          <div className="nn-dayhead">
            <b>{t.planTitle}</b>
            <span data-testid="plan-count">{t.planDone(doneCount(plan.items), plan.items.length)}</span>
          </div>
          <ol className="nn-tl">
            {items.map((item) => {
              const isNow = item.key === now;
              return (
                <li key={item.key} className={`nn-ev${item.done ? ' done' : ''}${isNow ? ' now' : ''}`} data-testid="plan-item">
                  <span className="nn-dot" aria-hidden>{item.done ? '✓' : ''}</span>
                  <div className="nn-eb">
                    {isNow && <small className="nn-nowtag">{h.now}</small>}
                    <b>{label(item)}</b>
                    <small>{item.done ? `✓ ${t.doneMark}` : t.aboutMinutes(item.minutes)}</small>
                    {!item.done && (
                      <button className={isNow ? 'btn btn-primary' : 'btn btn-outline'} onClick={() => go(item.route)}>
                        {t.start}
                      </button>
                    )}
                  </div>
                </li>
              );
            })}
          </ol>
          {doneCount(plan.items) === plan.items.length && <p className="success small">{t.allDone}</p>}
        </>
      )}
    </section>
  );
}
