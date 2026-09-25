import { describe, expect, it } from 'vitest';
import { calendarWeekdayLabels, heatLevel, niceMax, parseDay, shortDate, toWeeks } from './charts';
import { chartsMsg } from './locales/charts';

describe('heatLevel', () => {
  it("harakatlar sonini 0–4 bosqichga bo'ladi", () => {
    expect([0, 1, 2, 3, 4, 6, 7, 30].map(heatLevel)).toEqual([0, 1, 2, 2, 3, 3, 4, 4]);
  });
});

describe('toWeeks', () => {
  it("haftalarni dushanbadan boshlaydi, boshini bo'sh katak bilan to'ldiradi", () => {
    // 2026-09-24 — payshanba (dushanbadan 3 kun keyin)
    const days = ['2026-09-24', '2026-09-25', '2026-09-26', '2026-09-27', '2026-09-28'].map((date) => ({ date, count: 1 }));
    const weeks = toWeeks(days);
    expect(weeks).toHaveLength(2);
    expect(weeks[0].slice(0, 3)).toEqual([null, null, null]);
    expect(weeks[0][3]?.date).toBe('2026-09-24');
    expect(weeks[1][0]?.date).toBe('2026-09-28'); // keyingi dushanba
  });
});

describe('niceMax', () => {
  it('y o\'qi uchun 1-2-5 qadamli chegara', () => {
    expect([0, 1, 3, 7, 12, 48, 51].map(niceMax)).toEqual([1, 1, 5, 10, 20, 50, 100]);
  });
});

describe('parseDay', () => {
  it("faqat-sana satrini mahalliy kun sifatida o'qiydi (UTC emas)", () => {
    const d = parseDay('2026-09-25');
    expect([d.getFullYear(), d.getMonth(), d.getDate()]).toEqual([2026, 8, 25]);
    expect(shortDate('2026-09-05')).toBe('5.09');
  });
});

describe('calendarWeekdayLabels', () => {
  it('faqat dushanba, chorshanba, juma va yakshanbani yozadi', () => {
    expect(calendarWeekdayLabels(chartsMsg.uz.weekdays)).toEqual(['Du', '', 'Ch', '', 'Ju', '', 'Ya']);
    expect(calendarWeekdayLabels(chartsMsg.ru.weekdays)).toEqual(['Пн', '', 'Ср', '', 'Пт', '', 'Вс']);
  });
});

describe('chartsMsg', () => {
  it('kalendar xulosasi uch tilda, rus koʻplik shakllari bilan', () => {
    expect(chartsMsg.uz.calendarSummary(84, 12)).toBe('84 kunda 12 kun faol');
    expect(chartsMsg.ru.calendarSummary(84, 21)).toBe('Активны 21 день из 84');
    expect(chartsMsg.ru.calendarAria(84, 3)).toBe('За последние 84 дня вы были активны 3 дня');
    expect(chartsMsg.en.calendarSummary(84, 1)).toBe('1 active day out of 84');
    expect(chartsMsg.ru.actions(5)).toBe('действий');
  });
});
