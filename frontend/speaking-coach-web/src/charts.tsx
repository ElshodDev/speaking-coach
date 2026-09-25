// Kichik, tashqi kutubxonasiz grafiklar (SVG). Ranglar styles.css'dagi
// --series-* va --heat-* o'zgaruvchilaridan — tungi rejimda o'zi almashadi.
// Qoidalar: bitta o'q (hech qachon ikki xil shkala), 2px chiziqlar,
// ≥2 seriyada doim legend, hover/klaviatura bilan tooltip, jadval ko'rinishi.
import { useEffect, useRef, useState, type KeyboardEvent, type ReactNode, type RefObject } from 'react';
import { localeOf, useLang, useT } from './i18n';
import { chartsMsg } from './locales/charts';

function useWidth<T extends HTMLElement>(): [RefObject<T>, number] {
  const ref = useRef<T>(null);
  const [width, setWidth] = useState(320);
  useEffect(() => {
    if (!ref.current) return;
    const el = ref.current;
    setWidth(el.clientWidth || 320);
    const ro = new ResizeObserver(() => setWidth(el.clientWidth || 320));
    ro.observe(el);
    return () => ro.disconnect();
  }, []);
  return [ref, width];
}

/** Y o'qi uchun "chiroyli" yuqori chegara: 1, 2, 5 × 10ⁿ. */
export function niceMax(v: number): number {
  if (v <= 0) return 1;
  const pow = 10 ** Math.floor(Math.log10(v));
  for (const m of [1, 2, 5, 10]) if (m * pow >= v) return m * pow;
  return 10 * pow;
}

/**
 * "2026-09-25" (faqat sana) — MAHALLIY kun sifatida o'qiladi. new Date("2026-09-25")
 * uni UTC yarim tuni deb oladi va g'arbiy vaqt zonalarida kechagi kunni ko'rsatadi.
 */
export function parseDay(value: string): Date {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  return m ? new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3])) : new Date(value);
}

export const shortDate = (value: string) => {
  if (!value) return '';
  const d = parseDay(value);
  return `${d.getDate()}.${String(d.getMonth() + 1).padStart(2, '0')}`;
};

/**
 * side='above' — nuqta ustida (ustun va kataklar uchun);
 * 'left' / 'right' — chiziqli grafikda krestdan chapda yoki o'ngda, grafik
 * ichida: shunda tepadagi sarlavha va tugmalarni yopib qo'ymaydi.
 */
