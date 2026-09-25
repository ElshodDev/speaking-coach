import { useState, type FormEvent } from 'react';
import { LEVELS, apiJson, getLevel, setLevel, type Profile } from './api';
import { LangSelect, common, useT } from './i18n';
import { settingsMsg } from './locales/settings';

/**
 * Daraja tanlash — mehmon uchun ham (brauzerda saqlanadi). Kirgan
 * foydalanuvchida esa taxallus va musobaqada qatnashish ham bor, hammasi
 * serverdagi profilga yoziladi.
 */
export function Settings({ profile, onSaved }: { profile: Profile | null; onSaved: (p: Profile) => void }) {
  const [level, setLevelState] = useState(profile?.level ?? getLevel());
  const [name, setName] = useState(profile?.displayName ?? '');
  const [optIn, setOptIn] = useState(profile?.showOnLeaderboard ?? false);
  const [msg, setMsg] = useState('');
  const [isError, setIsError] = useState(false);
  const [busy, setBusy] = useState(false);
  const t = useT(settingsMsg);
  const cm = useT(common);
  const levelHint = (id: string) => t.levelHints[id as keyof typeof t.levelHints];

  function pickLevel(id: string) {
    setLevelState(id);
    setLevel(id); // darhol keyingi mashqqa ta'sir qiladi
    if (!profile) {
      setIsError(false);
      setMsg(t.levelPicked(id));
    }
  }

  async function save(e: FormEvent) {
    e.preventDefault();
    if (!profile) return;
    setBusy(true);
    setMsg('');
    try {
      const saved = await apiJson<{ displayName: string | null; level: string; showOnLeaderboard: boolean }>('/api/profile', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName: name.trim() || null, level, showOnLeaderboard: optIn }),
      });
      setIsError(false);
      setMsg(t.saved);
      onSaved({ ...profile, ...saved });
    } catch (err) {
      setIsError(true);
      setMsg(err instanceof Error ? err.message : cm.error);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card stack" onSubmit={save}>
      <h3>{t.title}</h3>

      <label style={{ display: 'block' }}>
        <span className="small" style={{ display: 'block', fontWeight: 600, marginBottom: 6 }}>
          {t.interfaceLanguage}
        </span>
        <LangSelect full />
      </label>

      <div>
        <div className="small" style={{ fontWeight: 600, marginBottom: 6 }}>
          {t.levelLabel}
        </div>
        <div className="segmented" role="group" aria-label={t.levelGroup}>
          {LEVELS.map((l) => (
            <button key={l.id} type="button" aria-pressed={level === l.id} onClick={() => pickLevel(l.id)} title={levelHint(l.id)}>
              {l.label}
            </button>
          ))}
        </div>
        <p className="muted tiny" style={{ marginTop: 6, marginBottom: 0 }}>
          {levelHint(level)}. {t.levelNote}
        </p>
      </div>

      {profile && (
        <>
          <div>
            <label className="small" style={{ fontWeight: 600 }} htmlFor="nick">
              {t.nickname}
            </label>
            <input
              id="nick"
              className="input"
              style={{ marginTop: 6 }}
              maxLength={30}
              placeholder={t.nicknamePlaceholder}
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          </div>

          <label className="switch small">
            <input type="checkbox" checked={optIn} onChange={(e) => setOptIn(e.target.checked)} />
            {t.leagueOptIn}
          </label>
          <p className="muted tiny" style={{ margin: 0 }}>
            {t.privacy}
          </p>

          <button type="submit" className="btn btn-primary" disabled={busy}>
            {cm.save}
          </button>
        </>
      )}
      {msg && <p className={`small ${isError ? 'error' : 'success'}`} style={{ margin: 0 }}>{msg}</p>}
    </form>
  );
}
