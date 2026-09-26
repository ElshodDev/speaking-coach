import { useCallback, useEffect, useRef, useState } from 'react';
import { apiFetch, apiJson, postJson } from './api';
import { common, useLang, useT } from './i18n';
import { telegramMsg } from './locales/telegram';

export interface TelegramStatus {
  enabled: boolean;
  botUsername: string;
  linked: boolean;
  username: string | null;
  reminderHour: number | null;
}

export const REMINDER_HOURS = [8, 13, 18, 20, 21];
export const hourLabel = (h: number) => `${String(h).padStart(2, '0')}:00`;

export function useTelegramStatus(loggedIn: boolean) {
  const [status, setStatus] = useState<TelegramStatus | null>(null);
  const refresh = useCallback(() => {
    if (!loggedIn) {
      setStatus(null);
      return Promise.resolve(null);
    }
    return apiJson<TelegramStatus>('/api/telegram/status')
      .then((s) => {
        setStatus(s);
        return s;
      })
      .catch(() => {
        setStatus(null);
        return null;
      });
  }, [loggedIn]);
  useEffect(() => {
    refresh();
  }, [refresh]);
  return [status, refresh] as const;
}

/**
 * Profil sahifasidagi Telegram bot kartasi. Ulash: sayt bir martalik havola
 * oladi (t.me/Bot?start=TOKEN) va Telegram'ni ochadi; foydalanuvchi START
 * bosgach, bu karta holatni o'zi tekshirib, "Ulangan"ga o'tadi.
 */
export function TelegramCard() {
  const t = useT(telegramMsg);
  const cm = useT(common);
  const { lang } = useLang();
  const [status, refresh] = useTelegramStatus(true);
  const [waiting, setWaiting] = useState(false);
  const [msg, setMsg] = useState('');
  const [error, setError] = useState('');
  const pollRef = useRef<number | null>(null);

  useEffect(() => () => {
    if (pollRef.current) window.clearInterval(pollRef.current);
  }, []);

  if (!status?.enabled) return null;

  async function connect() {
    setError('');
    try {
      const { url } = await postJson<{ url: string }>('/api/telegram/link', {
        tzOffsetMinutes: new Date().getTimezoneOffset(),
        lang,
      });
      window.open(url, '_blank', 'noopener');
      setWaiting(true);
      // 2 daqiqa davomida har 3 soniyada tekshiramiz — START bosilgach o'zi yangilanadi.
      let tries = 0;
      if (pollRef.current) window.clearInterval(pollRef.current);
      pollRef.current = window.setInterval(async () => {
        tries++;
        const s = await refresh();
        if (s?.linked || tries > 40) {
          if (pollRef.current) window.clearInterval(pollRef.current);
          setWaiting(false);
        }
      }, 3000);
    } catch (e) {
      setError(e instanceof Error ? e.message : cm.error);
    }
  }

  async function setReminder(value: string) {
    setError('');
    try {
      await apiJson('/api/telegram/settings', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reminderHour: value === 'off' ? null : Number(value) }),
      });
      await refresh();
      setMsg(t.saved);
    } catch (e) {
      setError(e instanceof Error ? e.message : cm.error);
    }
  }

  async function unlink() {
    if (!confirm(t.confirmUnlink)) return;
    await apiFetch('/api/telegram/link', { method: 'DELETE' });
    setMsg('');
    await refresh();
  }

  const botUrl = `https://t.me/${status.botUsername}`;

  return (
    <div className="card stack">
      <h3>{t.title}</h3>
      {!status.linked ? (
        <>
          <p className="muted small" style={{ margin: 0 }}>
            {t.pitch}
          </p>
          <button className="btn btn-primary" onClick={connect}>
            {t.connect}
          </button>
          {waiting && (
            <p className="small" role="status" style={{ margin: 0 }}>
              {t.waiting}{' '}
              <button className="btn-link small" onClick={() => refresh()}>
                {t.check}
              </button>
            </p>
          )}
        </>
      ) : (
        <>
          <p className="success small" style={{ margin: 0 }}>
            {t.linked(status.username)}
          </p>
          <label className="small" style={{ fontWeight: 600 }} htmlFor="tg-reminder">
            {t.reminder}
          </label>
          <select
            id="tg-reminder"
            className="input"
            style={{ maxWidth: 200 }}
            value={status.reminderHour === null ? 'off' : String(status.reminderHour)}
            onChange={(e) => setReminder(e.target.value)}
          >
            {REMINDER_HOURS.map((h) => (
              <option key={h} value={h}>
                {hourLabel(h)}
              </option>
            ))}
            {status.reminderHour !== null && !REMINDER_HOURS.includes(status.reminderHour) && (
              <option value={status.reminderHour}>{hourLabel(status.reminderHour)}</option>
            )}
            <option value="off">{t.reminderOff}</option>
          </select>
          <div className="row">
            <a className="btn btn-outline" href={botUrl} target="_blank" rel="noopener noreferrer">
              {t.open}
            </a>
            <button className="btn-link quiet small" onClick={unlink}>
              {t.unlink}
            </button>
          </div>
          {msg && <p className="success small" style={{ margin: 0 }}>{msg}</p>}
        </>
      )}
      {error && <p className="error small" role="alert">{error}</p>}
    </div>
  );
}

/** Bosh sahifadagi taklif: bot yoqilgan, lekin foydalanuvchi hali ulamagan bo'lsa. */
export function TelegramPromo({ go }: { go: (route: string) => void }) {
  const t = useT(telegramMsg);
  const [status] = useTelegramStatus(true);
  if (!status?.enabled || status.linked) return null;
  return (
    <div className="card cta">
      <div>
        <strong>{t.promoTitle}</strong>
        <div className="muted small">{t.promoText}</div>
      </div>
      <button className="btn btn-primary" onClick={() => go('profile')}>
        {t.promoButton}
      </button>
    </div>
  );
}
