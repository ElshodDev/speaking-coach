import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';

/**
 * Interfeys tillari. Tashqi kutubxonasiz, lekin TIP-XAVFSIZ:
 * har bir bo'lim (masalan Lug'at) o'z matnlarini `defineMessages(uz, { ru, en })`
 * bilan e'lon qiladi. Tarjimalarning shakli o'zbekcha nusxadan olinadi, shuning
 * uchun rus yoki ingliz tilida bitta kalit tushib qolsa ham — TypeScript
 * build'ni to'xtatadi. Sonlarga bog'liq matnlar oddiy funksiya:
 * `count: (n) => ...` — rus tilining "1 слово / 2 слова / 5 слов" qoidasi shu
 * funksiya ichida (ruPlural) hal qilinadi.
 */
export type Lang = 'uz' | 'ru' | 'en';

export const LANGS: { id: Lang; label: string; short: string }[] = [
  { id: 'uz', label: 'Oʻzbekcha', short: 'UZ' },
  { id: 'ru', label: 'Русский', short: 'RU' },
  { id: 'en', label: 'English', short: 'EN' },
];

const LANG_KEY = 'speakingCoach.lang';

/** Brauzer tilidan taxmin: rus → ru, ingliz → en, qolgani (o'zbek va boshqalar) → uz. */
export function detectLang(languages: readonly string[]): Lang {
  for (const l of languages) {
    const base = l.toLowerCase().split('-')[0];
    if (base === 'uz' || base === 'ru' || base === 'en') return base;
  }
  return 'uz';
}

let current: Lang = (() => {
  try {
    const saved = localStorage.getItem(LANG_KEY);
    if (saved === 'uz' || saved === 'ru' || saved === 'en') return saved;
  } catch {
    // saqlash taqiqlangan — brauzer tilidan olamiz
  }
  return typeof navigator !== 'undefined' ? detectLang(navigator.languages ?? [navigator.language]) : 'uz';
})();

/** Komponentdan tashqarida (api.ts, yordamchi funksiyalar) joriy til. */
export const getLang = (): Lang => current;

function storeLang(lang: Lang) {
  current = lang;
  try {
    localStorage.setItem(LANG_KEY, lang);
  } catch {
    // saqlab bo'lmasa — shu sahifa ochiq turgancha ishlaydi
  }
  if (typeof document !== 'undefined') document.documentElement.lang = lang;
}

/** Brauzer sana/son formatlari uchun to'liq til kodi. */
export const localeOf = (lang: Lang) => ({ uz: 'uz-Latn-UZ', ru: 'ru-RU', en: 'en-GB' })[lang];

export type Messages<T> = Record<Lang, T>;

/** Bo'lim matnlari: o'zbekcha — asosiy nusxa, rus va ingliz shu shaklga mos kelishi shart. */
export function defineMessages<T>(uz: T, rest: { ru: T; en: T }): Messages<T> {
  return { uz, ...rest };
}

/** Komponentdan tashqarida matn olish (masalan, xato xabarlari). */
export const msg = <T,>(m: Messages<T>): T => m[current];

/** Rus tilidagi ko'plik: 1 слово, 2 слова, 5 слов, 21 слово, 11 слов. */
export function ruPlural(n: number, one: string, few: string, many: string): string {
  const abs = Math.abs(n) % 100;
  const last = abs % 10;
  if (abs > 10 && abs < 20) return many;
  if (last === 1) return one;
  if (last >= 2 && last <= 4) return few;
  return many;
}

/** Ingliz tilidagi ko'plik: 1 word, 2 words. */
export const enPlural = (n: number, one: string, many: string) => (n === 1 ? one : many);

const LangContext = createContext<{ lang: Lang; setLang: (l: Lang) => void }>({
  lang: current,
  setLang: () => undefined,
});

export function LangProvider({ children }: { children: ReactNode }) {
  const [lang, setState] = useState<Lang>(current);

  useEffect(() => {
    document.documentElement.lang = lang;
  }, [lang]);

  const setLang = useCallback((l: Lang) => {
    storeLang(l);
    setState(l);
  }, []);

  return <LangContext.Provider value={{ lang, setLang }}>{children}</LangContext.Provider>;
}

export const useLang = () => useContext(LangContext);

/** Joriy tildagi bo'lim matnlari; til almashganda komponent qayta chiziladi. */
export function useT<T>(m: Messages<T>): T {
  return m[useContext(LangContext).lang];
}

/** Umumiy matnlar — bir nechta ekranda ishlatiladi. */
export const common = defineMessages(
  {
    back: 'Orqaga',
    login: 'Kirish',
    loginArrow: 'Kirish →',
    loginOrRegister: 'Kirish yoki roʻyxatdan oʻtish',
    close: 'Yopish',
    save: 'Saqlash',
    delete: 'Oʻchirish',
    loading: 'Yuklanmoqda...',
    listen: '🔊 Eshitish',
    error: 'Xatolik yuz berdi',
    serverError: (status: number) => `Server xatosi: ${status}`,
    language: 'Til',
  },
  {
    ru: {
      back: 'Назад',
      login: 'Войти',
      loginArrow: 'Войти →',
      loginOrRegister: 'Войти или зарегистрироваться',
      close: 'Закрыть',
      save: 'Сохранить',
      delete: 'Удалить',
      loading: 'Загрузка...',
      listen: '🔊 Прослушать',
      error: 'Произошла ошибка',
      serverError: (status: number) => `Ошибка сервера: ${status}`,
      language: 'Язык',
    },
    en: {
      back: 'Back',
      login: 'Log in',
      loginArrow: 'Log in →',
      loginOrRegister: 'Log in or sign up',
      close: 'Close',
      save: 'Save',
      delete: 'Delete',
      loading: 'Loading...',
      listen: '🔊 Listen',
      error: 'Something went wrong',
      serverError: (status: number) => `Server error: ${status}`,
      language: 'Language',
    },
  },
);

/** Til tanlagich: tepadagi panelda qisqa (UZ/RU/EN), Profilda to'liq nomlar bilan. */
export function LangSelect({ full = false }: { full?: boolean }) {
  const { lang, setLang } = useLang();
  const t = useT(common);
  return (
    <select
      className={full ? 'input' : 'lang-select'}
      aria-label={t.language}
      value={lang}
      onChange={(e) => setLang(e.target.value as Lang)}
    >
      {LANGS.map((l) => (
        <option key={l.id} value={l.id}>
          {full ? l.label : l.short}
        </option>
      ))}
    </select>
  );
}
