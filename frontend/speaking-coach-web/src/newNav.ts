// Yangi menyu ("Kundalik" dizayni): pastda 5 tugma (markazda AI ustoz), tepada Fikr va "Hammasi".
// Bayroq: VITE_NEW_NAV=true. O'chiq bo'lsa, hozirgi menyu o'zgarishsiz qoladi.
// Toza mantiq shu yerda (unit test qilinadi): faol bo'lim, "Hammasi" ro'yxati va qidiruv.
import type { IconName } from './Icon';

export const NEW_NAV = import.meta.env.VITE_NEW_NAV === 'true';

export const NAV2 = ['home', 'review', 'tutor', 'practice', 'progress'] as const;
export type Nav2 = (typeof NAV2)[number];

const PRACTICE_SECTIONS = new Set(['practice', 'mock', 'shadowing', 'learn', 'talk', 'dictation', 'part2']);
const PROFILE_SECTIONS = new Set(['profile', 'admin', 'teacher', 'login', 'register']);

/** Pastki menyuda qaysi tugma faol: Lug'at — Takrorlash ichida, AI ustoz — markazda. */
export function activeNav2(section: string, _sub?: string): Nav2 | null {
  void _sub;
  if (PROFILE_SECTIONS.has(section)) return null;
  if (section === 'review' || section === 'vocab') return 'review';
  if (section === 'tutor') return 'tutor';
  if (section === 'mistakes' || section === 'progress') return 'progress';
  if (PRACTICE_SECTIONS.has(section)) return 'practice';
  return 'home';
}

export type HubGroup = 'learn' | 'practice' | 'exam' | 'results' | 'community' | 'account';

export interface HubItem {
  id: string;
  route: string;
  group: HubGroup;
  icon: IconName;
  /** Qidiruv uchun qo'shimcha so'zlar (kichik harfda, apostrofsiz). */
  keys: string;
}

/** "Hammasi" ro'yxati: ilovadagi barcha asosiy joylar — yashirin tugma qolmasligi uchun. */
export const HUB: HubItem[] = [
  { id: 'review', route: 'review', group: 'learn', icon: 'review', keys: 'kartalar takrorlash xatolar karta' },
  { id: 'vocab', route: 'vocab', group: 'learn', icon: 'vocab', keys: 'lugat soz so\'z vocabulary words' },
  { id: 'learn', route: 'learn', group: 'learn', icon: 'learn', keys: 'darslar grammatika qoidalar' },
  { id: 'tutor', route: 'tutor', group: 'learn', icon: 'tutor', keys: 'ai ustoz dars mavzu' },
  { id: 'speaking', route: 'practice/speaking', group: 'practice', icon: 'speaking', keys: 'gapirish ovoz speaking' },
  { id: 'writing', route: 'practice/writing', group: 'practice', icon: 'writing', keys: 'yozish insho writing' },
  { id: 'reading', route: 'practice/reading', group: 'practice', icon: 'reading', keys: 'oqish matn reading' },
  { id: 'listening', route: 'practice/listening', group: 'practice', icon: 'listening', keys: 'tinglash eshitish listening' },
  { id: 'talk', route: 'talk', group: 'practice', icon: 'talk', keys: 'suhbat ai bilan rolli' },
  { id: 'dictation', route: 'dictation', group: 'practice', icon: 'dictation', keys: 'diktant eshitib yozish' },
  { id: 'shadowing', route: 'shadowing', group: 'practice', icon: 'shadowing', keys: 'shadowing takrorlab gapirish video podcast' },
  { id: 'part2', route: 'part2', group: 'exam', icon: 'part2', keys: 'ielts part 2 savol karta' },
  { id: 'mock', route: 'mock', group: 'exam', icon: 'mock', keys: 'sinov imtihon mock ielts cefr' },
  { id: 'progress', route: 'progress', group: 'results', icon: 'progress', keys: 'natijalar progress statistika ball' },
  { id: 'mistakes', route: 'mistakes', group: 'results', icon: 'x', keys: 'xatolarim xatolar' },
  { id: 'goal', route: 'welcome', group: 'results', icon: 'target', keys: 'maqsadim maqsad goal daraja' },
  { id: 'teacher', route: 'teacher', group: 'community', icon: 'users', keys: 'oqituvchi guruh guruhlarim vazifa sinf' },
  { id: 'join', route: 'profile', group: 'community', icon: 'users', keys: 'guruhga qoshilish kod taklif kodi' },
  { id: 'referral', route: 'profile', group: 'community', icon: 'gift', keys: 'dost taklif qilish referral havola' },
  { id: 'telegram', route: 'profile', group: 'community', icon: 'send', keys: 'telegram bot eslatma' },
  { id: 'profile', route: 'profile', group: 'account', icon: 'user', keys: 'profil hisob email parol sozlamalar til' },
  { id: 'install', route: 'profile', group: 'account', icon: 'phone', keys: 'ilovani ornatish install bosh ekran' },
  { id: 'data', route: 'profile', group: 'account', icon: 'gear', keys: 'malumot yuklab olish ochirish hisobni ochirish' },
];

export const HUB_GROUPS: HubGroup[] = ['learn', 'practice', 'exam', 'results', 'community', 'account'];

/** Qidiruv uchun matnni tekislash: kichik harf, ʻ ’ ' ` belgilarsiz, ortiqcha bo'shliqsiz. */
export function normalizeQuery(s: string): string {
  return s
    .toLowerCase()
    .replace(/[ʻʼ’‘'`]/g, '')
    .replace(/\s+/g, ' ')
    .trim();
}

/** Sarlavha, izoh yoki kalit so'zlar so'rovdagi barcha so'zlarni o'z ichiga olsa — mos. Bo'sh so'rov — hammasi. */
export function searchHub(items: HubItem[], query: string, label: (id: string) => { title: string; text: string }): HubItem[] {
  const words = normalizeQuery(query).split(' ').filter(Boolean);
  if (!words.length) return items;
  return items.filter((it) => {
    const l = label(it.id);
    const hay = normalizeQuery(`${l.title} ${l.text} ${it.keys}`);
    return words.every((w) => hay.includes(w));
  });
}

/** Profil kartasidagi maqsad: "IELTS 6.5", "CEFR B2" yoki "General"; belgilanmagan bo'lsa — null. */
export function goalShort(p: { goal?: string | null; targetScore?: string | null } | null, label: (goal: string) => string): string | null {
  if (!p?.goal) return null;
  const base = p.goal === 'ielts' ? 'IELTS' : p.goal === 'cefr' ? 'CEFR' : label(p.goal);
  return p.targetScore && p.goal !== 'general' ? `${base} ${p.targetScore}` : base;
}
