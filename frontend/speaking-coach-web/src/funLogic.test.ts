import { beforeEach, describe, expect, it } from 'vitest';
import { loadBest, quizUrl, saveBest, telegramShareUrl } from './funLogic';
import { funMsg } from './locales/fun';

const store: Record<string, string> = {};
beforeEach(() => {
  for (const k of Object.keys(store)) delete store[k];
  (globalThis as unknown as { localStorage: Storage }).localStorage = {
    getItem: (k: string) => store[k] ?? null,
    setItem: (k: string, v: string) => void (store[k] = v),
    removeItem: (k: string) => void delete store[k],
    clear: () => undefined,
    key: () => null,
    length: 0,
  };
});

describe('viktorina', () => {
  it('Telegram ulashish havolasi kodlangan', () => {
    const u = telegramShareUrl('https://x.app/#/quiz', 'Men 9/10 topdim! Sen-chi?');
    expect(u).toBe('https://t.me/share/url?url=https%3A%2F%2Fx.app%2F%23%2Fquiz&text=Men%209%2F10%20topdim!%20Sen-chi%3F');
    expect(quizUrl('https://x.app/')).toBe('https://x.app/#/quiz');
  });
  it('rekord: faqat oshganda', () => {
    expect(loadBest()).toBeNull();
    expect(saveBest(6, 10)).toBe(true);
    expect(loadBest()).toBe(60);
    expect(saveBest(5, 10)).toBe(false);
    expect(saveBest(6, 10)).toBe(false);
    expect(saveBest(9, 10)).toBe(true);
    expect(loadBest()).toBe(90);
    expect(saveBest(0, 0)).toBe(false);
  });
  it('maqtov darajalari va matnlar uch tilda', () => {
    expect(funMsg.uz.praise(10, 10)).toContain('Mukammal');
    expect(funMsg.uz.praise(8, 10)).toContain('Zoʻr');
    expect(funMsg.uz.praise(5, 10)).toContain('Yaxshi');
    expect(funMsg.uz.praise(2, 10)).toContain('Mashq');
    expect(funMsg.en.shareText(9, 10)).toContain('9/10');
    expect(funMsg.ru.shareText(9, 10)).toContain('9/10');
  });
});
