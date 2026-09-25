import type { ReviewStats } from './Review';

export type ExerciseKind = 'speaking' | 'writing' | 'reading' | 'listening';

export const EXERCISES: { kind: ExerciseKind; emoji: string; title: string; blurb: string }[] = [
  { kind: 'speaking', emoji: '🎙', title: 'Gapirish', blurb: 'Mavzuda gapiring — ravonlik, grammatika, lug\'at bahosi' },
  { kind: 'writing', emoji: '✍️', title: 'Yozish', blurb: 'Insho yozing — IELTS uslubidagi baho va tuzatishlar' },
  { kind: 'reading', emoji: '📖', title: "O'qish", blurb: 'Har safar yangi matn va 4 ta savol' },
  { kind: 'listening', emoji: '🎧', title: 'Tinglash', blurb: 'Nutqni tinglang va savollarga javob bering' },
];

export function ExerciseTiles({ onOpen }: { onOpen: (kind: ExerciseKind) => void }) {
  return (
    <div className="tiles">
      {EXERCISES.map((e) => (
        <button key={e.kind} className="tile" onClick={() => onOpen(e.kind)}>
          <span className="emoji" aria-hidden>
            {e.emoji}
          </span>
          <strong>{e.title}</strong>
          <span className="muted">{e.blurb}</span>
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
  const open = (kind: ExerciseKind) => go(`practice/${kind}`);

  if (!email) {
    return (
      <>
        <div className="card hero">
          <h1>Ingliz tilini har kuni 10 daqiqada</h1>
          <p>
            Gapiring, yozing, o'qing va tinglang. Sun'iy intellekt xatolaringizni topadi, ilova esa ularni esda
            qolguncha qaytarib turadi.
          </p>
          <div className="row">
            <button className="btn btn-primary" onClick={() => open('speaking')}>
              Sinab ko'rish
            </button>
            <button className="btn-link" style={{ color: '#fff' }} onClick={() => go('profile')}>
              Hisob ochish →
            </button>
          </div>
        </div>

        <div className="card">
          <h2>Qanday ishlaydi</h2>
          <div className="steps">
            <div className="step">
              <div>
                <strong>Mashq qiling.</strong>{' '}
                <span className="muted">Gapiring, insho yozing, matn o'qing yoki nutq tinglang.</span>
              </div>
            </div>
            <div className="step">
              <div>
                <strong>AI baholaydi.</strong>{' '}
                <span className="muted">Ball, aniq tuzatishlar va keyingi qadam bo'yicha maslahat.</span>
              </div>
            </div>
            <div className="step">
              <div>
                <strong>Takrorlab eslab qoling.</strong>{' '}
                <span className="muted">
                  Xatolaringiz kartalarga aylanadi va unutish arafasida qaytadi — yo'lda quloqchin bilan ham.
                </span>
              </div>
            </div>
          </div>
        </div>

        <h2 style={{ marginTop: 24 }}>Mashqlar</h2>
        <ExerciseTiles onOpen={open} />
      </>
    );
  }

  const name = email.split('@')[0];
  const goal = stats ? Math.min(100, Math.round((stats.reviewedToday / stats.dailyGoal) * 100)) : 0;

  return (
    <>
      <div className="page-header">
        <h1>Salom, {name}! 👋</h1>
        <p>{stats && stats.reviewedToday >= stats.dailyGoal ? 'Bugungi maqsad bajarildi — zo\'r!' : 'Bugun ham ozgina mashq qilamiz.'}</p>
      </div>

      {stats && (
        <div className="card">
          <div className="stats">
            <div className="stat">
              <div className="value">🔥 {stats.streakDays}</div>
              <div className="label">kun ketma-ket</div>
            </div>
            <div className="stat">
              <div className="value">
                {Math.min(stats.reviewedToday, stats.dailyGoal)}/{stats.dailyGoal}
              </div>
              <div className="label">bugungi maqsad</div>
            </div>
            <div className="stat">
              <div className="value">{stats.total}</div>
              <div className="label">jami karta</div>
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
            <strong>{stats.due} ta karta kutyapti</strong>
            <div className="muted small">Eslash vaqti keldi — 2-3 daqiqa yetadi.</div>
          </div>
          <button className="btn btn-primary" onClick={() => go('review')}>
            Takrorlash
          </button>
        </div>
      ) : (
        stats && (
          <div className="card soft small">
            ✅ Hozircha takrorlanadigan karta yo'q. Mashq qiling — xatolaringiz shu yerga tushadi.
          </div>
        )
      )}

      <button className="card cta" style={{ width: '100%', font: 'inherit', color: 'inherit', textAlign: 'left', cursor: 'pointer' }} onClick={() => go('progress')}>
        <div>
          <strong>🏆 Haftalik musobaqa va nishonlar</strong>
          <div className="muted small">Darajangiz, XP va faollik kalendaringiz</div>
        </div>
        <span aria-hidden>→</span>
      </button>

      <h2 style={{ marginTop: 24 }}>Mashqlar</h2>
      <ExerciseTiles onOpen={open} />
    </>
  );
}
