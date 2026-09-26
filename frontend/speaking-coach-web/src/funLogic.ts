// Kunlik so'z va viktorina uchun toza funksiyalar (unit test qilinadi).

export interface QuizQuestion {
  direction: 'word' | 'meaning';
  prompt: string;
  options: string[];
  answer: number;
}

const BEST = 'speakingCoach.quizBest';

/** Telegram orqali ulashish havolasi (ilova o'rnatilmagan bo'lsa ham brauzerda ochiladi). */
export function telegramShareUrl(url: string, text: string): string {
  return `https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`;
}

export function quizUrl(origin: string): string {
  return `${origin.replace(/\/+$/, '')}/#/quiz`;
}

/** Eng yaxshi natija — shu qurilmada (foiz sifatida, savollar soni har xil bo'lishi mumkin). */
export function loadBest(): number | null {
  try {
    const v = Number(localStorage.getItem(BEST));
    return Number.isFinite(v) && v > 0 ? v : null;
  } catch {
    return null;
  }
}

/** Yangi rekord bo'lsa saqlaydi va true qaytaradi. */
export function saveBest(score: number, total: number): boolean {
  if (total <= 0) return false;
  const pct = Math.round((score / total) * 100);
  const prev = loadBest();
  if (prev !== null && pct <= prev) return false;
  try {
    localStorage.setItem(BEST, String(pct));
  } catch {
    // saqlab bo'lmasa — rekordsiz ishlayveradi
  }
  return pct > 0;
}
