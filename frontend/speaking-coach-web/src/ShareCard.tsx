// Natija kartasi: Instagram/Telegram story (1080×1920) uchun rasm — brauzerda canvas'da chiziladi,
// serverga hech narsa yuborilmaydi. Telefonda "Ulashish" (Web Share), kompyuterda "Yuklab olish".
import { useEffect, useState } from 'react';
import { useT } from './i18n';
import { shareMsg } from './locales/share';

export interface ShareStats {
  name: string | null;
  level: number;
  xp: number;
  streak: number;
  bestStreak: number;
  activities: number;
  reviews: number;
  badges: number;
  badgesTotal: number;
}

export interface ShareTile {
  value: string;
  label: string;
}

type ShareText = {
  streak: string;
  bestStreak: string;
  activities: string;
  reviews: string;
  badges: string;
};

/** Kartadagi 4 ta ko'rsatkich. Joriy seriya 0 bo'lsa — eng uzun seriya ko'rsatiladi (0 kun maqtanadigan narsa emas). */
export function shareTiles(s: ShareStats, t: ShareText): ShareTile[] {
  const streak = s.streak > 0 ? { value: s.streak, label: t.streak } : { value: s.bestStreak, label: t.bestStreak };
  return [
    { value: String(streak.value), label: streak.label },
    { value: String(s.activities), label: t.activities },
    { value: String(s.reviews), label: t.reviews },
    { value: `${s.badges}/${s.badgesTotal}`, label: t.badges },
  ];
}

const W = 1080;
const H = 1920;
const FONT = "'Inter Variable', 'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif";

function roundRect(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number) {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}

/** Logotip rasmini yuklaydi (o'sha manzildan, xizmat ishchisi keshidan); yuklanmasa — null (karta logotipsiz chiziladi). */
function loadLogo(): Promise<HTMLImageElement | null> {
  return new Promise((resolve) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = () => resolve(null);
    img.src = '/icons/icon-192.png';
  });
}

/** Ilova logotipi (public/icons/icon-192.png), x/y — chap yuqori burchak, size — tomon uzunligi. */
function drawLogo(ctx: CanvasRenderingContext2D, img: HTMLImageElement | null, x: number, y: number, size: number) {
  if (img) ctx.drawImage(img, x, y, size, size);
}

/** Matn kenglikka sig'maguncha shriftni kichraytiradi. */
function fitText(ctx: CanvasRenderingContext2D, text: string, weight: number, size: number, maxWidth: number) {
  let s = size;
  ctx.font = `${weight} ${s}px ${FONT}`;
  while (s > 24 && ctx.measureText(text).width > maxWidth) {
    s -= 4;
    ctx.font = `${weight} ${s}px ${FONT}`;
  }
}

