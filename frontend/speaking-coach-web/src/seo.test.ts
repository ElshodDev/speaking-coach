import { describe, expect, it } from 'vitest';
import { buildRobots, buildSitemap, SITEMAP_PATHS } from './seo';

describe('seo', () => {
  it('robots.txt: hamma narsaga ruxsat, sitemap to‘liq manzil bilan', () => {
    const r = buildRobots('https://fluentuz.app/');
    expect(r).toContain('User-agent: *');
    expect(r).toContain('Allow: /');
    expect(r).toContain('Sitemap: https://fluentuz.app/sitemap.xml');
    expect(r).not.toContain('Disallow: /\n');
  });
  it('sitemap.xml: har sahifa to‘liq manzil va sana bilan, hash marshrutlarsiz', () => {
    const x = buildSitemap('https://fluentuz.app', '2026-11-21');
    for (const p of SITEMAP_PATHS) expect(x).toContain(`<loc>https://fluentuz.app${p}</loc>`);
    expect(x).toContain('<lastmod>2026-11-21</lastmod>');
    expect(x).not.toContain('#');
    expect(x.startsWith('<?xml')).toBe(true);
  });
  it('manzil oxiridagi / ikkilanmaydi', () => {
    expect(buildSitemap('https://a.b//', '2026-01-01')).not.toContain('b//');
  });
});
