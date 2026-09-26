// O'qituvchi bo'limi uchun toza funksiyalar (unit test qilinadi).

export const ASSIGNMENT_KINDS = [
  'practice:speaking',
  'practice:writing',
  'practice:reading',
  'practice:listening',
  'review',
  'mock:ielts:listening',
  'mock:ielts:reading',
  'mock:ielts:writing',
  'mock:ielts:speaking',
  'mock:cefr:writing',
  'mock:cefr:speaking',
] as const;

export type AssignmentKind = (typeof ASSIGNMENT_KINDS)[number];

export interface AssignmentStatus {
  done: boolean;
  late: boolean;
  doneAtUtc: string | null;
  activityId: string | null;
  score: string | null;
  progress: number | null;
  target: number | null;
}

/** Vazifani bajarish uchun ilovadagi sahifa (o'quvchi "Bajarish"ni bosganda). */
export function routeFor(kind: string): string {
  const map: Record<string, string> = {
    'practice:speaking': 'practice/speaking',
    'practice:writing': 'practice/writing',
    'practice:reading': 'practice/reading',
    'practice:listening': 'practice/listening',
    review: 'review',
    'mock:ielts:listening': 'mock/listening',
    'mock:ielts:reading': 'mock/reading-academic',
    'mock:ielts:writing': 'mock/writing-academic',
    'mock:ielts:speaking': 'mock/speaking',
    'mock:cefr:writing': 'mock/cefr-writing',
    'mock:cefr:speaking': 'mock/cefr-speaking',
  };
  return map[kind] ?? 'practice';
}

/** <input type="datetime-local"> qiymati (mahalliy vaqt) → UTC ISO; bo'sh bo'lsa null. */
export function localInputToUtc(value: string): string | null {
  if (!value) return null;
  const d = new Date(value); // "2026-10-01T18:00" — brauzer uni mahalliy vaqt deb o'qiydi
  return Number.isNaN(d.getTime()) ? null : d.toISOString();
}

export function isOverdue(dueAtUtc: string | null, status: AssignmentStatus, now = Date.now()): boolean {
  return !status.done && !!dueAtUtc && new Date(dueAtUtc).getTime() < now;
}

/** Taklif havolasi: sayt manzili + #/join/KOD. */
export function joinUrl(origin: string, code: string): string {
  return `${origin.replace(/\/+$/, '')}/#/join/${code}`;
}

/** Kiritilgan kod: katta harf, bo'shliq va tiresiz (server ham xuddi shunday tozalaydi). */
export function cleanCode(input: string): string {
  return input.toUpperCase().replace(/[\s-]/g, '');
}

/** Vazifalar: avval bajarilmaganlar (muddati yaqinlari oldinda), keyin bajarilganlar. */
export function sortTasks<T extends { dueAtUtc: string | null; createdAtUtc: string; status: AssignmentStatus }>(items: T[]): T[] {
  const due = (x: T) => (x.dueAtUtc ? new Date(x.dueAtUtc).getTime() : Number.MAX_SAFE_INTEGER);
  return [...items].sort((a, b) => {
    if (a.status.done !== b.status.done) return a.status.done ? 1 : -1;
    if (!a.status.done) return due(a) - due(b);
    return new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime();
  });
}

const PENDING_JOIN = 'speakingCoach.pendingJoin';

/** Kirmagan foydalanuvchi taklif havolasini ochsa — kod kirishdan keyin ham esda qoladi. */
export function savePendingJoin(code: string) {
  try {
    sessionStorage.setItem(PENDING_JOIN, code);
  } catch {
    // saqlab bo'lmasa — shunchaki bosh sahifaga qaytadi
  }
}

export function takePendingJoin(): string | null {
  try {
    const c = sessionStorage.getItem(PENDING_JOIN);
    sessionStorage.removeItem(PENDING_JOIN);
    return c && /^[A-Z0-9]{6}$/.test(c) ? c : null;
  } catch {
    return null;
  }
}

/** Muddat: "30.09.2026, 18:00" — raqamli format (ba'zi brauzerlarda o'zbekcha oy nomlari yo'q). */
export function formatDue(iso: string, locale: string): string {
  return new Date(iso).toLocaleString(locale, { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
}
