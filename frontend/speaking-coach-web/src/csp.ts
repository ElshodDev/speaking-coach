// Content-Security-Policy (vercel.json) tekshiruvi — vite.config.ts build paytida
// ishlatadi: VITE_API_URL manzili connect-src'da bo'lmasa, sayt serverga umuman
// ulana olmay qoladi. Shuning uchun buzilgan sayt deploy bo'lgandan ko'ra build to'xtaydi.

/** Siyosatdan bitta direktivaning manbalari: "connect-src 'self' https://x" → ["'self'", "https://x"]. */
export function cspSources(policy: string, directive: string): string[] {
  for (const part of policy.split(';')) {
    const [name, ...sources] = part.trim().split(/\s+/);
    if (name?.toLowerCase() === directive) return sources;
  }
  return [];
}

/** Manba (https://api.x.com, https://*.onrender.com, https://accounts.google.com/gsi/) shu origin'ni qamraydimi. */
export function sourceAllows(source: string, origin: string): boolean {
  let target: URL;
  try {
    target = new URL(origin);
  } catch {
    return false;
  }
  const m = /^(https?):\/\/(\*\.)?([^/:]+)(:\d+)?(\/.*)?$/i.exec(source);
  if (!m) return false;
  const [, scheme, wildcard, host, port, path] = m;
  if (target.protocol !== `${scheme.toLowerCase()}:`) return false;
  if ((port ? port.slice(1) : '') !== target.port) return false;
  // Yo'lli manba (…/gsi/) API ildizini qamramaydi — faqat to'liq host.
  if (path && path !== '/') return false;
  const h = target.hostname.toLowerCase();
  const base = host.toLowerCase();
  return wildcard ? h.endsWith(`.${base}`) : h === base;
}

/** API manzili (VITE_API_URL) siyosatning connect-src'iga sig'adimi. */
export function connectAllowed(policy: string, apiUrl: string): boolean {
  const sources = cspSources(policy, 'connect-src');
  const list = sources.length ? sources : cspSources(policy, 'default-src');
  return list.some((s) => s === '*' || sourceAllows(s, apiUrl));
}
