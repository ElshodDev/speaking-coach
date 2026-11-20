import { describe, expect, it } from 'vitest';
import { analyzePauses, fillerLevel, fluencyTips, frameRms, mixToMono, type FluencyStats } from './fluencyLogic';

// Sintetik "yozuv": FRAME_MS (30 ms) li kadrlar ketma-ketligi — ovoz (0.3) yoki jimlik (0.002).
const voice = (s: number) => Array(Math.round((s * 1000) / 30)).fill(0.3);
const silence = (s: number) => Array(Math.round((s * 1000) / 30)).fill(0.002);

const base: FluencyStats = {
  words: 120, seconds: 60, wpm: 120, pace: 'natural', fillers: 1, fillersPerMinute: 1,
  topFillers: [], repetitions: 0, selfCorrections: 0, longPauses: 1, pauseSeconds: 1.2,
};

describe('analyzePauses', () => {
  it('counts only pauses inside speech, not leading/trailing silence', () => {
    const rms = [...silence(2), ...voice(3), ...silence(1.5), ...voice(2), ...silence(0.5), ...voice(2), ...silence(3)];
    const r = analyzePauses(rms)!;
    expect(r.longPauses).toBe(1); // 1.5 s — uzun; 0.5 s — oddiy nafas
    expect(r.pauseSeconds).toBeCloseTo(1.5, 1);
    expect(r.speechSeconds).toBeCloseTo(9, 0);
  });

  it('adapts to a noisy room', () => {
    const noisy = (a: number[]) => a.map((v) => (v < 0.01 ? 0.05 : 0.4));
    const r = analyzePauses(noisy([...voice(2), ...silence(2), ...voice(2)]))!;
    expect(r.longPauses).toBe(1);
  });

  it('returns null for silent or too short recordings', () => {
    expect(analyzePauses(silence(5))).toBeNull();
    expect(analyzePauses(voice(0.5))).toBeNull();
  });
});

describe('audio helpers', () => {
  it('frameRms computes per-frame loudness', () => {
    const samples = new Float32Array(1000).fill(0.5);
    const rms = frameRms(samples, 1000, 100); // 100 namunali kadrlar
    expect(rms).toHaveLength(10);
    expect(rms[0]).toBeCloseTo(0.5);
  });

  it('mixToMono averages channels', () => {
    const m = mixToMono([new Float32Array([1, 1]), new Float32Array([0, 1])]);
    expect(Array.from(m)).toEqual([0.5, 1]);
  });
});

describe('fluencyTips', () => {
  it('praises a natural pace with few fillers', () => {
    expect(fluencyTips(base)).toEqual(['great']);
  });

  it('puts fillers and pauses first, then pace', () => {
    expect(fluencyTips({ ...base, fillersPerMinute: 8, longPauses: 6, pace: 'slow', wpm: 70 })).toEqual(['fillers', 'pauses', 'slow']);
    expect(fluencyTips({ ...base, pace: 'fast', wpm: 200 })).toEqual(['fast']);
    expect(fluencyTips({ ...base, repetitions: 4 })).toEqual(['repeats']);
  });

  it('gives no verdict when timing is unknown', () => {
    expect(fluencyTips({ ...base, seconds: null, wpm: null, pace: 'unknown', fillersPerMinute: null, longPauses: null })).toEqual([]);
  });

  it('filler levels', () => {
    expect(fillerLevel(null)).toBeNull();
    expect(fillerLevel(1)).toBe('good');
    expect(fillerLevel(3)).toBe('mid');
    expect(fillerLevel(6)).toBe('low');
  });
});
