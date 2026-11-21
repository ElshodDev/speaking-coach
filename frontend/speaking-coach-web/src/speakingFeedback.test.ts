import { describe, expect, it } from 'vitest';
import { speakingMsg } from './locales/speaking';
import { bandTone, criteriaRows, formatBand, otherCorrections, overallBandOf, priorityFixOf, type SpeakingEvaluationLike } from './speakingFeedback';

const base: SpeakingEvaluationLike = {
  fluency: { score: 62, reasoning: 'F' },
  grammar: { score: 55, reasoning: 'G' },
  vocabulary: { score: 60, reasoning: 'V' },
  topCorrections: [{ original: 'I have many parks', corrected: 'There are many parks', explanation: 'use there are' }],
};
const bands = { fluency: 6, lexical: 5.5, grammar: 5, pronunciation: 6.5 };

describe('gapirish natijasi: 4 mezon', () => {
  it('eski natija (bands yo\'q) — null: eski ko\'rinish ishlaydi', () => {
    expect(criteriaRows(base)).toBeNull();
    expect(overallBandOf(base)).toBeNull();
    expect(criteriaRows({ ...base, bands: null })).toBeNull();
  });

  it('yaroqsiz bandlar — null (NaN, 9 dan katta, yo\'q maydon)', () => {
    expect(criteriaRows({ ...base, bands: { ...bands, grammar: 11 } })).toBeNull();
    expect(criteriaRows({ ...base, bands: { ...bands, fluency: Number.NaN } })).toBeNull();
    expect(criteriaRows({ ...base, bands: { fluency: 6, lexical: 6, grammar: 6 } as never })).toBeNull();
  });

  it('to\'rt mezon: tartib, band va tegishli izoh (lug\'at izohi — lexical)', () => {
    const rows = criteriaRows({ ...base, bands, pronunciation: { score: 70, reasoning: 'P' } })!;
    expect(rows.map((r) => r.key)).toEqual(['fluency', 'lexical', 'grammar', 'pronunciation']);
    expect(rows.map((r) => r.band)).toEqual([6, 5.5, 5, 6.5]);
    expect(rows.map((r) => r.reasoning)).toEqual(['F', 'V', 'G', 'P']);
    // Talaffuz izohi yo'q bo'lsa ham qator bor (bo'sh izoh).
    expect(criteriaRows({ ...base, bands })![3].reasoning).toBe('');
  });

  it('umumiy band: serverdagisi; bo\'lmasa o\'rtacha yarim bandgacha', () => {
    expect(overallBandOf({ ...base, bands, overallBand: 6 })).toBe(6);
    expect(overallBandOf({ ...base, bands })).toBe(6); // (6+5.5+5+6.5)/4 = 5.75 → 6.0
    expect(overallBandOf({ ...base, bands: { fluency: 5, lexical: 5, grammar: 5, pronunciation: 5.5 } })).toBe(5); // 5.125 → 5.0
  });

  it('format va rang', () => {
    expect(formatBand(6)).toBe('6.0');
    expect(formatBand(5.5)).toBe('5.5');
    expect(bandTone(7)).toBe('great');
    expect(bandTone(5.5)).toBe('mid');
    expect(bandTone(4.5)).toBe('low');
  });

  it('bitta asosiy tuzatish: serverdagisi, yo\'qsa birinchi tuzatish, hech narsa yo\'q bo\'lsa null', () => {
    const fix = { issue: 'Use there are', original: 'I have many parks', corrected: 'There are many parks', tip: 'Say there is/are.' };
    expect(priorityFixOf({ ...base, priorityFix: fix })).toEqual(fix);
    expect(priorityFixOf(base)).toEqual({ issue: 'use there are', original: 'I have many parks', corrected: 'There are many parks', tip: '' });
    // Yaroqsiz fix (bo'sh maslahat) — birinchi tuzatishga qaytadi.
    expect(priorityFixOf({ ...base, priorityFix: { ...fix, tip: ' ' } })!.issue).toBe('use there are');
    expect(priorityFixOf({ ...base, topCorrections: [] })).toBeNull();
  });

  it('asosiy tuzatish "Tuzatishlar" ro\'yxatida takrorlanmaydi', () => {
    const two = { ...base, topCorrections: [base.topCorrections[0], { original: 'I am agree', corrected: 'I agree', explanation: 'no am' }] };
    // Server fix'i birinchi tuzatish bilan bir xil asl iboradan: ro'yxatda faqat ikkinchisi qoladi.
    const fix = { issue: 'i', original: 'I have many parks', corrected: 'There are many parks', tip: 't' };
    expect(otherCorrections({ ...two, priorityFix: fix }).map((c) => c.original)).toEqual(['I am agree']);
    // Fix serverdan kelmasa — birinchi tuzatish asosiy hisoblanadi va ro'yxatdan chiqadi.
    expect(otherCorrections(two).map((c) => c.original)).toEqual(['I am agree']);
    // Hech narsa yo'q bo'lsa — ro'yxat o'zgarmaydi.
    expect(otherCorrections({ ...base, topCorrections: [] })).toEqual([]);
  });

  it('matnlar uch tilda to\'liq; talaffuz "taxminiy" deb belgilangan', () => {
    for (const m of [speakingMsg.uz, speakingMsg.ru, speakingMsg.en]) {
      const f = m.four;
      for (const k of ['overall', 'estimate', 'overallNote', 'pronunciationNote', 'priorityTitle', 'priorityHint', 'tip'] as const) expect(f[k].length).toBeGreaterThan(2);
      for (const k of ['fluency', 'lexical', 'grammar', 'pronunciation'] as const) expect(f.criteria[k].length).toBeGreaterThan(2);
    }
    expect(speakingMsg.uz.four.estimate).toBe('taxminiy');
    expect(speakingMsg.uz.four.pronunciationNote).toMatch(/kam ishonchli/);
  });
});
