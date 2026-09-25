import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { ActivityCalendar, LineChart, type CalendarDay, type LineSeries } from './charts';
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
const WRITING_SERIES: LineSeries[] = [
  { key: 'taskAchievement', label: 'Vazifa', color: 'var(--series-1)' },
  { key: 'coherenceCohesion', label: "Bog'lanish", color: 'var(--series-2)' },
  { key: 'grammar', label: 'Grammatika', color: 'var(--series-3)' },
  { key: 'vocabulary', label: "Lug'at", color: 'var(--series-4)' },
];
const SPEAKING_SERIES: LineSeries[] = [
  { key: 'fluency', label: 'Ravonlik', color: 'var(--series-1)' },
  { key: 'grammar', label: 'Grammatika', color: 'var(--series-3)' },
  { key: 'vocabulary', label: "Lug'at", color: 'var(--series-4)' },
];

const tz = () => new Date().getTimezoneOffset();

export function Progress({ loggedIn, onLogin, go }: { loggedIn: boolean; onLogin: () => void; go: (r: string) => void }) {
  const [data, setData] = useState<ProgressData | null>(null);
  const [board, setBoard] = useState<LeaderboardData | null>(null);
  const [error, setError] = useState('');
  const [trend, setTrend] = useState<'writing' | 'speaking'>('writing');

  useEffect(() => {
    if (!loggedIn) return;
    apiJson<ProgressData>(`/api/progress?tzOffsetMinutes=${tz()}`)
      .then((d) => {
        setData(d);
        // Yozish bo'yicha ma'lumot bo'lmasa, Gapirishni ko'rsatamiz.
        if (d.writingTrend.length === 0 && d.speakingTrend.length > 0) setTrend('speaking');
      })
      .catch((e) => setError(e instanceof Error ? e.message : "Yuklab bo'lmadi"));
    apiJson<LeaderboardData>('/api/leaderboard')
      .then(setBoard)
      .catch(() => undefined);
  }, [loggedIn]);

  if (!loggedIn) {
    return (
      <>
        <PageHeader title="Natijalar" subtitle="Daraja, nishonlar, faollik kalendari va haftalik musobaqa." />
        <div className="card center">
          <div style={{ fontSize: '2.2rem' }}>📈</div>
          <p>Natijalaringizni kuzatish va musobaqada qatnashish uchun hisob oching.</p>
          <button className="btn btn-primary" onClick={onLogin}>
            Kirish yoki hisob ochish
          </button>
        </div>
      </>
    );
  }

  if (error) return <p className="error">{error}</p>;
  if (!data) return <p className="muted" style={{ marginTop: 24 }}>Yuklanmoqda...</p>;

  const lv = data.level;
  const into = lv.xp - lv.levelStartXp;
  const size = lv.nextLevelXp - lv.levelStartXp;
  const trendPoints = (trend === 'writing' ? data.writingTrend : data.speakingTrend).map((p) => ({ at: p.at, values: p.scores }));
  const earned = data.badges.filter((b) => b.earned).length;

  return (
    <>
      <PageHeader title="Natijalar" />

      <div className="card">
        <div className="level-ring">
          <div className="level-num" aria-label={`${lv.level}-daraja`}>
            {lv.level}
          </div>
          <div style={{ flex: 1 }}>
            <div className="spread">
              <strong>{lv.level}-daraja</strong>
              <span className="muted small">{lv.xp} XP</span>
            </div>
            <div className="progress" style={{ margin: '8px 0 4px' }}>
              <div style={{ width: `${Math.round((into / size) * 100)}%`, background: 'var(--primary)' }} />
            </div>
            <div className="muted tiny">
              Keyingi darajagacha {lv.nextLevelXp - lv.xp} XP
            </div>
          </div>
        </div>
        <p className="muted tiny" style={{ margin: '12px 0 0' }}>
          XP: Gapirish/Yozish — 20, O'qish/Tinglash — 15 + har to'g'ri javobga 5, har takrorlash — 2.
        </p>
      </div>

      <div className="card">
        <div className="stats">
          <div className="stat">
            <div className="value">{data.totalActivities}</div>
            <div className="label">mashq</div>
          </div>
          <div className="stat">
            <div className="value">{data.totalReviews}</div>
            <div className="label">takrorlash</div>
          </div>
          <div className="stat">
            <div className="value">🔥 {data.bestStreak}</div>
            <div className="label">eng uzun streak</div>
          </div>
        </div>
      </div>

      <div className="card">
        <h3>Faollik</h3>
        <ActivityCalendar days={data.calendar} />
      </div>

      <div className="card">
        <div className="spread" style={{ marginBottom: 10 }}>
          <h3 style={{ margin: 0 }}>Ballar dinamikasi</h3>
          <div className="segmented" role="group" aria-label="Mashq turi">
            <button aria-pressed={trend === 'writing'} onClick={() => setTrend('writing')}>
              Yozish
            </button>
            <button aria-pressed={trend === 'speaking'} onClick={() => setTrend('speaking')}>
              Gapirish
            </button>
          </div>
        </div>
        {trendPoints.length >= 2 ? (
          <LineChart
            title={trend === 'writing' ? 'Yozish ballari' : 'Gapirish ballari'}
            series={trend === 'writing' ? WRITING_SERIES : SPEAKING_SERIES}
            points={trendPoints}
          />
        ) : (
          <p className="muted small">
            Grafik uchun kamida 2 ta urinish kerak.{' '}
            <button className="btn-link" onClick={() => go(`practice/${trend}`)}>
              Mashq qilish →
            </button>
          </p>
        )}
      </div>

      <div className="card">
        <div className="spread">
          <h3 style={{ margin: 0 }}>Nishonlar</h3>
          <span className="muted small">
            {earned}/{data.badges.length}
          </span>
        </div>
        <div className="badges" style={{ marginTop: 12 }}>
          {data.badges.map((b) => (
            <div key={b.id} className={`badge-card ${b.earned ? '' : 'locked'}`} title={b.description}>
              <div className="emoji" aria-hidden>
                {b.emoji}
              </div>
              <div className="title">{b.title}</div>
              <div className="muted tiny">{b.earned ? 'Olindi' : b.description}</div>
            </div>
          ))}
        </div>
      </div>

      {board && <Leaderboard board={board} onJoin={() => go('profile')} />}

      {data.hardestCards.length > 0 && (
        <div className="card">
          <h3>Eng qiyin kartalaringiz</h3>
          <ul className="history">
            {data.hardestCards.map((c, i) => (
              <li key={i} className="spread small">
                <span className="break">
                  <s style={{ color: 'var(--danger)' }}>{c.front}</s> → <strong>{c.back}</strong>
                </span>
                <span className="muted tiny" style={{ whiteSpace: 'nowrap' }}>
                  {c.lapses} marta unutilgan
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
  const medal = (r: number) => (r === 1 ? '🥇' : r === 2 ? '🥈' : r === 3 ? '🥉' : r);
  return (
    <div className="card">
      <div className="spread">
        <h3 style={{ margin: 0 }}>🏆 Haftalik musobaqa</h3>
        <span className="muted tiny">dushanbadan beri</span>
      </div>
      <p className="muted small" style={{ marginTop: 6 }}>
        Bu hafta siz: <strong>{board.myXp} XP</strong>
        {board.myRank ? ` · ${board.myRank}-o'rin` : ''}
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
        <p className="muted small">Bu hafta hali hech kim ball to'plamagan — birinchi bo'ling!</p>
      )}
      {!board.optedIn && (
        <div className="card soft small" style={{ marginTop: 10 }}>
          Siz reytingda ko'rinmayapsiz. Qatnashish ixtiyoriy — faqat taxallusingiz ko'rinadi, email emas.{' '}
          <button className="btn-link" onClick={onJoin}>
            Qo'shilish →
          </button>
        </div>
      )}
    </div>
  );
}
