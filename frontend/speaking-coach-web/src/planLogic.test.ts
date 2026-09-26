import { describe, expect, it } from 'vitest';
import { doneCount, examDateRange, localIso, mockParts, orderItems, shouldOnboard, stepsFor, targetsFor, type PlanItem } from './planLogic';
import { planMsg } from './locales/plan';

const item = (key: string, done: boolean): PlanItem => ({ key, kind: key.startsWith('mock') ? 'mock' : key === 'review' ? 'review' : 'practice', route: '', done, minutes: 10, progress: null, target: null });

describe('tanishtiruv', () => {
  it('umumiy ingliz tilida maqsad/sana qadami yoʻq', () => {
    expect(stepsFor('general')).toEqual(['goal', 'level', 'minutes']);
    expect(stepsFor('ielts')).toEqual(['goal', 'level', 'target', 'minutes']);
    expect(stepsFor(null)).toHaveLength(4);
  });
  it('maqsad ball imtihonga mos', () => {
    expect(targetsFor('ielts')).toContain('6.5');
    expect(targetsFor('cefr')).toEqual(['B1', 'B2', 'C1']);
    expect(targetsFor('general')).toEqual([]);
  });
  it('faqat birinchi kirishda va faqat bosh sahifada', () => {
    expect(shouldOnboard('home', { onboarded: false })).toBe(true);
    expect(shouldOnboard('home', { onboarded: true })).toBe(false);
    expect(shouldOnboard('join/ABC234', { onboarded: false })).toBe(false); // taklif havolasini buzmaydi
    expect(shouldOnboard('home', {})).toBe(false); // eski server javobi — koʻrsatmaymiz
    expect(shouldOnboard('home', null)).toBe(false);
  });
  it('sana chegaralari: bugundan 2 yilgacha, mahalliy sana', () => {
    const now = new Date(2026, 8, 27, 23, 30);
    expect(localIso(now)).toBe('2026-09-27');
    expect(examDateRange(now)).toEqual({ min: '2026-09-27', max: '2028-09-27' });
  });
});

describe('bugungi reja', () => {
  it('bajarilganlar pastga tushadi, tartib saqlanadi', () => {
    const items = [item('review', true), item('practice:writing', false), item('mock:cefr:reading', false)];
    expect(orderItems(items).map((i) => i.key)).toEqual(['practice:writing', 'mock:cefr:reading', 'review']);
    expect(doneCount(items)).toBe(1);
  });
  it('mock kaliti', () => {
    expect(mockParts('mock:cefr:reading')).toEqual(['cefr', 'reading']);
  });
  it('matnlar uch tilda', () => {
    expect(planMsg.uz.countdown(12, 'IELTS')).toBe('IELTS imtihonigacha 12 kun');
    expect(planMsg.ru.countdown(2, 'CEFR')).toBe('До экзамена CEFR: 2 дня');
    expect(planMsg.en.countdown(1, 'IELTS')).toBe('1 day to your IELTS exam');
    expect(planMsg.en.items.review(3, 10)).toBe('🔁 Review — 3/10 cards');
    for (const m of [planMsg.uz, planMsg.ru, planMsg.en]) {
      expect(Object.keys(m.goals)).toEqual(['ielts', 'cefr', 'general']);
      expect(Object.keys(m.levels)).toEqual(['A2', 'B1', 'B2', 'C1']);
    }
  });
});
