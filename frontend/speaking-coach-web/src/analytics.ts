// Maxfiylikni hurmat qiladigan analitika (Plausible Events API).
//
// Qoidalar:
// - Faqat VITE_PLAUSIBLE_DOMAIN berilganda ishlaydi (feature flag: o'chirish uchun env'ni olib tashlab qayta deploy).
// - Cookie yo'q, foydalanuvchi ID'si yo'q, email yo'q, ovoz yo'q. Do Not Track yoqilgan bo'lsa — hech narsa yuborilmaydi.
// - Hodisa nomlari va xossalari QAT'IY ro'yxatda (ALLOWED). Boshqa narsa yuborilmaydi.
// - Manzillardan faqat birinchi bo'lak yuboriladi: "join/ABC123" → "join", "teacher/<id>" → "teacher".
// - Hech qachon xato tashlamaydi va ilovani sekinlashtirmaydi (fire-and-forget).

export type EventName =
  | 'pageview'
  | 'signup'
  | 'first_practice_started'
  | 'first_practice_completed'
  | 'review_completed'
  | 'mock_started'
  | 'mock_completed'
  | 'mic_consent_accepted'
  | 'tutor_started'
  | 'tutor_completed'
  | 'pwa_installed';

/** Har bir hodisa uchun ruxsat etilgan xossa nomlari. Qiymatlar qisqa va umumiy (shaxsiy emas). */
const ALLOWED: Record<EventName, readonly string[]> = {
  pageview: [],
  signup: [],
  first_practice_started: ['kind'],
  first_practice_completed: ['kind'],
  review_completed: [],
  mock_started: ['section'],
  mock_completed: ['section'],
  mic_consent_accepted: [],
  tutor_started: [],
  tutor_completed: [],
  pwa_installed: [],
};

const SAFE_VALUE = /^[a-z0-9-]{1,32}$/;
const PRACTICE_KINDS = new Set(['speaking', 'writing', 'reading', 'listening']);

/** Xossalarni tozalaydi: faqat ruxsat etilgan kalitlar va xavfsiz (kichik harf/raqam/tire) qiymatlar. */
export function cleanProps(name: EventName, props?: Record<string, unknown>): Record<string, string> | undefined {
  if (!props) return undefined;
  const out: Record<string, string> = {};
  for (const key of ALLOWED[name]) {
    const v = props[key];
    if (typeof v === 'string' && SAFE_VALUE.test(v)) out[key] = v;
  }
  return Object.keys(out).length ? out : undefined;
}

/** "join/ABC123" → "join": faqat bo'lim nomi, identifikatorlarsiz. */
export function routeToPath(route: string): string {
  const first = route.split('?')[0].split('/')[0].toLowerCase();
  return SAFE_VALUE.test(first) ? first : 'other';
}

export interface RouteEvent {
  name: EventName;
  props?: Record<string, string>;
  /** Brauzer bo'yicha bir marta (birinchi marta) — localStorage belgisi bilan. */
  once?: boolean;
}

/** Marshrutga qarab qaysi hodisalar yuborilishi kerak (toza funksiya — testlanadi). */
export function routeEvents(route: string): RouteEvent[] {
  const [section = '', sub = '', third = '', fourth = ''] = route.split('?')[0].split('/');
  const events: RouteEvent[] = [];
  // Onboarding — hisob yangi ochilganda ko'rsatiladi: "signup" shu yerda hisoblanadi (hamma kirish usullari uchun bir xil).
  if (section === 'welcome') events.push({ name: 'signup', once: true });
  if (section === 'practice' && PRACTICE_KINDS.has(sub)) {
    events.push({ name: 'first_practice_started', props: { kind: sub }, once: true });
  }
  if (section === 'mock') {
    const running =
      (sub === 'full' && !!third) || sub === 'cefr-full' || (!!sub && (third === 'ai' || third === 'next' || (third === 't' && !!fourth)));
    const clean = cleanProps('mock_started', { section: sub });
    if (running && clean) events.push({ name: 'mock_started', props: clean });
  }
  return events;
}

