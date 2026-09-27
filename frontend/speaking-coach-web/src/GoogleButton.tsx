import { useEffect, useRef } from 'react';
import { useLang } from './i18n';

/** Google Identity Services (GIS) — faqat bizga kerak bo'lgan qismi. */
interface GoogleIdApi {
  initialize: (options: { client_id: string; callback: (response: { credential: string }) => void; ux_mode?: 'popup' }) => void;
  renderButton: (parent: HTMLElement, options: Record<string, unknown>) => void;
}

declare global {
  interface Window {
    google?: { accounts: { id: GoogleIdApi } };
  }
}

const SCRIPT_SRC = 'https://accounts.google.com/gsi/client';
let scriptPromise: Promise<void> | null = null;

/** Google skripti faqat kerak bo'lganda (Profil sahifasida) va bir marta yuklanadi. */
export function loadGoogleScript(): Promise<void> {
  if (window.google?.accounts?.id) return Promise.resolve();
  scriptPromise ??= new Promise<void>((resolve, reject) => {
    const s = document.createElement('script');
    s.src = SCRIPT_SRC;
    s.async = true;
    s.defer = true;
    s.onload = () => resolve();
    s.onerror = () => {
      scriptPromise = null; // keyingi safar qayta urinib ko'rsin
      reject(new Error('Google script failed to load'));
    };
    document.head.appendChild(s);
  });
  return scriptPromise;
}

/**
 * Google'ning rasmiy "Google bilan kirish" tugmasi. Bosilganda Google oynasi
 * ochiladi va imzolangan ID token (credential) qaytadi — uni serverga
 * yuboramiz, server imzoni o'zi tekshiradi. Tugma matni interfeys tilida.
 */
export function GoogleButton({
  clientId,
  onCredential,
  onUnavailable,
}: {
  clientId: string;
  onCredential: (credential: string) => void;
  /** Skript yuklanmadi yoki tugma chizilmadi (bloklangan, internet sekin) — email kodi taklif qilinadi. */
  onUnavailable?: () => void;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const { lang } = useLang();
  // callback har renderda yangilanadi, Google esa birinchisini eslab qoladi — ref orqali eng yangisini chaqiramiz.
  const callbackRef = useRef(onCredential);
  callbackRef.current = onCredential;
  const unavailableRef = useRef(onUnavailable);
  unavailableRef.current = onUnavailable;

  useEffect(() => {
    let cancelled = false;
    // 10 soniyada tugma paydo bo'lmasa — foydalanuvchi kutib qolmasin.
    const timer = setTimeout(() => {
      if (!cancelled && !ref.current?.childElementCount) unavailableRef.current?.();
    }, 10_000);
    loadGoogleScript()
      .then(() => {
        const api = window.google?.accounts?.id;
        if (cancelled || !api || !ref.current) return;
        api.initialize({ client_id: clientId, callback: (r) => callbackRef.current(r.credential), ux_mode: 'popup' });
        ref.current.innerHTML = '';
        api.renderButton(ref.current, {
          type: 'standard',
          theme: 'outline',
          size: 'large',
          text: 'continue_with',
          shape: 'pill',
          locale: lang,
          width: Math.min(360, ref.current.clientWidth || 320),
        });
      })
      // Google bloklangan bo'lsa ham email bilan kirish ishlaydi.
      .catch(() => {
        if (!cancelled) unavailableRef.current?.();
      });
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [clientId, lang]);

  return <div ref={ref} className="google-button" />;
}
