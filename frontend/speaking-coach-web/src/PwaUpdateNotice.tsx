import { useEffect, useState } from 'react';
import { useT } from './i18n';
import { pwaMsg } from './locales/pwa';

export function PwaUpdateNotice() {
  const t = useT(pwaMsg);
  const [available, setAvailable] = useState(false);

  useEffect(() => {
    if (!('serviceWorker' in navigator)) return;
    const serviceWorker = navigator.serviceWorker;
    let hadController = Boolean(serviceWorker.controller);
    const onControllerChange = () => {
      if (hadController) setAvailable(true);
      else hadController = true;
    };
    serviceWorker.addEventListener('controllerchange', onControllerChange);
    return () => serviceWorker.removeEventListener('controllerchange', onControllerChange);
  }, []);

  if (!available) return null;

  return (
    <aside className="pwa-update" role="status" aria-live="polite">
      <span>{t.updateReady}</span>
      <button type="button" className="btn btn-primary" onClick={() => window.location.reload()}>
        {t.reload}
      </button>
      <button type="button" className="btn-link" onClick={() => setAvailable(false)}>
        {t.later}
      </button>
    </aside>
  );
}
