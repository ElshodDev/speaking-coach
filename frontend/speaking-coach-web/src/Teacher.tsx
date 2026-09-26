import { useCallback, useEffect, useState } from 'react';
import { apiJson, postJson } from './api';
import { ASSIGNMENT_KINDS, joinUrl, localInputToUtc, type AssignmentStatus, formatDue } from './groupLogic';
import { localeOf, useLang, useT } from './i18n';
import { teacherMsg } from './locales/teacher';
import { PageHeader } from './ui';

interface GroupSummary {
  id: string;
  name: string;
  joinCode: string;
  members: number;
  assignments: number;
}

interface GroupDetail {
  group: { id: string; name: string; joinCode: string };
  members: { userId: string; name: string; joinedAtUtc: string }[];
  assignments: { id: string; kind: string; target: number | null; instructions: string; dueAtUtc: string | null; createdAtUtc: string; done: number }[];
  results: Record<string, Record<string, AssignmentStatus>>;
}

/** O'qituvchi: guruhlar ro'yxati va yangi guruh ochish. */
export function TeacherHome({ go }: { go: (route: string) => void }) {
  const t = useT(teacherMsg);
  const [groups, setGroups] = useState<GroupSummary[] | null>(null);
  const [name, setName] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(() => {
    apiJson<GroupSummary[]>('/api/teacher/groups')
      .then(setGroups)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
  }, []);
  useEffect(load, [load]);

  async function create() {
    setBusy(true);
    setError('');
    try {
      const g = await postJson<{ id: string }>('/api/teacher/groups', { name });
      go(`teacher/${g.id}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader title={t.groupsTitle} subtitle={t.groupsSubtitle} onBack={() => go('profile')} />
      <div className="card stack">
        <input className="input" value={name} onChange={(e) => setName(e.target.value)} placeholder={t.newGroupName} aria-label={t.newGroupName} maxLength={80} />
        <button className="btn btn-primary" onClick={create} disabled={busy || name.trim().length < 2}>{t.create}</button>
        {error && <p className="error small" role="alert">{error}</p>}
      </div>
      {groups && groups.length === 0 && <p className="muted">{t.noGroups}</p>}
      {groups?.map((g) => (
        <button key={g.id} className="card" style={{ display: 'block', width: '100%', textAlign: 'left', cursor: 'pointer' }} onClick={() => go(`teacher/${g.id}`)}>
          <div className="spread">
            <strong>{g.name}</strong>
            <span className="muted small">{g.joinCode}</span>
          </div>
          <div className="muted small">{t.members(g.members)} · {t.assignmentsCount(g.assignments)}</div>
        </button>
      ))}
    </>
  );
}

type Tab = 'assignments' | 'results' | 'members';

/** O'qituvchi: bitta guruh — taklif, vazifalar, natijalar jadvali, o'quvchilar. */
export function TeacherGroup({ id, go }: { id: string; go: (route: string) => void }) {
  const t = useT(teacherMsg);
  const locale = localeOf(useLang().lang);
  const [data, setData] = useState<GroupDetail | null>(null);
  const [error, setError] = useState('');
  const [tab, setTab] = useState<Tab>('assignments');
  const [copied, setCopied] = useState(false);
  const [kind, setKind] = useState<string>('practice:speaking');
  const [target, setTarget] = useState(20);
  const [due, setDue] = useState('');
  const [instructions, setInstructions] = useState('');
  const [busy, setBusy] = useState(false);
  const [viewing, setViewing] = useState<{ title: string; data: unknown } | null>(null);

  const load = useCallback(() => {
    apiJson<GroupDetail>(`/api/teacher/groups/${id}`)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
  }, [id]);
  useEffect(load, [load]);

  const fmt = (iso: string) => formatDue(iso, locale);

  async function act(fn: () => Promise<unknown>) {
    setBusy(true);
    setError('');
    try {
      await fn();
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }

  if (!data) {
    return (
      <>
        <PageHeader title={t.groupsTitle} onBack={() => go('teacher')} backLabel={t.groupsTitle} />
        {error ? <p className="error" role="alert">{error}</p> : <p className="muted">…</p>}
      </>
    );
  }

  const link = joinUrl(window.location.origin + window.location.pathname, data.group.joinCode);
  const n = data.members.length;

  return (
    <>
      <PageHeader title={data.group.name} subtitle={t.members(n)} onBack={() => go('teacher')} backLabel={t.groupsTitle} />

      <div className="card stack" data-testid="invite">
        <div className="spread">
          <strong>{t.invite}</strong>
          <span>
            <span className="muted small">{t.code}: </span>
            <strong style={{ letterSpacing: 2 }}>{data.group.joinCode}</strong>
          </span>
        </div>
        <p className="muted small" style={{ margin: 0 }}>{t.inviteText}</p>
        <div className="quote small" style={{ margin: 0, overflowWrap: 'anywhere' }}>{link}</div>
        <div className="row">
          <button
            className="btn btn-primary"
            onClick={async () => {
              try {
                await navigator.clipboard.writeText(link);
                setCopied(true);
                setTimeout(() => setCopied(false), 2000);
              } catch {
                window.prompt(t.copy, link);
              }
            }}
          >
            {copied ? t.copied : t.copy}
          </button>
          <button className="btn btn-outline" disabled={busy} onClick={() => window.confirm(t.confirmNewCode) && act(() => postJson(`/api/teacher/groups/${id}/code`, {}))}>
            {t.newCode}
          </button>
        </div>
      </div>

      {error && <p className="error small" role="alert">{error}</p>}

      <div className="segmented" role="tablist" style={{ margin: '12px 0' }}>
        {(['assignments', 'results', 'members'] as const).map((k) => (
          <button key={k} role="tab" aria-selected={tab === k} aria-pressed={tab === k} onClick={() => setTab(k)}>
            {t.tabs[k]}
          </button>
        ))}
      </div>

      {tab === 'assignments' && (
        <>
          <div className="card stack" data-testid="new-assignment">
            <strong>{t.newAssignment}</strong>
            <label className="small field">
              {t.kind}
              <select className="input" value={kind} onChange={(e) => setKind(e.target.value)}>
                {ASSIGNMENT_KINDS.map((k) => <option key={k} value={k}>{t.kinds[k]}</option>)}
              </select>
            </label>
            {kind === 'review' && (
              <label className="small field">
                {t.target}
                <input className="input" type="number" min={1} max={500} value={target} onChange={(e) => setTarget(Number(e.target.value))} />
              </label>
            )}
            <label className="small field">
              {t.due}
              <input className="input" type="datetime-local" value={due} onChange={(e) => setDue(e.target.value)} />
            </label>
            <label className="small field">
              {t.instructions}
              <textarea className="input" rows={2} maxLength={1000} value={instructions} onChange={(e) => setInstructions(e.target.value)} placeholder={t.instructionsPlaceholder} />
            </label>
            <button
              className="btn btn-primary"
              disabled={busy}
              onClick={() =>
                act(async () => {
                  await postJson(`/api/teacher/groups/${id}/assignments`, {
                    kind,
                    target: kind === 'review' ? target : null,
                    instructions,
                    dueAtUtc: localInputToUtc(due),
                  });
                  setInstructions('');
                  setDue('');
                })
              }
            >
              {t.give}
            </button>
          </div>

          {data.assignments.length === 0 && <p className="muted">{t.noAssignments}</p>}
          {data.assignments.map((a) => (
            <div className="card" key={a.id} data-testid="assignment">
              <div className="spread">
                <strong>{t.kinds[a.kind] ?? a.kind}{a.kind === 'review' && a.target ? ` · ${t.reviewTarget(a.target)}` : ''}</strong>
                <span className="small">{t.doneOf(a.done, n)}</span>
              </div>
              {a.instructions && <p className="small" style={{ margin: '6px 0' }}>{a.instructions}</p>}
              <div className="muted tiny">
                {t.createdAt(fmt(a.createdAtUtc))} · {a.dueAtUtc ? t.dueAt(fmt(a.dueAtUtc)) : t.noDue}
              </div>
              <button className="btn-link small quiet" onClick={() => window.confirm(t.confirmDeleteAssignment) && act(() => apiJson(`/api/teacher/assignments/${a.id}`, { method: 'DELETE' }))}>
                {t.deleteAssignment}
              </button>
            </div>
          ))}
        </>
      )}

      {tab === 'results' && (
        <div className="card">
          {n === 0 || data.assignments.length === 0 ? (
            <p className="muted small">{n === 0 ? t.noMembers : t.noAssignments}</p>
          ) : (
            <div className="table-wrap" data-testid="results">
              <table className="data small">
                <thead>
                  <tr>
                    <th>{t.student}</th>
                    {data.assignments.map((a) => (
                      <th key={a.id} title={a.instructions}>{(t.kinds[a.kind] ?? a.kind).split(' ')[0]}<div className="muted tiny">{a.dueAtUtc ? fmt(a.dueAtUtc) : ''}</div></th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {data.members.map((m) => (
                    <tr key={m.userId}>
                      <td>{m.name}</td>
                      {data.assignments.map((a) => {
                        const s = data.results[a.id]?.[m.userId];
                        const cell = !s ? t.notDone
                          : s.done ? `${s.score ?? '✅'}${s.late ? ` (${t.late})` : ''}`
                          : s.progress !== null && s.target ? t.progress(s.progress, s.target)
                          : t.notDone;
                        return (
                          <td key={a.id}>
                            {s?.activityId ? (
                              <button
                                className="btn-link small"
                                onClick={async () => {
                                  const res = await apiJson(`/api/teacher/assignments/${a.id}/students/${m.userId}`);
                                  setViewing({ title: `${m.name} — ${t.kinds[a.kind] ?? a.kind}`, data: res });
                                }}
                              >
                                {cell}
                              </button>
                            ) : (
                              <span className={s?.done ? (s.late ? 'txt-mid' : 'txt-great') : 'muted'}>{cell}</span>
                            )}
                          </td>
                        );
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <p className="muted tiny">{t.privacyNote}</p>
        </div>
      )}

      {tab === 'members' && (
        <div className="card">
          {n === 0 && <p className="muted small">{t.noMembers}</p>}
          {data.members.map((m) => (
            <div key={m.userId} className="spread" style={{ padding: '8px 0', borderTop: '1px solid var(--border)' }}>
              <span>
                {m.name}
                <div className="muted tiny">{t.joined(fmt(m.joinedAtUtc))}</div>
              </span>
              <button className="btn-link small quiet" onClick={() => window.confirm(t.confirmRemove(m.name)) && act(() => apiJson(`/api/teacher/groups/${id}/members/${m.userId}`, { method: 'DELETE' }))}>
                {t.remove}
              </button>
            </div>
          ))}
          <div className="row" style={{ marginTop: 16 }}>
            <button
              className="btn btn-outline"
              onClick={() => {
                const name = window.prompt(t.rename, data.group.name);
                if (name && name.trim() !== data.group.name) void act(() => apiJson(`/api/teacher/groups/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name }) }));
              }}
            >
              {t.rename}
            </button>
            <button
              className="btn btn-danger"
              onClick={async () => {
                if (!window.confirm(t.confirmDeleteGroup(data.group.name))) return;
                await apiJson(`/api/teacher/groups/${id}`, { method: 'DELETE' });
                go('teacher');
              }}
            >
              {t.deleteGroup}
            </button>
          </div>
        </div>
      )}

      {viewing && <SubmissionView title={viewing.title} data={viewing.data} onClose={() => setViewing(null)} />}
    </>
  );
}

