import { describe, expect, it } from 'vitest';
import vercel from '../vercel.json';
import { connectAllowed, cspSources, sourceAllows } from './csp';

const policy = vercel.headers.flatMap((h) => h.headers).find((h) => h.key === 'Content-Security-Policy')!.value;

describe('csp helpers', () => {
  it('reads directive sources', () => {
    expect(cspSources("default-src 'self'; connect-src 'self' https://a.com", 'connect-src')).toEqual(["'self'", 'https://a.com']);
    expect(cspSources("default-src 'self'", 'img-src')).toEqual([]);
  });
  it('matches hosts, wildcards, scheme and port', () => {
    expect(sourceAllows('https://*.onrender.com', 'https://speaking-coach-api.onrender.com')).toBe(true);
    expect(sourceAllows('https://*.onrender.com', 'https://onrender.com.evil.com')).toBe(false);
    expect(sourceAllows('https://api.x.com', 'http://api.x.com')).toBe(false);
    expect(sourceAllows('https://api.x.com', 'https://api.x.com:8443')).toBe(false);
    expect(sourceAllows('https://accounts.google.com/gsi/', 'https://accounts.google.com')).toBe(false);
  });
  it('checks the API origin against connect-src', () => {
    expect(connectAllowed("connect-src 'self' https://api.x.com", 'https://api.x.com')).toBe(true);
    expect(connectAllowed("connect-src 'self'", 'https://api.x.com')).toBe(false);
    expect(connectAllowed("default-src https://api.x.com", 'https://api.x.com')).toBe(true);
  });
});

describe('vercel.json policy', () => {
  it('keeps frame-ancestors for Telegram and locks down objects/base', () => {
    expect(cspSources(policy, 'frame-ancestors')).toEqual(["'self'", 'https://web.telegram.org', 'https://*.telegram.org']);
    expect(cspSources(policy, 'object-src')).toEqual(["'none'"]);
    expect(cspSources(policy, 'base-uri')).toEqual(["'self'"]);
  });
  it('allows the hosts the app loads', () => {
    expect(connectAllowed(policy, 'https://speaking-coach-api.onrender.com')).toBe(true);
    const scripts = cspSources(policy, 'script-src');
    for (const s of ['https://accounts.google.com/gsi/', 'https://telegram.org', 'https://www.youtube.com']) expect(scripts).toContain(s);
    expect(cspSources(policy, 'frame-src')).toContain('https://www.youtube-nocookie.com');
    expect(cspSources(policy, 'img-src')).toContain('https://i.ytimg.com');
    expect(cspSources(policy, 'media-src')).toContain('blob:');
  });
});
