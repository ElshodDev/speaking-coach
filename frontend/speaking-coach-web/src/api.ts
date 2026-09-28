// Backend bilan barcha muloqot shu fayl orqali o'tadi: manzil (API_BASE),
// kirish tokeni va xato xabarlarini o'qish bir joyda.
import { common, getLang, msg } from './i18n';
import { networkMsg } from './locales/network';
import { noteServerResponse, trackRequest } from './netStatus';

// Lokalda .env fayl bo'lmasa, localhost:5000'ga tushadi. Vercel'da
// VITE_API_URL environment variable orqali Render manziliga yo'naltiriladi.
export const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const TOKEN_KEY = 'speakingCoach.token';

// Token localStorage'da saqlanadi — sahifa yangilanganda ham kirgan holat
// saqlanib qoladi. Ogohlantirish (README'da ham bor): agar saytda XSS
// zaifligi bo'lsa, begona skript localStorage'ni o'qiy oladi. Muqobili —
// httpOnly cookie, lekin frontend (vercel.app) va backend (onrender.com)
// turli domenlarda bo'lgani uchun cookie'lar brauzerlarda "third-party"
// hisoblanib bloklanishi mumkin. Bu loyiha uchun ongli tanlov.
export function getToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setToken(token: string | null) {
  try {
    if (token) localStorage.setItem(TOKEN_KEY, token);
    else {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(EMAIL_KEY);
    }
  } catch {
    // Brauzer saqlashni taqiqlagan bo'lsa (masalan maxfiy rejim sozlamasi) —
    // kirish shu sahifa yopilguncha ishlaydi, xolos.
  }
}

const EMAIL_KEY = 'speakingCoach.email';

/**
 * Oxirgi kirgan hisob emaili. Server uxlab yotganda (Render: 30–60 s) /me
 * javobini kutmasdan ilova kirgan holatda ochiladi — mehmon ekrani ko'rinib,
 * yozilgan ish "yo'qolib qolmaydi". Token bo'lmasa — hisobga olinmaydi.
 */
export function getSavedEmail(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY) ? localStorage.getItem(EMAIL_KEY) : null;
  } catch {
    return null;
  }
}

export function setSavedEmail(email: string | null) {
  try {
    if (email) localStorage.setItem(EMAIL_KEY, email);
    else localStorage.removeItem(EMAIL_KEY);
  } catch {
    // saqlab bo'lmasa — /me javobi kelguncha mehmon ko'rinishi, xolos
  }
}

/** apiFetch parametrlari: oddiy RequestInit + ixtiyoriy kutish muddati. */
export interface ApiInit extends RequestInit {
  /** Javob kutish muddati (ms). 0 — cheksiz. Berilmasa — yo'lga qarab tanlanadi. */
  timeoutMs?: number;
}

/** Oddiy so'rov: uxlab yotgan server uyg'onishiga (30–60 s) yetadi. */
export const DEFAULT_TIMEOUT_MS = 45_000;
/** AI baholash va audio yuklash — server uyg'onishi + Gemini javobi. */
export const AI_TIMEOUT_MS = 120_000;

// Gemini'ga murojaat qiladigan (yoki katta audio yuklaydigan) yo'llar.
const AI_PATHS = [
  /^\/api\/(speaking|writing)\/submit\b/,
  /^\/api\/(reading|listening)\/(generate|submit)\b/,
  /^\/api\/mock\/(ielts|cefr)\/[a-z-]+(\/new)?(\?|$)/,
  /^\/api\/talk\//,
  /^\/api\/shadowing\/check\b/,
  /^\/api\/(grammar\/quiz|topics\/suggest|words\/explain)\b/,
];

/** So'rov AI ishini kutadimi (uzoq davom etishi tabiiy). Toza funksiya — testlanadi. */
export const isAiPath = (path: string): boolean => AI_PATHS.some((re) => re.test(path));

/** So'rov uchun kutish muddati: chaqiruvchi bergani, aks holda AI/audio — uzoq, qolgani — oddiy. */
export function timeoutFor(path: string, init: ApiInit = {}): number {
  if (init.timeoutMs !== undefined) return init.timeoutMs;
  const upload = typeof FormData !== 'undefined' && init.body instanceof FormData;
  return upload || isAiPath(path) ? AI_TIMEOUT_MS : DEFAULT_TIMEOUT_MS;
}

