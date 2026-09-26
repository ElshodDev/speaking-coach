import { useCallback, useEffect, useState } from 'react';
import { apiJson, postJson } from './api';
import { cleanCode, formatDue, isOverdue, routeFor, sortTasks, type AssignmentStatus } from './groupLogic';
import { localeOf, useLang, useT } from './i18n';
import { teacherMsg } from './locales/teacher';
import { PageHeader } from './ui';

interface Task {
  id: string;
  groupId: string;
  groupName: string;
  kind: string;
  target: number | null;
  instructions: string;
  dueAtUtc: string | null;
  createdAtUtc: string;
  status: AssignmentStatus;
}

interface TasksData {
  groups: { id: string; name: string; teacher: string }[];
  assignments: Task[];
}

function useTasks(enabled: boolean) {
  const [data, setData] = useState<TasksData | null>(null);
  const load = useCallback(() => {
    if (!enabled) return;
    apiJson<TasksData>('/api/student/assignments').then(setData).catch(() => setData(null));
  }, [enabled]);
  useEffect(load, [load]);
  return { data, reload: load };
}

function TaskRow({ task, go }: { task: Task; go: (route: string) => void }) {
  const t = useT(teacherMsg);
  const locale = localeOf(useLang().lang);
  const s = task.status;
  const overdue = isOverdue(task.dueAtUtc, s);
  return (
    <div className="spread" style={{ padding: '10px 0', borderTop: '1px solid var(--border)', alignItems: 'center', gap: 10 }} data-testid="task">
      <span className="small" style={{ minWidth: 0 }}>
        <strong>{t.kinds[task.kind] ?? task.kind}{task.kind === 'review' && task.target ? ` · ${t.reviewTarget(task.target)}` : ''}</strong>
        {task.instructions && <div>{task.instructions}</div>}
        <div className="muted tiny">
          {t.fromGroup(task.groupName)}
          {task.dueAtUtc ? ` · ${t.dueAt(formatDue(task.dueAtUtc, locale))}` : ''}
        </div>
        {!s.done && s.progress !== null && s.target ? <div className="muted tiny">{t.progress(s.progress, s.target)}</div> : null}
        {overdue && <div className="txt-low tiny">{t.overdue}</div>}
      </span>
      {s.done ? (
        <span className={`small ${s.late ? 'txt-mid' : 'txt-great'}`} style={{ whiteSpace: 'nowrap' }}>
          {s.late ? t.lateLabel : t.doneLabel}{s.score ? ` · ${s.score}` : ''}
        </span>
      ) : (
        <button className="btn btn-primary" onClick={() => go(routeFor(task.kind))}>{t.start}</button>
      )}
    </div>
  );
}

/** Bosh sahifadagi karta: bajarilmagan vazifalar (bo'lmasa — ko'rinmaydi). */
export function TasksCard({ loggedIn, go }: { loggedIn: boolean; go: (route: string) => void }) {
  const t = useT(teacherMsg);
  const { data } = useTasks(loggedIn);
  if (!data) return null;
  const pending = sortTasks(data.assignments).filter((a) => !a.status.done);
  if (pending.length === 0) return null;
  return (
    <div className="card" data-testid="tasks-card">
      <div className="spread">
        <h3 style={{ margin: 0 }}>📚 {t.pending(pending.length)}</h3>
        <button className="btn-link small" onClick={() => go('tasks')}>{t.all}</button>
      </div>
      {pending.slice(0, 3).map((a) => <TaskRow key={a.id} task={a} go={go} />)}
    </div>
  );
}

