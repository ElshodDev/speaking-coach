import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  AI_SKILLS,
  AI_VERDICTS,
  RATE_FIRST_EVENT,
  SOURCES,
  aiKey,
  firstPracticeKey,
  isFirstPracticeRequest,
  isRatingKey,
  markAsked,
  noteRatingTrigger,
  percent,
  promptsEnabled,
  ratingPage,
  ratingsSummary,
  sendRating,
  sourceKey,
  wasAsked,
} from './ratings';

// Vitest Node muhitida localStorage va window yo'q — oddiy nusxalar.
const mem = new Map<string, string>();
(globalThis as { localStorage?: unknown }).localStorage = {
  getItem: (k: string) => mem.get(k) ?? null,
  setItem: (k: string, v: string) => void mem.set(k, v),
  removeItem: (k: string) => void mem.delete(k),
};

beforeEach(() => mem.clear());
afterEach(() => {
  vi.unstubAllEnvs();
  delete (globalThis as { window?: unknown }).window;
});

describe('kalitlar (backend RatingRules bilan bir xil ro‘yxat)', () => {
  it('hamma yaratiladigan kalit yaroqli', () => {
    expect(isRatingKey(firstPracticeKey(true))).toBe(true);
    expect(isRatingKey(firstPracticeKey(false))).toBe(true);
    for (const s of AI_SKILLS) for (const v of AI_VERDICTS) expect(isRatingKey(aiKey(s, v))).toBe(true);
    for (const s of SOURCES) expect(isRatingKey(sourceKey(s))).toBe(true);
  });

  it('noma‘lum yoki hiyla kalitlar rad etiladi', () => {
    for (const bad of ['', 'first_practice:maybe', 'ai_fair:listening:fair', 'ai_fair:speaking:great', 'source:tiktok', 'source:instagram ', 'SOURCE:instagram', 'first_practice:up\nsource:other', '<script>', 'first_practice:up;x']) {
      expect(isRatingKey(bad)).toBe(false);
    }
  });
});

describe('ratingsSummary', () => {
  it('kalitlarni to‘g‘ri guruhlaydi va noma‘lumlarini tashlaydi', () => {
    const s = ratingsSummary([
      { key: 'first_practice:up', count: 7 },
      { key: 'first_practice:down', count: 3 },
      { key: 'ai_fair:speaking:fair', count: 6 },
      { key: 'ai_fair:speaking:high', count: 3 },
      { key: 'ai_fair:speaking:low', count: 1 },
      { key: 'ai_fair:writing:fair', count: 2 },
      { key: 'source:instagram', count: 4 },
      { key: 'source:friend', count: 1 },
      { key: 'garbage', count: 99 },
      { key: 'source:instagram', count: -5 },
    ]);
    expect(s.firstPractice).toEqual({ up: 7, down: 3, total: 10 });
    expect(s.ai.speaking).toEqual({ fair: 6, high: 3, low: 1, total: 10 });
    expect(s.ai.writing.total).toBe(2);
    expect(s.aiAll.total).toBe(12);
    expect(s.source.instagram).toBe(4);
    expect(s.sourceTotal).toBe(5);
  });

  it('bo‘sh ro‘yxat — hammasi nol', () => {
    const s = ratingsSummary([]);
    expect(s.firstPractice.total + s.aiAll.total + s.sourceTotal).toBe(0);
  });

  it('percent: nolga bo‘lmaydi va yaxlitlaydi', () => {
    expect(percent(1, 0)).toBe(0);
    expect(percent(1, 3)).toBe(33);
    expect(percent(2, 3)).toBe(67);
  });
});

describe('sendRating', () => {
  it('yaroqsiz kalit yuborilmaydi', async () => {
    const post = vi.fn();
    expect(await sendRating('nope', post)).toBe(false);
    expect(post).not.toHaveBeenCalled();
  });

  it('yaroqli kalit /api/rating ga bo‘lim nomi bilan yuboriladi', async () => {
    const calls: [string, unknown][] = [];
    (globalThis as { window?: unknown }).window = { location: { hash: '#/practice/speaking' } };
    expect(await sendRating('first_practice:up', async (p, b) => void calls.push([p, b]))).toBe(true);
    expect(calls).toEqual([['/api/rating', { key: 'first_practice:up', page: '#/practice' }]]);
  });

  it('server xatosi ilovani buzmaydi', async () => {
    expect(await sendRating('source:other', async () => Promise.reject(new Error('boom')))).toBe(false);
  });

  it('sahifa — faqat bo‘lim nomi (identifikatorsiz)', () => {
    expect(ratingPage('#/join/ABC123')).toBe('#/join');
    expect(ratingPage('')).toBe('#/home');
    expect(ratingPage('#/practice/writing')).toBe('#/practice');
  });
});

describe('bir marta so‘rash va flag', () => {
  it('markAsked / wasAsked', () => {
    expect(wasAsked('first')).toBe(false);
    markAsked('first');
    expect(wasAsked('first')).toBe(true);
    expect(wasAsked('source')).toBe(false);
  });

  it('flag: VITE_FEEDBACK_PROMPTS=off bilan o‘chadi, boshqa hollarda yoqiq', () => {
    expect(promptsEnabled()).toBe(true);
    vi.stubEnv('VITE_FEEDBACK_PROMPTS', 'off');
    expect(promptsEnabled()).toBe(false);
    vi.stubEnv('VITE_FEEDBACK_PROMPTS', 'OFF');
    expect(promptsEnabled()).toBe(false);
    vi.stubEnv('VITE_FEEDBACK_PROMPTS', 'on');
    expect(promptsEnabled()).toBe(true);
  });
});

describe('birinchi mashq tetigi', () => {
  it('faqat mashq yuborishlari birinchi mashq hisoblanadi', () => {
    expect(isFirstPracticeRequest('POST', '/api/speaking/submit')).toBe(true);
    expect(isFirstPracticeRequest('POST', '/api/writing/submit?save=false')).toBe(true);
    expect(isFirstPracticeRequest('POST', '/api/part2/submit')).toBe(true);
    expect(isFirstPracticeRequest('GET', '/api/speaking/submit')).toBe(false);
    expect(isFirstPracticeRequest('POST', '/api/review/cards')).toBe(false);
  });

  function fakeWindow() {
    const events: string[] = [];
    (globalThis as { window?: unknown }).window = { dispatchEvent: (e: Event) => void events.push(e.type) };
    return events;
  }

  it('birinchi mashqdan keyin bir marta hodisa yuboriladi', () => {
    const events = fakeWindow();
    noteRatingTrigger('POST', '/api/speaking/submit');
    expect(events).toEqual([RATE_FIRST_EVENT]);
  });

  it('boshqa so‘rovlarda, so‘ralgan bo‘lsa yoki flag o‘chiq bo‘lsa — hodisa yo‘q', () => {
    const events = fakeWindow();
    noteRatingTrigger('POST', '/api/review/cards');
    markAsked('first');
    noteRatingTrigger('POST', '/api/speaking/submit');
    mem.clear();
    vi.stubEnv('VITE_FEEDBACK_PROMPTS', 'off');
    noteRatingTrigger('POST', '/api/speaking/submit');
    expect(events).toEqual([]);
  });
});
