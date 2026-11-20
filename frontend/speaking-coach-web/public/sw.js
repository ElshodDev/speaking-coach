// Service worker — ilovani telefonga "o'rnatish" (PWA) va sekin/uzilgan
// internetda ham tez ochilishi uchun.
//
// Qoidalar:
// - Backend (/api, boshqa domen: onrender.com) so'rovlari HECH QACHON
//   keshlanmaydi — baholash va kartalar har doim yangi bo'lishi kerak.
//   Authorization sarlavhali so'rovlar ham keshlanmaydi.
// - Sahifalar (navigatsiya: index.html, privacy.html, terms.html): avval
//   internetdan — yangi deploy darhol ko'rinsin. Internet sekin bo'lsa
//   (NAV_TIMEOUT_MS ichida javob kelmasa) — keshdagi nusxa ochiladi, yangisi
//   esa orqa fonda keshga yoziladi (keyingi ochilishda ko'rinadi). Internet
//   bo'lmasa — keshdan.
// - /assets/ ichidagi JS/CSS fayllar nomida xesh bor (masalan
//   index-yCz_3Dgm.js) — ular hech qachon o'zgarmaydi, shuning uchun
//   avval keshdan olinadi. Yangi deploy kelganda joriy va bitta oldingi
//   index.html (va ular yuklaydigan fayllar) tilga olmaydigan /assets/ fayllar
//   o'chiriladi — kesh cheksiz o'smaydi, eski versiyada ochiq turgan oyna esa
//   o'z fayllarini yo'qotmaydi.
// - /icons/ nomida xesh yo'q: keshdan darhol beriladi, lekin orqa fonda
//   yangilanadi (stale-while-revalidate).
//
// Yangi versiyada keshlash strategiyasi o'zgarsa, CACHE nomini oshiring
// (v2, v3...) — eski kesh "activate" paytida o'chiriladi.
const CACHE = 'speaking-coach-v5'; // v5: sahifa uchun 4 s kutish, eski /assets/ tozalanadi, ikonkalar SWR
const SHELL = ['/', '/manifest.webmanifest', '/icons/icon-192.png', '/icons/logo.svg'];
/** Sahifa shuncha vaqtda internetdan kelmasa — keshdagi nusxa ko'rsatiladi. */
const NAV_TIMEOUT_MS = 4000;

// ---- Toza funksiyalar (sw.test.ts shularni tekshiradi) ----

// index.html'da "/assets/x.js", JS ichida "assets/x.js" yoki "./x.js", CSS'da url(/assets/x.woff2).
const ASSET_REF = /(?:\/?assets\/|\.\/)([\w.-]+\.(?:js|css|woff2?|ttf|png|jpe?g|svg|webp|gif|mp3|wav|wasm|json))/g;

/** Matndagi (HTML/JS/CSS) /assets/ fayllari ro'yxati: ["/assets/index-abc.js", ...]. */
function referencedAssets(text) {
  const out = new Set();
  for (const m of String(text).matchAll(ASSET_REF)) out.add(`/assets/${m[1]}`);
  return out;
}

/**
 * Keshdagi /assets/ fayllaridan qaysilari endi kerak emas.
 * shells — saqlanadigan index.html matnlari (joriy va oldingi); readCached(path) —
 * keshdagi JS/CSS matni (yo'q bo'lsa null).
 * Kerakli = index.html tilga olganlar va ular (zanjir bo'ylab) tilga olganlar.
 */
async function staleAssets(cachedPaths, shells, readCached) {
  const keep = new Set();
  const queue = shells.filter(Boolean).flatMap((html) => [...referencedAssets(html)]);
  while (queue.length) {
    const path = queue.pop();
    if (keep.has(path)) continue;
    keep.add(path);
    if (!/\.(js|css)$/.test(path)) continue;
    const text = await readCached(path);
    if (text) for (const ref of referencedAssets(text)) if (!keep.has(ref)) queue.push(ref);
  }
  return cachedPaths.filter((p) => p.startsWith('/assets/') && !keep.has(p));
}

/** Keshlash mumkinmi: faqat o'z domenimiz, GET, Authorization'siz, /api emas. */
function cacheable(request, url, origin) {
  if (request.method !== 'GET') return false;
  if (url.origin !== origin) return false;
  if (url.pathname === '/api' || url.pathname.startsWith('/api/')) return false;
  if (request.headers.has('Authorization')) return false;
  return true;
}

self.swLogic = { referencedAssets, staleAssets, cacheable };

// ---- Kesh bilan ishlash ----

