import { describe, expect, it } from 'vitest';
import { IGNORED_ERRORS, scrubEvent, scrubText, scrubUrl, type ScrubbableEvent } from './monitoring';

describe('monitoring: maxfiylik', () => {
  it('matndan email, Bearer token va uzun hex tokenlar niqoblanadi', () => {
    expect(scrubText('Kirish xatosi: aziza.k@mail.uz parol xato')).toBe('Kirish xatosi: [email] parol xato');
    expect(scrubText('Authorization: Bearer abcdef1234567890XYZ')).toBe('Authorization: Bearer [token]');
    expect(scrubText(`token ${'a1'.repeat(32)} tugadi`)).toBe('token [token] tugadi');
    expect(scrubText('oddiy matn 12345')).toBe('oddiy matn 12345');
  });

  it('manzildan hash va so\'rov qismi olib tashlanadi (taklif kodi sizmaydi)', () => {
    expect(scrubUrl('https://fluentuz.app/#/join/K7M2QX?x=1')).toBe('https://fluentuz.app/');
    expect(scrubUrl('https://fluentuz.app/assets/a.js?v=2')).toBe('https://fluentuz.app/assets/a.js');
    expect(scrubUrl('/#/teacher/abc')).toBe('/');
  });

  it('hodisadan foydalanuvchi, cookie, sarlavha, tana, so\'rov qismi va breadcrumbs olib tashlanadi', () => {
    const event: ScrubbableEvent = {
      user: { id: '1', email: 'a@b.uz', ip_address: '1.2.3.4' },
      extra: { transcript: 'My name is Aziza' },
      breadcrumbs: [{ category: 'console', message: 'secret' }],
      message: 'Xato: a@b.uz',
      request: {
        url: 'https://fluentuz.app/#/join/K7M2QX',
        cookies: { a: 'b' },
        headers: { Authorization: 'Bearer abcdef1234567890' },
        data: '<audio bytes>',
        query_string: 'email=a@b.uz',
        env: { REMOTE_ADDR: '1.2.3.4' },
      },
      exception: { values: [{ value: 'Network error for a@b.uz' }, { value: 'ok' }] },
    };
    const out = scrubEvent(event);
    expect(out.user).toBeUndefined();
    expect(out.extra).toBeUndefined();
    expect(out.breadcrumbs).toBeUndefined();
    expect(out.request).toEqual({ url: 'https://fluentuz.app/' });
    expect(out.message).toBe('Xato: [email]');
    expect(out.exception?.values?.map((v) => v.value)).toEqual(['Network error for [email]', 'ok']);
    expect(JSON.stringify(out)).not.toMatch(/a@b\.uz|Aziza|abcdef12|1\.2\.3\.4|audio/);
  });

  it('tarmoq shovqini e\'tiborsiz qoldiriladi', () => {
    expect(IGNORED_ERRORS).toEqual(expect.arrayContaining(['Failed to fetch', 'Load failed']));
  });
});
