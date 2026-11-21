// Taklif (referral): havola, saqlash va ro'yxatdan o'tishga uzatish. Toza mantiq — testlanadi.
//
// Havola: https://fluentuz.app/?ref=KOD — KOD hash'dan OLDIN (oddiy query), shuning uchun hash-router o'zgarmaydi.
// Kod ilovaga kirganda bir marta o'qiladi, qurilmada (localStorage) saqlanadi, manzildan olib tashlanadi
// va ro'yxatdan o'tishda (email yoki Google) serverga yuboriladi; muvaffaqiyatdan keyin o'chiriladi.
// Analitikaga (Plausible) hech narsa yuborilmaydi: kod — foydalanuvchi identifikatori.

const KEY = 'sc.ref.v1';
/** Server kodi: Guid'ning base64url ko'rinishi — 22 belgi. */
const CODE = /^[A-Za-z0-9_-]{22}$/;

export const isValidRef = (code: string | null | undefined): code is string => !!code && CODE.test(code);

/** "?ref=KOD&x=1" ichidan yaroqli kodni oladi (yo'q yoki noto'g'ri bo'lsa — null). */
export function refFromSearch(search: string): string | null {
  try {
    const v = new URLSearchParams(search).get('ref')?.trim();
    return isValidRef(v) ? v : null;
  } catch {
    return null;
  }
}

/** Taklif havolasi (ulashish uchun). */
export function referralLink(origin: string, code: string): string {
  return `${origin.replace(/\/+$/, '')}/?ref=${encodeURIComponent(code)}`;
}

/** Manzilning ?ref qismini olib tashlaydi (qolgan parametrlar va hash saqlanadi). */
export function stripRef(search: string): string {
  const p = new URLSearchParams(search);
  p.delete('ref');
  const rest = p.toString();
  return rest ? `?${rest}` : '';
}

export interface InviteDecision {
  /** Saqlanadigan kod (yo'q bo'lsa null). */
  ref: string | null;
  /** Yangi manzil (pathname + search + hash) — o'zgarmasa null. */
  url: string | null;
}

/**
 * Taklif havolasi bilan kirganda: kodni olamiz, manzilni tozalaymiz. Kirmagan (tokensiz) va hash'siz mehmon
 * to'g'ridan-to'g'ri ro'yxatdan o'tish sahifasiga yo'naltiriladi (marshrut kodi o'zgarmaydi — faqat boshlang'ich hash).
 */
export function decideInvite(pathname: string, search: string, hash: string, hasToken: boolean): InviteDecision {
  const ref = refFromSearch(search);
  if (!ref) return { ref: null, url: null };
  const nextHash = !hasToken && (hash === '' || hash === '#' || hash === '#/' || hash === '#/home') ? '#/register' : hash;
  return { ref, url: `${pathname}${stripRef(search)}${nextHash}` };
}

function store(): Storage | null {
  try {
    return typeof localStorage === 'undefined' ? null : localStorage;
  } catch {
    return null;
  }
}

export function saveRef(code: string) {
  try {
    store()?.setItem(KEY, code);
  } catch {
    // Saqlab bo'lmasa — taklif ro'yxatdan o'tishgacha ishlamaydi, ilova esa odatdagidek ishlaydi.
  }
}

/** Saqlangan yaroqli kod yoki null. */
export function getRef(): string | null {
  try {
    const v = store()?.getItem(KEY);
    return isValidRef(v) ? v : null;
  } catch {
    return null;
  }
}

export function clearRef() {
  try {
    store()?.removeItem(KEY);
  } catch {
    // e'tiborsiz
  }
}

/** Ilova boshida bir marta (main.tsx): havoladagi kodni saqlaydi va manzilni tozalaydi. */
export function captureInvite() {
  if (typeof window === 'undefined') return;
  try {
    const hasToken = !!localStorage.getItem('speakingCoach.token');
    const d = decideInvite(window.location.pathname, window.location.search, window.location.hash, hasToken);
    if (!d.ref) return;
    // Hisobi bor (tokenli) foydalanuvchi taklifni ishlatolmaydi — kod saqlanmaydi.
    if (!hasToken) saveRef(d.ref);
    if (d.url) window.history.replaceState(null, '', d.url);
  } catch {
    // Taklif hech qachon ilovani buzmasligi kerak.
  }
}

/** Ro'yxatdan o'tish so'roviga qo'shiladigan maydon: { ref } yoki bo'sh. */
export function refPayload(): { ref?: string } {
  const r = getRef();
  return r ? { ref: r } : {};
}

/**
 * Telegram orqali YANGI hisob ochilgach: saqlangan taklif kodini serverga yuboradi (email/Google'da kod
 * ro'yxatdan o'tish so'rovining o'zida ketadi). Natijadan qat'i nazar kod o'chiriladi; hech qachon xato tashlamaydi.
 */
export async function claimReferral(isNew: boolean | undefined, post: (path: string, body: unknown) => Promise<unknown>): Promise<void> {
  const ref = getRef();
  if (!ref) return;
  try {
    if (isNew) await post('/api/referral/claim', { ref });
  } catch {
    // Taklif hech qachon kirishni to'xtatmasligi kerak.
  } finally {
    clearRef();
  }
}

export interface ReferralInfo {
  enabled: boolean;
  code: string;
  invited: number;
  bonusActive: boolean;
  bonusDays: number;
  bonusExtraExercises: number;
}
