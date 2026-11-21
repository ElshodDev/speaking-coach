import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { useT } from './i18n';
import { referralMsg } from './locales/referral';
import { referralLink, type ReferralInfo } from './referral';
import { Icon } from './Icon';

/** Profilda: do'stni taklif qilish. Server o'chirgan (feature flag) yoki xato bo'lsa — hech narsa ko'rsatilmaydi. */
export function ReferralCard() {
  const t = useT(referralMsg);
  const [info, setInfo] = useState<ReferralInfo | null>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let alive = true;
    apiJson<ReferralInfo>('/api/referral')
      .then((r) => alive && setInfo(r))
      .catch(() => undefined);
    return () => {
      alive = false;
    };
  }, []);

  if (!info?.enabled) return null;
  const link = referralLink(window.location.origin, info.code);

  async function copy() {
    try {
      await navigator.clipboard.writeText(link);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      // Nusxa olib bo'lmasa — havola maydonidan qo'lda olinadi.
    }
  }

  async function share() {
    if (typeof navigator.share === 'function') {
      try {
        await navigator.share({ title: 'FluentUZ', text: t.shareText, url: link });
        return;
      } catch {
        // Foydalanuvchi bekor qildi yoki ulashish ishlamadi — pastdagi usulga o'tamiz.
      }
    }
    // Telegram — bu auditoriyada eng ko'p ishlatiladigan kanal.
    window.open(`https://t.me/share/url?url=${encodeURIComponent(link)}&text=${encodeURIComponent(t.shareText)}`, '_blank', 'noopener');
  }

  return (
    <div className="card" data-testid="referral-card">
      <strong>{t.title}</strong>
      <p className="muted small" style={{ margin: '6px 0 12px' }}>{t.text(info.bonusDays, info.bonusExtraExercises)}</p>
      <label className="small" htmlFor="referral-link">{t.linkLabel}</label>
      <input id="referral-link" className="input" readOnly value={link} onFocus={(e) => e.currentTarget.select()} style={{ width: '100%', marginTop: 4 }} />
      <div className="row" style={{ gap: 8, marginTop: 10 }}>
        <button type="button" className="btn btn-outline" onClick={copy} data-testid="referral-copy">
          <Icon name="copy" size={16} /> {copied ? t.copied : t.copy}
        </button>
        <button type="button" className="btn btn-primary" onClick={share} data-testid="referral-share">
          <Icon name="share" size={16} /> {t.share}
        </button>
      </div>
      <p className="small" style={{ margin: '12px 0 0' }} data-testid="referral-invited">{t.invited(info.invited)}</p>
      {info.bonusActive && <p className="offline-note small" role="status" style={{ marginTop: 8 }}>{t.bonusOn(info.bonusExtraExercises)}</p>}
      <p className="muted tiny" style={{ margin: '8px 0 0' }}>{t.note}</p>
    </div>
  );
}
