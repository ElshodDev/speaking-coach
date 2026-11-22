import { describe, expect, it } from 'vitest';
import { activeNav2, HUB, normalizeQuery, searchHub } from './newNav';
import { hubMsg } from './locales/hub';

const uz = (id: string) => hubMsg.uz.items[id];

describe('new nav: active tab', () => {
  it('maps sections to the 5 bottom tabs; Lug\'at lives under Takrorlash', () => {
    expect(activeNav2('home')).toBe('home');
    expect(activeNav2('review')).toBe('review');
    expect(activeNav2('vocab')).toBe('review');
    expect(activeNav2('tutor')).toBe('tutor');
    expect(activeNav2('mock')).toBe('practice');
    expect(activeNav2('shadowing')).toBe('practice');
    expect(activeNav2('mistakes')).toBe('progress');
    expect(activeNav2('profile')).toBeNull();
    expect(activeNav2('teacher')).toBeNull();
  });
});

describe('new nav: Hammasi search', () => {
  it('returns everything for an empty query', () => {
    expect(searchHub(HUB, '  ', uz)).toHaveLength(HUB.length);
  });
  it('finds a hidden button by a keyword and ignores apostrophes', () => {
    expect(normalizeQuery("Lug'at")).toBe('lugat');
    const ids = (q: string) => searchHub(HUB, q, uz).map((i) => i.id);
    expect(ids('tele')).toContain('telegram');
    expect(ids("lug'at")).toContain('vocab');
    expect(ids('parol')).toContain('profile');
    expect(ids('hisobni o‘chirish')).toContain('data');
    expect(ids('xyzxyz')).toEqual([]);
  });
  it('requires every word of the query to match', () => {
    expect(searchHub(HUB, 'guruh vazifa', uz).map((i) => i.id)).toContain('teacher');
    expect(searchHub(HUB, 'guruh telegram', uz)).toEqual([]);
  });
  it('every item has a title in all three languages and a unique id', () => {
    for (const lang of ['uz', 'ru', 'en'] as const) {
      for (const i of HUB) expect(hubMsg[lang].items[i.id]?.title, `${lang}:${i.id}`).toBeTruthy();
    }
    expect(new Set(HUB.map((i) => i.id)).size).toBe(HUB.length);
  });
});
