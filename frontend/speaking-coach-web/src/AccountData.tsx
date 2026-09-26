import { useState } from 'react';
import { apiFetch, apiJson, setToken } from './api';
import { common, useT } from './i18n';
import { accountMsg } from './locales/account';

/**
 * Profil sahifasidagi "Maʼlumotlaringiz" kartasi: hamma narsani JSON qilib
 * yuklab olish va hisobni o'chirish (email qayta kiritilgach).
 */
export function AccountData({ email, onDeleted }: { email: string; onDeleted: () => void }) {
  const t = useT(accountMsg);
  const cm = useT(common);
  const [busy, setBusy] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [typed, setTyped] = useState('');
  const [error, setError] = useState('');

  async function download() {
    setBusy(true);
    setError('');
    try {
      const response = await apiFetch('/api/account/export');
      if (!response.ok) throw new Error(cm.serverError(response.status));
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `speaking-coach-${new Date().toISOString().slice(0, 10)}.json`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (e) {
      setError(e instanceof Error ? e.message : cm.error);
    } finally {
      setBusy(false);
    }
  }

  async function remove() {
    setBusy(true);
    setError('');
    try {
      await apiJson('/api/account', {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ confirmEmail: typed }),
      });
      setToken(null);
      alert(t.deleted);
      onDeleted();
    } catch (e) {
      setError(e instanceof Error ? e.message : cm.error);
    } finally {
      setBusy(false);
    }
  }

  const matches = typed.trim().toLowerCase() === email.toLowerCase();

  return (
    <div className="card stack">
      <h3>{t.dataTitle}</h3>
      <p className="muted small" style={{ margin: 0 }}>
        {t.dataText}
      </p>
      <button className="btn btn-outline" onClick={download} disabled={busy}>
        {busy && !confirming ? t.downloading : t.download}
      </button>

      {!confirming ? (
        <button className="btn-link quiet small" style={{ alignSelf: 'flex-start' }} onClick={() => setConfirming(true)}>
          {t.deleteOpen}
        </button>
      ) : (
        <div className="danger-zone stack">
          <p className="small" style={{ margin: 0 }}>
            ⚠️ {t.deleteWarn}
          </p>
          <label className="small" htmlFor="confirm-email">
            {t.deleteConfirmLabel(email)}
          </label>
          <input
            id="confirm-email"
            className="input"
            type="email"
            autoComplete="off"
            value={typed}
            onChange={(e) => setTyped(e.target.value)}
          />
          <div className="row">
            <button className="btn btn-danger" onClick={remove} disabled={busy || !matches}>
              {t.deleteButton}
            </button>
            <button className="btn-link small" onClick={() => { setConfirming(false); setTyped(''); }}>
              {t.cancel}
            </button>
          </div>
        </div>
      )}
      {error && <p className="error small" role="alert">{error}</p>}
    </div>
  );
}
