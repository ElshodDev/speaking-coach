// Serverdan kelgan JSON matnini xavfsiz o'qish: tarixdagi bitta buzuq yozuv
// (eski format, kesilgan matn) butun sahifani yiqitmasin.

/** JSON matni → obyekt; buzuq, bo'sh yoki obyekt bo'lmasa — null. */
export function safeJson<T extends object>(text: string | null | undefined): T | null {
  if (!text) return null;
  try {
    const v: unknown = JSON.parse(text);
    return v !== null && typeof v === 'object' ? (v as T) : null;
  } catch {
    return null;
  }
}

/** Baho maydoni ({ score: 6.5 }) dan son; yo'q yoki noto'g'ri bo'lsa — "–". */
export function scoreText(part: unknown): string {
  const score = part && typeof part === 'object' ? (part as { score?: unknown }).score : undefined;
  return typeof score === 'number' && Number.isFinite(score) ? String(score) : '–';
}

/** Matn maydoni: satr bo'lsa o'zi, aks holda bo'sh satr. */
export const textOf = (v: unknown): string => (typeof v === 'string' ? v : '');
