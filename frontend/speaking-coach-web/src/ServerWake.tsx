import { useEffect, useSyncExternalStore } from 'react';
import { wakeServer } from './api';
import { useT } from './i18n';
import { networkMsg } from './locales/network';
import { getSlowCount, subscribeSlow } from './netStatus';

let pinged = false;

/**
 * "Server uyg'onmoqda" banneri: biror so'rov 4 soniyadan ko'p javobsiz qolsa
 * pastda kichik xabar chiqadi (sahifani to'smaydi, bosishga xalaqit
 * bermaydi). Ilova ochilganda serverga bitta yengil "uyg'on" so'rovi ketadi.
 */
export function ServerWake() {
  const t = useT(networkMsg);
  const slow = useSyncExternalStore(subscribeSlow, getSlowCount, () => 0) > 0;

  useEffect(() => {
    // StrictMode'da effekt ikki marta ishlaydi — ping bitta bo'lsin.
    if (pinged) return;
    pinged = true;
    wakeServer();
  }, []);

  // role="status" doim DOM'da — ekran o'quvchi matn paydo bo'lganini e'lon qiladi.
  return (
    <div className={`server-wake${slow ? ' is-on' : ''}`} role="status" aria-live="polite" data-testid="server-wake">
      {slow && (
        <div className="server-wake-inner">
          <span className="server-wake-spinner" aria-hidden />
          <span>
            <strong>{t.wakeTitle}</strong> {t.wakeText}
          </span>
        </div>
      )}
    </div>
  );
}
