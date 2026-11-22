import { useCallback, useEffect, useRef, useState } from 'react';
import { apiJson, postJson } from './api';
import { openExternal } from './tgApp';
import { common, useLang, useT } from './i18n';
import { telegramMsg } from './locales/telegram';
import { readPromoDismissedAt, savePromoDismissedAt, shouldShowPromo } from './tgPromo';

export interface TelegramStatus {
  enabled: boolean;
  botUsername: string;
  linked: boolean;
  username: string | null;
  reminderHour: number | null;
}

const REMINDER_HOURS = [8, 13, 18, 20, 21];
const hourLabel = (h: number) => `${String(h).padStart(2, '0')}:00`;

function useTelegramStatus(loggedIn: boolean) {
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
 * Telegram'ni ulash: sayt bir martalik havola oladi (t.me/Bot?start=TOKEN), Telegram'ni ochadi va
 * START bosilguncha holatni tekshirib turadi (2 daqiqa, har 3 soniyada). Profil kartasi ham, bosh sahifadagi taklif ham shuni ishlatadi.
 */
function useTelegramConnect(refresh: () => Promise<TelegramStatus | null>) {
  const cm = useT(common);
  const { lang } = useLang();
  const [waiting, setWaiting] = useState(false);
  const [error, setError] = useState('');
  const pollRef = useRef<number | null>(null);

  useEffect(() => () => {
    if (pollRef.current) window.clearInterval(pollRef.current);
  }, []);

  async function connect() {
    setError('');
    try {
      const { url } = await postJson<{ url: string }>('/api/telegram/link', {
        tzOffsetMinutes: new Date().getTimezoneOffset(),
        lang,
      });
      openExternal(url);
      setWaiting(true);
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

  return { connect, waiting, error, setError };
}

/**
 * Profil sahifasidagi Telegram bot kartasi. Ulash: sayt bir martalik havola
 * oladi (t.me/Bot?start=TOKEN) va Telegram'ni ochadi; foydalanuvchi START
 * bosgach, bu karta holatni o'zi tekshirib, "Ulangan"ga o'tadi.
 */
export function TelegramCard() {
  const t = useT(telegramMsg);
  const cm = useT(common);
  const [status, refresh] = useTelegramStatus(true);
  const [msg, setMsg] = useState('');
  const { connect, waiting, error, setError } = useTelegramConnect(refresh);

  if (!status?.enabled) return null;

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
    setError('');
    try {
      // apiJson xato javobda otadi — "uzildi" deb faqat server tasdiqlaganda ko'rsatamiz.
      await apiJson('/api/telegram/link', { method: 'DELETE' });
      setMsg('');
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : cm.error);
    }
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
          <BotFeatures />
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
          <p className="muted small" style={{ margin: 0 }}>{t.linkedHint}</p>
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
            <a
              className="btn btn-outline"
              href={botUrl}
              target="_blank"
              rel="noopener noreferrer"
              onClick={(e) => {
                e.preventDefault();
                openExternal(botUrl);
              }}
            >
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

/**
 * Bosh sahifadagi taklif: bot yoqilgan, lekin foydalanuvchi hali ulamagan bo'lsa. "Ulash" Telegram'ni darhol ochadi
 * (profilga o'tkazmaydi). "Keyinroq" — 7 kunga yashiradi, qayta-qayta bezovta qilmaydi.
 */
export function TelegramPromo({ go }: { go: (route: string) => void }) {
  const t = useT(telegramMsg);
  const [status, refresh] = useTelegramStatus(true);
  const { connect, waiting, error } = useTelegramConnect(refresh);
  const [hidden, setHidden] = useState(() => !shouldShowPromo(readPromoDismissedAt(), Date.now()));
  if (!status?.enabled || status.linked || hidden) return null;
  return (
    <div className="card stack bot-promo" data-testid="bot-promo">
      <div>
        <strong>{t.promoTitle}</strong>
        <div className="muted small">{t.promoText}</div>
      </div>
      <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
        <button className="btn btn-primary" onClick={connect} data-testid="bot-promo-connect">
          {t.promoButton}
        </button>
        <button
          className="btn btn-outline"
          onClick={() => {
            savePromoDismissedAt(Date.now());
            setHidden(true);
          }}
          data-testid="bot-promo-later"
        >
          {t.promoLater}
        </button>
        <button className="btn-link small" onClick={() => go('profile')}>
          {t.promoMore}
        </button>
      </div>
      {waiting && (
        <p className="small" role="status" style={{ margin: 0 }}>
          {t.waiting}{' '}
          <button className="btn-link small" onClick={() => refresh()}>
            {t.check}
          </button>
        </p>
      )}
      {error && <p className="error small" role="alert" style={{ margin: 0 }}>{error}</p>}
    </div>
  );
}

/** Bot nima qila olishi — qisqa ro'yxat (profil kartasi va bosh sahifadagi taklifda). */
function BotFeatures() {
  const t = useT(telegramMsg);
  return (
    <ul className="bot-features" data-testid="bot-features">
      {t.features.map((f) => (
        <li key={f}>{f}</li>
      ))}
    </ul>
  );
}
