import { useEffect, useRef, useState, type FormEvent } from 'react';
import { apiFetch, apiJson, postJson, setToken } from './api';
import { common, useLang, useT } from './i18n';
import { authMsg } from './locales/auth';
import { GoogleButton } from './GoogleButton';
import { chromeIntent, currentInApp, isAndroid, isIos } from './inApp';
import { inTelegram } from './tgApp';

/** Server javobi: yo token (kirildi), yo "kodni kiriting", yo shunchaki ok. */
interface AuthResponse {
  token?: string;
  email?: string;
  needsVerification?: boolean;
  ok?: boolean;
}

/** code / code-verify — parolsiz kirish (emailga kod); qolganlari — parol bilan. */
type Mode = 'code' | 'code-verify' | 'register' | 'login' | 'verify' | 'forgot' | 'reset';

const RESEND_SECONDS = 60;

/**
 * Kirish / ro'yxatdan o'tish formasi yoki (kirgan bo'lsa) hisob kartasi.
 *
 * Email yuborish sozlangan bo'lsa, asosiy yo'l — parolsiz: email → 6 xonali
 * kod → kirildi (hisob bo'lmasa, o'zi ochiladi). Telefonda parol o'ylab topish
 * va yozish shart emas. Parol bilan kirish ham qoladi ("🔑 Parol bilan kirish").
 * Parol bilan ro'yxatdan o'tish → emailga 6 xonali kod → kodni kiritish → hisob ochiladi.
 * Shunday qilib bazada faqat haqiqatan mavjud va egasi qo'lidagi emaillar
 * qoladi. "Parolni unutdim" ham xuddi shu kod orqali ishlaydi.
 */
