import { useState, type FormEvent } from 'react';
import { apiFetch, postJson, setToken } from './api';

interface AuthResponse {
  token: string;
  email: string;
}

/**
 * Sahifa tepasidagi kirish/ro'yxatdan o'tish paneli. Kirmagan bo'lsa ham
 * barcha mashqlar ishlaydi — faqat natijalar tarixga saqlanmaydi. Shu
 * sababli panel yig'ilgan holatda boshlanadi va foydalanuvchini to'smaydi.
 */
export function AuthPanel({
  email,
  onChange,
}: {
  email: string | null;
  onChange: (email: string | null) => void;
}) {
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<'login' | 'register'>('login');
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
      setOpen(false);
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

  const box = { padding: '0.75rem 1rem', border: '1px solid #e5e7eb', borderRadius: '0.5rem', marginTop: '1rem' };

  if (email) {
    return (
      <div style={{ ...box, display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '0.5rem' }}>
        <span style={{ fontSize: '0.9rem', overflowWrap: 'anywhere' }}>
          👤 <strong>{email}</strong>
        </span>
        <button onClick={logout} style={linkButton}>
          Chiqish
        </button>
      </div>
    );
  }

  if (!open) {
    return (
      <div style={{ ...box, display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '0.5rem' }}>
        <span style={{ fontSize: '0.85rem', color: '#6b7280' }}>Natijalaringiz saqlanishi uchun tizimga kiring.</span>
        <button onClick={() => setOpen(true)} style={linkButton}>
          Kirish
        </button>
      </div>
    );
  }

  const input = {
    width: '100%',
    padding: '0.5rem',
    fontSize: '0.95rem',
    border: '1px solid #d1d5db',
    borderRadius: '0.375rem',
    boxSizing: 'border-box' as const,
    marginBottom: '0.5rem',
  };

  return (
    <form onSubmit={submit} style={box}>
      <div style={{ display: 'flex', gap: '1rem', marginBottom: '0.75rem' }}>
        <button type="button" onClick={() => setMode('login')} style={{ ...linkButton, fontWeight: mode === 'login' ? 700 : 400 }}>
          Kirish
        </button>
        <button type="button" onClick={() => setMode('register')} style={{ ...linkButton, fontWeight: mode === 'register' ? 700 : 400 }}>
          Ro'yxatdan o'tish
        </button>
      </div>
      <input
        type="email"
        placeholder="Email"
        autoComplete="email"
        required
        value={formEmail}
        onChange={(e) => setFormEmail(e.target.value)}
        style={input}
      />
      <input
        type="password"
        placeholder={mode === 'register' ? 'Parol (kamida 8 belgi)' : 'Parol'}
        autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
        required
        minLength={mode === 'register' ? 8 : undefined}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        style={input}
      />
      {error && <p style={{ color: '#dc2626', fontSize: '0.85rem', margin: '0 0 0.5rem' }}>{error}</p>}
      <div style={{ display: 'flex', gap: '0.5rem' }}>
        <button
          type="submit"
          disabled={busy}
          style={{
            padding: '0.5rem 1rem',
            backgroundColor: '#2563eb',
            color: 'white',
            border: 'none',
            borderRadius: '0.375rem',
            cursor: busy ? 'not-allowed' : 'pointer',
          }}
        >
          {busy ? 'Kuting...' : mode === 'login' ? 'Kirish' : "Ro'yxatdan o'tish"}
        </button>
        <button type="button" onClick={() => setOpen(false)} style={linkButton}>
          Bekor qilish
        </button>
      </div>
    </form>
  );
}

const linkButton = {
  background: 'none',
  border: 'none',
  color: '#2563eb',
  cursor: 'pointer',
  fontSize: '0.9rem',
  padding: 0,
};

/** Kirmagan foydalanuvchiga tarix nega bo'sh ekanini tushuntiradi. */
export function HistoryHint({ loggedIn }: { loggedIn: boolean }) {
  if (loggedIn) return null;
  return (
    <p style={{ marginTop: '2rem', color: '#6b7280', fontSize: '0.85rem' }}>
      Siz mehmon sifatida ishlayapsiz — natijalar baholanadi, lekin tarixga saqlanmaydi. Saqlash uchun yuqorida
      tizimga kiring.
    </p>
  );
}
