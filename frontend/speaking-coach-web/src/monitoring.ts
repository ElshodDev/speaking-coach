// Xato monitoringi (Sentry) — maxfiylik birinchi o'rinda.
//
// - Faqat VITE_SENTRY_DSN berilganda ishlaydi (feature flag). Berilmasa Sentry kodi umuman yuklanmaydi.
// - Sentry kutubxonasi dinamik import qilinadi: asosiy bundle'ga qo'shilmaydi.
// - Shaxsiy ma'lumot yuborilmaydi: foydalanuvchi, cookie, sarlavha, so'rov tanasi (ovoz/matn) kesib tashlanadi;
//   email va tokenlar matndan niqoblanadi; manzildan hash va so'rov qismi olib tashlanadi;
//   console / bosish / tarmoq izlari (breadcrumbs) yuborilmaydi.
// - Performance (traces) va session replay o'chiq.

const EMAIL = /[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/g;
const BEARER = /Bearer\s+[A-Za-z0-9._~+/=-]{8,}/gi;
const LONG_TOKEN = /\b[A-Fa-f0-9]{32,}\b/g;

/** Matndan email va tokenlarni niqoblaydi. */
export function scrubText(text: string): string {
  return text.replace(EMAIL, '[email]').replace(BEARER, 'Bearer [token]').replace(LONG_TOKEN, '[token]');
}

/** Manzildan hash (marshrut: kod/ID bo'lishi mumkin), so'rov qismi va foydalanuvchi ma'lumotini olib tashlaydi. */
export function scrubUrl(url: string): string {
  try {
    const u = new URL(url);
    return `${u.origin}${u.pathname}`;
  } catch {
    return url.split('#')[0].split('?')[0];
  }
}

/** Sentry hodisasining kerakli qismi (to'liq tipni import qilmaslik uchun — kutubxona kech yuklanadi). */
export interface ScrubbableEvent {
  user?: unknown;
  message?: string;
  request?: { url?: string; cookies?: unknown; headers?: unknown; data?: unknown; query_string?: unknown; env?: unknown };
  exception?: { values?: { value?: string }[] };
  breadcrumbs?: unknown[];
  extra?: unknown;
}

/** Hodisani yuborishdan oldin tozalaydi. null qaytarsa — hodisa yuborilmaydi. */
export function scrubEvent<T extends ScrubbableEvent>(event: T): T {
  delete event.user;
  delete event.extra;
  delete event.breadcrumbs;
  if (event.request) {
    delete event.request.cookies;
    delete event.request.headers;
    delete event.request.data;
    delete event.request.query_string;
    delete event.request.env;
    if (event.request.url) event.request.url = scrubUrl(event.request.url);
  }
  if (typeof event.message === 'string') event.message = scrubText(event.message);
  for (const ex of event.exception?.values ?? []) {
    if (typeof ex.value === 'string') ex.value = scrubText(ex.value);
  }
  return event;
}

/** Shovqin: foydalanuvchi tarmog'i va brauzer kengaytmalari — bizning xatomiz emas. */
export const IGNORED_ERRORS = [
  'Failed to fetch',
  'Load failed',
  'NetworkError',
  'AbortError',
  'ResizeObserver loop',
  'Non-Error promise rejection captured',
];

type SentryModule = typeof import('@sentry/browser');
let sentry: SentryModule | null = null;

/** VITE_SENTRY_DSN berilgan bo'lsa — Sentry'ni kech (dinamik) yuklab, sozlaydi. */
export async function initMonitoring(): Promise<void> {
  const dsn = (import.meta.env.VITE_SENTRY_DSN as string | undefined)?.trim();
  if (!dsn) return;
  try {
    const mod = await import('@sentry/browser');
    mod.init({
      dsn,
      environment: (import.meta.env.VITE_SENTRY_ENV as string | undefined) || (import.meta.env.PROD ? 'production' : 'development'),
      release: (import.meta.env.VITE_SENTRY_RELEASE as string | undefined) || undefined,
      tracesSampleRate: 0,
      replaysSessionSampleRate: 0,
      replaysOnErrorSampleRate: 0,
      ignoreErrors: IGNORED_ERRORS,
      beforeBreadcrumb: () => null,
      beforeSend: (event) => scrubEvent(event as unknown as ScrubbableEvent) as unknown as typeof event,
    });
    sentry = mod;
  } catch {
    // Monitoring hech qachon ilovani buzmasligi kerak.
  }
}

/** ErrorBoundary kabi joylardan: xatoni (agar monitoring yoqilgan bo'lsa) yuboradi. */
export function captureError(error: unknown): void {
  try {
    sentry?.captureException(error);
  } catch {
    // e'tiborsiz
  }
}