async function drawShareCard(s: ShareStats, t: (typeof shareMsg)['uz'], site: string): Promise<Blob> {
  try {
    await document.fonts?.load(`800 96px 'Inter Variable'`);
    await document.fonts?.ready;
  } catch {
    // shrift yuklanmasa — tizim shrifti bilan chiziladi
  }
  const logo = await loadLogo();
  const canvas = document.createElement('canvas');
  canvas.width = W;
  canvas.height = H;
  const ctx = canvas.getContext('2d');
  if (!ctx) throw new Error('canvas');

  // Fon: to'q ko'k (ilovaning rasmiy rangi) va juda yengil doiralar.
  const bg = ctx.createLinearGradient(0, 0, 0, H);
  bg.addColorStop(0, '#0b1b3f');
  bg.addColorStop(1, '#13306e');
  ctx.fillStyle = bg;
  ctx.fillRect(0, 0, W, H);
  ctx.fillStyle = 'rgba(255,255,255,0.04)';
  for (const [x, y, r] of [[980, 180, 260], [80, 1500, 320], [900, 1750, 180]]) {
    ctx.beginPath();
    ctx.arc(x, y, r, 0, Math.PI * 2);
    ctx.fill();
  }

  ctx.textBaseline = 'alphabetic';
  ctx.fillStyle = '#fff';
  ctx.textAlign = 'left';

  // Logotip va nom.
  drawLogo(ctx, logo, 90, 150, 96);
  ctx.font = `700 52px ${FONT}`;
  ctx.fillText('FluentUZ', 214, 214);

  ctx.font = `500 44px ${FONT}`;
  ctx.fillStyle = 'rgba(255,255,255,0.85)';
  ctx.fillText(t.tagline, 90, 400);

  ctx.fillStyle = '#fff';
  fitText(ctx, s.name ?? 'FluentUZ', 800, 120, W - 180);
  ctx.fillText(s.name ?? 'FluentUZ', 90, 540);

  // Daraja "tabletkasi".
  const levelText = t.level(s.level, s.xp);
  ctx.font = `700 48px ${FONT}`;
  const lw = ctx.measureText(levelText).width + 72;
  roundRect(ctx, 90, 600, lw, 92, 46);
  ctx.fillStyle = '#fff';
  ctx.fill();
  ctx.fillStyle = '#0b1b3f';
  ctx.fillText(levelText, 126, 663);

  // 2×2 ko'rsatkichlar.
  const tiles = shareTiles(s, t);
  const tw = (W - 180 - 40) / 2;
  const th = 330;
  tiles.forEach((tile, i) => {
    const x = 90 + (i % 2) * (tw + 40);
    const y = 800 + Math.floor(i / 2) * (th + 40);
    roundRect(ctx, x, y, tw, th, 28);
    ctx.fillStyle = 'rgba(255,255,255,0.07)';
    ctx.fill();
    ctx.strokeStyle = 'rgba(255,255,255,0.16)';
    ctx.lineWidth = 2;
    ctx.stroke();
    // Emoji o'rniga qisqa ko'k chiziq — rasmiy uslub.
    ctx.fillStyle = '#7fb0ff';
    ctx.fillRect(x + 44, y + 70, 64, 8);
    ctx.fillStyle = '#fff';
    fitText(ctx, tile.value, 800, 110, tw - 88);
    ctx.fillText(tile.value, x + 44, y + 235);
    ctx.font = `500 40px ${FONT}`;
    ctx.fillStyle = 'rgba(255,255,255,0.85)';
    ctx.fillText(tile.label, x + 44, y + 292);
  });

  // Pastda: taklif va manzil.
  ctx.fillStyle = 'rgba(255,255,255,0.85)';
  ctx.font = `500 42px ${FONT}`;
  ctx.fillText(t.cta, 90, 1640);
  ctx.fillStyle = '#fff';
  fitText(ctx, site, 700, 54, W - 180);
  ctx.fillText(site, 90, 1712);

  return new Promise((resolve, reject) => canvas.toBlob((b) => (b ? resolve(b) : reject(new Error('toBlob'))), 'image/png'));
}

export function ShareCardDialog({ stats, onClose }: { stats: ShareStats; onClose: () => void }) {
  const t = useT(shareMsg);
  const [blob, setBlob] = useState<Blob | null>(null);
  const [url, setUrl] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    let alive = true;
    let objectUrl = '';
    drawShareCard(stats, t, window.location.host)
      .then((b) => {
        if (!alive) return;
        objectUrl = URL.createObjectURL(b);
        setBlob(b);
        setUrl(objectUrl);
      })
      .catch(() => alive && setError(t.failed));
    return () => {
      alive = false;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
    // t o'zgarsa (til) — qayta chiziladi.
  }, [stats, t]);

  const file = blob ? new File([blob], t.fileName, { type: 'image/png' }) : null;
  const canShare = !!file && typeof navigator !== 'undefined' && !!navigator.canShare?.({ files: [file] });

  async function share() {
    if (!file) return;
    try {
      await navigator.share({ files: [file], text: `${t.tagline} — ${window.location.origin}` });
    } catch {
      // foydalanuvchi bekor qildi
    }
  }

  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true" aria-label={t.title} onClick={onClose}>
      <div className="modal card share-modal" onClick={(e) => e.stopPropagation()} data-testid="share-card">
        <div className="spread">
          <h3 style={{ margin: 0 }}>{t.title}</h3>
          <button className="btn-link" onClick={onClose} aria-label={t.close}>✕</button>
        </div>
        {error && <p className="error" role="alert">{error}</p>}
        {!url && !error && <p className="muted">{t.making}</p>}
        {url && <img src={url} alt={t.title} className="share-preview" data-testid="share-image" />}
        <p className="muted tiny" style={{ margin: 0 }}>{t.hint}</p>
        <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
          {canShare && (
            <button className="btn btn-primary" onClick={share}>{t.share}</button>
          )}
          {url && (
            <a className={canShare ? 'btn btn-outline' : 'btn btn-primary'} href={url} download={t.fileName} data-testid="share-download">
              {t.download}
            </a>
          )}
        </div>
      </div>
    </div>
  );
}
