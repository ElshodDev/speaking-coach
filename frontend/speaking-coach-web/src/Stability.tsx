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
function verdict(range: number): { text: string; cls: string } {
  if (range <= 5) return { text: 'Barqaror', cls: 'txt-great' };
  if (range <= 15) return { text: "O'rtacha", cls: 'txt-mid' };
  return { text: 'Beqaror', cls: 'txt-low' };
}

export function StabilityTable({ stats, failed }: { stats: DimensionStats[]; failed: number }) {
  return (
    <div className="card">
      <h3>Barqarorlik natijasi ({stats[0]?.scores.length ?? 0} ta urinish)</h3>
      {failed > 0 && (
        <p className="error small">
          {failed} ta urinish xato bilan tugadi (masalan Gemini band edi) — statistika faqat muvaffaqiyatlilar bo'yicha.
        </p>
      )}
      <div className="table-wrap">
        <table className="data">
          <thead>
            <tr>
              <th>Mezon</th>
              <th>Ballar</th>
              <th>O'rtacha</th>
              <th>Farq</th>
              <th>Baho</th>
            </tr>
          </thead>
          <tbody>
            {stats.map((s) => {
              const v = verdict(s.range);
              return (
                <tr key={s.label}>
                  <td>{s.label}</td>
                  <td>{s.scores.join(', ')}</td>
                  <td>{s.avg.toFixed(1)}</td>
                  <td>
                    {s.min}–{s.max} ({s.range})
                  </td>
                  <td className={v.cls} style={{ fontWeight: 600 }}>
                    {v.text}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <p className="muted tiny" style={{ marginTop: 8, marginBottom: 0 }}>
        Bu urinishlar tarixga saqlanmadi (<code>?save=false</code>). Farq ≤5 — barqaror, 6–15 — o'rtacha, &gt;15 — beqaror.
      </p>
    </div>
  );
}
