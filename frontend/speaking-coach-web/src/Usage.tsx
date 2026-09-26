import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { accountMsg } from './locales/account';

export interface Usage {
  exercises: { used: number; limit: number; left: number };
  words: { used: number; limit: number; left: number };
  unlimited: boolean;
  guest: boolean;
}

/** Userga kunlik AI limitini tushunarli qilib ko'rsatish uchun matn (test qilinadi). */
export function usageText(u: Usage, userLimit: number, t: typeof accountMsg.uz): string | null {
  if (u.unlimited) return null;
  if (u.exercises.left === 0) return t.usageEmpty;
  return u.guest ? t.usageGuest(u.exercises.left, u.exercises.limit, userLimit) : t.usageUser(u.exercises.left, u.exercises.limit);
}

/**
 * "Bugun yana N ta mashq" — limitga yetib, kutilmagan xatoga duch kelishdan
 * oldin foydalanuvchi qancha qolganini ko'rib tursin. userKey o'zgarsa
 * (kirish/chiqish) qayta so'raladi.
 */
export function UsageNote({ userKey, userLimit = 30 }: { userKey: string; userLimit?: number }) {
  const [usage, setUsage] = useState<Usage | null>(null);
  const t = useT(accountMsg);

  useEffect(() => {
    apiJson<Usage>('/api/usage')
      .then(setUsage)
      .catch(() => setUsage(null));
  }, [userKey]);

  const text = usage && usageText(usage, userLimit, t);
  if (!text) return null;
  return (
    <p className={`usage small ${usage!.exercises.left === 0 ? 'usage-empty' : ''}`} role="status">
      ⚡ {text}
    </p>
  );
}
