import { describe, expect, it } from 'vitest';
import { showFooterOnPhone } from './Footer';

describe('showFooterOnPhone', () => {
  it('bosh sahifa, profil va kirish ekranlarida ko‘rinadi', () => {
    for (const s of ['', 'home', 'profile', 'login', 'register', 'welcome']) expect(showFooterOnPhone(s)).toBe(true);
  });
  it('ishchi ekranlarda yashirin', () => {
    for (const s of ['practice', 'mock', 'review', 'vocab', 'talk', 'shadowing', 'dictation', 'quiz', 'learn', 'progress']) expect(showFooterOnPhone(s)).toBe(false);
  });
});
