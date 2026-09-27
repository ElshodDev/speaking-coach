// Mock testni ro'yxatdan tanlash: har bo'limda 10+ tayyor test/variant.
// Foydalanuvchi o'zi tanlaydi, "keyingi ishlanmagan"ni boshlaydi yoki (L/R) AI yangisini so'raydi.
import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { mockMsg } from './locales/mock';
import { mockPickMsg } from './locales/mockPick';
import { PageHeader } from './ui';

export interface CatalogEntry {
  id: string;
  number: number;
  /** Bot orqali qo'shilgan testning o'z nomi; tayyor va AI testlarda null. */
  title: string | null;
  topics: string[];
  ai: boolean;
  done: boolean;
  lastScore: number | null;
  lastAtUtc: string | null;
}

interface BankResponse {
  items: CatalogEntry[];
  done: number;
  total: number;
}

type Filter = 'all' | 'fresh' | 'done';

/** Marshrut qismi: "t/<id>" — aniq test, "next" — keyingi ishlanmagan, "ai" — AI yangisini tuzadi. */
export const pickRoute = (base: string, id: string) => `${base}/t/${encodeURIComponent(id)}`;

export function MockPicker({
  exam,
  module,
  variant,
  base,
  title,
  go,
  allowAi = false,
}: {
  exam: 'ielts' | 'cefr';
  module: 'listening' | 'reading' | 'speaking' | 'writing';
  variant?: 'academic' | 'general';
  /** Masalan "mock/listening" — tanlangan test shu yo'lning davomida ochiladi. */
  base: string;
  title: string;
  go: (route: string) => void;
  allowAi?: boolean;
}) {
  const t = useT(mockPickMsg);
  const tm = useT(mockMsg);
  const [data, setData] = useState<BankResponse | null>(null);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState<Filter>('all');

  useEffect(() => {
    const q = variant ? `?variant=${variant}` : '';
    apiJson<BankResponse>(`/api/mock/${exam}/${module}/bank${q}`)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
  }, [exam, module, variant]);

  const isSet = module === 'speaking' || module === 'writing';
  const label = (e: CatalogEntry) => e.title ?? (isSet ? t.set(e.number) : t.test(e.number));
  const score = (x: number) => (exam === 'ielts' ? t.band(x) : t.score75(x));
  const items = (data?.items ?? []).filter((e) => (filter === 'all' ? true : filter === 'done' ? e.done : !e.done));
  const allDone = !!data && data.total > 0 && data.done >= data.total;

  return (
    <>
      <PageHeader title={title} subtitle={t.subtitle} onBack={() => go('mock')} backLabel={tm.title} />
      {error && <p className="error" role="alert">{error}</p>}
      {!data && !error && <p className="muted">{t.loading}</p>}
      {data && (
        <div className="stack" data-testid="mock-picker">
          <div className="pick-toolbar">
            <div className="pick-progress" aria-label={t.progress(data.done, data.total)}>
              <div className="pick-progress-bar">
                <span style={{ width: `${data.total ? Math.round((data.done / data.total) * 100) : 0}%` }} />
              </div>
              <span className="small muted">{t.progress(data.done, data.total)}</span>
            </div>
            <div className="pick-actions">
              <button className="btn btn-primary" onClick={() => go(`${base}/next`)} data-testid="pick-next">
                {allDone ? t.nextAll : t.next}
              </button>
              {allowAi && (
                <button className="btn btn-outline" onClick={() => go(`${base}/ai`)} title={t.aiNote} data-testid="pick-ai">
                  {t.ai}
                </button>
              )}
            </div>
          </div>
          <div className="chips" role="group">
            {(['all', 'fresh', 'done'] as const).map((f) => (
              <button key={f} aria-pressed={filter === f} onClick={() => setFilter(f)}>
                {f === 'all' ? t.all : f === 'fresh' ? t.fresh : t.doneFilter}
                <span className="n"> {f === 'all' ? data.total : f === 'done' ? data.done : data.total - data.done}</span>
              </button>
            ))}
          </div>
          {items.length === 0 && <p className="muted">{t.empty}</p>}
          <ul className="pick-grid" role="list">
            {items.map((e) => (
              <li key={e.id}>
                <button
                  className={`pick-card${e.done ? ' done' : ''}`}
                  onClick={() => go(pickRoute(base, e.id))}
                  data-testid="pick-card"
                >
                  <span className="pick-head">
                    <span className="pick-num">{label(e)}</span>
                    {e.ai && <span className="badge">{t.aiBadge}</span>}
                    {e.done && (
                      <span className="badge badge-done">
                        {e.lastScore != null ? `✓ ${score(e.lastScore)}` : t.doneBadge}
                      </span>
                    )}
                  </span>
                  {e.topics.length > 0 && (
                    <span className="pick-topics">
                      {e.topics.slice(0, 4).map((topic, i) => (
                        <span key={i}>{topic}</span>
                      ))}
                    </span>
                  )}
                  <span className="pick-go">{e.done ? t.again : t.start}</span>
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </>
  );
}
