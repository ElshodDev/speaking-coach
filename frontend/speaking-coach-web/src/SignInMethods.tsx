// Profil: qaysi usullar bilan kirish mumkin + yetishmaganini qo'shish.
// Telegram orqali ochilgan hisobda email ichki (tg-…@telegram.invalid) —
// haqiqiy email qo'shilgach, parol ham o'rnatish mumkin bo'ladi.
import { useState, type FormEvent } from 'react';
import { postJson, type SignInMethods as Methods } from './api';
import { common, useT } from './i18n';
import { signInMsg } from './locales/signin';

type Open = null | 'email' | 'password';

export function SignInMethods({
  methods,
  onEmailChanged,
  onPasswordSaved,
}: {
  methods: Methods;
  /** Email tasdiqlangach — App yangi emailni eslab qoladi va profilni qayta yuklaydi. */
  onEmailChanged: (email: string) => void;
  onPasswordSaved: () => void;
}) {
  const t = useT(signInMsg);
  const cm = useT(common);
  const [open, setOpen] = useState<Open>(null);
  const [email, setEmail] = useState('');
  const [codeSentTo, setCodeSentTo] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');

  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError('');
    setInfo('');
    try {
      await action();
    } catch (e) {
      setError(e instanceof Error ? e.message : cm.error);
    } finally {
      setBusy(false);
    }
  }

  function close() {
    setOpen(null);
    setCodeSentTo(null);
    setCode('');
    setCurrent('');
    setNext('');
    setError('');
  }

  function submitEmail(e: FormEvent) {
    e.preventDefault();
    run(async () => {
      if (!codeSentTo) {
        const r = await postJson<{ email: string }>('/api/account/email/request', { email });
        setCodeSentTo(r.email ?? email);
        return;
      }
      const r = await postJson<{ email: string }>('/api/account/email/confirm', { email: codeSentTo, code });
      close();
      setInfo(t.emailSaved);
      onEmailChanged(r.email ?? codeSentTo);
    });
  }

  function submitPassword(e: FormEvent) {
    e.preventDefault();
    run(async () => {
      await postJson('/api/account/password', methods.password ? { currentPassword: current, newPassword: next } : { newPassword: next });
      close();
      setInfo(t.passwordSaved);
      onPasswordSaved();
    });
  }

  const rows: { key: string; label: string; on: boolean }[] = [
    { key: 'telegram', label: t.telegram, on: methods.telegram },
    { key: 'google', label: t.google, on: methods.google },
    { key: 'password', label: t.password, on: methods.realEmail && methods.password },
  ];

  return (
    <div className="card stack" data-testid="signin-methods">
      <div>
        <h3 style={{ margin: 0 }}>{t.title}</h3>
        <p className="muted small" style={{ margin: '4px 0 0' }}>{t.text}</p>
      </div>
      <ul className="signin-list">
        {rows.map((r) => (
          <li key={r.key} data-on={r.on}>
            <span>{r.label}</span>
            <span className={r.on ? 'badge success' : 'muted small'}>{r.on ? `✓ ${t.on}` : t.off}</span>
          </li>
        ))}
      </ul>
      {!methods.telegram && <p className="muted tiny" style={{ margin: 0 }}>{t.telegramHint}</p>}

      {open === null && (
        <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
          {!methods.realEmail && (
            <button className="btn btn-outline btn-sm" onClick={() => setOpen('email')} data-testid="add-email">
              {t.addEmail}
            </button>
          )}
          {methods.realEmail && (
            <button className="btn btn-outline btn-sm" onClick={() => setOpen('password')} data-testid="set-password">
              {methods.password ? t.changePassword : t.setPassword}
            </button>
          )}
        </div>
      )}

      {open === 'email' && (
        <form className="stack" onSubmit={submitEmail} data-testid="add-email-form">
          <p className="muted small" style={{ margin: 0 }}>{codeSentTo ? t.codeSent(codeSentTo) : t.addEmailText}</p>
          {!codeSentTo ? (
            <input
              className="input"
              type="email"
              aria-label={t.email}
              placeholder={t.email}
              autoComplete="email"
              autoCapitalize="none"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          ) : (
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
          )}
          <div className="row" style={{ gap: 8 }}>
            <button type="submit" className="btn btn-primary" disabled={busy}>
              {busy ? t.wait : codeSentTo ? t.confirm : t.sendCode}
            </button>
            <button type="button" className="btn-link small" onClick={close}>
              {t.cancel}
            </button>
          </div>
        </form>
      )}

      {open === 'password' && (
        <form className="stack" onSubmit={submitPassword} data-testid="password-form">
          {methods.password && (
            <input
              className="input"
              type="password"
              aria-label={t.currentPassword}
              placeholder={t.currentPassword}
              autoComplete="current-password"
              required
              value={current}
              onChange={(e) => setCurrent(e.target.value)}
            />
          )}
          <input
            className="input"
            type="password"
            aria-label={t.newPassword}
            placeholder={t.newPassword}
            autoComplete="new-password"
            minLength={8}
            required
            value={next}
            onChange={(e) => setNext(e.target.value)}
          />
          <div className="row" style={{ gap: 8 }}>
            <button type="submit" className="btn btn-primary" disabled={busy}>
              {busy ? t.wait : t.save}
            </button>
            <button type="button" className="btn-link small" onClick={close}>
              {t.cancel}
            </button>
          </div>
        </form>
      )}

      {error && <p className="error small" role="alert" style={{ margin: 0 }}>{error}</p>}
      {info && <p className="success small" style={{ margin: 0 }}>{info}</p>}
    </div>
  );
}
