import { describe, expect, it } from 'vitest';
import { computeStats } from './Stability';

describe('computeStats', () => {
  it("min, max, o'rtacha, farq va standart og'ishni hisoblaydi", () => {
    const s = computeStats('Grammatika', [72, 68, 75, 71, 70]);
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
