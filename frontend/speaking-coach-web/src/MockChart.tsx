import { useEffect, useRef, useState } from 'react';
import { niceMax } from './charts';
import { useT } from './i18n';
import { mockMsg } from './locales/mock';

export interface ChartData {
  kind: string;
  title: string;
  unit: string;
  categories: string[];
  series: { name: string; values: number[] }[];
}

/**
 * IELTS Task 1 diagrammasi: guruhlangan ustunlar. Bitta o'q, gorizontal
 * setka, har doim legend (≥2 seriya), ustunga olib borilganda aniq qiymat,
 * va jadval ko'rinishi (ekran o'quvchi va kichik ekran uchun).
 * Ranglar styles.css'dagi --series-1..3 — tungi rejimda o'zi almashadi.
 */
export function MockChart({ data }: { data: ChartData }) {
  const t = useT(mockMsg);
  const [table, setTable] = useState(false);
  // Haqiqiy kenglikda chizamiz (viewBox'ni cho'zmasdan) — telefonda ham
  // yozuvlar 11px bo'lib qoladi, kichrayib ketmaydi.
  const ref = useRef<HTMLDivElement>(null);
  const [W, setW] = useState(340);
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const update = () => setW(Math.max(280, el.clientWidth || 340));
    update();
    const ro = new ResizeObserver(update);
    ro.observe(el);
    return () => ro.disconnect();
  }, [table]);

  const H = 280;
  const pad = { top: 16, right: 8, bottom: 44, left: 36 };
  const innerW = W - pad.left - pad.right;
  const innerH = H - pad.top - pad.bottom;
  const max = niceMax(Math.max(...data.series.flatMap((s) => s.values)));
  const ticks = Array.from({ length: 6 }, (_, i) => (max / 5) * i);
  const groupW = innerW / data.categories.length;
  const gap = 2;
  const barW = Math.max(6, Math.min(28, (groupW * 0.72) / data.series.length - gap));
  const y = (v: number) => pad.top + innerH - (v / max) * innerH;

  return (
    <figure className="mock-chart" style={{ margin: '12px 0' }}>
      <figcaption style={{ fontWeight: 600, marginBottom: 6 }}>
        {data.title} <span className="muted small">({data.unit})</span>
      </figcaption>

      <div className="legend small" style={{ display: 'flex', gap: 14, flexWrap: 'wrap', marginBottom: 4 }}>
        {data.series.map((s, i) => (
          <span key={s.name} style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            <span aria-hidden style={{ width: 12, height: 12, borderRadius: 3, background: `var(--series-${i + 1})` }} />
            {s.name}
          </span>
        ))}
      </div>

      {table ? (
        <div className="table-wrap">
          <table className="data">
            <thead>
              <tr>
                <th />
                {data.series.map((s) => (
                  <th key={s.name}>{s.name}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data.categories.map((c, ci) => (
                <tr key={c}>
                  <td>{c}</td>
                  {data.series.map((s) => (
                    <td key={s.name}>
                      {s.values[ci]} {data.unit === '%' ? '%' : ''}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <div ref={ref}>
        <svg viewBox={`0 0 ${W} ${H}`} width={W} height={H} role="img" aria-label={data.title} style={{ display: 'block' }}>
          {ticks.map((v) => (
            <g key={v}>
              <line x1={pad.left} x2={W - pad.right} y1={y(v)} y2={y(v)} stroke="var(--border)" strokeWidth={1} />
              <text x={pad.left - 6} y={y(v) + 4} textAnchor="end" fontSize={11} fill="var(--muted)">
                {Number.isInteger(v) ? v : v.toFixed(1)}
              </text>
            </g>
          ))}
          {data.categories.map((c, ci) => {
            const groupX = pad.left + ci * groupW;
            const totalW = data.series.length * (barW + gap) - gap;
            const startX = groupX + (groupW - totalW) / 2;
            return (
              <g key={c}>
                {data.series.map((s, si) => {
                  const v = s.values[ci];
                  const top = y(v);
                  const h = Math.max(1, pad.top + innerH - top);
                  const x = startX + si * (barW + gap);
                  const r = Math.min(4, barW / 2, h);
                  // Faqat yuqori burchaklar yumaloq — ustun asosi o'qqa tekis turadi.
                  const d = `M${x},${top + h} V${top + r} Q${x},${top} ${x + r},${top} H${x + barW - r} Q${x + barW},${top} ${x + barW},${top + r} V${top + h} Z`;
                  return (
                    <path key={s.name} d={d} fill={`var(--series-${si + 1})`}>
                      <title>{`${s.name} · ${c}: ${v} ${data.unit}`}</title>
                    </path>
                  );
                })}
                <text x={groupX + groupW / 2} y={H - pad.bottom + 16} textAnchor="middle" fontSize={11} fill="var(--text)">
                  {wrapLabel(c, groupW).map((line, li) => (
                    <tspan key={li} x={groupX + groupW / 2} dy={li === 0 ? 0 : 13}>
                      {line}
                    </tspan>
                  ))}
                </text>
              </g>
            );
          })}
          <line x1={pad.left} x2={W - pad.right} y1={pad.top + innerH} y2={pad.top + innerH} stroke="var(--muted)" strokeWidth={1} />
        </svg>
        </div>
      )}

      <button type="button" className="btn-link small" onClick={() => setTable((v) => !v)}>
        {table ? t.hideTable : t.showTable}
      </button>
    </figure>
  );
}

/** Uzun toifa nomini ustun kengligiga sig'adigan 2 qatorga bo'ladi (~6px/harf). */
export function wrapLabel(label: string, width: number): string[] {
  const maxChars = Math.max(4, Math.floor(width / 6.2));
  if (label.length <= maxChars) return [label];
  const words = label.split(' ');
  const lines: string[] = [''];
  for (const w of words) {
    const cur = lines[lines.length - 1];
    if (cur && (cur + ' ' + w).length > maxChars && lines.length < 2) lines.push(w);
    else lines[lines.length - 1] = cur ? `${cur} ${w}` : w;
  }
  return lines;
}
