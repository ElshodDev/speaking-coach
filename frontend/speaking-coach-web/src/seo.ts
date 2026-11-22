// Qidiruv tizimlari uchun fayllar (build paytida vite.config.ts yozadi).
// Faqat haqiqiy sahifalar: hash marshrutlar (#/...) alohida sahifa emas, shuning uchun sitemap'ga kiritilmaydi.

/** Indekslanadigan sahifalar (yo'l → qisqa tavsif). */
export const SITEMAP_PATHS = ['/', '/privacy.html', '/terms.html'] as const;

/** Qidiruv robotlari kirmasligi kerak bo'lgan yo'llar: API va shaxsiy sahifalar yo'q (hammasi hash ichida), shuning uchun faqat servis fayllari. */
const ROBOTS_DISALLOW = ['/sw.js'] as const;

export function buildRobots(siteUrl: string): string {
  const base = siteUrl.replace(/\/+$/, '');
  return ['User-agent: *', 'Allow: /', ...ROBOTS_DISALLOW.map((p) => `Disallow: ${p}`), '', `Sitemap: ${base}/sitemap.xml`, ''].join('\n');
}

export function buildSitemap(siteUrl: string, lastmod: string): string {
  const base = siteUrl.replace(/\/+$/, '');
  const urls = SITEMAP_PATHS.map(
    (p) => `  <url>\n    <loc>${base}${p}</loc>\n    <lastmod>${lastmod}</lastmod>\n  </url>`,
  ).join('\n');
  return `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}\n</urlset>\n`;
}
