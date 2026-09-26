import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { BarChart } from './charts';
import { common, localeOf, msg, useLang, useT } from './i18n';
import { adminMsg } from './locales/admin';
import { PageHeader } from './ui';

interface AdminOverview {
  totalUsers: number;
  verifiedUsers: number;
  newUsers7d: number;
  activeToday: number;
  active7d: number;
  active30d: number;
  activitiesByType: Record<string, number>;
  activitiesByType7d: Record<string, number>;
  totalReviews: number;
  reviews7d: number;
  totalCards: number;
  daily: { date: string; signups: number; activeUsers: number; activities: number; reviews: number }[];
  recentUsers: { email: string; createdAtUtc: string; lastActiveAtUtc: string | null; activities: number; level: string; verified: boolean }[];
}

const TYPES = ['Speaking', 'Writing', 'Reading', 'Listening'] as const;

/**
 * Admin panel — faqat serverdagi Admin:Emails ro'yxatidagi foydalanuvchiga.
 * Faqat umumiy sonlar va niqoblangan emaillar: kimningdir inshosi yoki
 * kartalari bu yerda ko'rinmaydi.
 */
export function Admin() {
  const [data, setData] = useState<AdminOverview | null>(null);
  const [error, setError] = useState('');
  const t = useT(adminMsg);
  const cm = useT(common);
  const locale = localeOf(useLang().lang);

  useEffect(() => {
    apiJson<AdminOverview>(`/api/admin/overview?tzOffsetMinutes=${new Date().getTimezoneOffset()}`)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : msg(adminMsg).loadFailed));
  }, []);

  if (error) {
    return (
      <>
        <PageHeader title={t.title} />
        <p className="error">{error}</p>
      </>
    );
  }
  if (!data) return <p className="muted" style={{ marginTop: 24 }}>{cm.loading}</p>;

  const tiles = [
    { label: t.totalUsers, value: data.totalUsers, sub: t.newThisWeek(data.newUsers7d) },
    { label: t.verifiedUsers, value: data.verifiedUsers, sub: t.verifiedShare(data.totalUsers ? Math.round((data.verifiedUsers / data.totalUsers) * 100) : 0) },
    { label: t.activeToday, value: data.activeToday },
    { label: t.active7d, value: data.active7d },
    { label: t.active30d, value: data.active30d },
    { label: t.reviews7d, value: data.reviews7d, sub: t.totalOf(data.totalReviews.toLocaleString(locale)) },
    { label: t.totalCards, value: data.totalCards },
  ];

  return (
    <>
      <PageHeader title={t.title} subtitle={t.subtitle} />

      <div className="tiles" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))' }}>
        {tiles.map((tile) => (
          <div key={tile.label} className="card" style={{ margin: 0 }}>
            <div className="muted tiny">{tile.label}</div>
            <div style={{ fontSize: '1.6rem', fontWeight: 700 }}>{tile.value.toLocaleString(locale)}</div>
            {tile.sub && <div className="muted tiny">{tile.sub}</div>}
          </div>
        ))}
      </div>

      <div className="card">
        <h3>{t.activeUsersTitle}</h3>
        <BarChart label={t.activeUsersLabel} data={data.daily.map((d) => ({ date: d.date, value: d.activeUsers }))} />
      </div>

      <div className="card">
        <h3>{t.activitiesTitle}</h3>
        <BarChart label={t.activitiesLabel} data={data.daily.map((d) => ({ date: d.date, value: d.activities }))} />
      </div>

      <div className="card">
        <h3>{t.signupsTitle}</h3>
        <BarChart label={t.signupsLabel} data={data.daily.map((d) => ({ date: d.date, value: d.signups }))} />
      </div>

      <div className="card">
        <h3>{t.typesTitle}</h3>
        <div className="table-wrap">
          <table className="data">
            <thead>
              <tr>
                <th>{t.colType}</th>
                <th>{t.col7d}</th>
                <th>{t.colTotal}</th>
              </tr>
            </thead>
            <tbody>
              {TYPES.map((k) => (
                <tr key={k}>
                  <td>{t.types[k]}</td>
                  <td>{data.activitiesByType7d[k] ?? 0}</td>
                  <td>{data.activitiesByType[k] ?? 0}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card">
        <h3>{t.recentTitle}</h3>
        <div className="table-wrap">
          <table className="data">
            <thead>
              <tr>
                <th>{t.colUser}</th>
                <th>{t.colLevel}</th>
                <th>{t.colActivities}</th>
                <th>{t.colLastActive}</th>
              </tr>
            </thead>
            <tbody>
              {data.recentUsers.map((u, i) => (
                <tr key={i}>
                  <td>
                    {u.email} <span title={u.verified ? t.verified : t.unverified}>{u.verified ? '✅' : '⏳'}</span>
                    <div className="muted tiny">{new Date(u.createdAtUtc).toLocaleDateString(locale)}</div>
                  </td>
                  <td>{u.level}</td>
                  <td>{u.activities}</td>
                  <td>{u.lastActiveAtUtc ? new Date(u.lastActiveAtUtc).toLocaleString(locale) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
