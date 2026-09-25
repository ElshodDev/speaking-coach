import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { BarChart } from './charts';
import { PageHeader } from './ui';

interface AdminOverview {
  totalUsers: number;
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
  recentUsers: { email: string; createdAtUtc: string; lastActiveAtUtc: string | null; activities: number; level: string }[];
}

const TYPE_LABEL: Record<string, string> = {
  Speaking: '🎙 Gapirish',
  Writing: '✍️ Yozish',
  Reading: "📖 O'qish",
  Listening: '🎧 Tinglash',
};

/**
 * Admin panel — faqat serverdagi Admin:Emails ro'yxatidagi foydalanuvchiga.
 * Faqat umumiy sonlar va niqoblangan emaillar: kimningdir inshosi yoki
 * kartalari bu yerda ko'rinmaydi.
 */
export function Admin() {
  const [data, setData] = useState<AdminOverview | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    apiJson<AdminOverview>(`/api/admin/overview?tzOffsetMinutes=${new Date().getTimezoneOffset()}`)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : "Yuklab bo'lmadi"));
  }, []);

  if (error) {
    return (
      <>
        <PageHeader title="Admin panel" />
        <p className="error">{error}</p>
      </>
    );
  }
  if (!data) return <p className="muted" style={{ marginTop: 24 }}>Yuklanmoqda...</p>;

  const tiles = [
    { label: 'Jami foydalanuvchi', value: data.totalUsers, sub: `+${data.newUsers7d} shu hafta` },
    { label: 'Bugun faol', value: data.activeToday },
    { label: '7 kunda faol', value: data.active7d },
    { label: '30 kunda faol', value: data.active30d },
    { label: 'Takrorlashlar (7 kun)', value: data.reviews7d, sub: `jami ${data.totalReviews}` },
    { label: 'Jami kartalar', value: data.totalCards },
  ];

  return (
    <>
      <PageHeader title="Admin panel" subtitle="Faqat umumiy statistika — shaxsiy matnlar ko'rsatilmaydi." />

      <div className="tiles" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))' }}>
        {tiles.map((t) => (
          <div key={t.label} className="card" style={{ margin: 0 }}>
            <div className="muted tiny">{t.label}</div>
            <div style={{ fontSize: '1.6rem', fontWeight: 700 }}>{t.value.toLocaleString()}</div>
            {t.sub && <div className="muted tiny">{t.sub}</div>}
          </div>
        ))}
      </div>

      <div className="card">
        <h3>Faol foydalanuvchilar (kunlik)</h3>
        <BarChart label="faol foydalanuvchi" data={data.daily.map((d) => ({ date: d.date, value: d.activeUsers }))} />
      </div>

      <div className="card">
        <h3>Bajarilgan mashqlar (kunlik)</h3>
        <BarChart label="mashq" data={data.daily.map((d) => ({ date: d.date, value: d.activities }))} />
      </div>

      <div className="card">
        <h3>Yangi ro'yxatdan o'tishlar (kunlik)</h3>
        <BarChart label="yangi foydalanuvchi" data={data.daily.map((d) => ({ date: d.date, value: d.signups }))} />
      </div>

      <div className="card">
        <h3>Mashq turlari</h3>
        <div className="table-wrap">
          <table className="data">
            <thead>
              <tr>
                <th>Tur</th>
                <th>7 kun</th>
                <th>Jami</th>
              </tr>
            </thead>
            <tbody>
              {Object.keys(TYPE_LABEL).map((k) => (
                <tr key={k}>
                  <td>{TYPE_LABEL[k]}</td>
                  <td>{data.activitiesByType7d[k] ?? 0}</td>
                  <td>{data.activitiesByType[k] ?? 0}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card">
        <h3>Oxirgi ro'yxatdan o'tganlar</h3>
        <div className="table-wrap">
          <table className="data">
            <thead>
              <tr>
                <th>Foydalanuvchi</th>
                <th>Daraja</th>
                <th>Mashq</th>
                <th>Oxirgi faollik</th>
              </tr>
            </thead>
            <tbody>
              {data.recentUsers.map((u, i) => (
                <tr key={i}>
                  <td>
                    {u.email}
                    <div className="muted tiny">{new Date(u.createdAtUtc).toLocaleDateString()}</div>
                  </td>
                  <td>{u.level}</td>
                  <td>{u.activities}</td>
                  <td>{u.lastActiveAtUtc ? new Date(u.lastActiveAtUtc).toLocaleString() : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
