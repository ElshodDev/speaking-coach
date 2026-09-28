import { describe, expect, it } from 'vitest';

// Oʻzbek imlosi: oʻ/gʻ — ʻ (U+02BB), tutuq belgisi — ʼ (U+02BC). Tipografik
// ’ (U+2019) va ‘ (U+2018) oʻzbekcha matnda ishlatilmaydi (ekran oʻquvchi va
// qidiruv ularni boshqa harf deb biladi). Test src/locales dagi HAR BIR
// bo'limning `uz` matnlarini tekshiradi — yangi fayl qo'shilsa, avtomatik.

/**
 * Hali tuzatilmagan fayllar (boshqa agent/lead tuzatadi) — tuzatilgach shu
 * ro'yxatdan olib tashlang, shunda ular ham tekshiriladi.
 */
const PENDING: string[] = [];

const BAD = /[‘’]/;
// ASCII apostrof oʻ/gʻ oʻrnida: "o'z", "bog'liq" (inglizcha "don't" kabi so'zlar bundan mustasno).
const ASCII_OG = /(?<![A-Za-z])(?:[a-zA-Z]*[oOgG])'(?=[a-zʻ])/;

const modules = import.meta.glob<Record<string, unknown>>('./locales/*.ts', { eager: true });

const SAMPLE_ARGS: unknown[][] = [[], [1], [2, 3, 4], ['x', 'y', 'z'], [5, 'x', 7]];

/** uz qiymatidagi barcha matnlar: satrlar, funksiya manbasi va natijalari. */
function collect(value: unknown, path: string, out: { path: string; text: string; source?: boolean }[], depth = 0) {
  if (depth > 8) return;
  if (typeof value === 'string') {
    out.push({ path, text: value });
  } else if (typeof value === 'function') {
    out.push({ path, text: value.toString(), source: true });
    for (const args of SAMPLE_ARGS) {
      try {
        const r = (value as (...a: unknown[]) => unknown)(...args);
        if (typeof r === 'string') out.push({ path: `${path}(${args.join(',')})`, text: r });
      } catch {
        // boshqa turdagi argument kutadi — manbasi baribir tekshirildi
      }
    }
  } else if (Array.isArray(value)) {
    value.forEach((v, i) => collect(v, `${path}[${i}]`, out, depth + 1));
  } else if (value && typeof value === 'object') {
    for (const [k, v] of Object.entries(value)) collect(v, `${path}.${k}`, out, depth + 1);
  }
}

function uzTexts() {
  const out: { path: string; text: string; source?: boolean }[] = [];
  for (const [file, mod] of Object.entries(modules)) {
    const name = file.split('/').pop()!;
    if (PENDING.includes(name)) continue;
    for (const [exp, value] of Object.entries(mod)) {
      if (value && typeof value === 'object' && 'uz' in value && 'ru' in value && 'en' in value) {
        collect((value as { uz: unknown }).uz, `${name}:${exp}.uz`, out);
      }
    }
  }
  return out;
}

describe('Uzbek orthography in locale files', () => {
  const texts = uzTexts();

  it('finds the locale objects', () => {
    expect(Object.keys(modules).length).toBeGreaterThan(20);
    expect(texts.length).toBeGreaterThan(500);
  });

  it('never uses typographic quotes ’ ‘ as Uzbek letters', () => {
    const bad = texts.filter((t) => BAD.test(t.text)).map((t) => `${t.path}: ${t.text.slice(0, 80)}`);
    expect(bad).toEqual([]);
  });

  it("never uses an ASCII apostrophe for oʻ/gʻ (o'z → oʻz)", () => {
    const bad = texts.filter((t) => !t.source && ASCII_OG.test(t.text)).map((t) => `${t.path}: ${t.text.slice(0, 80)}`);
    expect(bad).toEqual([]);
  });
});
