import { useState, type FormEvent } from 'react';
import { apiFetch, postJson, setToken } from './api';

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
      setError(err instanceof Error ? err.message : 'Xato yuz berdi');
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
          <div className="muted tiny">Hisob</div>
          <strong>{email}</strong>
        </div>
        <button className="btn btn-outline" onClick={logout}>
          Chiqish
        </button>
      </div>
    );
  }

  return (
    <form className="card stack" onSubmit={submit}>
      <div className="segmented" role="group" aria-label="Kirish turi">
        <button type="button" aria-pressed={mode === 'register'} onClick={() => setMode('register')}>
          Ro'yxatdan o'tish
        </button>
        <button type="button" aria-pressed={mode === 'login'} onClick={() => setMode('login')}>
          Kirish
        </button>
      </div>
      <p className="muted small">
        {mode === 'register'
          ? 'Hisob ochsangiz, natijalaringiz saqlanadi va xatolaringiz takrorlash kartalariga aylanadi.'
          : 'Email va parolingiz bilan kiring.'}
      </p>
      <input
        className="input"
        type="email"
        placeholder="Email"
        autoComplete="email"
        required
        value={formEmail}
        onChange={(e) => setFormEmail(e.target.value)}
      />
      <input
        className="input"
        type="password"
        placeholder={mode === 'register' ? 'Parol (kamida 8 belgi)' : 'Parol'}
        autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
        required
        minLength={mode === 'register' ? 8 : undefined}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      {error && <p className="error small">{error}</p>}
      <button type="submit" className="btn btn-primary block" disabled={busy}>
        {busy ? 'Kuting...' : mode === 'login' ? 'Kirish' : "Hisob ochish"}
      </button>
    </form>
  );
}
