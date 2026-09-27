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

interface TelegramWebApp {
  ready(): void;
  expand(): void;
  openTelegramLink?(url: string): void;
  openLink?(url: string): void;
}

declare global {
  interface Window {
    Telegram?: { WebApp?: TelegramWebApp };
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
      try {
        window.Telegram?.WebApp?.ready();
        window.Telegram?.WebApp?.expand();
      } catch {
        /* eski Telegram versiyasi */
      }
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
