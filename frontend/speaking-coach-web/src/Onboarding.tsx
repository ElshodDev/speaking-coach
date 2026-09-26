import { useState } from 'react';
import { apiJson, setLevel, type Profile } from './api';
import { useT } from './i18n';
import { planMsg } from './locales/plan';
import { examDateRange, GOALS, LEVELS, MINUTES, stepsFor, targetsFor, type Goal } from './planLogic';
import { PageHeader } from './ui';

function Choice({ selected, onClick, title, hint, testId }: { selected: boolean; onClick: () => void; title: string; hint?: string; testId?: string }) {
  return (
    <button type="button" className="choice" aria-pressed={selected} onClick={onClick} data-testid={testId}>
      <strong>{title}</strong>
      {hint && <span className="muted small">{hint}</span>}
    </button>
  );
}

/**
 * Tanishtiruv: maqsad → daraja → maqsad ball va sana → kunlik vaqt.
 * Birinchi kirishda bosh sahifa o'rniga ochiladi; keyin Profildan qayta
 * ochib, maqsadni o'zgartirish mumkin. "Keyinroq" — qayta so'ramaydi.
 */
export function Onboarding({ profile, go, onSaved }: { profile: Profile | null; go: (route: string) => void; onSaved: (p: Profile) => void }) {
  const t = useT(planMsg);
  const [goal, setGoal] = useState<Goal | null>((profile?.goal as Goal | null) ?? null);
  const [level, setLevelState] = useState<string | null>(profile?.onboarded ? profile.level : null);
  const [target, setTarget] = useState(profile?.targetScore ?? '');
  const [examDate, setExamDate] = useState(profile?.examDate ?? '');
  const [minutes, setMinutes] = useState<number>(profile?.dailyMinutes ?? 20);
  const [step, setStep] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const steps = stepsFor(goal);
  const current = steps[Math.min(step, steps.length - 1)];
  const last = step >= steps.length - 1;
  const range = examDateRange();
  const canNext = current === 'goal' ? goal !== null : current === 'level' ? level !== null : true;

  async function save(skip: boolean) {
    setBusy(true);
    setError('');
    try {
      const body = skip
        ? { skip: true }
        : {
            skip: false,
            goal,
            level,
            targetScore: goal === 'general' ? null : target || null,
            examDate: goal === 'general' ? null : examDate || null,
            dailyMinutes: minutes,
            tzOffsetMinutes: new Date().getTimezoneOffset(),
          };
      const saved = await apiJson<{ goal: string | null; level: string; targetScore: string | null }>('/api/profile/onboarding', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (profile) {
        onSaved({
          ...profile,
          level: saved.level,
          goal: saved.goal,
          targetScore: saved.targetScore,
          examDate: skip ? profile.examDate : goal === 'general' ? null : examDate || null,
          dailyMinutes: skip ? profile.dailyMinutes : minutes,
          onboarded: true,
        });
      }
      setLevel(saved.level);
      go('home');
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader title={t.welcomeTitle} subtitle={t.welcomeText} />
      <div className="card stack" data-testid="onboarding">
        <div className="spread">
          <span className="muted small" data-testid="onboarding-step">{t.stepOf(step + 1, steps.length)}</span>
          <div className="progress" style={{ flex: 1, marginLeft: 12, maxWidth: 200 }} aria-hidden="true">
            <div style={{ width: `${((step + 1) / steps.length) * 100}%` }} />
          </div>
        </div>

        {current === 'goal' && (
          <fieldset className="stack" style={{ border: 0, padding: 0, margin: 0 }}>
            <legend><h3 style={{ margin: '0 0 8px' }}>{t.goalQ}</h3></legend>
            {GOALS.map((g) => (
              <Choice key={g} selected={goal === g} onClick={() => { if (goal !== g) setTarget(''); setGoal(g); }} title={t.goals[g][0]} hint={t.goals[g][1]} testId={`goal-${g}`} />
            ))}
          </fieldset>
        )}

        {current === 'level' && (
          <fieldset className="stack" style={{ border: 0, padding: 0, margin: 0 }}>
            <legend><h3 style={{ margin: '0 0 8px' }}>{t.levelQ}</h3></legend>
            {LEVELS.map((l) => (
              <Choice key={l} selected={level === l} onClick={() => setLevelState(l)} title={t.levels[l]} />
            ))}
            <button type="button" className="btn-link small" style={{ alignSelf: 'flex-start' }} onClick={() => { setLevelState('B1'); setStep(step + 1); }}>
              {t.notSure}
            </button>
            <p className="muted tiny" style={{ margin: 0 }}>{t.levelHint}</p>
          </fieldset>
        )}

        {current === 'target' && goal && (
          <div className="stack">
            <h3 style={{ margin: 0 }}>{t.targetQ}</h3>
            <label className="small field">
              {t.targetLabel(goal)}
              <div className="chips" role="group">
                {targetsFor(goal).map((v) => (
                  <button key={v} type="button" aria-pressed={target === v} onClick={() => setTarget(target === v ? '' : v)}>{v}</button>
                ))}
                <button type="button" aria-pressed={target === ''} onClick={() => setTarget('')}>{t.noTarget}</button>
              </div>
            </label>
            <label className="small field">
              {t.examDateLabel}
              <input className="input" type="date" value={examDate} min={range.min} max={range.max} onChange={(e) => setExamDate(e.target.value)} />
            </label>
            <p className="muted tiny" style={{ margin: 0 }}>{t.examDateHint}</p>
          </div>
        )}

        {current === 'minutes' && (
          <fieldset className="stack" style={{ border: 0, padding: 0, margin: 0 }}>
            <legend><h3 style={{ margin: '0 0 8px' }}>{t.minutesQ}</h3></legend>
            {MINUTES.map((m) => (
              <Choice key={m} selected={minutes === m} onClick={() => setMinutes(m)} title={t.minutes(m)} hint={t.minutesHint[m]} />
            ))}
          </fieldset>
        )}

        {error && <p className="error small" role="alert">{error}</p>}

        <div className="spread" style={{ flexWrap: 'wrap', gap: 8 }}>
          {step > 0 ? (
            <button type="button" className="btn btn-outline" onClick={() => setStep(step - 1)} disabled={busy}>{t.back}</button>
          ) : (
            <button type="button" className="btn-link small quiet" onClick={() => void save(true)} disabled={busy}>{t.later}</button>
          )}
          {last ? (
            <button type="button" className="btn btn-primary" onClick={() => void save(false)} disabled={busy || !goal || !level}>{busy ? '⏳' : t.finish}</button>
          ) : (
            <button type="button" className="btn btn-primary" onClick={() => setStep(step + 1)} disabled={!canNext}>{t.next}</button>
          )}
        </div>
      </div>
    </>
  );
}
