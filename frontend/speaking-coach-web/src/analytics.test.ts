import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { analyticsEnabled, apiEvent, cleanProps, onApiSuccess, onRoute, routeEvents, routeToPath, track } from './analytics';

describe('analytics: toza funksiyalar', () => {
  it('manzildan faqat bo\'lim nomi olinadi (kod va identifikatorlarsiz)', () => {
    expect(routeToPath('join/K7M2QX')).toBe('join');
    expect(routeToPath('teacher/8e6a-1c')).toBe('teacher');
    expect(routeToPath('practice/speaking')).toBe('practice');
    expect(routeToPath('mock/full/academic/abc')).toBe('mock');
    expect(routeToPath('')).toBe('other');
    expect(routeToPath('Weird%20Path')).toBe('other');
  });

  it('xossalar: faqat ruxsat etilgan kalit va xavfsiz qiymat', () => {
    expect(cleanProps('first_practice_started', { kind: 'speaking', email: 'a@b.uz' })).toEqual({ kind: 'speaking' });
    expect(cleanProps('first_practice_started', { kind: 'A@b.uz' })).toBeUndefined();
    expect(cleanProps('mock_started', { section: 'ielts-listening', token: 'x' })).toEqual({ section: 'ielts-listening' });
    expect(cleanProps('signup', { anything: 'x' })).toBeUndefined();
    expect(cleanProps('review_completed', undefined)).toBeUndefined();
  });

  it('marshrut → hodisalar', () => {
    expect(routeEvents('welcome')).toEqual([{ name: 'signup', once: true }]);
    expect(routeEvents('practice/writing')).toEqual([{ name: 'first_practice_started', props: { kind: 'writing' }, once: true }]);
    expect(routeEvents('practice')).toEqual([]);
    expect(routeEvents('home')).toEqual([]);
    // Mock: ro'yxat sahifasi — boshlanish emas; test tanlangach yoki AI/keyingi — boshlanish.
    expect(routeEvents('mock/listening')).toEqual([]);
    expect(routeEvents('mock/listening/t/abc123')).toEqual([{ name: 'mock_started', props: { section: 'listening' } }]);
    expect(routeEvents('mock/reading/ai')).toEqual([{ name: 'mock_started', props: { section: 'reading' } }]);
    expect(routeEvents('mock/cefr-full')).toEqual([{ name: 'mock_started', props: { section: 'cefr-full' } }]);
    expect(routeEvents('mock/full/academic/xyz')).toEqual([{ name: 'mock_started', props: { section: 'full' } }]);
    expect(routeEvents('mock/result/123')).toEqual([]);
  });

  it('API muvaffaqiyati → hodisa (faqat POST, yo\'l bo\'yicha)', () => {
    expect(apiEvent('POST', '/api/speaking/submit?save=false')).toEqual({ name: 'first_practice_completed', props: { kind: 'speaking' }, once: true });
    expect(apiEvent('POST', '/api/reading/submit')).toEqual({ name: 'first_practice_completed', props: { kind: 'reading' }, once: true });
    expect(apiEvent('POST', '/api/mock/ielts/listening')).toEqual({ name: 'mock_completed', props: { section: 'ielts-listening' } });
    expect(apiEvent('POST', '/api/mock/cefr/speaking')).toEqual({ name: 'mock_completed', props: { section: 'cefr-speaking' } });
    expect(apiEvent('GET', '/api/mock/ielts/listening/new')).toBeNull();
    expect(apiEvent('POST', '/api/auth/login')).toBeNull();
  });
});