/**
 * O'quvchining ishi: matnlar (insho, xat) va baholash — umumiy ko'rinishda
 * (har xil mashq turining natija shakli har xil, shuning uchun muhim
 * maydonlarni chiqaramiz).
 */
function SubmissionView({ title, data, onClose }: { title: string; data: unknown; onClose: () => void }) {
  const t = useT(teacherMsg);
  const act = (data as { activity?: { prompt?: Record<string, unknown>; result?: Record<string, unknown> } | null }).activity;
  const texts: string[] = [];
  const pick = (o: unknown) => {
    if (!o || typeof o !== 'object') return;
    for (const [k, v] of Object.entries(o as Record<string, unknown>)) {
      if (typeof v === 'string' && ['text', 'transcript', 'task1', 'task2', 'strengths', 'reasoning', 'encouragement', 'nextFocus'].includes(k) && v.trim()) texts.push(v);
    }
  };
  pick(act?.prompt);
  pick(act?.result);
  const result = act?.result as Record<string, unknown> | undefined;
  for (const key of ['tasks', 'texts', 'task1', 'task2'] as const) {
    const v = result?.[key];
    if (Array.isArray(v)) v.forEach((x) => typeof x === 'object' && x && typeof (x as { text?: unknown }).text === 'string' && texts.push((x as { text: string }).text));
    else if (v && typeof v === 'object' && typeof (v as { text?: unknown }).text === 'string') texts.push((v as { text: string }).text);
  }
  return (
    <div role="dialog" aria-modal="true" aria-label={t.submission} style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,.45)', zIndex: 50, display: 'flex', alignItems: 'flex-end', justifyContent: 'center' }} onClick={onClose}>
      <div className="card stack" style={{ maxHeight: '85vh', overflow: 'auto', width: 'min(640px, 100%)', margin: 0, borderRadius: '16px 16px 0 0' }} onClick={(e) => e.stopPropagation()}>
        <div className="spread">
          <strong>{title}</strong>
          <button className="btn-link" onClick={onClose}>{t.close}</button>
        </div>
        {[...new Set(texts)].map((x, i) => (
          <p key={i} className="quote small" style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{x}</p>
        ))}
      </div>
    </div>
  );
}