function Tooltip({ x, y, side = 'above', children }: { x: number; y: number; side?: 'above' | 'left' | 'right'; children: ReactNode }) {
  const transform = side === 'above' ? undefined : side === 'left' ? 'translate(calc(-100% - 12px), 0)' : 'translate(12px, 0)';
  return (
    <div className="chart-tooltip" style={{ left: x, top: y, ...(transform ? { transform } : {}) }} role="status">
      {children}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Chiziqli grafik: bir nechta mezonning vaqt davomidagi bali (0–100, bitta o'q).

export interface LineSeries {
  key: string;
  label: string;
  color: string; // CSS rang, masalan 'var(--series-1)'
}

export interface LinePoint {
  at: string;
  values: Record<string, number>;
}

export function LineChart({ series, points, title }: { series: LineSeries[]; points: LinePoint[]; title: string }) {
  const [ref, width] = useWidth<HTMLDivElement>();
  const [active, setActive] = useState<number | null>(null);
  const [showTable, setShowTable] = useState(false);
  const t = useT(chartsMsg);
  const locale = localeOf(useLang().lang);

  const height = 200;
  const m = { top: 12, right: 14, bottom: 24, left: 30 };
  const w = Math.max(160, width - m.left - m.right);
  const h = height - m.top - m.bottom;
  const x = (i: number) => m.left + (points.length === 1 ? w / 2 : (i / (points.length - 1)) * w);
  const y = (v: number) => m.top + h - (v / 100) * h;

  function onMove(clientX: number, rect: DOMRect) {
    const px = clientX - rect.left;
    let best = 0;
    for (let i = 1; i < points.length; i++) if (Math.abs(x(i) - px) < Math.abs(x(best) - px)) best = i;
    setActive(best);
  }

  function onKey(e: KeyboardEvent<SVGSVGElement>) {
    if (e.key === 'ArrowRight') setActive((a) => Math.min(points.length - 1, (a ?? -1) + 1));
    else if (e.key === 'ArrowLeft') setActive((a) => Math.max(0, (a ?? points.length) - 1));
    else if (e.key === 'Escape') setActive(null);
    else return;
    e.preventDefault();
  }

  return (
    <div>
      <div className="legend" aria-hidden>
        {series.map((s) => (
          <span key={s.key}>
            <i className="line-key" style={{ background: s.color }} />
            {s.label}
          </span>
        ))}
      </div>
      <div className="chart" ref={ref}>
        <svg
          height={height}
          role="img"
          aria-label={t.lineAria(title, points.length)}
          tabIndex={0}
          onKeyDown={onKey}
          onBlur={() => setActive(null)}
          onPointerMove={(e) => onMove(e.clientX, e.currentTarget.getBoundingClientRect())}
          onPointerLeave={() => setActive(null)}
        >
          {[0, 50, 100].map((v) => (
            <g key={v}>
              <line x1={m.left} x2={m.left + w} y1={y(v)} y2={y(v)} stroke={v === 0 ? 'var(--axis)' : 'var(--grid)'} strokeWidth={1} />
              <text className="tick" x={m.left - 6} y={y(v) + 4} textAnchor="end">
                {v}
              </text>
            </g>
          ))}
          <text className="tick" x={x(0)} y={height - 6} textAnchor={points.length === 1 ? 'middle' : 'start'}>
            {shortDate(points[0].at)}
          </text>
          {points.length > 1 && (
            <text className="tick" x={x(points.length - 1)} y={height - 6} textAnchor="end">
              {shortDate(points[points.length - 1].at)}
            </text>
          )}

          {active !== null && <line x1={x(active)} x2={x(active)} y1={m.top} y2={m.top + h} stroke="var(--axis)" strokeWidth={1} />}

          {series.map((s) => (
            <g key={s.key}>
              <polyline
                fill="none"
                stroke={s.color}
                strokeWidth={2}
                strokeLinejoin="round"
                strokeLinecap="round"
                points={points.map((p, i) => `${x(i)},${y(p.values[s.key] ?? 0)}`).join(' ')}
              />
              {/* Oxirgi nuqta — joriy holat; faol nuqtada hamma seriyalar belgilanadi. */}
              {[points.length - 1, ...(active !== null && active !== points.length - 1 ? [active] : [])].map((i) => (
                <circle key={i} cx={x(i)} cy={y(points[i].values[s.key] ?? 0)} r={4} fill={s.color} stroke="var(--surface)" strokeWidth={2} />
              ))}
            </g>
          ))}
        </svg>
        {active !== null && (
          <Tooltip x={x(active)} y={m.top} side={x(active) > width / 2 ? 'left' : 'right'}>
            <div className="tt-title">{new Date(points[active].at).toLocaleDateString(locale)}</div>
            {series.map((s) => (
              <div className="tt-row" key={s.key}>
                <i className="line-key" style={{ background: s.color }} />
                <strong>{points[active].values[s.key]}</strong>
                <span className="name">{s.label}</span>
              </div>
            ))}
          </Tooltip>
        )}
      </div>
      <button className="btn-link tiny" style={{ marginTop: 6 }} onClick={() => setShowTable((v) => !v)}>
        {showTable ? t.hideTable : t.showTable}
      </button>
      {showTable && (
        <div className="table-wrap" style={{ marginTop: 6 }}>
          <table className="data">
            <thead>
              <tr>
                <th>{t.date}</th>
                {series.map((s) => (
                  <th key={s.key}>{s.label}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {points.map((p, i) => (
                <tr key={i}>
                  <td>{new Date(p.at).toLocaleDateString(locale)}</td>
                  {series.map((s) => (
                    <td key={s.key}>{p.values[s.key]}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Faollik kalendari (GitHub uslubida): ustun = hafta, qator = hafta kuni.

export interface CalendarDay {
  date: string; // yyyy-MM-dd (mahalliy kun)
  count: number;
}

/** Harakatlar sonini 0–4 bosqichga aylantiradi (bitta rangning ochdan to'qqa shkalasi). */
export function heatLevel(count: number): 0 | 1 | 2 | 3 | 4 {
  if (count <= 0) return 0;
  if (count === 1) return 1;
  if (count <= 3) return 2;
  if (count <= 6) return 3;
  return 4;
}

/**
 * Kunlarni hafta ustunlariga joylaydi: har ustun dushanbadan yakshanbagacha.
 * Birinchi hafta dushanbadan boshlanmasa, boshi bo'sh (null) qoladi.
 */
export function toWeeks(days: CalendarDay[]): (CalendarDay | null)[][] {
  if (days.length === 0) return [];
  const [yy, mm, dd] = days[0].date.split('-').map(Number);
  const firstWeekday = (new Date(yy, mm - 1, dd).getDay() + 6) % 7; // 0 = dushanba
  const cells: (CalendarDay | null)[] = [...Array(firstWeekday).fill(null), ...days];
  const weeks: (CalendarDay | null)[][] = [];
  for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
  return weeks;
}

/** Kalendar chetidagi yorliqlar: joy tor, shuning uchun faqat Du, Ch, Ju, Ya. */
export const calendarWeekdayLabels = (weekdays: readonly string[]) => weekdays.map((d, i) => (i % 2 === 0 ? d : ''));

export function ActivityCalendar({ days }: { days: CalendarDay[] }) {
  const [ref, width] = useWidth<HTMLDivElement>();
  const [hover, setHover] = useState<{ day: CalendarDay; x: number; y: number } | null>(null);
  const weeks = toWeeks(days);
  const labelW = 22;
  const gap = 3;
  const cell = Math.max(8, Math.min(16, Math.floor((width - labelW) / Math.max(1, weeks.length)) - gap));
  const svgW = labelW + weeks.length * (cell + gap);
  const svgH = 7 * (cell + gap);
  const activeDays = days.filter((d) => d.count > 0).length;
  const t = useT(chartsMsg);
  const locale = localeOf(useLang().lang);

  return (
    <div>
      <div className="chart" ref={ref}>
        <svg
          width={svgW}
          height={svgH}
          style={{ width: svgW }}
          role="img"
          aria-label={t.calendarAria(days.length, activeDays)}
          onPointerLeave={() => setHover(null)}
        >
          {calendarWeekdayLabels(t.weekdays).map((l, r) =>
            l ? (
              <text key={r} className="tick" x={0} y={r * (cell + gap) + cell - 1}>
                {l}
              </text>
            ) : null,
          )}
          {weeks.map((week, c) =>
            week.map((d, r) =>
              d ? (
                <rect
                  key={d.date}
                  className={`heat-${heatLevel(d.count)}`}
                  x={labelW + c * (cell + gap)}
                  y={r * (cell + gap)}
                  width={cell}
                  height={cell}
                  rx={2}
                  onPointerEnter={() => setHover({ day: d, x: labelW + c * (cell + gap) + cell / 2, y: r * (cell + gap) })}
                />
              ) : null,
            ),
          )}
        </svg>
        {hover && (
          <Tooltip x={Math.min(Math.max(hover.x, 70), width - 70)} y={hover.y}>
            <div className="tt-row">
              <strong>{hover.day.count}</strong>
              <span className="name">
                {t.actions(hover.day.count)} · {parseDay(hover.day.date).toLocaleDateString(locale)}
              </span>
            </div>
          </Tooltip>
        )}
      </div>
      <div className="spread tiny muted" style={{ marginTop: 8 }}>
        <span>{t.calendarSummary(days.length, activeDays)}</span>
        <span className="legend" style={{ margin: 0, flexWrap: 'nowrap', gap: 4, whiteSpace: 'nowrap' }}>
          {t.less}
          {[0, 1, 2, 3, 4].map((l) => (
            <i key={l} className={`swatch heat-${l}`} />
          ))}
          {t.more}
        </span>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Ustunli grafik — bitta seriya (masalan kunlik faol foydalanuvchilar).

export function BarChart({ data, label }: { data: { date: string; value: number }[]; label: string }) {
  const [ref, width] = useWidth<HTMLDivElement>();
  const [active, setActive] = useState<number | null>(null);
  const t = useT(chartsMsg);
  const locale = localeOf(useLang().lang);
  const height = 160;
  const m = { top: 10, right: 6, bottom: 22, left: 28 };
  const w = Math.max(120, width - m.left - m.right);
  const h = height - m.top - m.bottom;
  const max = niceMax(Math.max(1, ...data.map((d) => d.value)));
  const band = w / Math.max(1, data.length);
  const barW = Math.min(24, Math.max(3, band - 2)); // ≤24px, orasida 2px havo
  const y = (v: number) => m.top + h - (v / max) * h;

  // Yuqori uchi 4px yumaloq, pastki qismi to'g'ri burchak (asosga tayanadi).
  function barPath(x0: number, v: number) {
    const top = y(v);
    const base = m.top + h;
    const r = Math.min(4, barW / 2, base - top);
    return `M${x0},${base} V${top + r} Q${x0},${top} ${x0 + r},${top} H${x0 + barW - r} Q${x0 + barW},${top} ${x0 + barW},${top + r} V${base} Z`;
  }

  return (
    <div className="chart" ref={ref}>
      <svg height={height} role="img" aria-label={t.barAria(label, data.length)} onPointerLeave={() => setActive(null)}>
        {[0, max / 2, max].map((v) => (
          <g key={v}>
            <line x1={m.left} x2={m.left + w} y1={y(v)} y2={y(v)} stroke={v === 0 ? 'var(--axis)' : 'var(--grid)'} strokeWidth={1} />
            <text className="tick" x={m.left - 6} y={y(v) + 4} textAnchor="end">
              {Number.isInteger(v) ? v : v.toFixed(1)}
            </text>
          </g>
        ))}
        {data.map((d, i) => {
          const x0 = m.left + i * band + (band - barW) / 2;
          return (
            <g key={d.date} onPointerEnter={() => setActive(i)}>
              {/* Ko'rinmas keng nishon — ingichka ustunga sichqoncha tegishi oson bo'lsin. */}
              <rect x={m.left + i * band} y={m.top} width={band} height={h} fill="transparent" />
              {d.value > 0 && <path d={barPath(x0, d.value)} fill="var(--series-1)" opacity={active === null || active === i ? 1 : 0.55} />}
            </g>
          );
        })}
        <text className="tick" x={m.left} y={height - 6}>
          {shortDate(data[0]?.date ?? '')}
        </text>
        <text className="tick" x={m.left + w} y={height - 6} textAnchor="end">
          {shortDate(data[data.length - 1]?.date ?? '')}
        </text>
      </svg>
      {active !== null && data[active] && (
        <Tooltip
          x={m.left + active * band + band / 2}
          y={m.top}
          side={m.left + active * band + band / 2 > width / 2 ? 'left' : 'right'}
        >
          <div className="tt-title">{parseDay(data[active].date).toLocaleDateString(locale)}</div>
          <div className="tt-row">
            <strong>{data[active].value}</strong>
            <span className="name">{label}</span>
          </div>
        </Tooltip>
      )}
    </div>
  );
}