/** API muvaffaqiyatli bo'lgach: qaysi hodisa (yo'l bo'yicha, javob tanasini o'qimasdan). */
export function apiEvent(method: string, path: string): RouteEvent | null {
  if (method.toUpperCase() !== 'POST') return null;
  const p = path.split('?')[0];
  const practice: [RegExp, string][] = [
    [/^\/api\/speaking\/submit$/, 'speaking'],
    [/^\/api\/writing\/submit$/, 'writing'],
    [/^\/api\/part2\/submit$/, 'speaking'],
    [/^\/api\/reading\/submit$/, 'reading'],
    [/^\/api\/listening\/submit$/, 'listening'],
  ];
  for (const [re, kind] of practice) {
    if (re.test(p)) return { name: 'first_practice_completed', props: { kind }, once: true };
  }
  // AI ustoz: dars boshlandi / tugatildi (identifikatorsiz, xossasiz).
  if (p === '/api/tutor/start') return { name: 'tutor_started' };
  if (/^\/api\/tutor\/[0-9a-f-]{36}\/finish$/i.test(p)) return { name: 'tutor_completed' };
  const mock = /^\/api\/mock\/(ielts|cefr)\/(speaking|writing|listening|reading)$/.exec(p);
  if (mock) return { name: 'mock_completed', props: { section: `${mock[1]}-${mock[2]}` } };
  return null;
}

// ---------------------------------------------------------------------------
// Yuborish
// ---------------------------------------------------------------------------

const ENDPOINT = 'https://plausible.io/api/event';
const FLAG_PREFIX = 'sc.an.';

function domain(): string | null {
  const d = (import.meta.env.VITE_PLAUSIBLE_DOMAIN as string | undefined)?.trim();
  return d ? d : null;
}

function optedOut(): boolean {
  try {
    if (typeof navigator !== 'undefined' && (navigator.doNotTrack === '1' || (window as { doNotTrack?: string }).doNotTrack === '1')) return true;
    return localStorage.getItem('sc.analytics') === 'off';
  } catch {
    return false;
  }
}

export function analyticsEnabled(): boolean {
  return domain() !== null && !optedOut();
}

function alreadyDone(key: string): boolean {
  try {
    return localStorage.getItem(FLAG_PREFIX + key) === '1';
  } catch {
    return false;
  }
}

function markDone(key: string) {
  try {
    localStorage.setItem(FLAG_PREFIX + key, '1');
  } catch {
    // localStorage yo'q — hodisa har safar yuborilishi mumkin; muhim emas.
  }
}

function send(name: EventName, path: string, props?: Record<string, string>) {
  const d = domain();
  if (!d) return;
  try {
    const url = `${window.location.origin}/${path === 'home' ? '' : path}`;
    void fetch(ENDPOINT, {
      method: 'POST',
      // text/plain — oldindan so'rov (preflight) kerak bo'lmasligi uchun (Plausible skripti ham shunday qiladi).
      headers: { 'Content-Type': 'text/plain' },
      body: JSON.stringify({ n: name, u: url, d, p: props }),
      keepalive: true,
    }).catch(() => undefined);
  } catch {
    // Analitika hech qachon ilovani buzmasligi kerak.
  }
}

let lastPath = '';

/** Sahifa ko'rinishi (faqat bo'lim nomi bilan). Ketma-ket bir xil bo'limda takrorlanmaydi. */
export function trackPageview(route: string) {
  if (!analyticsEnabled()) return;
  const path = routeToPath(route);
  if (path === lastPath) return;
  lastPath = path;
  send('pageview', path);
}

/** Hodisa yuborish. Noma'lum xossalar tashlab yuboriladi. */
export function track(name: EventName, props?: Record<string, unknown>, opts: { once?: boolean } = {}) {
  if (!analyticsEnabled()) return;
  if (opts.once) {
    if (alreadyDone(name)) return;
    markDone(name);
  }
  send(name, routeToPath(window.location.hash.replace(/^#\/?/, '') || 'home'), cleanProps(name, props));
}

/** Marshrut o'zgarganda App chaqiradi. */
export function onRoute(route: string) {
  if (!analyticsEnabled()) return;
  trackPageview(route);
  for (const e of routeEvents(route)) track(e.name, e.props, { once: e.once });
}

/** api.ts muvaffaqiyatli javobdan keyin chaqiradi. */
export function onApiSuccess(method: string, path: string) {
  if (!analyticsEnabled()) return;
  const e = apiEvent(method, path);
  if (e) track(e.name, e.props, { once: e.once });
}

/** O'rnatish hodisasi: brauzer "appinstalled"ni yuborganda. */
export function initAnalytics() {
  if (typeof window === 'undefined') return;
  window.addEventListener('appinstalled', () => track('pwa_installed', undefined, { once: true }));
}
