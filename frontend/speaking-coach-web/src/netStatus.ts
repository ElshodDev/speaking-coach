// Sekin so'rovlar signali. Render'ning bepul serveri 15 daqiqa jim turgach
// "uxlaydi" va birinchi so'rovda 30–60 soniyada uyg'onadi. Shu paytda
// foydalanuvchi "ilova qotib qoldi" deb o'ylamasligi uchun ServerWake
// banneri ko'rsatiladi. Bu modul — React'siz kichik pub/sub (testlanadi).

/** Shuncha vaqtdan beri javob kelmagan so'rov — "sekin". */
export const SLOW_AFTER_MS = 4000;

/**
 * Oxirgi javobdan beri shuncha vaqt o'tmagan bo'lsa, server uyg'oq deb
 * hisoblaymiz (Render ~15 daqiqada uxlaydi). Uyg'oq serverda AI so'rovlari
 * (baholash 5–30 soniya) sekin bo'lishi tabiiy — ular uchun banner chiqmaydi.
 */
export const AWAKE_FOR_MS = 14 * 60 * 1000;

type Listener = () => void;

const listeners = new Set<Listener>();
let slowCount = 0;
let lastResponseAt = 0;

function emit() {
  listeners.forEach((l) => l());
}

/** Hozir nechta so'rov "sekin" holatda (0 — banner yashirin). */
export const getSlowCount = (): number => slowCount;

export function subscribeSlow(listener: Listener): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

/** Serverdan (har qanday statusli) javob keldi — demak u uyg'oq. */
export function noteServerResponse(now = Date.now()) {
  lastResponseAt = now;
}

export function serverLikelyAwake(now = Date.now()): boolean {
  return lastResponseAt > 0 && now - lastResponseAt < AWAKE_FOR_MS;
}

/**
 * So'rov boshlandi. Qaytgan funksiya so'rov tugaganda (muvaffaqiyatli yoki
 * xato bilan) chaqiriladi. `ai` — uzoq davom etishi tabiiy bo'lgan so'rov.
 */
export function trackRequest(ai = false, slowAfterMs = SLOW_AFTER_MS): () => void {
  let slow = false;
  let finished = false;
  const timer = setTimeout(() => {
    if (finished) return;
    if (ai && serverLikelyAwake()) return;
    slow = true;
    slowCount += 1;
    emit();
  }, slowAfterMs);
  return () => {
    if (finished) return;
    finished = true;
    clearTimeout(timer);
    if (slow) {
      slowCount -= 1;
      emit();
    }
  };
}

/** Faqat testlar uchun: holatni tozalash. */
export function resetNetStatus() {
  slowCount = 0;
  lastResponseAt = 0;
  listeners.clear();
}