describe('analytics: yuborish', () => {
  const fetchMock = vi.fn(() => Promise.resolve(new Response(null, { status: 202 })));

  // Testlar Node muhitida: brauzer ob'ektlarining eng kichik soxta nusxalari.
  function fakeBrowser() {
    const store = new Map<string, string>();
    vi.stubGlobal('localStorage', {
      getItem: (k: string) => store.get(k) ?? null,
      setItem: (k: string, v: string) => void store.set(k, v),
      clear: () => store.clear(),
    });
    vi.stubGlobal('window', { location: { hash: '#/practice/speaking', origin: 'https://fluentuz.app' }, addEventListener: () => undefined });
    vi.stubGlobal('navigator', { doNotTrack: null });
  }

  beforeEach(() => {
    fetchMock.mockClear();
    vi.stubGlobal('fetch', fetchMock);
    fakeBrowser();
  });
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('flag yo\'q (VITE_PLAUSIBLE_DOMAIN bo\'sh) — hech narsa yuborilmaydi', () => {
    vi.stubEnv('VITE_PLAUSIBLE_DOMAIN', '');
    expect(analyticsEnabled()).toBe(false);
    track('signup');
    onRoute('welcome');
    onApiSuccess('POST', '/api/speaking/submit');
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('yoqilganda: to\'g\'ri tanada, shaxsiy ma\'lumotsiz; "once" hodisa bir marta', () => {
    vi.stubEnv('VITE_PLAUSIBLE_DOMAIN', 'fluentuz.app');
    expect(analyticsEnabled()).toBe(true);
    track('first_practice_started', { kind: 'speaking', email: 'secret@x.uz' }, { once: true });
    track('first_practice_started', { kind: 'speaking' }, { once: true });
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toBe('https://plausible.io/api/event');
    const body = JSON.parse(String(init.body));
    expect(body).toMatchObject({ n: 'first_practice_started', d: 'fluentuz.app', p: { kind: 'speaking' } });
    expect(JSON.stringify(body)).not.toContain('secret');
    expect(Object.keys(body).sort()).toEqual(['d', 'n', 'p', 'u']);
  });

  it('opt-out: localStorage sc.analytics=off — hech narsa yuborilmaydi', () => {
    vi.stubEnv('VITE_PLAUSIBLE_DOMAIN', 'fluentuz.app');
    localStorage.setItem('sc.analytics', 'off');
    track('signup');
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('Do Not Track yoqilgan bo\'lsa — hech narsa yuborilmaydi', () => {
    vi.stubEnv('VITE_PLAUSIBLE_DOMAIN', 'fluentuz.app');
    vi.stubGlobal('navigator', { doNotTrack: '1' });
    expect(analyticsEnabled()).toBe(false);
    track('signup');
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('fetch xato bersa — ilova buzilmaydi', () => {
    vi.stubEnv('VITE_PLAUSIBLE_DOMAIN', 'fluentuz.app');
    vi.stubGlobal('fetch', () => {
      throw new Error('boom');
    });
    fakeBrowser();
    expect(() => track('signup')).not.toThrow();
  });
});

describe('AI ustoz hodisalari', () => {
  it('dars boshlandi va tugatildi — identifikatorsiz, xossasiz', () => {
    expect(apiEvent('POST', '/api/tutor/start')).toEqual({ name: 'tutor_started' });
    expect(apiEvent('POST', '/api/tutor/0b7e6b0e-4c4b-4a53-9a43-1b2f3c4d5e6f/finish')).toEqual({ name: 'tutor_completed' });
  });

  it('boshqa ustoz so‘rovlari hodisa bermaydi', () => {
    expect(apiEvent('GET', '/api/tutor/start')).toBeNull();
    expect(apiEvent('POST', '/api/tutor/0b7e6b0e-4c4b-4a53-9a43-1b2f3c4d5e6f/answer')).toBeNull();
    expect(apiEvent('POST', '/api/tutor/0b7e6b0e-4c4b-4a53-9a43-1b2f3c4d5e6f/ask')).toBeNull();
    expect(apiEvent('POST', '/api/tutor/finish')).toBeNull();
    expect(apiEvent('POST', '/api/tutor/../x/finish')).toBeNull();
  });

  it('hodisalar xossa qabul qilmaydi (identifikator sizib chiqmaydi)', () => {
    expect(cleanProps('tutor_started', { topic: 'Modal verbs', id: 'abc' })).toBeUndefined();
    expect(cleanProps('tutor_completed', { email: 'a@b.uz' })).toBeUndefined();
  });
});
