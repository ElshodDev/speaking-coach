import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { answerMicConsent, hasMicConsent, requestMicConsent, resetConsentForTests } from './consent';
import { consentMsg } from './locales/consent';

describe('mikrofon roziligi', () => {
  const fetchMock = vi.fn(() => Promise.resolve(new Response(null, { status: 202 })));

  beforeEach(() => {
    const store = new Map<string, string>();
    vi.stubGlobal('localStorage', { getItem: (k: string) => store.get(k) ?? null, setItem: (k: string, v: string) => void store.set(k, v), removeItem: (k: string) => void store.delete(k) });
    vi.stubGlobal('window', { location: { hash: '#/practice/speaking', origin: 'https://fluentuz.app' }, addEventListener: () => undefined });
    vi.stubGlobal('navigator', { doNotTrack: null });
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockClear();
    resetConsentForTests();
  });
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('birinchi marta so\'raladi; rozilik saqlanadi va keyingi safar so\'ralmaydi', async () => {
    expect(hasMicConsent()).toBe(false);
    const first = requestMicConsent();
    let settled = false;
    void first.then(() => (settled = true));
    await Promise.resolve();
    expect(settled).toBe(false); // dialog javobini kutyapti
    answerMicConsent(true);
    expect(await first).toBe(true);
    expect(hasMicConsent()).toBe(true);
    expect(await requestMicConsent()).toBe(true); // darhol, dialogsiz
  });

  it('bekor qilinsa — false va saqlanmaydi (keyingi safar yana so\'raladi)', async () => {
    const p = requestMicConsent();
    answerMicConsent(false);
    expect(await p).toBe(false);
    expect(hasMicConsent()).toBe(false);
    const again = requestMicConsent();
    answerMicConsent(true);
    expect(await again).toBe(true);
  });

  it('bir vaqtda bir nechta so\'rov — bitta javob hammasini hal qiladi', async () => {
    const a = requestMicConsent();
    const b = requestMicConsent();
    answerMicConsent(true);
    expect(await Promise.all([a, b])).toEqual([true, true]);
  });

  it('flag o\'chiq (VITE_MIC_CONSENT=off) — dialog yo\'q, darhol ruxsat', async () => {
    vi.stubEnv('VITE_MIC_CONSENT', 'off');
    expect(await requestMicConsent()).toBe(true);
    expect(hasMicConsent()).toBe(false);
  });

  it('qabul qilinganda analitika hodisasi bir marta, xossalarsiz yuboriladi', async () => {
    vi.stubEnv('VITE_PLAUSIBLE_DOMAIN', 'fluentuz.app');
    const p = requestMicConsent();
    answerMicConsent(true);
    await p;
    localStorage.removeItem('sc.micConsent.v1'); // rozilik o'chirilsa ham, hodisa takrorlanmaydi (brauzerga bir marta)
    const p2 = requestMicConsent();
    answerMicConsent(true);
    await p2;
    const sent = fetchMock.mock.calls.map((c) => JSON.parse(String((c as unknown as [string, RequestInit])[1].body)));
    expect(sent.filter((e) => e.n === 'mic_consent_accepted')).toHaveLength(1);
    expect(sent[0]).toMatchObject({ n: 'mic_consent_accepted', d: 'fluentuz.app' });
    expect(sent[0].p).toBeUndefined();
  });

  it('matn uch tilda ham to\'liq va ovoz AI\'ga yuborilishini aytadi', () => {
    for (const m of [consentMsg.uz, consentMsg.ru, consentMsg.en]) {
      for (const key of ['title', 'body', 'warning', 'accept', 'cancel', 'privacy'] as const) expect(m[key].length).toBeGreaterThan(3);
      expect(m.body).toMatch(/Gemini/);
    }
  });
});