/** Server xatosi: xabar foydalanuvchi tilida, status — dasturiy tekshiruv uchun (masalan 401). */
export class ApiError extends Error {
  /** HTTP status; 0 — javob umuman kelmadi (internet yo'q, server javob bermadi). */
  readonly status: number;
  /** Mashina uchun kod: serverdan ("ai_busy") yoki tarmoq xatosi ("offline" | "timeout" | "network"). */
  readonly code?: string;
  /** Qancha soniyadan keyin qayta urinish mumkin (503 ai_busy, 429). */
  readonly retryAfterSeconds?: number;
  constructor(message: string, status: number, extra: { code?: string; retryAfterSeconds?: number } = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    if (extra.code !== undefined) this.code = extra.code;
    if (extra.retryAfterSeconds !== undefined) this.retryAfterSeconds = extra.retryAfterSeconds;
  }
}

/** Javob umuman kelmaganda — "Failed to fetch" o'rniga tushunarli xabar. */
export function networkError(timedOut: boolean, online = typeof navigator === 'undefined' || navigator.onLine !== false): ApiError {
  const t = msg(networkMsg);
  if (!online) return new ApiError(t.offline, 0, { code: 'offline' });
  if (timedOut) return new ApiError(t.timeout, 0, { code: 'timeout' });
  return new ApiError(t.unreachable, 0, { code: 'network' });
}

/**
 * fetch'ning o'zi, faqat manzil oldiga API_BASE va (bo'lsa) token qo'shiladi.
 * Qo'shimcha: kutish muddati (timeoutFor), tarmoq xatosi → ApiError(status 0),
 * sekin so'rovlar signali (ServerWake banneri uchun).
 */
export async function apiFetch(path: string, init: ApiInit = {}): Promise<Response> {
  const { timeoutMs: _ignored, signal: callerSignal, ...rest } = init;
  const headers = new Headers(init.headers);
  const token = getToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  // Server xato xabarlarini va so'z tarjimasini shu tilda qaytaradi.
  headers.set('Accept-Language', getLang());

  // Chaqiruvchining signali va o'zimizning taymer — bitta controller'da
  // (AbortSignal.any eski Safari'da yo'q).
  const controller = new AbortController();
  let timedOut = false;
  const timeout = timeoutFor(path, init);
  const timer =
    timeout > 0
      ? setTimeout(() => {
          timedOut = true;
          controller.abort();
        }, timeout)
      : undefined;
  const onCallerAbort = () => controller.abort(callerSignal?.reason);
  if (callerSignal) {
    if (callerSignal.aborted) controller.abort(callerSignal.reason);
    else callerSignal.addEventListener('abort', onCallerAbort, { once: true });
  }
  const finished = trackRequest(isAiPath(path) || timeout > DEFAULT_TIMEOUT_MS);

  try {
    const response = await fetch(`${API_BASE}${path}`, { ...rest, headers, signal: controller.signal });
    noteServerResponse();
    return response;
  } catch (err) {
    // Chaqiruvchi o'zi bekor qilgan bo'lsa — asl AbortError (xato xabari kerak emas).
    if (callerSignal?.aborted && !timedOut) throw err;
    throw networkError(timedOut);
  } finally {
    if (timer !== undefined) clearTimeout(timer);
    callerSignal?.removeEventListener('abort', onCallerAbort);
    finished();
  }
}

/** Retry-After sarlavhasi (soniyalarda) yoki null. */
function retryAfterHeader(response: Response): number | undefined {
  const v = Number(response.headers.get('Retry-After'));
  return Number.isFinite(v) && v > 0 ? Math.round(v) : undefined;
}

/** Xato javobidan ApiError: server xabari, kodi va qayta urinish vaqti bilan. */
export async function errorFromResponse(response: Response): Promise<ApiError> {
  const body = (await response.json().catch(() => ({}))) as {
    error?: string;
    detail?: string;
    code?: string;
    retryAfterSeconds?: number;
  };
  const code = typeof body.code === 'string' ? body.code : undefined;
  const retryAfterSeconds =
    typeof body.retryAfterSeconds === 'number' && body.retryAfterSeconds > 0
      ? Math.round(body.retryAfterSeconds)
      : retryAfterHeader(response);
  const t = msg(networkMsg);
  const fallback =
    code === 'ai_busy'
      ? t.aiBusy(retryAfterSeconds)
      : response.status >= 502 && response.status <= 504
        ? t.serverDown
        : msg(common).serverError(response.status);
  return new ApiError(body.error ?? body.detail ?? fallback, response.status, { code, retryAfterSeconds });
}

