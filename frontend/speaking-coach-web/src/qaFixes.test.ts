import { describe, expect, it } from 'vitest';
import { teacherMsg } from './locales/teacher';
import { upgradeMsg } from './locales/upgrade';
import { MIN_WORDS, wordCount } from './upgradeLogic';

describe('QA tuzatishlari', () => {
  it('taklif: o\'qituvchi ismi bo\'lmasa — tasodifiy kod emas, "O\'qituvchingiz"', () => {
    expect(teacherMsg.uz.joinInvite('9-B', null)).toBe('Oʻqituvchingiz sizni “9-B” guruhiga taklif qilmoqda.');
    expect(teacherMsg.en.joinInvite('9-B', 'Aziza')).toBe('Aziza is inviting you to “9-B”.');
    expect(teacherMsg.ru.joinInvite('9-B', null)).toContain('Ваш преподаватель');
  });

  it('javobni yaxshilash: qisqa javob oldindan tushuntiriladi (server chegarasi bilan bir xil)', () => {
    expect(MIN_WORDS).toBe(15);
    const short = 'I would like to learn to play guitar because music help me relax.';
    expect(wordCount(short)).toBeLessThan(MIN_WORDS);
    expect(upgradeMsg.uz.tooShort(13)).toContain('13 ta soʻz');
    expect(upgradeMsg.en.tooShort(13)).toContain('At least 15 words');
  });
});
