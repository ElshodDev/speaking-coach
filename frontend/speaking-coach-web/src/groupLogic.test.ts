import { describe, expect, it } from 'vitest';
import { cleanCode, formatDue, isOverdue, joinUrl, localInputToUtc, routeFor, savePendingJoin, sortTasks, takePendingJoin, ASSIGNMENT_KINDS, type AssignmentStatus } from './groupLogic';
import { teacherMsg } from './locales/teacher';

const st = (done: boolean): AssignmentStatus => ({ done, late: false, doneAtUtc: null, activityId: null, score: null, progress: null, target: null });

describe('groupLogic', () => {
  it('maps every assignment kind to a page in the app', () => {
    const routes = ASSIGNMENT_KINDS.map(routeFor);
    expect(routes).not.toContain('practice');
    expect(routeFor('mock:cefr:writing')).toBe('mock/cefr-writing');
    expect(routeFor('review')).toBe('review');
    expect(routeFor('unknown')).toBe('practice');
  });

  it('has a label for every kind in every language', () => {
    for (const lang of ['uz', 'ru', 'en'] as const) for (const k of ASSIGNMENT_KINDS) expect(teacherMsg[lang].kinds[k]).toBeTruthy();
  });

  it('converts a datetime-local value to UTC', () => {
    expect(localInputToUtc('')).toBeNull();
    const iso = localInputToUtc('2026-10-01T18:00')!;
    expect(new Date(iso).getTime()).toBe(new Date('2026-10-01T18:00').getTime());
  });

  it('builds the invite link and cleans typed codes', () => {
    expect(joinUrl('https://site.app/', 'K7M2QX')).toBe('https://site.app/#/join/K7M2QX');
    expect(cleanCode(' k7m-2qx ')).toBe('K7M2QX');
  });

  it('marks only unfinished past-due tasks as overdue', () => {
    const now = Date.parse('2026-10-02T00:00:00Z');
    expect(isOverdue('2026-10-01T00:00:00Z', st(false), now)).toBe(true);
    expect(isOverdue('2026-10-01T00:00:00Z', st(true), now)).toBe(false);
    expect(isOverdue(null, st(false), now)).toBe(false);
  });

  it('sorts unfinished tasks first, soonest due first', () => {
    const t = (id: string, due: string | null, done: boolean) => ({ id, dueAtUtc: due, createdAtUtc: '2026-09-01T00:00:00Z', status: st(done) });
    const sorted = sortTasks([t('done', null, true), t('late', '2026-10-05T00:00:00Z', false), t('nodue', null, false), t('soon', '2026-10-01T00:00:00Z', false)]);
    expect(sorted.map((x) => x.id)).toEqual(['soon', 'late', 'nodue', 'done']);
  });

  it('remembers an invite code across login, once', () => {
    const store = new Map<string, string>();
    (globalThis as { sessionStorage?: unknown }).sessionStorage ??= {
      getItem: (k: string) => store.get(k) ?? null,
      setItem: (k: string, v: string) => void store.set(k, v),
      removeItem: (k: string) => void store.delete(k),
    };
    savePendingJoin('K7M2QX');
    expect(takePendingJoin()).toBe('K7M2QX');
    expect(takePendingJoin()).toBeNull();
    savePendingJoin('<script>');
    expect(takePendingJoin()).toBeNull();
  });
});

describe('formatDue', () => {
  it('raqamli sana va vaqt', () => {
    const s = formatDue('2026-09-30T13:00:00Z', 'ru-RU');
    expect(s).toMatch(/\d{2}\.\d{2}\.2026/);
    expect(s).toMatch(/\d{2}:\d{2}/);
  });
});
