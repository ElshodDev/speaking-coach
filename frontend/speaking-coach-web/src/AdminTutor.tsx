import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { adminTutorMsg } from './locales/adminTutor';

export interface TutorStats {
  days: number;
  started: number;
  startedFree: number;
  startedRecommended: number;
  startedAssigned: number;
  completed: number;
  learners: number;
  completionPercent: number | null;
  avgTestPercent: number | null;
  strong: number;
  ok: number;
  weak: number;
  wroteOwn: number;
  ratedUp: number;
  ratedDown: number;
  helpfulPercent: number | null;
  lessonAssignments: number;
  topTopics: { topic: string; lessons: number }[];
}

const DAYS = 30;

/** Admin paneli: AI ustoz o'lchovlari — boshlangan/tugagan darslar, mini-test, baho, mavzular. */
export function AdminTutor() {
  const t = useT(adminTutorMsg);
  const [s, setS] = useState<TutorStats | null>(null);

  useEffect(() => {
    let alive = true;
    apiJson<TutorStats>(`/api/admin/tutor?days=${DAYS}`)
      .then((d) => alive && setS(d))
      .catch(() => alive && setS(null));
    return () => {
      alive = false;
    };
  }, []);

  if (!s) return null;
  const empty = s.started === 0 && s.completed === 0;
  return (
    <div className="card stack" data-testid="admin-tutor">
      <div className="spread">
        <h3 style={{ margin: 0 }}>{t.title}</h3>
        <span className="muted tiny">{t.days(DAYS)}</span>
      </div>
      {empty ? (
        <p className="muted small" style={{ margin: 0 }}>{t.empty}</p>
      ) : (
        <div className="stack small">
          <div>
            <strong>{t.started(s.started)}</strong>
            <div className="muted">{t.sources(s.startedFree, s.startedRecommended, s.startedAssigned)}</div>
          </div>
          <div>
            <strong>{t.completed(s.completed, s.learners)}</strong>
            {s.completionPercent !== null && <span className="muted"> · {t.completion(s.completionPercent)}</span>}
          </div>
          {s.avgTestPercent !== null && <div>{t.avg(s.avgTestPercent)}</div>}
          {s.completed > 0 && <div className="muted">{t.mastery(s.strong, s.ok, s.weak)} · {t.wrote(s.wroteOwn)}</div>}
          {s.ratedUp + s.ratedDown > 0 && (
            <div>{t.rating(s.ratedUp, s.ratedDown)}{s.helpfulPercent !== null && <span className="muted"> · {t.helpful(s.helpfulPercent)}</span>}</div>
          )}
          <div className="muted">{t.assignments(s.lessonAssignments)}</div>
          {s.topTopics.length > 0 && (
            <div>
              <strong>{t.topics}</strong>
              <div className="muted">{s.topTopics.map((x) => `${x.topic} (${x.lessons})`).join(' · ')}</div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
