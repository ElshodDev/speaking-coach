// Bir nechta ekranda takrorlanadigan kichik UI bo'laklari. Ranglar va
// o'lchamlar styles.css'dagi o'zgaruvchilardan keladi.
import type { ReactNode } from 'react';
import type { HistoryItem } from './api';
import { common, getLang, localeOf, msg, useT } from './i18n';
import { uiMsg } from './locales/ui';

export interface ScoreWithReasoning {
  score: number;
  reasoning: string;
}

export interface CorrectionItem {
  original: string;
  corrected: string;
  explanation: string;
}

/** Ballni darajaga aylantiradi: rang va qisqa so'z shunga qarab tanlanadi. */
export function levelOf(score: number): { key: 'great' | 'good' | 'mid' | 'low'; label: string } {
  const key = score >= 85 ? 'great' : score >= 70 ? 'good' : score >= 50 ? 'mid' : 'low';
  return { key, label: msg(uiMsg).level[key] };
}

/** Sana va vaqt joriy til formatida. */
export const formatDateTime = (iso: string) => new Date(iso).toLocaleString(localeOf(getLang()));

export function PageHeader({
  title,
  subtitle,
  onBack,
  backLabel,
}: {
  title: string;
  subtitle?: string;
  onBack?: () => void;
  backLabel?: string;
}) {
  const t = useT(uiMsg);
  return (
    <div className="page-header">
      {onBack && (
        <button className="back" onClick={onBack}>
          ← {backLabel ?? t.practiceBack}
        </button>
      )}
      <h1>{title}</h1>
      {subtitle && <p>{subtitle}</p>}
    </div>
  );
}

export function ScoreBar({ label, data }: { label: string; data: ScoreWithReasoning }) {
  useT(uiMsg); // til almashganda daraja nomi ham yangilansin
  const lvl = levelOf(data.score);
  return (
    <div className="score">
      <div className="top">
        <span>{label}</span>
        <span className={`txt-${lvl.key}`}>
          {data.score} · {lvl.label}
        </span>
      </div>
      <div className="bar" role="meter" aria-valuemin={0} aria-valuemax={100} aria-valuenow={data.score} aria-label={label}>
        <div className={`fill-${lvl.key}`} style={{ width: `${Math.max(3, data.score)}%` }} />
      </div>
      <div className="why">{data.reasoning}</div>
    </div>
  );
}

export function Corrections({ items }: { items: CorrectionItem[] }) {
  const t = useT(uiMsg);
  if (items.length === 0) return null;
  return (
    <>
      <h3 style={{ marginTop: 18 }}>{t.corrections}</h3>
      <ul className="corrections">
        {items.map((c, i) => (
          <li key={i}>
            <s>{c.original}</s> → <strong>{c.corrected}</strong>
            <div className="muted small">{c.explanation}</div>
          </li>
        ))}
      </ul>
    </>
  );
}

export function Feedback({ encouragement, nextFocus }: { encouragement: string; nextFocus: string }) {
  const t = useT(uiMsg);
  return (
    <div className="stack" style={{ marginTop: 16 }}>
      <div className="encourage">💬 {encouragement}</div>
      <p className="small">
        <strong>{t.nextFocus}</strong> {nextFocus}
      </p>
    </div>
  );
}

/**
 * "Oldingi urinishlar" ro'yxati. Har bir mashq turi yozuvni o'zicha
 * ko'rsatadi (renderRow); bitta yaroqsiz yozuv butun ro'yxatni
 * yiqitmasligi uchun har biri alohida himoyalangan.
 */
export function HistoryList({ items, renderRow }: { items: HistoryItem[]; renderRow: (item: HistoryItem) => ReactNode }) {
  const t = useT(uiMsg);
  if (items.length === 0) return null;
  return (
    <div className="card">
      <h3>{t.history(items.length)}</h3>
      <ul className="history">
        {items.map((item) => {
          let row: ReactNode = null;
          try {
            row = renderRow(item);
          } catch {
            row = null;
          }
          return row ? (
            <li key={item.id}>
              <div className="muted tiny">{formatDateTime(item.createdAtUtc)}</div>
              {row}
            </li>
          ) : null;
        })}
      </ul>
    </div>
  );
}

/** Kirmagan foydalanuvchiga tarix nega bo'sh ekanini tushuntiradi. */
export function GuestNote({ loggedIn, onLogin }: { loggedIn: boolean; onLogin?: () => void }) {
  const t = useT(uiMsg);
  const c = useT(common);
  if (loggedIn) return null;
  return (
    <div className="card soft small">
      {t.guest}{' '}
      {onLogin && (
        <button className="btn-link" onClick={onLogin}>
          {c.loginArrow}
        </button>
      )}
    </div>
  );
}
