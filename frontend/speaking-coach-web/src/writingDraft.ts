// Yozish mashqi qoralamasi: sahifa yopilsa, internet uzilsa yoki telefon
// ilovani "o'ldirsa" ham yozilgan matn yo'qolmasin. Har mavzu uchun alohida,
// faqat shu brauzerda (localStorage). Toza funksiyalar — testlanadi.
import type { PickedTopic } from './TopicPicker';

export const DRAFTS_KEY = 'speakingCoach.writingDrafts';
/** Eng ko'pi shuncha mavzu qoralamasi saqlanadi (eskilari o'chadi). */
export const MAX_DRAFTS = 10;
/** 30 kundan eski qoralama — unutilgan, tiklanmaydi. */
export const DRAFT_TTL_MS = 30 * 24 * 60 * 60 * 1000;

export interface WritingDraft {
  topic: PickedTopic;
  text: string;
  savedAt: number;
}

type Store = Record<string, WritingDraft>;
type StorageLike = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;

/** Mavzu kaliti: bankdagi id, AI mavzusida — matnning qisqa xeshi. */
export function draftKey(topic: Pick<PickedTopic, 'id' | 'text' | 'category'>): string {
  if (topic.id) return `id:${topic.id}`;
  let h = 5381;
  for (let i = 0; i < topic.text.length; i++) h = ((h << 5) + h + topic.text.charCodeAt(i)) | 0;
  return `t:${topic.category}:${(h >>> 0).toString(36)}`;
}

function storage(): StorageLike | null {
  try {
    return typeof localStorage === 'undefined' ? null : localStorage;
  } catch {
    return null;
  }
}

function read(s: StorageLike | null, now: number): Store {
  if (!s) return {};
  try {
    const raw = JSON.parse(s.getItem(DRAFTS_KEY) ?? '{}') as unknown;
    if (!raw || typeof raw !== 'object') return {};
    const out: Store = {};
    for (const [k, d] of Object.entries(raw as Store)) {
      if (d && typeof d.text === 'string' && d.topic && typeof d.topic.text === 'string' && now - d.savedAt < DRAFT_TTL_MS) {
        out[k] = d;
      }
    }
    return out;
  } catch {
    return {};
  }
}

function write(s: StorageLike | null, store: Store) {
  if (!s) return;
  try {
    if (Object.keys(store).length === 0) s.removeItem(DRAFTS_KEY);
    else s.setItem(DRAFTS_KEY, JSON.stringify(store));
  } catch {
    // joy tugagan yoki saqlash taqiqlangan — qoralamasiz ishlayveramiz
  }
}

/** Qoralamani saqlash; bo'sh matn — qoralamani o'chirish. */
export function saveDraft(topic: PickedTopic, text: string, now = Date.now(), s: StorageLike | null = storage()) {
  const store = read(s, now);
  const key = draftKey(topic);
  if (!text.trim()) {
    delete store[key];
  } else {
    store[key] = { topic, text, savedAt: now };
    // Eng eskilarini tashlab yuboramiz.
    const keys = Object.keys(store).sort((a, b) => store[b].savedAt - store[a].savedAt);
    for (const k of keys.slice(MAX_DRAFTS)) delete store[k];
  }
  write(s, store);
}

/** Shu mavzuning qoralamasi (yoki null). */
export function loadDraft(topic: PickedTopic, now = Date.now(), s: StorageLike | null = storage()): WritingDraft | null {
  return read(s, now)[draftKey(topic)] ?? null;
}

/** Eng oxirgi qoralama — sahifa ochilganda shu mavzu va matn tiklanadi. */
export function latestDraft(now = Date.now(), s: StorageLike | null = storage()): WritingDraft | null {
  const all = Object.values(read(s, now));
  return all.sort((a, b) => b.savedAt - a.savedAt)[0] ?? null;
}

export function clearDraft(topic: PickedTopic, now = Date.now(), s: StorageLike | null = storage()) {
  const store = read(s, now);
  delete store[draftKey(topic)];
  write(s, store);
}
