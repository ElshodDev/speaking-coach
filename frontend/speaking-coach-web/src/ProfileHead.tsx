import { Icon } from './Icon';
import { useT } from './i18n';
import { hubMsg } from './locales/hub';
import { planMsg } from './locales/plan';
import { goalShort } from './newNav';
import type { Profile } from './api';

/** Profilning tepasi: kim, daraja, maqsad, seriya va 3 ta tez tugma (Maqsadim, Natijalarim, Sozlamalar). */
export function ProfileHead({ profile, name, streak, go, onSettings }: { profile: Profile | null; name: string; streak: number; go: (r: string) => void; onSettings: () => void }) {
  const h = useT(hubMsg);
  const tp = useT(planMsg);
  const goal = goalShort(profile, (g) => tp.goals[g]?.[0] ?? g);
  return (
    <div data-testid="profile-head">
      <div className="nn-pcard">
        <span className="avatar nn-pav" aria-hidden>{name.charAt(0).toUpperCase() || '?'}</span>
        <b>{name}</b>
      </div>
      <div className="nn-stats">
        <div><b>{profile?.level ?? '—'}</b><small>{h.pLevel}</small></div>
        <div><b>{goal ?? '—'}</b><small>{h.pGoal}</small></div>
        <div><b>{streak}</b><small>{h.pStreak}</small></div>
      </div>
      <div className="nn-big3">
        <button onClick={() => go('welcome')} data-testid="ph-goal"><Icon name="target" size={20} />{h.pMyGoal}</button>
        <button onClick={() => go('progress')} data-testid="ph-results"><Icon name="progress" size={20} />{h.pResults}</button>
        <button onClick={onSettings} data-testid="ph-settings"><Icon name="gear" size={20} />{h.pSettings}</button>
      </div>
    </div>
  );
}
