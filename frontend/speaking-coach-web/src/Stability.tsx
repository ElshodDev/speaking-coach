// LLM baholash barqarorligini o'lchash — Speaking va Writing ikkalasi ham
// shu fayldagi yordamchilarni ishlatadi.
//
// G'oya: bitta va aynan bir xil kirishni (audio yoki matn) Gemini'ga bir
// necha marta yuboramiz va har safar kelgan ballarni solishtiramiz. Agar
// model "adolatli hakam" bo'lsa, ballar deyarli bir xil chiqishi kerak.
// Katta tarqalish esa — bitta urinishdagi ballga to'liq ishonib bo'lmasligini
// bildiradi. temperature=0.2 past bo'lsa ham, LLM 100% deterministik emas.

// Nechta marta yuborish — ko'proq = aniqroq statistika, lekin bepul tier
// limitiga tezroq yetadi va uzoqroq kutiladi (har biri 5-15 soniya).
export const STABILITY_RUNS = 5;

export interface DimensionStats {
  label: string;
  scores: number[];
  min: number;
  max: number;
  avg: number;
  range: number; // max - min
  stdDev: number; // standart og'ish — ballar o'rtachadan qanchalik uzoqlashadi
}

export function computeStats(label: string, scores: number[]): DimensionStats {
  const min = Math.min(...scores);
  const max = Math.max(...scores);
  const avg = scores.reduce((sum, s) => sum + s, 0) / scores.length;
  const variance = scores.reduce((sum, s) => sum + (s - avg) ** 2, 0) / scores.length;
  return { label, scores, min, max, avg, range: max - min, stdDev: Math.sqrt(variance) };
}

/**
 * fn'ni ketma-ket (parallel emas!) n marta chaqiradi. Parallel yuborsak,
 * Gemini bepul tier'ining daqiqalik limitiga (429) darhol urilamiz —
 * ketma-ket yuborish sekinroq, lekin ishonchliroq. Bitta urinish xato
 * bersa, qolganlari davom etadi — natijada nechta muvaffaqiyatli bo'lgani
 * ko'rsatiladi.
 */
export async function runSequentially<T>(
  n: number,
  fn: () => Promise<T>,
  onProgress: (done: number, failed: number) => void,
): Promise<{ results: T[]; failed: number }> {
  const results: T[] = [];
  let failed = 0;
  for (let i = 0; i < n; i++) {
    try {
      results.push(await fn());
    } catch {
      failed++;
    }
    onProgress(i + 1, failed);
  }
  return { results, failed };
}

// Tarqalish (max - min) bo'yicha oddiy baho. Chegara qiymatlari ixtiyoriy
// tanlangan — 100 ballik shkalada 5 ballgacha farq amalda sezilmaydi,
// 15 dan ortig'i esa foydalanuvchi uchun sezilarli (masalan 65 va 82).
function verdict(range: number): { text: string; color: string } {
  if (range <= 5) return { text: 'Barqaror', color: '#16a34a' };
  if (range <= 15) return { text: "O'rtacha", color: '#ca8a04' };
  return { text: 'Beqaror', color: '#dc2626' };
}

export function StabilityTable({ stats, failed }: { stats: DimensionStats[]; failed: number }) {
  const cell = { padding: '0.4rem 0.5rem', borderBottom: '1px solid #e5e7eb', textAlign: 'left' as const };

  return (
    <div style={{ marginTop: '1.5rem', padding: '1rem', border: '1px solid #e5e7eb', borderRadius: '0.5rem' }}>
      <h3 style={{ marginTop: 0 }}>Barqarorlik natijasi ({stats[0]?.scores.length ?? 0} ta urinish)</h3>
      {failed > 0 && (
        <p style={{ color: '#dc2626', fontSize: '0.85rem' }}>
          {failed} ta urinish xato bilan tugadi (masalan Gemini band edi) — statistika faqat muvaffaqiyatlilar bo'yicha.
        </p>
      )}
      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
          <thead>
            <tr>
              <th style={cell}>Mezon</th>
              <th style={cell}>Ballar</th>
              <th style={cell}>O'rtacha</th>
              <th style={cell}>Farq</th>
              <th style={cell}>Baho</th>
            </tr>
          </thead>
          <tbody>
            {stats.map((s) => {
              const v = verdict(s.range);
              return (
                <tr key={s.label}>
                  <td style={cell}>{s.label}</td>
                  <td style={cell}>{s.scores.join(', ')}</td>
                  <td style={cell}>{s.avg.toFixed(1)}</td>
                  <td style={cell}>
                    {s.min}–{s.max} ({s.range})
                  </td>
                  <td style={{ ...cell, color: v.color, fontWeight: 600 }}>{v.text}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <p style={{ color: '#6b7280', fontSize: '0.8rem', marginBottom: 0 }}>
        Bu urinishlar tarixga saqlanmadi (<code>?save=false</code>). Farq ≤5 — barqaror, 6–15 — o'rtacha, &gt;15 —
        beqaror.
      </p>
    </div>
  );
}
