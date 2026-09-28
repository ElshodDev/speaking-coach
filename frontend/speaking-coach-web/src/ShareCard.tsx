// Natija kartasi: Instagram/Telegram story (1080×1920) uchun rasm — brauzerda canvas'da chiziladi,
// serverga hech narsa yuborilmaydi. Telefonda "Ulashish" (Web Share), kompyuterda "Yuklab olish".
import { useEffect, useState } from 'react';
import { useT } from './i18n';
import { shareMsg } from './locales/share';
import { LOGO } from './Logo';

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
  emoji: string;
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
    { emoji: '🔥', value: String(streak.value), label: streak.label },
    { emoji: '📚', value: String(s.activities), label: t.activities },
    { emoji: '🔁', value: String(s.reviews), label: t.reviews },
    { emoji: '🏅', value: `${s.badges}/${s.badgesTotal}`, label: t.badges },
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

/** Ilova logotipi (Logo.tsx bilan bir xil shakllar), x/y — chap yuqori burchak, size — tomon uzunligi. */
function drawLogo(ctx: CanvasRenderingContext2D, x: number, y: number, size: number) {
  ctx.save();
  ctx.translate(x, y);
  ctx.scale(size / 100, size / 100);
  roundRect(ctx, 0, 0, 100, 100, 24);
  ctx.fillStyle = LOGO.bg;
  ctx.fill();
  ctx.lineWidth = 3;
  ctx.strokeStyle = 'rgba(255,255,255,0.35)';
  ctx.stroke();
  if (typeof Path2D !== 'undefined') {
    ctx.globalAlpha = 0.92;
    ctx.fillStyle = '#ffffff';
    ctx.fill(new Path2D(LOGO.back));
    ctx.globalAlpha = 1;
    const front = new Path2D(LOGO.front);
    ctx.lineWidth = 4;
    ctx.lineJoin = 'round';
    ctx.strokeStyle = LOGO.bg;
    ctx.stroke(front);
    ctx.fillStyle = LOGO.accent;
    ctx.fill(front);
    ctx.fillStyle = LOGO.dots;
    for (const cx of [52, 62, 72]) {
      ctx.beginPath();
      ctx.arc(cx, 61, 3.5, 0, Math.PI * 2);
      ctx.fill();
    }
  }
  ctx.restore();
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

export async function drawShareCard(s: ShareStats, t: (typeof shareMsg)['uz'], site: string): Promise<Blob> {
  try {
    await document.fonts?.load(`800 96px 'Inter Variable'`);
    await document.fonts?.ready;
  } catch {
    // shrift yuklanmasa — tizim shrifti bilan chiziladi
  }
  const canvas = document.createElement('canvas');
  canvas.width = W;
  canvas.height = H;
  const ctx = canvas.getContext('2d');
  if (!ctx) throw new Error('canvas');

  // Fon: ko'k → binafsha gradient va yumshoq doiralar.
  const bg = ctx.createLinearGradient(0, 0, W, H);
  bg.addColorStop(0, '#1d4ed8');
  bg.addColorStop(0.55, '#4f46e5');
  bg.addColorStop(1, '#7c3aed');
  ctx.fillStyle = bg;
  ctx.fillRect(0, 0, W, H);
  ctx.fillStyle = 'rgba(255,255,255,0.08)';
  for (const [x, y, r] of [[980, 180, 260], [80, 1500, 320], [900, 1750, 180]]) {
    ctx.beginPath();
    ctx.arc(x, y, r, 0, Math.PI * 2);
    ctx.fill();
  }

  ctx.textBaseline = 'alphabetic';
  ctx.fillStyle = '#fff';
  ctx.textAlign = 'left';

  // Logotip va nom.
  drawLogo(ctx, 90, 150, 96);
  ctx.font = `700 52px ${FONT}`;
  ctx.fillText('Speaking Coach', 214, 214);

  ctx.font = `500 44px ${FONT}`;
  ctx.fillStyle = 'rgba(255,255,255,0.85)';
  ctx.fillText(t.tagline, 90, 400);

  ctx.fillStyle = '#fff';
  fitText(ctx, s.name ?? 'Speaking Coach', 800, 120, W - 180);
  ctx.fillText(s.name ?? 'Speaking Coach', 90, 540);

  // Daraja "tabletkasi".
  const levelText = t.level(s.level, s.xp);
  ctx.font = `700 48px ${FONT}`;
  const lw = ctx.measureText(levelText).width + 72;
  roundRect(ctx, 90, 600, lw, 92, 46);
  ctx.fillStyle = '#fff';
  ctx.fill();
  ctx.fillStyle = '#3730a3';
  ctx.fillText(levelText, 126, 663);

  // 2×2 ko'rsatkichlar.
  const tiles = shareTiles(s, t);
  const tw = (W - 180 - 40) / 2;
  const th = 330;
  tiles.forEach((tile, i) => {
    const x = 90 + (i % 2) * (tw + 40);
    const y = 800 + Math.floor(i / 2) * (th + 40);
    roundRect(ctx, x, y, tw, th, 44);
    ctx.fillStyle = 'rgba(255,255,255,0.14)';
    ctx.fill();
    ctx.strokeStyle = 'rgba(255,255,255,0.25)';
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.fillStyle = '#fff';
    ctx.font = `76px ${FONT}`;
    ctx.fillText(tile.emoji, x + 44, y + 110);
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
