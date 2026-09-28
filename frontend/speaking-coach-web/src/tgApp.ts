// Telegram Mini App: sayt bot ichida ("📱 Ilovani ochish") ochilganda Telegram
// manzilning # qismiga o'z parametrlarini qo'shadi (tgWebAppData = imzolangan
// initData). Ularni marshrutlash hash'ni o'zgartirishidan OLDIN — modul
// yuklanganda — ushlab olamiz. Bo'lim "?tgroute=..." orqali keladi.

export interface TelegramLaunch {
  initData: string | null;
  route: string;
}

/** Manzildan Mini App parametrlari (Telegram ichida bo'lmasa — null). Toza funksiya — testlanadi. */
export function parseLaunch(hash: string, search: string): TelegramLaunch | null {
  const h = hash.replace(/^#/, '');
  if (!h.includes('tgWebAppData=')) return null;
  let initData: string | null = null;
  try {
    initData = new URLSearchParams(h).get('tgWebAppData');
  } catch {
    initData = null;
  }
  const raw = new URLSearchParams(search).get('tgroute') ?? '';
  // Faqat ilova ichidagi marshrut: "shadowing", "mock/cefr-reading" — tashqi manzil emas.
  const route = /^[a-z0-9][a-z0-9/_-]{0,80}$/i.test(raw) ? raw : '';
  return { initData: initData || null, route };
}

export const telegramLaunch: TelegramLaunch | null =
  typeof window === 'undefined' ? null : parseLaunch(window.location.hash, window.location.search);

interface TelegramBackButton {
  show(): void;
  hide(): void;
  onClick(cb: () => void): void;
  offClick(cb: () => void): void;
}

export interface TelegramWebApp {
  ready(): void;
  expand(): void;
  openTelegramLink?(url: string): void;
  openLink?(url: string): void;
  /** Bot API 6.1+ */
  BackButton?: TelegramBackButton;
  /** Bot API 7.7+: pastga surish Mini App'ni yopmasin (matn aylantirishda tasodifan yopilardi). */
  disableVerticalSwipes?(): void;
  isVersionAtLeast?(version: string): boolean;
}

declare global {
  interface Window {
    Telegram?: { WebApp?: TelegramWebApp };
    TelegramWebviewProxy?: unknown;
  }
}

/**
 * Sahifa haqiqatan Telegram ilovasida ochilganmi: mobil va desktop klientlar
 * `TelegramWebviewProxy` ni qo'shadi, web.telegram.org esa iframe ichida ochadi
 * (boshqa saytlar iframe qila olmaydi — vercel.json'dagi frame-ancestors).
 * Oddiy brauzerda "#tgWebAppData=..." li havola bilan birovning hisobiga
 * avtomatik kiritib yuborish (login CSRF) shu tekshiruv bilan to'siladi.
 */
export function genuineTelegram(w: Pick<Window, 'parent' | 'TelegramWebviewProxy'> = window): boolean {
  try {
    return w.TelegramWebviewProxy !== undefined || w.parent !== (w as unknown);
  } catch {
    return false;
  }
}

const IN_TG_KEY = 'speakingCoach.inTelegram';

/** Sayt Telegram ichida ochilganmi (sahifa yangilansa ham eslab qolinadi). */
export function inTelegram(): boolean {
  if (telegramLaunch) return true;
  try {
    return sessionStorage.getItem(IN_TG_KEY) === '1';
  } catch {
    return false;
  }
}

/** Telegram'ning rasmiy skripti (faqat Telegram ichida yuklanadi): ready() va to'liq ekran. */
export function readyTelegram(timeoutMs = 4000): Promise<void> {
  try {
    sessionStorage.setItem(IN_TG_KEY, '1');
  } catch {
    /* saqlab bo'lmadi — muhim emas */
  }
  return new Promise((resolve) => {
    const done = () => {
      const wa = window.Telegram?.WebApp;
      if (wa) applyTelegramUi(wa);
      resolve();
    };
    if (window.Telegram?.WebApp) return done();
    const s = document.createElement('script');
    s.src = 'https://telegram.org/js/telegram-web-app.js';
    s.async = true;
    s.onload = done;
    s.onerror = () => resolve();
    document.head.appendChild(s);
    window.setTimeout(resolve, timeoutMs);
  });
}

/** Versiya tekshiruvi: eski skriptda isVersionAtLeast bo'lmasa — funksiya borligiga ishonamiz. */
function supports(wa: TelegramWebApp, version: string): boolean {
  try {
    return wa.isVersionAtLeast ? wa.isVersionAtLeast(version) : true;
  } catch {
    return false;
  }
}

/** ready(), to'liq ekran va (7.7+) pastga surib yopishni o'chirish. */
export function applyTelegramUi(wa: TelegramWebApp) {
  try {
    wa.ready();
    wa.expand();
  } catch {
    /* eski Telegram versiyasi */
  }
  try {
    if (wa.disableVerticalSwipes && supports(wa, '7.7')) wa.disableVerticalSwipes();
  } catch {
    /* qo'llab-quvvatlanmaydi — muhim emas */
  }
}

// Hozir BackButton'ga bog'langan ishlovchi — yangisini bog'lashdan oldin
// eskisi albatta offClick qilinadi (aks holda bosishda ikki marta "orqaga").
let backHandler: (() => void) | null = null;

/**
 * Telegram'ning tepadagi "‹ Orqaga" tugmasi: showBack — ko'rsatish/yashirish,
 * onBack — bosilganda. Har marshrut o'zgarishida chaqiriladi (App.tsx).
 */
export function syncTgBack(showBack: boolean, onBack: () => void, wa: TelegramWebApp | undefined = typeof window === 'undefined' ? undefined : window.Telegram?.WebApp,
) {
  const bb = wa?.BackButton;
  if (!wa || !bb || !supports(wa, '6.1')) return;
  try {
    if (backHandler) {
      bb.offClick(backHandler);
      backHandler = null;
    }
    if (showBack) {
      const handler = () => onBack();
      bb.onClick(handler);
      backHandler = handler;
      bb.show();
    } else {
      bb.hide();
    }
  } catch {
    /* eski Telegram versiyasi */
  }
}

/** Mini App parametrlarini manzildan olib tashlaydi va kerakli bo'limga o'tadi. */
export function cleanLaunchUrl(route: string) {
  window.history.replaceState(null, '', `${window.location.pathname}#/${route}`);
  window.dispatchEvent(new HashChangeEvent('hashchange'));
}

/** t.me havolasi: Telegram ichida — Telegram'ning o'zi ochadi, brauzerda — yangi oyna. */
export function openExternal(url: string) {
  const wa = window.Telegram?.WebApp;
  if (inTelegram() && wa) {
    if (url.startsWith('https://t.me/') && wa.openTelegramLink) return wa.openTelegramLink(url);
    if (wa.openLink) return wa.openLink(url);
  }
  window.open(url, '_blank', 'noopener');
}
