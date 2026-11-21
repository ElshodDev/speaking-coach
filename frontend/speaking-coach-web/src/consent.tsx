import { useEffect, useRef, useState } from 'react';
import { track } from './analytics';
import { useT } from './i18n';
import { consentMsg } from './locales/consent';

// Birinchi yozishdan oldin: "ovoz AI'ga yuboriladi" roziligi.
// - Rozilik shu qurilmada saqlanadi (localStorage) va bir marta so'raladi.
// - O'chirish (feature flag): VITE_MIC_CONSENT=off bilan build qilinsa — dialog umuman chiqmaydi.
// - Mikrofon ishlatiladigan har bir joy getUserMedia'dan OLDIN `await requestMicConsent()` chaqiradi.

const KEY = 'sc.micConsent.v1';

type Resolver = (ok: boolean) => void;
let pending: Resolver[] = [];
const listeners = new Set<() => void>();

export function consentEnabled(): boolean {
  return import.meta.env.VITE_MIC_CONSENT !== 'off';
}

export function hasMicConsent(): boolean {
  try {
    return localStorage.getItem(KEY) === '1';
  } catch {
    return false;
  }
}

function notify() {
  listeners.forEach((l) => l());
}

/** Dialogdagi tugmalar shuni chaqiradi (testlar ham). */
export function answerMicConsent(ok: boolean) {
  if (ok) {
    try {
      localStorage.setItem(KEY, '1');
    } catch {
      // Saqlab bo'lmadi — keyingi safar yana so'raladi; muhim emas.
    }
    track('mic_consent_accepted', undefined, { once: true });
  }
  const resolvers = pending;
  pending = [];
  resolvers.forEach((r) => r(ok));
  notify();
}

/** true — davom etish mumkin (rozilik bor yoki flag o'chiq); false — foydalanuvchi bekor qildi. */
export function requestMicConsent(): Promise<boolean> {
  if (!consentEnabled() || hasMicConsent()) return Promise.resolve(true);
  return new Promise((resolve) => {
    pending.push(resolve);
    notify();
  });
}

/** App ichida bir marta turadi: so'rov bo'lsa dialog ko'rsatadi. */
export function MicConsentHost() {
  const t = useT(consentMsg);
  const [open, setOpen] = useState(false);
  const acceptRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const sync = () => setOpen(pending.length > 0);
    listeners.add(sync);
    return () => {
      listeners.delete(sync);
    };
  }, []);

  useEffect(() => {
    if (!open) return;
    acceptRef.current?.focus();
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && answerMicConsent(false);
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open]);

  if (!open) return null;
  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="consent-title" aria-describedby="consent-body" onClick={() => answerMicConsent(false)}>
      <div className="modal card consent-modal" onClick={(e) => e.stopPropagation()} data-testid="mic-consent">
        <h3 id="consent-title" style={{ margin: 0 }}>{t.title}</h3>
        <p id="consent-body" style={{ margin: 0 }}>{t.body}</p>
        <p className="muted small" style={{ margin: 0 }}>{t.warning}</p>
        <a className="btn-link small" href="/privacy.html" target="_blank" rel="noopener">{t.privacy}</a>
        <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
          <button ref={acceptRef} className="btn btn-primary" onClick={() => answerMicConsent(true)} data-testid="mic-consent-accept">{t.accept}</button>
          <button className="btn btn-outline" onClick={() => answerMicConsent(false)} data-testid="mic-consent-cancel">{t.cancel}</button>
        </div>
      </div>
    </div>
  );
}

/** Faqat testlar uchun: ichki holatni tozalash. */
export function resetConsentForTests() {
  pending = [];
  listeners.clear();
}
