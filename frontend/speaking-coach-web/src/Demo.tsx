// Demo hisob: ro'yxatdan o'tmasdan, bir bosishda — server tayyor tarix,
// kartalar va seriyali vaqtinchalik hisob ochadi (24 soat). "#/demo" havolasi
// to'g'ridan-to'g'ri demo'ni ochadi (README'dagi tugma shunga olib keladi).
import { useEffect, useRef, useState } from 'react';
import { apiJson, getToken, postJson, setToken } from './api';
import { useT } from './i18n';
import { demoMsg } from './locales/demo';

export const DEMO_DOMAIN = 'demo.speakingcoach.invalid';

export const isDemoEmail = (email: string | null | undefined) => !!email && email.endsWith('@' + DEMO_DOMAIN);

let enabledPromise: Promise<boolean> | null = null;

/** Serverda demo yoqilganmi (/api/auth/config, bir marta so'raladi). */
export function useDemoEnabled(): boolean {
  const [enabled, setEnabled] = useState(false);
  useEffect(() => {
    enabledPromise ??= apiJson<{ demo?: boolean }>('/api/auth/config')
      .then((c) => c.demo === true)
      .catch(() => {
        enabledPromise = null;
        return false;
      });
    let alive = true;
    enabledPromise.then((v) => alive && setEnabled(v));
    return () => {
      alive = false;
    };
  }, []);
  return enabled;
}

export async function startDemo(): Promise<string> {
  const r = await postJson<{ token: string; email: string }>('/api/auth/demo', { tzOffsetMinutes: new Date().getTimezoneOffset() });
  setToken(r.token);
  return r.email;
}

/** "#/demo" sahifasi: demo hisobni ochadi va bosh sahifaga o'tkazadi. */
export function DemoStart({ email, onStarted, go }: { email: string | null; onStarted: (email: string) => void; go: (route: string) => void }) {
  const t = useT(demoMsg);
  const [error, setError] = useState('');
  const [attempt, setAttempt] = useState(0);
  const started = useRef(-1);

  useEffect(() => {
    // Allaqachon kirgan (o'z hisobi yoki demo) — hisobni almashtirmaymiz.
    if (email || getToken()) {
      go('home');
      return;
    }
    if (started.current === attempt) return; // StrictMode'da ikki marta ochilmasin
    started.current = attempt;
    setError('');
    startDemo()
      .then(onStarted)
      .catch((e) => setError(e instanceof Error ? e.message : t.failed));
  }, [attempt, email, go, onStarted, t.failed]);

  return (
    <div className="card" style={{ marginTop: 24 }} data-testid="demo-start" role="status">
      {error ? (
        <>
          <h3 style={{ marginTop: 0 }}>{t.failed}</h3>
          <p className="muted small" role="alert">{error}</p>
          <div className="row">
            <button className="btn btn-primary" onClick={() => setAttempt((a) => a + 1)}>{t.retry}</button>
            <button className="btn-link" onClick={() => go('home')}>{t.home}</button>
          </div>
        </>
      ) : (
        <>
          <h3 style={{ marginTop: 0 }}>⏳ {t.creating}</h3>
          <p className="muted small">{t.creatingHint}</p>
        </>
      )}
    </div>
  );
}

/** Kirish formasi ostidagi taklif (demo yoqilgan bo'lsa). */
export function DemoOffer({ go }: { go: (route: string) => void }) {
  const t = useT(demoMsg);
  const enabled = useDemoEnabled();
  if (!enabled) return null;
  return (
    <div className="card cta" data-testid="demo-offer">
      <div>
        <strong>{t.offerTitle}</strong>
        <div className="muted small">{t.offerText}</div>
      </div>
      <button className="btn btn-outline" onClick={() => go('demo')}>{t.offerButton}</button>
    </div>
  );
}

/** Bosh sahifadagi (mehmon) qisqa tugma. */
export function DemoHeroButton({ go }: { go: (route: string) => void }) {
  const t = useT(demoMsg);
  const enabled = useDemoEnabled();
  if (!enabled) return null;
  return (
    <button className="btn-link" style={{ color: '#fff' }} onClick={() => go('demo')} data-testid="demo-hero">
      {t.heroButton}
    </button>
  );
}

/** Demo hisobda tepada: nima ekanligi va o'z hisobini ochish. */
export function DemoBanner({ onRegister }: { onRegister: () => void }) {
  const t = useT(demoMsg);
  return (
    <div className="demo-banner" role="note" data-testid="demo-banner">
      <span>👀 {t.banner}</span>
      <button className="btn-link" onClick={onRegister}>{t.bannerButton} →</button>
    </div>
  );
}
