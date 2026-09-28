// "Mening xatolarim": AI tuzatishlari turlarga ajratilgan (server hisoblaydi, AI'siz),
// har bir tur uchun maslahat, oxirgi misollar va mos dars.
import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { common, useT } from './i18n';
import { mistakesMsg } from './locales/mistakes';
import { progressMsg } from './locales/progress';
import { PageHeader } from './ui';

export interface MistakeExample {
  original: string;
  corrected: string;
  explanation: string;
  atUtc: string;
}

export interface MistakeCategory {
  id: string;
  count: number;
  last30: number;
  prev30: number;
  examples: MistakeExample[];
}

export interface MistakeReport {
  total: number;
  last30: number;
  categories: MistakeCategory[];
}

/** Xato turi → ilovadagi grammatika darsi (yo'q bo'lsa — mavzuli lug'at yoki hech narsa). */
export const LESSON_FOR: Record<string, string | undefined> = {
  articles: 'learn/grammar/a2-articles',
  prepositions: 'learn/grammar/a2-prepositions-time-place',
  agreement: 'learn/grammar/a2-present-simple-continuous',
  tense: 'learn/grammar/b1-present-perfect-past-simple',
  plural: 'learn/grammar/a2-countable-some-any',
  comparatives: 'learn/grammar/a2-comparatives',
  word_choice: 'learn/words',
};

const ICON: Record<string, string> = {
  articles: '🅰️',
  prepositions: '📍',
  agreement: '🤝',
  tense: '⏳',
  plural: '👥',
  comparatives: '📏',
  word_order: '🔀',
  spelling: '🔤',
  word_choice: '💬',
  other: '🧩',
};

/** Oxirgi 30 kun va undan oldingi 30 kun farqi: manfiy — kamaydi (yaxshi). */
export function trendOf(c: Pick<MistakeCategory, 'last30' | 'prev30'>): number {
  return c.last30 - c.prev30;
}

export function Mistakes({ loggedIn, onLogin, go }: { loggedIn: boolean; onLogin: () => void; go: (route: string) => void }) {
  const t = useT(mistakesMsg);
  const cm = useT(common);
  const tp = useT(progressMsg);
  const [data, setData] = useState<MistakeReport | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!loggedIn) return;
    apiJson<MistakeReport>('/api/mistakes')
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : cm.error));
  }, [loggedIn, cm.error]);

  const header = <PageHeader title={t.title} subtitle={t.subtitle} onBack={() => go('progress')} backLabel={tp.title} />;

  if (!loggedIn) {
    return (
      <>
        {header}
        <div className="card center">
          <p>{t.guestText}</p>
          <button className="btn btn-primary" onClick={onLogin}>{t.login}</button>
        </div>
      </>
    );
  }
  if (error) return <>{header}<p className="error" role="alert">{error}</p></>;
  if (!data) return <>{header}<p className="muted">{cm.loading}</p></>;

  if (data.total === 0) {
    return (
      <>
        {header}
        <div className="card center" data-testid="mistakes-empty">
          <div style={{ fontSize: '2.2rem' }}>🎯</div>
          <h3>{t.emptyTitle}</h3>
          <p className="muted">{t.emptyText}</p>
          <div className="row" style={{ justifyContent: 'center', gap: 8, flexWrap: 'wrap' }}>
            <button className="btn btn-primary" onClick={() => go('practice/speaking')}>{t.speak}</button>
            <button className="btn btn-outline" onClick={() => go('practice/writing')}>{t.write}</button>
          </div>
        </div>
      </>
    );
  }

  const max = Math.max(...data.categories.map((c) => c.count), 1);
  return (
    <>
      {header}
      <p className="muted small" style={{ margin: '4px 0 0' }}>
        {t.total(data.total)} · {t.last30(data.last30)}
      </p>
      <div className="mistake-grid" data-testid="mistakes">
        {data.categories.map((c, i) => {
          const cat = t.cats[c.id] ?? t.cats.other;
          const d = trendOf(c);
          const lesson = LESSON_FOR[c.id];
          return (
            <section key={c.id} className={`card mistake-card${i === 0 ? ' top' : ''}`} data-testid="mistake-card" data-cat={c.id}>
              <div className="mistake-head">
                <span className="mistake-icon" aria-hidden>{ICON[c.id] ?? '🧩'}</span>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <h3 style={{ margin: 0 }}>{cat.name}</h3>
                  <div className="mistake-bar" aria-hidden>
                    <span style={{ width: `${Math.round((c.count / max) * 100)}%` }} />
                  </div>
                </div>
                <div className="mistake-count">
                  <strong>{t.times(c.count)}</strong>
                  {(c.last30 > 0 || c.prev30 > 0) && (
                    <span className={`tiny ${d < 0 ? 'good' : d > 0 ? 'bad' : 'muted'}`} title={t.trendHint}>
                      {d < 0 ? t.better(-d) : d > 0 ? t.worse(d) : t.same}
                    </span>
                  )}
                </div>
              </div>
              {i === 0 && <span className="badge" style={{ alignSelf: 'flex-start' }}>{t.top}</span>}
              <p className="small" style={{ margin: 0 }}>💡 {cat.tip}</p>
              <details open={i === 0}>
                <summary className="small muted">{t.examples} ({c.examples.length})</summary>
                <ul className="mistake-examples">
                  {c.examples.map((e, k) => (
                    <li key={k}>
                      <div className="break">
                        <s>{e.original}</s> → <strong>{e.corrected}</strong>
                      </div>
                      {e.explanation && <div className="muted tiny">{e.explanation}</div>}
                    </li>
                  ))}
                </ul>
              </details>
              {lesson && (
                <button className="btn btn-outline btn-sm" style={{ alignSelf: 'flex-start' }} onClick={() => go(lesson)}>
                  {lesson.startsWith('learn/words') ? t.openWords : t.openLesson}
                </button>
              )}
            </section>
          );
        })}
      </div>
      <p className="muted tiny" style={{ marginTop: 12 }}>{t.note}</p>
    </>
  );
}
