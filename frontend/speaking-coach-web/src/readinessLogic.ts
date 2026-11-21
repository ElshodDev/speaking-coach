// Imtihonga tayyorlik bahosi — server javobi turlari va ko'rsatish uchun toza funksiyalar.

export type Skill = 'listening' | 'reading' | 'writing' | 'speaking';
export type Confidence = 'none' | 'low' | 'medium' | 'high';
export type Status = 'no_goal' | 'unknown' | 'no_target' | 'on_track' | 'close' | 'behind';

export interface SkillReadiness {
  skill: Skill;
  estimate: number | null;
  low: number | null;
  high: number | null;
  confidence: Confidence;
  mocks: number;
  practice: number;
  lastAtUtc: string | null;
}

export interface Readiness {
  exam: 'ielts' | 'cefr' | null;
  max: number;
  overall: number | null;
  overallLow: number | null;
  overallHigh: number | null;
  overallLevel: string | null;
  target: string | null;
  targetValue: number | null;
  gap: number | null;
  status: Status;
  daysToExam: number | null;
  focusSkill: Skill | null;
  focusReason: 'missing' | 'weakest' | null;
  skills: SkillReadiness[];
  /** Server: Features:DayOneDiagnostic yoqiq (eski serverda yo'q). */
  diagnosticEnabled?: boolean;
}

/** IELTS — bitta kasr (6.5), CEFR — butun ball (51). */
export function formatScore(exam: Readiness['exam'], v: number | null): string {
  if (v === null) return '—';
  return exam === 'cefr' ? String(Math.round(v)) : v.toFixed(1);
}

/** Shkala bo'yicha foiz (0–100) — chiziqlar uchun. */
export function pct(v: number | null, max: number): number {
  if (v === null || max <= 0) return 0;
  return Math.max(0, Math.min(100, (100 * v) / max));
}

/** Oraliq ("5.5–6.5"); bir xil bo'lsa — bitta qiymat. */
export function formatRange(exam: Readiness['exam'], low: number | null, high: number | null): string {
  if (low === null || high === null) return '';
  const a = formatScore(exam, low);
  const b = formatScore(exam, high);
  return a === b ? a : `${a}–${b}`;
}

/**
 * Fokus ko'nikma uchun yo'nalish: ma'lumot yo'q — shu bo'limning mock'i (boshlang'ich test yoqiq bo'lsa —
 * qisqa mashq, 60 daqiqalik mock emas); zaif — oddiy mashq.
 */
export function focusRoute(r: Readiness): string | null {
  if (!r.focusSkill || !r.exam) return null;
  if (r.focusReason === 'missing' && !r.diagnosticEnabled) {
    if (r.exam === 'cefr') return `mock/cefr-${r.focusSkill}`;
    return r.focusSkill === 'reading' ? 'mock/reading-academic' : r.focusSkill === 'writing' ? 'mock/writing-academic' : `mock/${r.focusSkill}`;
  }
  return `practice/${r.focusSkill}`;
}

/** Holat rangi (mavjud .txt-good / .txt-mid / .txt-low sinflari). */
export function statusTone(s: Status): 'good' | 'mid' | 'low' | null {
  return s === 'on_track' ? 'good' : s === 'close' ? 'mid' : s === 'behind' ? 'low' : null;
}

// ---- Birinchi kunlik boshlang'ich test ----

export const DIAGNOSTIC_SKILLS: Skill[] = ['listening', 'reading', 'writing', 'speaking'];

export interface DiagnosticStep {
  skill: Skill;
  done: boolean;
  /** Qisqa mashq sahifasi (60 daqiqalik mock emas). */
  route: string;
}

export type DiagnosticState = 'off' | 'todo' | 'done';

/** Qadamlar: ko'nikma bo'yicha baho bor bo'lsa (mashq yoki mock'dan) — bajarilgan. */
export function diagnosticSteps(r: Readiness): DiagnosticStep[] {
  return DIAGNOSTIC_SKILLS.map((skill) => ({
    skill,
    done: r.skills.some((s) => s.skill === skill && s.estimate !== null),
    route: `practice/${skill}`,
  }));
}

/** off — flag o'chiq yoki imtihon maqsadi yo'q; todo — hali to'rttasi ham bajarilmagan; done — baho tayyor. */
export function diagnosticState(r: Readiness): DiagnosticState {
  if (!r.diagnosticEnabled || !r.exam) return 'off';
  return diagnosticSteps(r).every((s) => s.done) ? 'done' : 'todo';
}

/** Keyingi bajarilmagan qadam (yo'q bo'lsa — null). */
export function nextDiagnosticStep(r: Readiness): DiagnosticStep | null {
  return diagnosticSteps(r).find((s) => !s.done) ?? null;
}
