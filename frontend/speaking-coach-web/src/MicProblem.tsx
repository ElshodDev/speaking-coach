import { useT } from './i18n';
import { chromeIntent, currentInApp, isAndroid } from './inApp';
import { micMsg } from './locales/mic';
import type { MicProblem } from './micErrors';

/**
 * Mikrofon muammosi: aniq sabab, yechim va "Qayta urinish" tugmasi.
 * Ilova ichidagi brauzerda (Telegram, Instagram…) — Chrome/Safari'da ochish maslahati.
 */
export function MicProblemNote({ problem, onRetry }: { problem: MicProblem; onRetry?: () => void }) {
  const t = useT(micMsg);
  const app = problem === 'denied' || problem === 'unsupported' || problem === 'other' ? currentInApp() : null;
  const android = typeof navigator !== 'undefined' && isAndroid(navigator.userAgent);

  return (
    <div className="inapp-note small" role="alert" style={{ marginTop: 10 }} data-testid="mic-problem">
      <p style={{ margin: 0 }}>{t[problem]}</p>
      {app && <p style={{ margin: '6px 0 0' }}>{t.deniedInApp(app)}</p>}
      {(onRetry || (app && android)) && (
        <div className="row" style={{ marginTop: 8, gap: 8, flexWrap: 'wrap' }}>
          {onRetry && problem !== 'insecure' && (
            <button className="btn btn-outline btn-sm" onClick={onRetry}>
              {t.retry}
            </button>
          )}
          {app && android && (
            <a className="btn btn-outline btn-sm" href={chromeIntent(window.location)}>
              {t.openChrome}
            </a>
          )}
        </div>
      )}
    </div>
  );
}
