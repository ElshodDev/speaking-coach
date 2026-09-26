import { useEffect, useState, type FormEvent } from 'react';
import { apiFetch, apiJson, postJson, setToken } from './api';
import { common, useLang, useT } from './i18n';
import { authMsg } from './locales/auth';
import { GoogleButton } from './GoogleButton';

/** Server javobi: yo token (kirildi), yo "kodni kiriting", yo shunchaki ok. */
interface AuthResponse {
  token?: string;
  email?: string;
  needsVerification?: boolean;
  ok?: boolean;
}

type Mode = 'register' | 'login' | 'verify' | 'forgot' | 'reset';

const RESEND_SECONDS = 60;

/**
 * Kirish / ro'yxatdan o'tish formasi yoki (kirgan bo'lsa) hisob kartasi.
 *
 * Email tasdiqlash yoqilgan bo'lsa (serverda xat yuborish sozlangan):
 * ro'yxatdan o'tish → emailga 6 xonali kod → kodni kiritish → hisob ochiladi.
 * Shunday qilib bazada faqat haqiqatan mavjud va egasi qo'lidagi emaillar
 * qoladi. "Parolni unutdim" ham xuddi shu kod orqali ishlaydi.
 */
export function AuthPanel({ email, onChange }: { email: string | null; onChange: (email: string | null) => void }) {
  const [mode, setMode] = useState<Mode>('register');
  const [formEmail, setFormEmail] = useState('');
  const [password, setPassword] = useState('');
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');
  const [busy, setBusy] = useState(false);
  const [resendIn, setResendIn] = useState(0);
  const [canReset, setCanReset] = useState(false);
  const [googleClientId, setGoogleClientId] = useState<string | null>(null);
  const t = useT(authMsg);
  const cm = useT(common);
  const { lang } = useLang();

  // Xat yuborish sozlanmagan bo'lsa, "Parolni unutdim" ma'nosiz — ko'rsatmaymiz.
  useEffect(() => {
    apiJson<{ emailVerification: boolean; googleClientId?: string | null }>('/api/auth/config')
      .then((c) => {
        setCanReset(c.emailVerification);
        setGoogleClientId(c.googleClientId ?? null);
      })
      .catch(() => setCanReset(false));
  }, []);

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

  function submit(e: FormEvent) {
    e.preventDefault();
    run(async () => {
      if (mode === 'register' || mode === 'login') {
        const data = await postJson<AuthResponse>(`/api/auth/${mode}`, { email: formEmail, password });
        if (data.needsVerification) {
          if (data.email) setFormEmail(data.email);
          go('verify');
          setResendIn(RESEND_SECONDS);
          return;
        }
        finish(data);
      } else if (mode === 'verify') {
        finish(await postJson<AuthResponse>('/api/auth/verify', { email: formEmail, code }));
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
      await postJson(mode === 'reset' ? '/api/auth/forgot' : '/api/auth/resend', { email: formEmail });
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
      onChange={(e) => setCode(e.target.value.replace(/[^\d ]/g, ''))}
    />
  );

  const resendButton = (
    <button type="button" className="btn-link small" onClick={resend} disabled={busy || resendIn > 0}>
      {resendIn > 0 ? t.resendIn(resendIn) : t.resend}
    </button>
  );

  if (mode === 'verify' || mode === 'reset') {
    const verify = mode === 'verify';
    return (
      <form className="card stack" onSubmit={submit}>
        <h3>{verify ? t.verifyTitle : t.resetTitle}</h3>
        <p className="muted small" style={{ margin: 0 }}>
          {verify ? t.verifyText(formEmail) : t.resetText(formEmail)}
        </p>
        {codeInput}
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
          <button type="button" className="btn-link small" onClick={() => go('login')}>
            {t.back}
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

  return (
    <form className="card stack" onSubmit={submit}>
      <div className="segmented" role="group" aria-label={t.modeGroup}>
        <button type="button" aria-pressed={mode === 'register'} onClick={() => go('register')}>
          {t.register}
        </button>
        <button type="button" aria-pressed={mode === 'login'} onClick={() => go('login')}>
          {t.login}
        </button>
      </div>
      <p className="muted small">{mode === 'register' ? t.registerHint : t.loginHint}</p>
      {googleClientId && (
        <>
          <GoogleButton clientId={googleClientId} onCredential={google} />
          <div className="divider">
            <span>{t.or}</span>
          </div>
        </>
      )}
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
      <input
        className="input"
        type="password"
        placeholder={mode === 'register' ? t.newPassword : t.password}
        aria-label={mode === 'register' ? t.newPassword : t.password}
        autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
        required
        minLength={mode === 'register' ? 8 : undefined}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      {messages}
      <button type="submit" className="btn btn-primary block" disabled={busy}>
        {busy ? t.wait : mode === 'login' ? t.login : t.createAccount}
      </button>
      {mode === 'register' && (
        <p className="muted tiny" style={{ margin: 0 }}>
          {t.consent} <a href={`/privacy.html#${lang}`}>{t.privacyLink}</a>
          {t.consentEnd === '.' ? '.' : ` ${t.consentEnd}`}
        </p>
      )}
      {mode === 'login' && canReset && (
        <button type="button" className="btn-link small" onClick={() => go('forgot')}>
          {t.forgot}
        </button>
      )}
    </form>
  );
}