/** apiFetch + JSON o'qish. Server xato qaytarsa, uning xabari bilan ApiError otiladi. */
export async function apiJson<T>(path: string, init: ApiInit = {}): Promise<T> {
  const response = await apiFetch(path, init);
  if (!response.ok) throw await errorFromResponse(response);
  // 204 / bo'sh javob — JSON yo'q.
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/** JSON body bilan POST — eng ko'p ishlatiladigan holat uchun qisqartma. */
export function postJson<T>(path: string, body: unknown, init: ApiInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set('Content-Type', 'application/json');
  return apiJson<T>(path, { ...init, method: 'POST', headers, body: JSON.stringify(body) });
}

/**
 * Ilova ochilganda serverni uyg'otib qo'yish (javobini kutmaymiz): foydalanuvchi
 * birinchi tugmani bosguncha Render serveri uyg'onib ulguradi.
 */
export function wakeServer() {
  const finished = trackRequest(false);
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), 90_000);
  fetch(`${API_BASE}/health/live`, { signal: controller.signal, cache: 'no-store' })
    .then(() => noteServerResponse())
    .catch(() => undefined)
    .finally(() => {
      clearTimeout(timer);
      finished();
    });
}

export interface HistoryItem {
  id: string;
  createdAtUtc: string;
  promptData: string;
  responseData: string;
}

/** Tarix ixtiyoriy: xato bo'lsa bo'sh ro'yxat — asosiy oqim to'xtamasin. */
export async function loadHistory(kind: string): Promise<HistoryItem[]> {
  try {
    return await apiJson<HistoryItem[]>(`/api/${kind}/history`);
  } catch {
    return [];
  }
}

// ---- Ingliz tili darajasi (A2–C1) ----
// Mehmon uchun ham ishlashi kerak, shuning uchun brauzerda saqlanadi;
// kirgan foydalanuvchida esa serverdagi profil bilan sinxronlanadi.
export const LEVELS = [
  { id: 'A2', label: 'A2' },
  { id: 'B1', label: 'B1' },
  { id: 'B2', label: 'B2' },
  { id: 'C1', label: 'C1' },
] as const;

const LEVEL_KEY = 'speakingCoach.level';

export function getLevel(): string {
  try {
    const v = localStorage.getItem(LEVEL_KEY);
    return v && LEVELS.some((l) => l.id === v) ? v : 'B1';
  } catch {
    return 'B1';
  }
}

export function setLevel(level: string) {
  try {
    localStorage.setItem(LEVEL_KEY, level);
  } catch {
    // saqlab bo'lmasa — shu sahifa ochiq turgancha ishlaydi
  }
}

export interface Profile {
  email: string;
  displayName: string | null;
  level: string;
  showOnLeaderboard: boolean;
  isAdmin: boolean;
  /** Maqsad (tanishtiruvda tanlanadi): "ielts" | "cefr" | "general" | null. */
  goal?: string | null;
  targetScore?: string | null;
  examDate?: string | null;
  dailyMinutes?: number;
  /** false — birinchi kirish: bosh sahifa o'rniga tanishtiruv ochiladi. */
  onboarded?: boolean;
  /** Qaysi usullar bilan kirish mumkin (profil sahifasidagi "Kirish usullari"). */
  signIn?: SignInMethods;
}

export interface SignInMethods {
  password: boolean;
  google: boolean;
  telegram: boolean;
  /** false — Telegram orqali ochilgan hisob: email ichki (tg-…@telegram.invalid), koʻrsatilmaydi. */
  realEmail: boolean;
}

/** Telegram orqali ochilgan hisobning ichki manzili — foydalanuvchiga koʻrsatilmaydi. */
export const isSyntheticEmail = (email: string | null | undefined): boolean =>
  !!email && email.toLowerCase().endsWith('@telegram.invalid');

/** Foydalanuvchiga koʻrsatiladigan nom: taxallus, haqiqiy email yoki "Telegram". */
export function accountLabel(email: string | null, displayName?: string | null): string {
  if (!email) return '';
  if (!isSyntheticEmail(email)) return email;
  return displayName?.trim() || 'Telegram';
}
