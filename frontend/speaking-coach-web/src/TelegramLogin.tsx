// Sayt orqali Telegram bilan kirish: sayt bot havolasini va 2 xonali raqamni
// beradi, foydalanuvchi botda shu raqamni bosadi, sayt esa har 2 soniyada
// "tasdiqlandimi?" deb so'raydi. Raqamni solishtirish — begona odam o'z
// havolasini yuborib, sizni o'z hisobiga "kiritib qo'yishidan" himoya.
import { useEffect, useRef, useState } from 'react';
import { ApiError, apiJson, postJson } from './api';
import { common, useLang, useT } from './i18n';
import { tgLoginMsg } from './locales/tgLogin';
import { inTelegram, openExternal } from './tgApp';

interface StartResponse {
  loginId: string;
  pollToken: string;
  botUrl: string;
  code: string;
  expiresInSeconds: number;
}

export interface TelegramLoginResult {
  token: string;
  email: string;
  isNew?: boolean;
}

type PollResponse =
  | { status: 'pending' | 'expired' | 'rejected' }
  | ({ status: 'confirmed' } & TelegramLoginResult);

type State =
  | { kind: 'idle' }
  | { kind: 'starting' }
  | { kind: 'waiting'; start: StartResponse; until: number }
  | { kind: 'rejected' | 'expired' };

const POLL_MS = 2000;

/** Serverdan kelgan holatni ekrandagi holatga aylantiradi. Toza funksiya — testlanadi. */
export function nextState(poll: PollResponse['status'], now: number, until: number): 'wait' | 'done' | 'rejected' | 'expired' {
  if (poll === 'confirmed') return 'done';
  if (poll === 'rejected') return 'rejected';
  if (poll === 'expired' || now >= until) return 'expired';
  return 'wait';
}

export function TelegramLogin({ onSuccess }: { onSuccess: (r: TelegramLoginResult) => void }) {
  const t = useT(tgLoginMsg);
  const cm = useT(common);
  const { lang } = useLang();
  const [state, setState] = useState<State>({ kind: 'idle' });
  const [error, setError] = useState('');
  const done = useRef(false);
  const successRef = useRef(onSuccess);
  successRef.current = onSuccess;

  async function start() {
    setError('');
    setState({ kind: 'starting' });
    try {
      const s = await postJson<StartResponse>('/api/auth/telegram/start', { lang });
      done.current = false;
      setState({ kind: 'waiting', start: s, until: Date.now() + s.expiresInSeconds * 1000 });
      // Telegram ichida (Mini App/brauzer) — bot shu yerning o'zida ochiladi.
      if (inTelegram()) openExternal(s.botUrl);
    } catch (e) {
      setState({ kind: 'idle' });
      setError(e instanceof Error ? e.message : cm.error);
    }
  }

  const waiting = state.kind === 'waiting' ? state : null;

  useEffect(() => {
    if (!waiting) return;
    let stopped = false;
    let inFlight = false;
    const poll = async () => {
      if (stopped || inFlight || done.current) return;
      inFlight = true;
      try {
        const r = await apiJson<PollResponse>('/api/auth/telegram/poll', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ pollToken: waiting.start.pollToken }),
          timeoutMs: 15_000,
        });
        if (stopped) return;
        const next = nextState(r.status, Date.now(), waiting.until);
        if (next === 'done' && r.status === 'confirmed') {
          done.current = true;
          successRef.current({ token: r.token, email: r.email, isNew: r.isNew });
        } else if (next === 'rejected' || next === 'expired') {
          setState({ kind: next });
        }
      } catch (e) {
        // Tarmoq uzilishi — keyingi urinishda davom etadi; muddati tugagan bo'lsa to'xtaymiz.
        if (!stopped && Date.now() >= waiting.until) setState({ kind: 'expired' });
        if (e instanceof ApiError && e.status === 429) {
          // juda tez-tez — keyingi taymer kutib turadi
        }
      } finally {
        inFlight = false;
      }
    };
    const id = window.setInterval(poll, POLL_MS);
    // Foydalanuvchi Telegram'dan qaytganda — kutmasdan darhol tekshiramiz.
    const onVisible = () => {
      if (document.visibilityState === 'visible') void poll();
    };
    document.addEventListener('visibilitychange', onVisible);
    window.addEventListener('focus', onVisible);
    return () => {
      stopped = true;
      window.clearInterval(id);
      document.removeEventListener('visibilitychange', onVisible);
      window.removeEventListener('focus', onVisible);
    };
  }, [waiting]);

  if (state.kind === 'waiting') {
    const { start: s } = state;
    return (
      <div className="tg-login card soft stack" data-testid="tg-login-wait" aria-live="polite">
        <ol className="small tg-steps">
          <li>{t.step1}</li>
          <li>{t.step2(s.code)}</li>
          <li>{t.step3}</li>
        </ol>
        <div className="tg-code" aria-label={t.codeLabel} data-testid="tg-code">{s.code}</div>
        <a
          className="btn btn-telegram block"
          href={s.botUrl}
          target="_blank"
          rel="noopener noreferrer"
          data-testid="tg-open"
          onClick={(e) => {
            if (inTelegram()) {
              e.preventDefault();
              openExternal(s.botUrl);
            }
          }}
        >
          {t.open}
        </a>
        <div className="spread">
          <span className="muted tiny">⏳ {t.waiting}</span>
          <button type="button" className="btn-link small" onClick={() => setState({ kind: 'idle' })}>
            {t.cancel}
          </button>
        </div>
      </div>
    );
  }

  if (state.kind === 'rejected' || state.kind === 'expired') {
    return (
      <div className="tg-login stack" data-testid="tg-login-failed">
        <p className="error small" role="alert" style={{ margin: 0 }}>{state.kind === 'rejected' ? t.rejected : t.expired}</p>
        <button type="button" className="btn btn-telegram block" onClick={start}>
          {t.retry}
        </button>
      </div>
    );
  }

  return (
    <div className="tg-login stack">
      <button type="button" className="btn btn-telegram block" onClick={start} disabled={state.kind === 'starting'} data-testid="tg-login">
        <TelegramIcon /> {state.kind === 'starting' ? t.starting : t.button}
      </button>
      <p className="muted tiny" style={{ margin: 0, textAlign: 'center' }}>{t.hint}</p>
      {error && <p className="error small" role="alert" style={{ margin: 0 }}>{error}</p>}
    </div>
  );
}

function TelegramIcon() {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true" style={{ flex: 'none' }}>
      <path
        fill="currentColor"
        d="M21.9 4.3 18.8 19c-.2 1-.8 1.3-1.7.8l-4.6-3.4-2.2 2.1c-.2.2-.5.4-.9.4l.3-4.7 8.6-7.8c.4-.3-.1-.5-.6-.2L7.1 12.9 2.5 11.5c-1-.3-1-1 .2-1.5L20.6 3.1c.8-.3 1.6.2 1.3 1.2z"
      />
    </svg>
  );
}
