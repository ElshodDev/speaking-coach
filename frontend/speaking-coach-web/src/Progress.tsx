import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { ActivityCalendar, LineChart, type CalendarDay, type LineSeries } from './charts';
import { common, msg, useT } from './i18n';
import { progressMsg, type BadgeId } from './locales/progress';
import { PageHeader } from './ui';

interface LevelInfo {
  level: number;
  xp: number;
  levelStartXp: number;
  nextLevelXp: number;
}

interface Badge {
  id: string;
  emoji: string;
  title: string;
  description: string;
  earned: boolean;
}

interface TrendPoint {
  at: string;
  scores: Record<string, number>;
}

export interface ProgressData {
  level: LevelInfo;
  bestStreak: number;
  totalActivities: number;
  totalReviews: number;
  activitiesByType: Record<string, number>;
  calendar: CalendarDay[];
  speakingTrend: TrendPoint[];
  writingTrend: TrendPoint[];
  badges: Badge[];
  hardestCards: { front: string; back: string; lapses: number }[];
}

interface LeaderboardData {
  weekStartUtc: string;
  entries: { rank: number; name: string; xp: number; isMe: boolean }[];
  myXp: number;
  myRank: number | null;
  optedIn: boolean;
  hasDisplayName: boolean;
}

// Seriya ranglari qat'iy tartibda (mezon → rang doim bir xil): grammatika
// Yozishda ham, Gapirishda ham bir xil rangda ko'rinadi.
// Nomlar (label) joriy tildagi matnlardan olinadi — qarang: series().
const WRITING_SERIES = [
  { key: 'taskAchievement', color: 'var(--series-1)' },
  { key: 'coherenceCohesion', color: 'var(--series-2)' },
  { key: 'grammar', color: 'var(--series-3)' },
  { key: 'vocabulary', color: 'var(--series-4)' },
] as const;
const SPEAKING_SERIES = [
  { key: 'fluency', color: 'var(--series-1)' },
  { key: 'grammar', color: 'var(--series-3)' },
  { key: 'vocabulary', color: 'var(--series-4)' },
] as const;

type Texts = (typeof progressMsg)['uz'];
const series = (list: typeof WRITING_SERIES | typeof SPEAKING_SERIES, t: Texts): LineSeries[] =>
  list.map((s) => ({ ...s, label: t.criteria[s.key] }));

/** Nishon matni joriy tilda; server yangi nishon qo'shsa — serverdagi matn. */
const badgeText = (b: Badge, t: Texts) => t.badges[b.id as BadgeId] ?? { title: b.title, description: b.description };

const tz = () => new Date().getTimezoneOffset();