/** Berilgan index.html'larga (joriy va oldingi) kerak bo'lmagan /assets/ fayllarni o'chiradi. */
async function pruneAssets(cache, shells) {
  if (!shells.some(Boolean)) return;
  const keys = await cache.keys();
  const byPath = new Map(keys.map((req) => [new URL(req.url).pathname, req]));
  const stale = await staleAssets([...byPath.keys()], shells, async (path) => {
    const res = await cache.match(byPath.get(path) ?? path);
    return res ? res.text() : null;
  });
  await Promise.all(stale.map((path) => cache.delete(byPath.get(path) ?? path)));
}

/** Internetdan kelgan sahifani keshga yozadi; ilova sahifasi o'zgargan bo'lsa — eski fayllarni tozalaydi. */
async function storePage(key, response) {
  const cache = await caches.open(CACHE);
  if (key !== '/') {
    await cache.put(key, response);
    return;
  }
  const old = await cache.match('/');
  const oldHtml = old ? await old.text() : null;
  const html = await response.clone().text();
  await cache.put('/', response);
  if (html !== oldHtml) await pruneAssets(cache, [html, oldHtml]);
}

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(CACHE).then((cache) => cache.addAll(SHELL)));
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      const keys = await caches.keys();
      await Promise.all(keys.filter((k) => k !== CACHE).map((k) => caches.delete(k)));
      const cache = await caches.open(CACHE);
      const shell = await cache.match('/');
      if (shell) await pruneAssets(cache, [await shell.text()]).catch(() => undefined);
      await self.clients.claim();
    })(),
  );
});

/** Ilovaning o'zi (SPA): marshrut hash'da, shuning uchun "/" va "/index.html" bitta sahifa. */
const isAppShell = (pathname) => pathname === '/' || pathname === '/index.html';

const wait = (ms) => new Promise((resolve) => setTimeout(() => resolve(null), ms));

self.addEventListener('fetch', (event) => {
  const request = event.request;
  const url = new URL(request.url);
  // Backend, boshqa domenlar (API, YouTube, Telegram skripti) va tokenli so'rovlar — tegmaymiz.
  if (!cacheable(request, url, self.location.origin)) return;

  // Sahifalar: network-first, lekin NAV_TIMEOUT_MS dan keyin keshdagisi. Har sahifa
  // O'Z manzili bilan keshlanadi — oflaynda /privacy.html o'rniga bosh sahifa ochilib qolmasin.
  if (request.mode === 'navigate') {
    const key = isAppShell(url.pathname) ? '/' : url.pathname;
    const network = fetch(request);
    // Keshga yozish — sahifa keshdan ochilgan bo'lsa ham oxirigacha bajariladi.
    // (Bu .then birinchi ro'yxatdan o'tgani uchun javob sahifaga berilishidan oldin nusxalanadi.)
    event.waitUntil(
      network
        .then((response) => (response.ok && response.type === 'basic' ? storePage(key, response.clone()) : undefined))
        .catch(() => undefined),
    );
    event.respondWith(
      (async () => {
        const cached = await caches.match(key);
        if (cached) {
          const fresh = await Promise.race([network.catch(() => null), wait(NAV_TIMEOUT_MS)]);
          return fresh ?? cached;
        }
        try {
          return await network;
        } catch {
          // Noma'lum sahifa oflaynda — hech bo'lmasa ilovaning o'zi.
          return (await caches.match('/')) ?? Response.error();
        }
      })(),
    );
    return;
  }

  // Xeshli fayllar: cache-first (nomi o'zgarmaydi).
  if (url.pathname.startsWith('/assets/')) {
    event.respondWith(
      caches.match(request).then(
        (cached) =>
          cached ??
          fetch(request).then((response) => {
            if (response.ok) {
              const copy = response.clone();
              caches.open(CACHE).then((cache) => cache.put(request, copy));
            }
            return response;
          }),
      ),
    );
    return;
  }

  // Ikonkalar: stale-while-revalidate — keshdan darhol, orqa fonda yangilanadi.
  if (url.pathname.startsWith('/icons/')) {
    const update = fetch(request).then(async (response) => {
      if (response.ok) {
        // Nusxa — await'dan OLDIN: aks holda sahifa javob tanasini o'qib ulgurib, clone() xato beradi.
        const copy = response.clone();
        const cache = await caches.open(CACHE);
        await cache.put(request, copy);
      }
      return response;
    });
    event.waitUntil(update.catch(() => undefined));
    event.respondWith(caches.match(request).then((cached) => cached ?? update));
  }
});
