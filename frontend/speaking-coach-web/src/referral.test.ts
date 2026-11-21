import { beforeEach, describe, expect, it } from 'vitest';
import { clearRef, decideInvite, getRef, isValidRef, referralLink, refFromSearch, refPayload, saveRef, stripRef } from './referral';

const mem = new Map<string, string>();
(globalThis as { localStorage?: unknown }).localStorage = {
  getItem: (k: string) => mem.get(k) ?? null,
  setItem: (k: string, v: string) => void mem.set(k, v),
  removeItem: (k: string) => void mem.delete(k),
};

const CODE = 'YqxWmQd2S02uZlprQXelPQ';

beforeEach(() => mem.clear());

describe('referral', () => {
  it('yaroqli kod 22 belgili base64url', () => {
    expect(isValidRef(CODE)).toBe(true);
    expect(isValidRef('-3t59GhmIkS26YBHx2aBgA')).toBe(true);
    for (const bad of ['', 'short', CODE + 'x', 'a b'.padEnd(22, 'c'), '<script>'.padEnd(22, 'x'), null, undefined]) expect(isValidRef(bad)).toBe(false);
  });

  it('?ref dan kodni oladi, noto‘g‘ri bo‘lsa null', () => {
    expect(refFromSearch(`?ref=${CODE}`)).toBe(CODE);
    expect(refFromSearch(`?x=1&ref=${CODE}`)).toBe(CODE);
    expect(refFromSearch('?ref=nope')).toBeNull();
    expect(refFromSearch('')).toBeNull();
  });

  it('havola hash’dan oldin turadi (router o‘zgarmaydi) va ortiqcha / ni olib tashlaydi', () => {
    expect(referralLink('https://fluentuz.app', CODE)).toBe(`https://fluentuz.app/?ref=${CODE}`);
    expect(referralLink('https://fluentuz.app/', CODE)).toBe(`https://fluentuz.app/?ref=${CODE}`);
    expect(referralLink('https://fluentuz.app', CODE)).not.toContain('#');
  });

  it('stripRef boshqa parametrlarni saqlaydi', () => {
    expect(stripRef(`?ref=${CODE}`)).toBe('');
    expect(stripRef(`?a=1&ref=${CODE}`)).toBe('?a=1');
  });

  it('mehmon, hash’siz: ro‘yxatdan o‘tishga yo‘naltiriladi va ?ref olib tashlanadi', () => {
    const d = decideInvite('/', `?ref=${CODE}`, '', false);
    expect(d).toEqual({ ref: CODE, url: '/#/register' });
    expect(decideInvite('/', `?ref=${CODE}`, '#/home', false).url).toBe('/#/register');
  });

  it('boshqa sahifaga ochilgan havola o‘z hash’ini saqlaydi; tokenli foydalanuvchi yo‘naltirilmaydi', () => {
    expect(decideInvite('/', `?ref=${CODE}`, '#/practice', false).url).toBe('/#/practice');
    expect(decideInvite('/', `?ref=${CODE}`, '', true).url).toBe('/');
  });

  it('kodsiz yoki noto‘g‘ri kodli manzil — hech narsa o‘zgarmaydi', () => {
    expect(decideInvite('/', '', '', false)).toEqual({ ref: null, url: null });
    expect(decideInvite('/', '?ref=bad', '', false)).toEqual({ ref: null, url: null });
  });

  it('saqlash, o‘qish va tozalash; yaroqsiz qiymat o‘qilmaydi', () => {
    expect(getRef()).toBeNull();
    expect(refPayload()).toEqual({});
    saveRef(CODE);
    expect(getRef()).toBe(CODE);
    expect(refPayload()).toEqual({ ref: CODE });
    clearRef();
    expect(getRef()).toBeNull();
    mem.set('sc.ref.v1', 'garbage');
    expect(getRef()).toBeNull();
  });
});