/** "Vazifalarim" sahifasi: barcha vazifalar va guruhlar. */
export function TasksPage({ go }: { go: (route: string) => void }) {
  const t = useT(teacherMsg);
  const { data, reload } = useTasks(true);
  return (
    <>
      <PageHeader title={t.tasksTitle} subtitle={t.tasksSubtitle} />
      {!data ? (
        <p className="muted">…</p>
      ) : (
        <>
          <div className="card">
            {data.assignments.length === 0 ? <p className="muted small">{t.noTasks}</p> : sortTasks(data.assignments).map((a) => <TaskRow key={a.id} task={a} go={go} />)}
          </div>
          {data.groups.length > 0 && (
            <div className="card">
              <h3>{t.myGroups}</h3>
              {data.groups.map((g) => (
                <div key={g.id} className="spread" style={{ padding: '8px 0', borderTop: '1px solid var(--border)' }}>
                  <span className="small">
                    <strong>{g.name}</strong>
                    <div className="muted tiny">{g.teacher}</div>
                  </span>
                  <button
                    className="btn-link small quiet"
                    onClick={async () => {
                      if (!window.confirm(t.confirmLeave(g.name))) return;
                      await apiJson(`/api/student/groups/${g.id}`, { method: 'DELETE' });
                      reload();
                    }}
                  >
                    {t.leave}
                  </button>
                </div>
              ))}
            </div>
          )}
        </>
      )}
      <JoinByCode go={go} />
    </>
  );
}

/** Kod bilan qo'shilish (Profil va "Vazifalarim" sahifasida). */
export function JoinByCode({ go }: { go: (route: string) => void }) {
  const t = useT(teacherMsg);
  const [code, setCode] = useState('');
  const clean = cleanCode(code);
  return (
    <div className="card stack">
      <strong>{t.joinTitle}</strong>
      <p className="muted small" style={{ margin: 0 }}>{t.joinText}</p>
      <div className="row">
        <input
          className="input"
          style={{ flex: 1, textTransform: 'uppercase', letterSpacing: 2 }}
          value={code}
          onChange={(e) => setCode(e.target.value)}
          placeholder={t.joinPlaceholder}
          aria-label={t.joinTitle}
          maxLength={10}
          autoCapitalize="characters"
          autoComplete="off"
        />
        <button className="btn btn-primary" disabled={clean.length !== 6} onClick={() => go(`join/${clean}`)}>{t.joinButton}</button>
      </div>
    </div>
  );
}

/** #/join/KOD — taklif havolasi: guruh haqida ma'lumot va rozilik bilan qo'shilish. */
export function JoinGroup({ code, loggedIn, go, onLogin }: { code: string; loggedIn: boolean; go: (route: string) => void; onLogin: () => void }) {
  const t = useT(teacherMsg);
  const [info, setInfo] = useState<{ id: string; name: string; teacher: string; isTeacher: boolean; isMember: boolean } | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!loggedIn) return;
    apiJson<typeof info>(`/api/groups/join/${encodeURIComponent(code)}`)
      .then(setInfo)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
  }, [code, loggedIn]);

  return (
    <>
      <PageHeader title={t.joinGroupTitle} />
      <div className="card stack" data-testid="join">
        {!loggedIn ? (
          <>
            <p style={{ margin: 0 }}>{t.loginToJoin}</p>
            <button className="btn btn-primary" onClick={onLogin}>{t.joinButton}</button>
          </>
        ) : error ? (
          <p className="error" role="alert">{error}</p>
        ) : !info ? (
          <p className="muted">…</p>
        ) : info.isTeacher ? (
          <>
            <p style={{ margin: 0 }}>{t.ownGroup}</p>
            <button className="btn btn-outline" onClick={() => go(`teacher/${info.id}`)}>{t.open}</button>
          </>
        ) : info.isMember ? (
          <>
            <p style={{ margin: 0 }}>{t.alreadyMember}</p>
            <button className="btn btn-primary" onClick={() => go('tasks')}>{t.toTasks}</button>
          </>
        ) : (
          <>
            <p style={{ margin: 0, fontSize: '1.05rem' }}>{t.joinInvite(info.name, info.teacher)}</p>
            <p className="muted small" style={{ margin: 0 }}>{t.joinConsent}</p>
            <button
              className="btn btn-primary block"
              disabled={busy}
              onClick={async () => {
                setBusy(true);
                try {
                  await postJson(`/api/groups/join/${encodeURIComponent(code)}`, {});
                  go('tasks');
                } catch (e) {
                  setError(e instanceof Error ? e.message : String(e));
                } finally {
                  setBusy(false);
                }
              }}
            >
              {t.joinConfirm}
            </button>
          </>
        )}
      </div>
    </>
  );
}
