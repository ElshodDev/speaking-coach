// Tanishtiruv va "Bugungi reja" uchun toza funksiyalar (unit test qilinadi).

export type Goal = 'ielts' | 'cefr' | 'general';

export const GOALS: Goal[] = ['ielts', 'cefr', 'general'];
export const LEVELS = ['A2', 'B1', 'B2', 'C1'] as const;
export const MINUTES = [10, 20, 30, 45] as const;
export const IELTS_TARGETS = ['5.0', '5.5', '6.0', '6.5', '7.0', '7.5', '8.0'];
export const CEFR_TARGETS = ['B1', 'B2', 'C1'];

export interface PlanItem {
  key: string;
  kind: 'review' | 'practice' | 'mock';
  route: string;
  done: boolean;
  minutes: number;
  progress: number | null;
  target: number | null;
}

export interface TodayPlan {
  goal: Goal | null;
  targetScore: string | null;
  examDate: string | null;
  daysToExam: number | null;
  dailyMinutes: number;
  onboarded: boolean;
  items: PlanItem[];
}

export type OnboardingStep = 'goal' | 'level' | 'target' | 'minutes';

/** Umumiy ingliz tilida maqsad ball va imtihon sanasi so'ralmaydi. */
export function stepsFor(goal: Goal | null): OnboardingStep[] {
  return goal === 'general' ? ['goal', 'level', 'minutes'] : ['goal', 'level', 'target', 'minutes'];
}

export function targetsFor(goal: Goal | null): string[] {
  return goal === 'ielts' ? IELTS_TARGETS : goal === 'cefr' ? CEFR_TARGETS : [];
}

/** Mahalliy sana "YYYY-MM-DD" (input type=date uchun). */
export function localIso(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

export function examDateRange(now = new Date()): { min: string; max: string } {
  const max = new Date(now);
  max.setFullYear(max.getFullYear() + 2);
  return { min: localIso(now), max: localIso(max) };
}

/** Birinchi kirishda (faqat bosh sahifada) tanishtiruvni ko'rsatish kerakmi. */
export function shouldOnboard(route: string, profile: { onboarded?: boolean } | null): boolean {
  return !!profile && profile.onboarded === false && (route === 'home' || route === '');
}

export function doneCount(items: PlanItem[]): number {
  return items.filter((i) => i.done).length;
}

/** Bajarilmaganlar oldinda (tartib saqlanadi), bajarilganlar pastda. */
export function orderItems(items: PlanItem[]): PlanItem[] {
  return [...items.filter((i) => !i.done), ...items.filter((i) => i.done)];
}

/** "mock:ielts:reading" → ["ielts", "reading"]. */
export function mockParts(key: string): [string, string] {
  const [, exam = 'ielts', module = ''] = key.split(':');
  return [exam, module];
}
