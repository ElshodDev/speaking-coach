import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { ratingMsg } from './locales/rating';
import { AI_SKILLS, SOURCES, percent, ratingsSummary, type RatingItem } from './ratings';

const DAYS = 30;

/** Admin paneli: bir bosishli baholar xulosasi (birinchi mashq, AI adolatliligi, manba). */
export function AdminRatings() {
  const t = useT(ratingMsg);
  const [items, setItems] = useState<RatingItem[] | null>(null);

  useEffect(() => {
    let alive = true;
    apiJson<{ items: RatingItem[] }>(`/api/admin/ratings?days=${DAYS}`)
      .then((r) => alive && setItems(r.items))
      .catch(() => alive && setItems([]));
    return () => {
      alive = false;
    };
  }, []);

  if (items === null) return null;
  const s = ratingsSummary(items);
  const empty = s.firstPractice.total + s.aiAll.total + s.sourceTotal === 0;

  return (
    <div className="card stack" data-testid="admin-ratings">
      <div className="spread">
        <h3 style={{ margin: 0 }}>{t.adminTitle}</h3>
        <span className="muted tiny">{t.adminDays(DAYS)}</span>
      </div>
      {empty && <p className="muted small" style={{ margin: 0 }}>{t.adminEmpty}</p>}

      {s.firstPractice.total > 0 && (
        <div>
          <strong className="small">{t.adminFirst}</strong>
          <div className="small">
            {t.adminHelpful}: {s.firstPractice.up} ({percent(s.firstPractice.up, s.firstPractice.total)}%) · {t.adminNotHelpful}: {s.firstPractice.down} ({percent(s.firstPractice.down, s.firstPractice.total)}%) ·{' '}
            <span className="muted">{t.adminN(s.firstPractice.total)}</span>
          </div>
        </div>
      )}

      {s.aiAll.total > 0 && (
        <div>
          <strong className="small">{t.adminAi}</strong>
          {AI_SKILLS.filter((k) => s.ai[k].total > 0).map((k) => {
            const c = s.ai[k];
            return (
              <div key={k} className="small">
                {t.adminSkills[k]}: {t.aiFair} {percent(c.fair, c.total)}% · {t.aiHigh} {percent(c.high, c.total)}% · {t.aiLow} {percent(c.low, c.total)}%{' '}
                <span className="muted">{t.adminN(c.total)}</span>
              </div>
            );
          })}
        </div>
      )}

      {s.sourceTotal > 0 && (
        <div>
          <strong className="small">{t.adminSource}</strong>
          <div className="small">
            {SOURCES.filter((k) => s.source[k] > 0)
              .map((k) => `${t.sources[k]}: ${s.source[k]}`)
              .join(' · ')}{' '}
            <span className="muted">{t.adminN(s.sourceTotal)}</span>
          </div>
        </div>
      )}
    </div>
  );
}
