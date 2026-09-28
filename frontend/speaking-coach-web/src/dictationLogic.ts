// Diktant: yozilgan gapni asl gap bilan so'zma-so'z solishtirish (AI'siz, toza funksiyalar).

export type WordStatus = 'ok' | 'wrong' | 'missing' | 'extra';

export interface WordMark {
  status: WordStatus;
  /** Asl gapdagi so'z (extra bo'lsa — bo'sh). */
  expected: string;
  /** Foydalanuvchi yozgani (missing bo'lsa — bo'sh). */
  typed: string;
}

export interface DictationCheck {
  marks: WordMark[];
  correct: number;
  total: number;
}

/** Katta-kichik harf, tinish belgilari va apostrof turi hisobga olinmaydi. */
export function normalizeWord(w: string): string {
  return w
    .toLowerCase()
    .replace(/[’‘`´]/g, "'")
    .replace(/^[^a-z0-9']+|[^a-z0-9']+$/g, '');
}

export function words(text: string): string[] {
  return text
    .split(/\s+/)
    .map((w) => w.trim())
    .filter((w) => normalizeWord(w).length > 0);
}

/**
 * Eng uzun umumiy ketma-ketlik bo'yicha tekislash. Ketma-ket "tushib qolgan" va
 * "ortiqcha" so'zlar juftlanib "wrong" (xato yozilgan) bo'ladi.
 */
export function checkDictation(expectedText: string, typedText: string): DictationCheck {
  const exp = words(expectedText);
  const typ = words(typedText);
  const a = exp.map(normalizeWord);
  const b = typ.map(normalizeWord);
  const n = a.length;
  const m = b.length;
  const lcs: number[][] = Array.from({ length: n + 1 }, () => new Array<number>(m + 1).fill(0));
  for (let i = n - 1; i >= 0; i--)
    for (let j = m - 1; j >= 0; j--) lcs[i][j] = a[i] === b[j] ? lcs[i + 1][j + 1] + 1 : Math.max(lcs[i + 1][j], lcs[i][j + 1]);

  const raw: WordMark[] = [];
  let i = 0;
  let j = 0;
  while (i < n || j < m) {
    if (i < n && j < m && a[i] === b[j]) {
      raw.push({ status: 'ok', expected: exp[i++], typed: typ[j++] });
    } else if (j < m && (i >= n || lcs[i][j + 1] >= lcs[i + 1][j])) {
      raw.push({ status: 'extra', expected: '', typed: typ[j++] });
    } else {
      raw.push({ status: 'missing', expected: exp[i++], typed: '' });
    }
  }

  // Yonma-yon turgan "extra" + "missing" → bitta "wrong" (masalan, "hause" ↔ "house").
  const marks: WordMark[] = [];
  for (let k = 0; k < raw.length; k++) {
    const cur = raw[k];
    const next = raw[k + 1];
    if (next && ((cur.status === 'extra' && next.status === 'missing') || (cur.status === 'missing' && next.status === 'extra'))) {
      marks.push({ status: 'wrong', expected: cur.expected || next.expected, typed: cur.typed || next.typed });
      k++;
    } else {
      marks.push(cur);
    }
  }
  return { marks, correct: marks.filter((x) => x.status === 'ok').length, total: exp.length };
}

/** Yangi gaplar: avval yaqinda ko'rilmaganlar (tasodifiy tartibda), yetmasa — qolganlari. */
export function pickSentences(all: string[], seen: string[], count: number, rnd: () => number = Math.random): string[] {
  const fresh = all.filter((s) => !seen.includes(s));
  const old = all.filter((s) => seen.includes(s));
  return [...shuffle(fresh, rnd), ...shuffle(old, rnd)].slice(0, count);
}

function shuffle<T>(arr: T[], rnd: () => number): T[] {
  const copy = [...arr];
  for (let k = copy.length - 1; k > 0; k--) {
    const r = Math.floor(rnd() * (k + 1));
    [copy[k], copy[r]] = [copy[r], copy[k]];
  }
  return copy;
}
