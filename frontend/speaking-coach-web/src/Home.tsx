import type { ReviewStats } from './Review';
import { useT } from './i18n';
import { homeMsg } from './locales/home';
import { UsageNote } from './Usage';
import { TelegramPromo } from './TelegramCard';
import { GroupsShortcut, TasksCard } from './StudentTasks';

export type ExerciseKind = 'speaking' | 'writing' | 'reading' | 'listening';

/** Mashq turlari; nomi va tavsifi tanlangan tilda homeMsg.exercises'dan olinadi. */
export const EXERCISES: { kind: ExerciseKind; emoji: string }[] = [
  { kind: 'speaking', emoji: '🎙' },
  { kind: 'writing', emoji: '✍️' },
  { kind: 'reading', emoji: '📖' },
  { kind: 'listening', emoji: '🎧' },
];

export function ExerciseTiles({ onOpen }: { onOpen: (kind: ExerciseKind) => void }) {
  const t = useT(homeMsg);
  return (
    <div className="tiles">
      {EXERCISES.map((e) => (
        <button key={e.kind} className="tile" onClick={() => onOpen(e.kind)}>
          <span className="emoji" aria-hidden>
            {e.emoji}
          </span>
          <strong>{t.exercises[e.kind].title}</strong>
          <span className="muted">{t.exercises[e.kind].blurb}</span>
        </button>
      ))}
    </div>
  );
}

/**
 * Bosh sahifa. Ikki xil holat:
 * - mehmon: ilova nima va qanday ishlashini 3 qadamda tushuntiradi;
 * - kirgan foydalanuvchi: bugungi holat (streak, kunlik maqsad) va eng
 *   muhim keyingi harakat — navbatdagi kartalarni takrorlash.
 */
export function Home({
  email,
  stats,
  go,
}: {
  email: string | null;
  stats: ReviewStats | null;
  go: (route: string) => void;
}) {
  const t = useT(homeMsg);
  const open = (kind: ExerciseKind) => go(`practice/${kind}`);

  if (!email) {
    return (
      <>
        <div className="card hero">
          <h1>{t.heroTitle}</h1>
          <p>{t.heroText}</p>
          <div className="row">
            <button className="btn btn-primary" onClick={() => open('speaking')}>
              {t.tryIt}
            </button>
            <button className="btn-link" style={{ color: '#fff' }} onClick={() => go('profile')}>
              {t.createAccount}
            </button>
          </div>
        </div>

        <div className="card">
          <h2>{t.howItWorks}</h2>
          <div className="steps">
            {[
              [t.step1Title, t.step1],
              [t.step2Title, t.step2],
              [t.step3Title, t.step3],
            ].map(([title, text]) => (
              <div className="step" key={title}>
                <div>
                  <strong>{title}</strong> <span className="muted">{text}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        <h2 style={{ marginTop: 24 }}>{t.practice}</h2>
        <UsageNote userKey="guest" />
        <ExerciseTiles onOpen={open} />
      </>
    );
  }

  const name = email.split('@')[0];
  const goal = stats ? Math.min(100, Math.round((stats.reviewedToday / stats.dailyGoal) * 100)) : 0;

  return (
    <>
      <div className="page-header">
        <h1>{t.hello(name)}</h1>
        <p>{stats && stats.reviewedToday >= stats.dailyGoal ? t.goalDone : t.goalTodo}</p>
      </div>

      <TasksCard loggedIn go={go} />
      <GroupsShortcut go={go} />

      {stats && (
        <div className="card">
          <div className="stats">
            <div className="stat">
              <div className="value">🔥 {stats.streakDays}</div>
              <div className="label">{t.streakLabel}</div>
            </div>
            <div className="stat">
              <div className="value">
                {Math.min(stats.reviewedToday, stats.dailyGoal)}/{stats.dailyGoal}
              </div>
              <div className="label">{t.goalLabel}</div>
            </div>
            <div className="stat">
              <div className="value">{stats.total}</div>
              <div className="label">{t.totalLabel}</div>
            </div>
          </div>
          <div className="progress" style={{ marginTop: 14 }}>
            <div style={{ width: `${goal}%` }} />
          </div>
        </div>
      )}

      {stats && stats.due > 0 ? (
        <div className="card cta">
          <div>
            <strong>{t.dueTitle(stats.due)}</strong>
            <div className="muted small">{t.dueText}</div>
          </div>
          <button className="btn btn-primary" onClick={() => go('review')}>
            {t.review}
          </button>
        </div>
      ) : (
        stats && (
          <div className="card soft small">{t.nothingDue}</div>
        )
      )}

      <TelegramPromo go={go} />

      <button className="card cta" style={{ width: '100%', font: 'inherit', color: 'inherit', textAlign: 'left', cursor: 'pointer' }} onClick={() => go('progress')}>
        <div>
          <strong>{t.leagueTitle}</strong>
          <div className="muted small">{t.leagueText}</div>
        </div>
        <span aria-hidden>→</span>
      </button>

      <h2 style={{ marginTop: 24 }}>{t.practice}</h2>
      <UsageNote userKey={email} />
      <ExerciseTiles onOpen={open} />
    </>
  );
}
