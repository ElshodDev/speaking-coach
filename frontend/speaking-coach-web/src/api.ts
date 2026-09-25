// Backend bilan barcha muloqot shu fayl orqali o'tadi: manzil (API_BASE),
// kirish tokeni va xato xabarlarini o'qish bir joyda.

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
    else localStorage.removeItem(TOKEN_KEY);
  } catch {
    // Brauzer saqlashni taqiqlagan bo'lsa (masalan maxfiy rejim sozlamasi) —
    // kirish shu sahifa yopilguncha ishlaydi, xolos.
  }
}

/** fetch'ning o'zi, faqat manzil oldiga API_BASE va (bo'lsa) token qo'shiladi. */
export function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers);
  const token = getToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  return fetch(`${API_BASE}${path}`, { ...init, headers });
}

/** apiFetch + JSON o'qish. Server xato qaytarsa, uning xabari bilan Error otiladi. */
export async function apiJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await apiFetch(path, init);
  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new Error(body.error ?? body.detail ?? `Server xatosi: ${response.status}`);
  }
  return response.json();
}

/** JSON body bilan POST — eng ko'p ishlatiladigan holat uchun qisqartma. */
export function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiJson<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
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
