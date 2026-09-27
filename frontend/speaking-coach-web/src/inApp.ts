// Ilovalar ichidagi brauzerlar (Instagram, Telegram, Facebook, TikTok…) — Google
// ularda "Google bilan kirish"ni bloklaydi (403 disallowed_useragent). Bunday joyda
// Google tugmasi o'rniga tushuntirish va "Chrome'da ochish" ko'rsatiladi.

/** Ilova nomi (ichki brauzer bo'lsa) yoki null. Toza funksiya — testlanadi. */
export function inAppBrowser(ua: string, hasTelegramProxy = false): string | null {
  if (hasTelegramProxy || /Telegram/i.test(ua)) return 'Telegram';
  if (/Instagram/i.test(ua)) return 'Instagram';
  if (/FBAN|FBAV|FB_IAB|FBIOS|FB4A|MessengerForiOS/i.test(ua)) return 'Facebook';
  if (/musical_ly|BytedanceWebview|TikTok|trill_/i.test(ua)) return 'TikTok';
  if (/\bLine\//i.test(ua)) return 'LINE';
  if (/Snapchat/i.test(ua)) return 'Snapchat';
  // Android WebView (masalan, boshqa ilova ichida ochilgan havola).
  if (/Android/i.test(ua) && /; wv\)/.test(ua)) return 'WebView';
  return null;
}

export const isAndroid = (ua: string) => /Android/i.test(ua);
export const isIos = (ua: string) => /iPhone|iPad|iPod/i.test(ua);

/** Android: joriy saytni Chrome'da ochadigan intent havolasi (hash — marshrut — kerak emas). */
export function chromeIntent(loc: Pick<Location, 'host' | 'pathname'>): string {
  return `intent://${loc.host}${loc.pathname}#Intent;scheme=https;package=com.android.chrome;end`;
}

export function currentInApp(): string | null {
  if (typeof navigator === 'undefined') return null;
  const w = window as Window & { TelegramWebviewProxy?: unknown };
  return inAppBrowser(navigator.userAgent, w.TelegramWebviewProxy !== undefined);
}
