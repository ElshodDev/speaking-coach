import { useState, type FormEvent } from 'react';
import { LEVELS, apiJson, getLevel, setLevel, type Profile } from './api';

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

  function pickLevel(id: string) {
    setLevelState(id);
    setLevel(id); // darhol keyingi mashqqa ta'sir qiladi
    if (!profile) {
      setIsError(false);
      setMsg(`Daraja: ${id} ✅`);
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
      setMsg('Saqlandi ✅');
      onSaved({ ...profile, ...saved });
    } catch (err) {
      setIsError(true);
      setMsg(err instanceof Error ? err.message : 'Xato yuz berdi');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card stack" onSubmit={save}>
      <h3>Sozlamalar</h3>

      <div>
        <div className="small" style={{ fontWeight: 600, marginBottom: 6 }}>
          Ingliz tili darajangiz
        </div>
        <div className="segmented" role="group" aria-label="Daraja">
          {LEVELS.map((l) => (
            <button key={l.id} type="button" aria-pressed={level === l.id} onClick={() => pickLevel(l.id)} title={l.hint}>
              {l.label}
            </button>
          ))}
        </div>
        <p className="muted tiny" style={{ marginTop: 6, marginBottom: 0 }}>
          {LEVELS.find((l) => l.id === level)?.hint}. Matnlar uzunligi, savollar va izohlar shunga moslashadi.
        </p>
      </div>

      {profile && (
        <>
          <div>
            <label className="small" style={{ fontWeight: 600 }} htmlFor="nick">
              Taxallus (musobaqada ko'rinadi)
            </label>
            <input
              id="nick"
              className="input"
              style={{ marginTop: 6 }}
              maxLength={30}
              placeholder="masalan: Aziza_B2"
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          </div>

          <label className="switch small">
            <input type="checkbox" checked={optIn} onChange={(e) => setOptIn(e.target.checked)} />
            Haftalik musobaqada qatnashaman
          </label>
          <p className="muted tiny" style={{ margin: 0 }}>
            Reytingda faqat taxallus va haftalik XP ko'rinadi — email va mashqlaringiz hech kimga ko'rsatilmaydi.
          </p>

          <button type="submit" className="btn btn-primary" disabled={busy}>
            Saqlash
          </button>
        </>
      )}
      {msg && <p className={`small ${isError ? 'error' : 'success'}`} style={{ margin: 0 }}>{msg}</p>}
    </form>
  );
}
