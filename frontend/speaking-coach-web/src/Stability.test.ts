import { describe, expect, it } from 'vitest';
import { computeStats, verdict } from './Stability';
import { stabilityMsg } from './locales/stability';

describe('computeStats', () => {
  it("min, max, o'rtacha, farq va standart og'ishni hisoblaydi", () => {
    const s = computeStats('Grammatika', [72, 68, 75, 71, 70]);
    expect(s.label).toBe('Grammatika');
    expect(s.min).toBe(68);
    expect(s.max).toBe(75);
    expect(s.range).toBe(7);
    expect(s.avg).toBeCloseTo(71.2);
    expect(s.stdDev).toBeCloseTo(2.315, 2);
  });

  it("bir xil ballarda farq ham, og'ish ham 0", () => {
    const s = computeStats('Vazifa', [80, 80, 80]);
    expect(s.range).toBe(0);
    expect(s.stdDev).toBe(0);
  });
});

describe('verdict', () => {
  it('farq chegaralari: ≤5 barqaror, 6–15 oʻrtacha, >15 beqaror', () => {
    expect(verdict(5)).toEqual({ level: 'stable', cls: 'txt-great' });
    expect(verdict(6).level).toBe('medium');
    expect(verdict(15).level).toBe('medium');
    expect(verdict(16)).toEqual({ level: 'unstable', cls: 'txt-low' });
  });

  it('baho soʻzlari uch tilda', () => {
    expect(stabilityMsg.uz.verdict[verdict(7).level]).toBe('Oʻrtacha');
    expect(stabilityMsg.uz.verdict.stable).toBe('Barqaror');
    expect(stabilityMsg.uz.verdict.unstable).toBe('Beqaror');
    expect(stabilityMsg.ru.verdict[verdict(20).level]).toBe('Нестабильно');
    expect(stabilityMsg.en.verdict[verdict(0).level]).toBe('Stable');
  });

  it('urinishlar soni rus tilida toʻgʻri koʻplikda', () => {
    expect(stabilityMsg.ru.title(5)).toContain('5 попыток');
    expect(stabilityMsg.ru.progress(2, 5, 1)).toBe('Выполнено 2/5 (1 ошибка)...');
    expect(stabilityMsg.uz.progress(2, 5, 0)).toBe('2/5 bajarildi...');
  });
});
