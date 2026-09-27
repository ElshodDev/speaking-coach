import type { ReviewStats } from './Review';
import { useT } from './i18n';
import { homeMsg } from './locales/home';
import { UsageNote } from './Usage';
import { TelegramPromo } from './TelegramCard';
import { GroupsShortcut, TasksCard } from './StudentTasks';
import { TodayPlan } from './TodayPlan';
import { QuizTile, WordOfDay } from './WordOfDay';
import { DemoHeroButton, isDemoEmail } from './Demo';
import { demoMsg } from './locales/demo';
import { useWide } from './layout';

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
  const td = useT(demoMsg);
  const wide = useWide();
  const open = (kind: ExerciseKind) => go(`practice/${kind}`);

  if (!email) {
    const hero = (
      <div className="card hero">
        <h1>{t.heroTitle}</h1>
        <p>{t.heroText}</p>
        <div className="row">
          <button className="btn btn-primary" onClick={() => open('speaking')}>
            {t.tryIt}
          </button>
          <button className="btn-link" style={{ color: '#fff' }} onClick={() => go('register')}>
            {t.createAccount}
          </button>
          <DemoHeroButton go={go} />
        </div>
      </div>
    );
    const how = (
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
    );
    const practice = (
      <>
        <h2 style={{ marginTop: 24 }}>{t.practice}</h2>
        <UsageNote userKey="guest" />
        <ExerciseTiles onOpen={open} />
      </>
    );
    const word = <WordOfDay loggedIn={false} onLogin={() => go('login')} />;
    const quiz = <QuizTile go={go} />;
    if (wide) {
      return (
        <>
          {hero}
          <div className="split">
            <div>
              {how}
              {practice}
            </div>
            <aside>
              {word}
              {quiz}
            </aside>
          </div>
        </>
      );
    }
    return (
      <>
        {hero}
        {how}
        {word}
        {quiz}
        {practice}
      </>
    );
  }

  const name = isDemoEmail(email) ? td.name : email.split('@')[0];
  const goal = stats ? Math.min(100, Math.round((stats.reviewedToday / stats.dailyGoal) * 100)) : 0;

  const header = (
    <div className="page-header">
      <h1>{t.hello(name)}</h1>
      <p>{stats && stats.reviewedToday >= stats.dailyGoal ? t.goalDone : t.goalTodo}</p>
    </div>
  );
  const plan = (
    <>
      <TodayPlan go={go} />
      <TasksCard loggedIn go={go} />
      <GroupsShortcut go={go} />
    </>
  );
  const statsCard = stats && (
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
  );
  const due =
    stats && stats.due > 0 ? (
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
      stats && <div className="card soft small">{t.nothingDue}</div>
    );
  const league = (
    <button className="card cta" style={{ width: '100%', font: 'inherit', color: 'inherit', textAlign: 'left', cursor: 'pointer' }} onClick={() => go('progress')}>
      <div>
        <strong>{t.leagueTitle}</strong>
        <div className="muted small">{t.leagueText}</div>
      </div>
      <span aria-hidden>→</span>
    </button>
  );
  const practice = (
    <>
      <h2 style={{ marginTop: 24 }}>{t.practice}</h2>
      <UsageNote userKey={email} />
      <ExerciseTiles onOpen={open} />
    </>
  );
  const promo = !isDemoEmail(email) && <TelegramPromo go={go} />;

  // Kompyuter: chapda reja va mashqlar, o'ngda holat (seriya, navbatdagi kartalar, so'z).
  if (wide) {
    return (
      <>
        {header}
        <div className="split">
          <div>
            {plan}
            {practice}
            <WordOfDay loggedIn />
          </div>
          <aside>
            {statsCard}
            {due}
            <QuizTile go={go} />
            {league}
            {promo}
          </aside>
        </div>
      </>
    );
  }

  return (
    <>
      {header}
      {plan}
      {statsCard}
      {due}
      <WordOfDay loggedIn />
      <QuizTile go={go} />
      {promo}
      {league}
      {practice}
    </>
  );
}
