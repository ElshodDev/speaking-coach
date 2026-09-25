import { useState, type FormEvent } from 'react';
import { apiFetch, postJson, setToken } from './api';
import { common, useT } from './i18n';
import { authMsg } from './locales/auth';

interface AuthResponse {
  token: string;
  email: string;
}

/**
 * Kirish / ro'yxatdan o'tish formasi yoki (kirgan bo'lsa) hisob kartasi.
 * Kirmasdan ham barcha mashqlar ishlaydi — faqat natijalar saqlanmaydi.
 */
export function AuthPanel({ email, onChange }: { email: string | null; onChange: (email: string | null) => void }) {
  const [mode, setMode] = useState<'login' | 'register'>('register');
  const [formEmail, setFormEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const t = useT(authMsg);
  const cm = useT(common);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError('');
    try {
      const data = await postJson<AuthResponse>(`/api/auth/${mode}`, { email: formEmail, password });
      setToken(data.token);
      setPassword('');
      onChange(data.email);
    } catch (err) {
      setError(err instanceof Error ? err.message : cm.error);
    } finally {
      setBusy(false);
    }
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

  return (
    <form className="card stack" onSubmit={submit}>
      <div className="segmented" role="group" aria-label={t.modeGroup}>
        <button type="button" aria-pressed={mode === 'register'} onClick={() => setMode('register')}>
          {t.register}
        </button>
        <button type="button" aria-pressed={mode === 'login'} onClick={() => setMode('login')}>
          {t.login}
        </button>
      </div>
      <p className="muted small">
        {mode === 'register'
          ? t.registerHint
          : t.loginHint}
      </p>
      <input
        className="input"
        type="email"
        placeholder={t.email}
        autoComplete="email"
        required
        value={formEmail}
        onChange={(e) => setFormEmail(e.target.value)}
      />
      <input
        className="input"
        type="password"
        placeholder={mode === 'register' ? t.newPassword : t.password}
        autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
        required
        minLength={mode === 'register' ? 8 : undefined}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      {error && <p className="error small">{error}</p>}
      <button type="submit" className="btn btn-primary block" disabled={busy}>
        {busy ? t.wait : mode === 'login' ? t.login : t.createAccount}
      </button>
    </form>
  );
}
