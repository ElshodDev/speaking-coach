import { useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { teacherMsg } from './locales/teacher';

export interface LessonInsightsData {
  members: number;
  completed: number;
  avgTestPercent: number | null;
  strong: number;
  ok: number;
  weak: number;
  wroteOwn: number;
  topMistakes: { original: string; corrected: string; explanation: string; students: number }[];
}

/** AI dars vazifasi tahlili (o'qituvchi): kim bajardi, mini-test o'rtachasi, o'zlashtirish, sinfdagi eng ko'p xatolar. */
export function LessonInsights({ assignmentId }: { assignmentId: string }) {
  const t = useT(teacherMsg);
  const [data, setData] = useState<LessonInsightsData | null>(null);
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function toggle() {
    if (open) {
      setOpen(false);
      return;
    }
    setOpen(true);
    setBusy(true);
    setError('');
    try {
      const d = await apiJson<{ insights: LessonInsightsData }>(`/api/teacher/assignments/${assignmentId}/insights`);
      setData(d.insights);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div style={{ marginTop: 6 }}>
      <button className="btn-link small" onClick={() => void toggle()} aria-expanded={open} data-testid="insights-toggle">
        {open ? t.insightsHide : t.insights}
      </button>
      {open && (
        <div className="stack small" style={{ marginTop: 6 }} data-testid="insights">
          {error && <p className="error small" role="alert">{error}</p>}
          {busy && <span className="muted">…</span>}
          {data && (data.completed === 0 ? (
            <p className="muted" style={{ margin: 0 }}>{t.insightsNone}</p>
          ) : (
            <>
              <div><strong>{t.insightsDone(data.completed, data.members)}</strong>{data.avgTestPercent !== null && <> · {t.insightsAvg(data.avgTestPercent)}</>}</div>
              <div className="muted">{t.insightsMastery(data.strong, data.ok, data.weak)} · {t.insightsWrote(data.wroteOwn)}</div>
              {data.topMistakes.length > 0 && (
                <div>
                  <strong>{t.insightsMistakes}</strong>
                  <ul style={{ margin: '4px 0 0', paddingLeft: 18 }}>
                    {data.topMistakes.map((m, i) => (
                      <li key={i}>
                        <s>{m.original}</s> → <strong>{m.corrected}</strong> <span className="muted">({t.insightsStudents(m.students)})</span>
                        {m.explanation && <div className="muted tiny">{m.explanation}</div>}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </>
          ))}
        </div>
      )}
    </div>
  );
}
