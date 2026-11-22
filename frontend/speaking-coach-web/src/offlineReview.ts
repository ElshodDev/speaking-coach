// Oflayn takrorlash: faqat BUGUNGI (muddati kelgan) kartalar qurilmada saqlanadi.
//
// Qoidalar:
// - Feature flag: VITE_OFFLINE_REVIEW=off — o'chiriladi (hech narsa saqlanmaydi, eskisi o'chadi).
// - Kesh faqat bir kunlik va bitta hisobga tegishli: kun yoki hisob o'zgarsa — ishlatilmaydi va o'chadi.
// - Chiqishda (token o'chganda) kesh va navbat butunlay o'chadi (umumiy qurilmada kartalar qolmasin).
// - Oflaynda berilgan baholar navbatga yoziladi va internet qaytganda ketma-ket yuboriladi.
//   Server takror/erta baholarni o'zi e'tiborsiz qoldiradi (ReviewService.GradeAsync), shuning uchun qayta yuborish xavfsiz.
// - Faqat baholash oflayn ishlaydi. Karta qo'shish/o'chirish va statistika — internet bilan.
// Bu modul api.ts'ni import qilmaydi (api.ts uni chaqiradi) — davra bog'lanish bo'lmasligi uchun.

const KEY = 'sc.offReview.v1';

export interface OfflineCard {
  id: string;
  kind: 'Correction' | 'Question' | 'Manual' | 'Word';
  source: string | null;
  front: string;
  back: string;
  note: string;
}

export interface QueuedGrade {
  id: string;
  grade: number;
}

interface Store {
  user: string;
  day: string;
  cards: OfflineCard[];
  queue: QueuedGrade[];
}

function offlineReviewEnabled(): boolean {
  return (import.meta.env.VITE_OFFLINE_REVIEW as string | undefined)?.trim().toLowerCase() !== 'off';
}

/** Mahalliy kun: "2026-11-21" (server ham "bugun"ni mahalliy vaqt bo'yicha hisoblaydi). */
export function dayKey(now: Date = new Date()): string {
  const m = String(now.getMonth() + 1).padStart(2, '0');
  const d = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${m}-${d}`;
}

function read(): Store | null {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    const s = JSON.parse(raw) as Store;
    if (!s || typeof s.user !== 'string' || typeof s.day !== 'string' || !Array.isArray(s.cards) || !Array.isArray(s.queue)) return null;
    return s;
  } catch {
    return null;
  }
}

function write(s: Store | null) {
  try {
    if (s) localStorage.setItem(KEY, JSON.stringify(s));
    else localStorage.removeItem(KEY);
  } catch {
    // Saqlab bo'lmasa (joy yo'q / maxfiy rejim) — oflayn rejim ishlamaydi, ilova esa odatdagidek ishlaydi.
  }
}

/** Joriy hisob va bugungi kunga tegishli saqlov (boshqasi bo'lsa — o'chiriladi). */
function current(user: string, now: Date): Store | null {
  const s = read();
  if (!s) return null;
  if (s.user !== user || s.day !== dayKey(now)) {
    // Eski kunning navbatdagi baholari yuboriladi (yo'qolmasin), lekin kartalar o'chadi.
    if (s.user === user && s.queue.length > 0) {
      const kept: Store = { user, day: dayKey(now), cards: [], queue: s.queue };
      write(kept);
      return kept;
    }
    write(null);
    return null;
  }
  return s;
}

/** Bugungi muddati kelgan kartalarni saqlaydi (internet bor paytda; faqat to'liq ro'yxat, "Lug'at" doirasi emas). */
export function saveDue(user: string, cards: OfflineCard[], now: Date = new Date()) {
  if (!offlineReviewEnabled() || !user) return;
  const prev = current(user, now);
  write({ user, day: dayKey(now), cards, queue: prev?.queue ?? [] });
}

/** Oflayn ko'rsatiladigan kartalar: saqlanganlar minus allaqachon baholanganlar. null — saqlov yo'q. */
export function loadDue(user: string, now: Date = new Date()): OfflineCard[] | null {
  if (!offlineReviewEnabled() || !user) return null;
  const s = current(user, now);
  if (!s || s.cards.length === 0) return null;
  const done = new Set(s.queue.map((q) => q.id));
  return s.cards.filter((c) => !done.has(c.id));
}

/** Oflayn berilgan bahoni navbatga yozadi. false — saqlab bo'lmadi (baho yo'qolishi mumkin). */
export function enqueueGrade(user: string, id: string, grade: number, now: Date = new Date()): boolean {
  if (!offlineReviewEnabled() || !user) return false;
  const s = current(user, now) ?? { user, day: dayKey(now), cards: [], queue: [] };
  if (!s.queue.some((q) => q.id === id)) s.queue.push({ id, grade });
  write(s);
  const check = read();
  return !!check && check.queue.some((q) => q.id === id);
}

export function pendingGrades(user: string, now: Date = new Date()): QueuedGrade[] {
  if (!user) return [];
  return current(user, now)?.queue ?? [];
}

export type SendGrade = (id: string, grade: number) => Promise<'ok' | 'drop' | 'offline'>;

/**
 * Navbatni ketma-ket yuboradi. 'ok' va 'drop' (masalan karta o'chirilgan, 404) — navbatdan chiqadi;
 * 'offline' — to'xtaydi, qolganlari keyinga. Qaytaradi: yuborilganlar soni va qolganlar.
 */
export async function flushQueue(user: string, send: SendGrade, now: Date = new Date()): Promise<{ sent: number; left: number }> {
  let sent = 0;
  for (;;) {
    const s = current(user, now);
    const next = s?.queue[0];
    if (!s || !next) return { sent, left: 0 };
    const result = await send(next.id, next.grade);
    if (result === 'offline') return { sent, left: s.queue.length };
    const again = read();
    if (again) {
      again.queue = again.queue.filter((q) => q.id !== next.id);
      write(again);
    }
    if (result === 'ok') sent += 1;
  }
}

/** Chiqishda: kartalar ham, navbat ham o'chadi. */
export function clearOfflineReview() {
  write(null);
}
