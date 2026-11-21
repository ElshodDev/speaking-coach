import { afterEach, describe, expect, it, vi } from 'vitest';
import { dismissedRecently } from './DiagnosticCard';
import { diagnosticMsg } from './locales/diagnostic';
import { diagnosticState, diagnosticSteps, focusRoute, nextDiagnosticStep, type Readiness, type SkillReadiness } from './readinessLogic';

const skill = (s: SkillReadiness['skill'], estimate: number | null): SkillReadiness => ({
  skill: s, estimate, low: estimate === null ? null : estimate - 1, high: estimate === null ? null : estimate + 1,
  confidence: estimate === null ? 'none' : 'low', mocks: 0, practice: estimate === null ? 0 : 1, lastAtUtc: null,
});

const mk = (estimates: Partial<Record<SkillReadiness['skill'], number | null>>, over: Partial<Readiness> = {}): Readiness => ({
  exam: 'ielts', max: 9, overall: null, overallLow: null, overallHigh: null, overallLevel: null, target: '6.5', targetValue: 6.5,
  gap: null, status: 'unknown', daysToExam: 30, focusSkill: 'listening', focusReason: 'missing', diagnosticEnabled: true,
  skills: (['listening', 'reading', 'writing', 'speaking'] as const).map((s) => skill(s, estimates[s] ?? null)),
  ...over,
});

describe('boshlang\'ich test', () => {
  it('holat: flag o\'chiq yoki imtihon yo\'q — off; hammasi yo\'q — todo; to\'rttasi bor — done', () => {
    expect(diagnosticState(mk({}, { diagnosticEnabled: false }))).toBe('off');
    expect(diagnosticState(mk({}, { diagnosticEnabled: undefined }))).toBe('off');   // eski server
    expect(diagnosticState(mk({}, { exam: null }))).toBe('off');
    expect(diagnosticState(mk({}))).toBe('todo');
    expect(diagnosticState(mk({ listening: 5, reading: 5 }))).toBe('todo');
    expect(diagnosticState(mk({ listening: 5, reading: 5, writing: 5, speaking: 5 }))).toBe('done');
  });

  it('qadamlar tartibi, bajarilganlari va keyingisi; yo\'nalish — qisqa mashq', () => {
    const r = mk({ listening: 5.5, writing: 5 });
    const steps = diagnosticSteps(r);
    expect(steps.map((s) => [s.skill, s.done])).toEqual([['listening', true], ['reading', false], ['writing', true], ['speaking', false]]);
    expect(steps.every((s) => s.route.startsWith('practice/'))).toBe(true);
    expect(nextDiagnosticStep(r)?.skill).toBe('reading');
    expect(nextDiagnosticStep(mk({ listening: 1, reading: 1, writing: 1, speaking: 1 }))).toBeNull();
  });

  it('fokus yo\'nalishi: boshlang\'ich test yoqiq bo\'lsa — qisqa mashq, o\'chiq bo\'lsa — avvalgidek mock', () => {
    expect(focusRoute(mk({}, { focusSkill: 'reading', focusReason: 'missing' }))).toBe('practice/reading');
    expect(focusRoute(mk({}, { focusSkill: 'reading', focusReason: 'missing', diagnosticEnabled: false }))).toBe('mock/reading-academic');
    expect(focusRoute(mk({}, { focusSkill: 'listening', focusReason: 'missing', diagnosticEnabled: undefined }))).toBe('mock/listening');
    expect(focusRoute(mk({}, { focusSkill: 'writing', focusReason: 'weakest' }))).toBe('practice/writing');
  });

  it('"Keyinroq": 3 kun ko\'rsatilmaydi', () => {
    const store = new Map<string, string>();
    vi.stubGlobal('localStorage', { getItem: (k: string) => store.get(k) ?? null, setItem: (k: string, v: string) => void store.set(k, v) });
    const now = Date.UTC(2026, 10, 21);
    expect(dismissedRecently(now)).toBe(false);
    store.set('sc.diag.dismissedAt', String(now - 2 * 86_400_000));
    expect(dismissedRecently(now)).toBe(true);
    store.set('sc.diag.dismissedAt', String(now - 4 * 86_400_000));
    expect(dismissedRecently(now)).toBe(false);
    store.set('sc.diag.dismissedAt', 'junk');
    expect(dismissedRecently(now)).toBe(false);
  });

  it('matnlar uch tilda to\'liq; baho "taxminiy", ishonch past deb aytiladi', () => {
    for (const m of [diagnosticMsg.uz, diagnosticMsg.ru, diagnosticMsg.en]) {
      expect(m.title.length).toBeGreaterThan(5);
      expect(m.text('IELTS')).toContain('IELTS');
      expect(m.progress(2)).toContain('2');
      expect(m.ready('IELTS', '5.5', '4.5–6.5')).toContain('5.5');
      expect(m.ready('IELTS', '5.5', '')).not.toContain('()');
      for (const k of ['listening', 'reading', 'writing', 'speaking'] as const) expect(m.steps[k].length).toBeGreaterThan(3);
    }
    expect(diagnosticMsg.uz.note).toMatch(/ishonch past/);
  });
});

afterEach(() => vi.unstubAllGlobals());
