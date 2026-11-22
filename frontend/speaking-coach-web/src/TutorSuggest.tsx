import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { mistakesMsg } from './locales/mistakes';
import { tutorMsg } from './locales/tutor';

export interface TutorRecommendation {
  category: string;
  topic: string;
  count: number;
  last30: number;
  example: { original: string; corrected: string } | null;
}

/** AI ustoz sahifasiga o'tish: mavzu oldindan yoziladi (foydalanuvchi "Darsni boshlash"ni o'zi bosadi). */
export const tutorTopicRoute = (topic: string): string => `tutor/t/${encodeURIComponent(topic)}`;

/** Marshrutdagi mavzu: noto'g'ri kodlangan yoki juda uzun bo'lsa — null. */
export function topicFromRoute(raw: string | undefined): string | null {
  if (!raw) return null;
  try {
    const t = decodeURIComponent(raw).split(/\s+/).filter(Boolean).join(' ');
    return t.length >= 2 && t.length <= 80 ? t : null;
  } catch {
    return null;
  }
}

/**
 * "Xatolaringiz asosida sizga shu mavzu kerak": AI'siz hisoblangan tavsiya (server), bir bosishda dars.
 * Tavsiya bo'lmasa (yoki yuklanmasa, yoki ustoz o'chiq) — hech narsa ko'rsatilmaydi.
 */
export function TutorSuggest({ loggedIn, go, limit = 1 }: { loggedIn: boolean; go: (route: string) => void; limit?: number }) {
  const t = useT(tutorMsg);
  const tm = useT(mistakesMsg);
  const [items, setItems] = useState<TutorRecommendation[]>([]);

  useEffect(() => {
    if (!loggedIn) return;
    let alive = true;
    apiJson<{ items: TutorRecommendation[] }>('/api/tutor/recommendations')
      .then((d) => alive && setItems(d.items))
      .catch(() => alive && setItems([]));
    return () => {
      alive = false;
    };
  }, [loggedIn]);

  const shown = items.slice(0, limit);
  if (!loggedIn || shown.length === 0) return null;
  return (
    <div className="card stack" data-testid="tutor-suggest-card">
      <strong>{t.suggestTitle}</strong>
      {shown.map((r) => (
        <div key={r.category} className="stack" data-testid="tutor-suggest-item">
          <div>
            <strong>{r.topic}</strong>
            <div className="muted small">{t.suggestWhy((tm.cats[r.category] ?? tm.cats.other).name, r.last30 > 0 ? r.last30 : r.count, r.last30 > 0)}</div>
            {r.example && <div className="small break"><s>{r.example.original}</s> → <strong>{r.example.corrected}</strong></div>}
          </div>
          <button className="btn btn-outline" style={{ alignSelf: 'flex-start' }} onClick={() => go(tutorTopicRoute(r.topic))} data-testid="tutor-suggest-go">
            {t.suggestLearn}
          </button>
        </div>
      ))}
    </div>
  );
}