export function Progress({ loggedIn, onLogin, go }: { loggedIn: boolean; onLogin: () => void; go: (r: string) => void }) {
  const [data, setData] = useState<ProgressData | null>(null);
  const [board, setBoard] = useState<LeaderboardData | null>(null);
  const [error, setError] = useState('');
  const [trend, setTrend] = useState<'writing' | 'speaking'>('writing');
  const t = useT(progressMsg);
  const cm = useT(common);

  useEffect(() => {
    if (!loggedIn) return;
    apiJson<ProgressData>(`/api/progress?tzOffsetMinutes=${tz()}`)
      .then((d) => {
        setData(d);
        // Yozish bo'yicha ma'lumot bo'lmasa, Gapirishni ko'rsatamiz.
        if (d.writingTrend.length === 0 && d.speakingTrend.length > 0) setTrend('speaking');
      })
      .catch((e) => setError(e instanceof Error ? e.message : msg(progressMsg).loadFailed));
    apiJson<LeaderboardData>('/api/leaderboard')
      .then(setBoard)
      .catch(() => undefined);
  }, [loggedIn]);

  if (!loggedIn) {
    return (
      <>
        <PageHeader title={t.title} subtitle={t.guestSubtitle} />
        <div className="card center">
          <div style={{ fontSize: '2.2rem' }}>📈</div>
          <p>{t.guestText}</p>
          <button className="btn btn-primary" onClick={onLogin}>
            {t.guestButton}
          </button>
        </div>
      </>
    );
  }

  if (error) return <p className="error">{error}</p>;
  if (!data) return <p className="muted" style={{ marginTop: 24 }}>{cm.loading}</p>;

  const lv = data.level;
  const into = lv.xp - lv.levelStartXp;
  const size = lv.nextLevelXp - lv.levelStartXp;
  const trendPoints = (trend === 'writing' ? data.writingTrend : data.speakingTrend).map((p) => ({ at: p.at, values: p.scores }));
  const earned = data.badges.filter((b) => b.earned).length;

  return (
    <>
      <PageHeader title={t.title} />

      <div className="card">
        <div className="level-ring">
          <div className="level-num" aria-label={t.levelTitle(lv.level)}>
            {lv.level}
          </div>
          <div style={{ flex: 1 }}>
            <div className="spread">
              <strong>{t.levelTitle(lv.level)}</strong>
              <span className="muted small">{lv.xp} XP</span>
            </div>
            <div className="progress" style={{ margin: '8px 0 4px' }}>
              <div style={{ width: `${Math.round((into / size) * 100)}%`, background: 'var(--primary)' }} />
            </div>
            <div className="muted tiny">
              {t.toNextLevel(lv.nextLevelXp - lv.xp)}
            </div>
          </div>
        </div>
        <p className="muted tiny" style={{ margin: '12px 0 0' }}>
          {t.xpRules}
        </p>
      </div>

      <div className="card">
        <div className="stats">
          <div className="stat">
            <div className="value">{data.totalActivities}</div>
            <div className="label">{t.statActivities}</div>
          </div>
          <div className="stat">
            <div className="value">{data.totalReviews}</div>
            <div className="label">{t.statReviews}</div>
          </div>
          <div className="stat">
            <div className="value">🔥 {data.bestStreak}</div>
            <div className="label">{t.statBestStreak}</div>
          </div>
        </div>
      </div>

      <div className="card">
        <h3>{t.activity}</h3>
        <ActivityCalendar days={data.calendar} />
      </div>

      <div className="card">
        <div className="spread" style={{ marginBottom: 10 }}>
          <h3 style={{ margin: 0 }}>{t.trendTitle}</h3>
          <div className="segmented" role="group" aria-label={t.trendKind}>
            <button aria-pressed={trend === 'writing'} onClick={() => setTrend('writing')}>
              {t.writing}
            </button>
            <button aria-pressed={trend === 'speaking'} onClick={() => setTrend('speaking')}>
              {t.speaking}
            </button>
          </div>
        </div>
        {trendPoints.length >= 2 ? (
          <LineChart
            title={trend === 'writing' ? t.writingScores : t.speakingScores}
            series={series(trend === 'writing' ? WRITING_SERIES : SPEAKING_SERIES, t)}
            points={trendPoints}
          />
        ) : (
          <p className="muted small">
            {t.needTwo}{' '}
            <button className="btn-link" onClick={() => go(`practice/${trend}`)}>
              {t.practiceArrow}
            </button>
          </p>
        )}
      </div>

      <div className="card">
        <div className="spread">
          <h3 style={{ margin: 0 }}>{t.badgesTitle}</h3>
          <span className="muted small">
            {earned}/{data.badges.length}
          </span>
        </div>
        <div className="badges" style={{ marginTop: 12 }}>
          {data.badges.map((b) => {
            const bt = badgeText(b, t);
            return (
              <div key={b.id} className={`badge-card ${b.earned ? '' : 'locked'}`} title={bt.description}>
                <div className="emoji" aria-hidden>
                  {b.emoji}
                </div>
                <div className="title">{bt.title}</div>
                <div className="muted tiny">{b.earned ? t.earned : bt.description}</div>
              </div>
            );
          })}
        </div>
      </div>

      {board && <Leaderboard board={board} onJoin={() => go('profile')} />}

      {data.hardestCards.length > 0 && (
        <div className="card">
          <h3>{t.hardestTitle}</h3>
          <ul className="history">
            {data.hardestCards.map((c, i) => (
              <li key={i} className="spread small">
                <span className="break">
                  <s style={{ color: 'var(--danger)' }}>{c.front}</s> → <strong>{c.back}</strong>
                </span>
                <span className="muted tiny" style={{ whiteSpace: 'nowrap' }}>
                  {t.lapses(c.lapses)}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </>
  );
}

function Leaderboard({ board, onJoin }: { board: LeaderboardData; onJoin: () => void }) {
  const t = useT(progressMsg);
  const medal = (r: number) => (r === 1 ? '🥇' : r === 2 ? '🥈' : r === 3 ? '🥉' : r);
  return (
    <div className="card">
      <div className="spread">
        <h3 style={{ margin: 0 }}>{t.leagueTitle}</h3>
        <span className="muted tiny">{t.sinceMonday}</span>
      </div>
      <p className="muted small" style={{ marginTop: 6 }}>
        {t.thisWeekYou} <strong>{board.myXp} XP</strong>
        {board.myRank ? ` · ${t.rank(board.myRank)}` : ''}
      </p>
      {board.entries.length > 0 ? (
        <ol className="leaderboard">
          {board.entries.map((e, i) => (
            <li key={i} className={e.isMe ? 'me' : ''}>
              <span className="rank">{medal(e.rank)}</span>
              <span className="break">{e.name}</span>
              <span className="xp">{e.xp} XP</span>
            </li>
          ))}
        </ol>
      ) : (
        <p className="muted small">{t.emptyBoard}</p>
      )}
      {!board.optedIn && (
        <div className="card soft small" style={{ marginTop: 10 }}>
          {t.notVisible}{' '}
          <button className="btn-link" onClick={onJoin}>
            {t.join}
          </button>
        </div>
      )}
    </div>
  );
}