export function AuthPanel({
  email,
  onChange,
  initialMode = 'login',
}: {
  email: string | null;
  onChange: (email: string | null) => void;
  /** Tepadagi "Kirish" → login; "Hisob ochish" → register. */
  initialMode?: 'login' | 'register';
}) {
  // Sozlama kelguncha parolsiz forma; xat yuborish o'chiq bo'lsa — parol bilan.
  const [mode, setMode] = useState<Mode>('code');
  // Kod oynasidan "Orqaga" — qaysi formadan kelgan bo'lsa, o'shanga.
  const [cameFrom, setCameFrom] = useState<'login' | 'register'>(initialMode);
  const [showPassword, setShowPassword] = useState(false);
  const [formEmail, setFormEmail] = useState('');
  const [password, setPassword] = useState('');
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');
  const [busy, setBusy] = useState(false);
  const [resendIn, setResendIn] = useState(0);
  const [canReset, setCanReset] = useState(false);
  const [googleClientId, setGoogleClientId] = useState<string | null>(null);
  const [googleBroken, setGoogleBroken] = useState(false);
  const [copied, setCopied] = useState(false);
  // Avtomatik yuborilgan oxirgi kod — bir xil kod ikki marta yuborilmasin.
  const lastTried = useRef('');
  const inApp = currentInApp();
  const t = useT(authMsg);
  const cm = useT(common);
  const { lang } = useLang();

  // Xat yuborish sozlanmagan bo'lsa, "Parolni unutdim" ma'nosiz — ko'rsatmaymiz.
  useEffect(() => {
    apiJson<{ emailVerification: boolean; googleClientId?: string | null }>('/api/auth/config')
      .then((c) => {
        setCanReset(c.emailVerification);
        setGoogleClientId(c.googleClientId ?? null);
        if (!c.emailVerification) setMode((m) => (m === 'code' ? initialMode : m));
      })
      .catch(() => {
        setCanReset(false);
        setMode((m) => (m === 'code' ? initialMode : m));
      });
  }, [initialMode]);

  // "Qayta yuborish" tugmasi uchun teskari sanoq (server ham 60 s cheklaydi).
  useEffect(() => {
    if (resendIn <= 0) return;
    const id = setTimeout(() => setResendIn((s) => s - 1), 1000);
    return () => clearTimeout(id);
  }, [resendIn]);

  function go(next: Mode) {
    setMode(next);
    setError('');
    setInfo('');
    setCode('');
    lastTried.current = '';
  }

  function finish(data: AuthResponse) {
    if (data.token) {
      setToken(data.token);
      setPassword('');
      setCode('');
      onChange(data.email ?? formEmail);
    }
  }

  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError('');
    setInfo('');
    try {
      await action();
    } catch (err) {
      setError(err instanceof Error ? err.message : cm.error);
    } finally {
      setBusy(false);
    }
  }

  function sendLoginCode() {
    run(async () => {
      const data = await postJson<AuthResponse>('/api/auth/code/request', { email: formEmail });
      if (data.email) setFormEmail(data.email);
      go('code-verify');
      setResendIn(RESEND_SECONDS);
    });
  }

  function verifyLoginCode(value: string) {
    run(async () => finish(await postJson<AuthResponse>('/api/auth/code/verify', { email: formEmail, code: value })));
  }

  function submit(e: FormEvent) {
    e.preventDefault();
    if (mode === 'code') return sendLoginCode();
    if (mode === 'code-verify') return verifyLoginCode(code);
    run(async () => {
      if (mode === 'register' || mode === 'login') {
        const data = await postJson<AuthResponse>(`/api/auth/${mode}`, { email: formEmail, password });
        if (data.needsVerification) {
          if (data.email) setFormEmail(data.email);
          setCameFrom(mode);
          go('verify');
          setResendIn(RESEND_SECONDS);
          return;
        }
        finish(data);
      } else if (mode === 'verify') {
        // Parol kod bilan birga yuboriladi — server uni faqat to'g'ri kodda o'rnatadi (hisobni egallab olishdan himoya).
        finish(await postJson<AuthResponse>('/api/auth/verify', { email: formEmail, code, password }));
      } else if (mode === 'forgot') {
        await postJson('/api/auth/forgot', { email: formEmail });
        go('reset');
        setResendIn(RESEND_SECONDS);
      } else {
        finish(await postJson<AuthResponse>('/api/auth/reset', { email: formEmail, code, newPassword: password }));
      }
    });
  }

  function google(credential: string) {
    run(async () => finish(await postJson<AuthResponse>('/api/auth/google', { credential })));
  }

  function resend() {
    run(async () => {
      await postJson(mode === 'reset' ? '/api/auth/forgot' : mode === 'code-verify' ? '/api/auth/code/request' : '/api/auth/resend', { email: formEmail });
      setResendIn(RESEND_SECONDS);
      setInfo(t.resent);
    });
  }

  async function logout() {
    // Avval serverdagi sessiyani o'chiramiz (token darhol yaroqsiz bo'ladi),
    // keyin brauzerdagini. Server javob bermasa ham, brauzerdan baribir
    // o'chiramiz — foydalanuvchi "chiqa olmay qolmasin".
    await apiFetch('/api/auth/logout', { method: 'POST' }).catch(() => undefined);
    setToken(null);
    onChange(null);
  }

  if (email) {
    return (
      <div className="card spread">
        <div className="break">
          <div className="muted tiny">{t.account}</div>
          <strong>{email}</strong>
        </div>
        <button className="btn btn-outline" onClick={logout}>
          {t.logout}
        </button>
      </div>
    );
  }

  const messages = (
    <>
      {error && <p className="error small" role="alert">{error}</p>}
      {info && <p className="success small">{info}</p>}
    </>
  );

  const codeInput = (
    <input
      className="input code-input"
      aria-label={t.code}
      placeholder="••••••"
      inputMode="numeric"
      autoComplete="one-time-code"
      maxLength={7}
      required
      value={code}
      onChange={(e) => {
        const v = e.target.value.replace(/[^\d ]/g, '');
        setCode(v);
        // Parolsiz kirishda 6-raqam kiritilishi bilan — tugmasiz (SMS/xatdan avtomatik to'ldirilganda ham).
        const digits = v.replace(/\D/g, '');
        if (mode === 'code-verify' && !busy && digits.length === 6 && digits !== lastTried.current) {
          lastTried.current = digits;
          verifyLoginCode(v);
        }
      }}
    />
  );

  const resendButton = (
    <button type="button" className="btn-link small" style={{ whiteSpace: 'nowrap' }} onClick={resend} disabled={busy || resendIn > 0}>
      {resendIn > 0 ? t.resendIn(resendIn) : t.resend}
    </button>
  );

  function copyLink() {
    const url = window.location.origin + window.location.pathname;
    navigator.clipboard?.writeText(url).then(
      () => setCopied(true),
      () => window.prompt(t.copyLink, url),
    );
  }

  const ua = typeof navigator === 'undefined' ? '' : navigator.userAgent;
  // Ilova ichidagi brauzer (Instagram, Telegram…): Google bloklaydi — tushuntiramiz va tashqi brauzerni taklif qilamiz.
  const googleBlock =
    inApp || inTelegram() ? (
      googleClientId ? (
        <div className="inapp-note" data-testid="inapp-note">
          <strong className="small">{t.inAppTitle(inApp ?? 'Telegram')}</strong>
          <p className="muted tiny" style={{ margin: '4px 0 8px' }}>{t.inAppText}</p>
          <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
            {isAndroid(ua) && (
              <a className="btn btn-outline btn-sm" href={chromeIntent(window.location)}>
                {t.openChrome}
              </a>
            )}
            <button type="button" className="btn btn-outline btn-sm" onClick={copyLink}>
              {copied ? t.copied : t.copyLink}
            </button>
          </div>
          {isIos(ua) && <p className="muted tiny" style={{ margin: '8px 0 0' }}>{t.iosHint}</p>}
        </div>
      ) : null
    ) : googleClientId && !googleBroken ? (
      <>
        <GoogleButton clientId={googleClientId} onCredential={google} onUnavailable={() => setGoogleBroken(true)} />
        <div className="divider">
          <span>{t.or}</span>
        </div>
      </>
    ) : googleClientId && googleBroken ? (
      <p className="muted tiny" style={{ margin: 0 }} data-testid="google-failed">{t.googleFailed}</p>
    ) : null;

  const emailField = (
    <div className="small field">
      <label htmlFor="auth-email">{t.email}</label>
      <input
        id="auth-email"
        className="input"
        type="email"
        placeholder={t.emailPlaceholder}
        autoComplete="email"
        inputMode="email"
        autoCapitalize="none"
        autoCorrect="off"
        spellCheck={false}
        enterKeyHint={mode === 'code' ? 'send' : 'next'}
        required
        value={formEmail}
        onChange={(e) => setFormEmail(e.target.value)}
      />
    </div>
  );

  if (mode === 'code-verify') {
    return (
      <form className="card stack" onSubmit={submit} data-testid="code-verify">
        <h3>{t.codeTitle}</h3>
        <p className="muted small" style={{ margin: 0 }}>
          {t.codeSent(formEmail)}
        </p>
        {codeInput}
        {messages}
        <button type="submit" className="btn btn-primary block" disabled={busy || code.replace(/\D/g, '').length !== 6}>
          {busy ? t.wait : t.signIn}
        </button>
        <div className="spread">
          <button type="button" className="btn-link small" style={{ whiteSpace: 'nowrap' }} onClick={() => go('code')}>
            {t.changeEmail}
          </button>
          {resendButton}
        </div>
      </form>
    );
  }

  if (mode === 'code') {
    const register = initialMode === 'register';
    return (
      <form className="card stack" onSubmit={submit} data-testid="auth-form">
        <div>
          <h2 style={{ margin: 0 }}>{register ? t.registerTitle : t.loginTitle}</h2>
          <p className="muted small" style={{ margin: '6px 0 0' }}>{t.codeHint}</p>
        </div>
        {register && (
          <ul className="small" style={{ margin: 0, paddingLeft: 0, listStyle: 'none', display: 'grid', gap: 4 }} data-testid="auth-benefits">
            {t.benefits.map((b) => <li key={b}>{b}</li>)}
          </ul>
        )}
        {googleBlock}
        {emailField}
        {messages}
        <button type="submit" className="btn btn-primary block" disabled={busy} data-testid="send-code">
          {busy ? t.wait : t.sendLoginCode}
        </button>
        <p className="muted tiny" style={{ margin: 0 }}>
          {t.consent} <a href={`/privacy.html#${lang}`}>{t.privacyLink}</a>
          {t.consentEnd === '.' ? '.' : ` ${t.consentEnd}`}
        </p>
        <p className="small" style={{ margin: 0, paddingTop: 12, borderTop: '1px solid var(--border)', textAlign: 'center' }}>
          <button type="button" className="btn-link" onClick={() => go(initialMode)} data-testid="with-password">
            {t.withPassword}
          </button>
        </p>
      </form>
    );
  }

  if (mode === 'verify' || mode === 'reset') {
    const verify = mode === 'verify';
    return (
      <form className="card stack" onSubmit={submit}>
        <h3>{verify ? t.verifyTitle : t.resetTitle}</h3>
        <p className="muted small" style={{ margin: 0 }}>
          {verify ? t.verifyText(formEmail) : t.resetText(formEmail)}
        </p>
        {codeInput}
        {verify && password.length < 8 && (
          <input
            className="input"
            type="password"
            placeholder={t.newPassword}
            aria-label={t.newPassword}
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        )}
        {!verify && (
          <input
            className="input"
            type="password"
            placeholder={t.newPasswordLabel}
            aria-label={t.newPasswordLabel}
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        )}
        {messages}
        <button type="submit" className="btn btn-primary block" disabled={busy || code.replace(/\D/g, '').length !== 6}>
          {busy ? t.wait : verify ? t.confirm : t.savePassword}
        </button>
        <div className="spread">
          <button type="button" className="btn-link small" style={{ whiteSpace: 'nowrap' }} onClick={() => go(verify ? cameFrom : 'login')}>
            {verify ? t.changeEmail : t.back}
          </button>
          {resendButton}
        </div>
      </form>
    );
  }

  if (mode === 'forgot') {
    return (
      <form className="card stack" onSubmit={submit}>
        <h3>{t.forgotTitle}</h3>
        <p className="muted small" style={{ margin: 0 }}>
          {t.forgotText}
        </p>
        <input
          className="input"
          type="email"
          placeholder={t.email}
          aria-label={t.email}
          autoComplete="email"
          required
          value={formEmail}
          onChange={(e) => setFormEmail(e.target.value)}
        />
        {messages}
        <button type="submit" className="btn btn-primary block" disabled={busy}>
          {busy ? t.wait : t.sendCode}
        </button>
        <button type="button" className="btn-link small" onClick={() => go('login')}>
          {t.back}
        </button>
      </form>
    );
  }

  const register = mode === 'register';
  return (
    <form className="card stack" onSubmit={submit} data-testid="auth-form">
      <div>
        <h2 style={{ margin: 0 }}>{register ? t.registerTitle : t.loginTitle}</h2>
        <p className="muted small" style={{ margin: '6px 0 0' }}>{register ? t.registerHint : t.loginHint}</p>
      </div>
      {register && (
        <ul className="small" style={{ margin: 0, paddingLeft: 0, listStyle: 'none', display: 'grid', gap: 4 }} data-testid="auth-benefits">
          {t.benefits.map((b) => <li key={b}>{b}</li>)}
        </ul>
      )}
      {/* Ilova ichidagi brauzerda (Telegram, Instagram…) Google kirish oynasi ishlamaydi (Google o'zi bloklaydi). */}
      {googleBlock}
      {emailField}
      <div className="small field">
        <label htmlFor="auth-password">{t.password}</label>
        <span className="password-field">
          <input
            id="auth-password"
            className="input"
            type={showPassword ? 'text' : 'password'}
            autoComplete={register ? 'new-password' : 'current-password'}
            aria-describedby={register ? 'auth-password-hint' : undefined}
            required
            minLength={register ? 8 : undefined}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <button
            type="button"
            className="password-toggle"
            aria-label={showPassword ? t.hidePassword : t.showPassword}
            aria-pressed={showPassword}
            onClick={() => setShowPassword((v) => !v)}
          >
            <span aria-hidden="true">{showPassword ? '🙈' : '👁'}</span>
          </button>
        </span>
        {register && <span id="auth-password-hint" className="muted tiny">{t.passwordHint}</span>}
      </div>
      {messages}
      <button type="submit" className="btn btn-primary block" disabled={busy}>
        {busy ? t.wait : register ? t.createAccount : t.login}
      </button>
      {register && canReset && <p className="muted tiny" style={{ margin: 0 }}>{t.nextStep}</p>}
      {register && (
        <p className="muted tiny" style={{ margin: 0 }}>
          {t.consent} <a href={`/privacy.html#${lang}`}>{t.privacyLink}</a>
          {t.consentEnd === '.' ? '.' : ` ${t.consentEnd}`}
        </p>
      )}
      {!register && canReset && (
        <button type="button" className="btn-link small" style={{ alignSelf: 'flex-start' }} onClick={() => go('forgot')}>
          {t.forgot}
        </button>
      )}
      <p className="small" style={{ margin: 0, paddingTop: 12, borderTop: '1px solid var(--border)', textAlign: 'center' }}>
        {register ? t.haveAccount : t.noAccount}{' '}
        <button type="button" className="btn-link" onClick={() => go(register ? 'login' : 'register')} data-testid="auth-switch">
          {register ? t.toLogin : t.toRegister}
        </button>
      </p>
      {canReset && (
        <button type="button" className="btn-link small" style={{ alignSelf: 'center' }} onClick={() => go('code')} data-testid="with-code">
          {t.withCode}
        </button>
      )}
    </form>
  );
}
