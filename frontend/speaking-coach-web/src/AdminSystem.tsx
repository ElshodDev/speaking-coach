import { useCallback, useEffect, useState } from 'react';
import { apiJson } from './api';
import { localeOf, msg, useLang, useT } from './i18n';
import { adminSystemMsg, type SystemIssue } from './locales/adminSystem';

export interface SystemStatus {
  database: { checked: boolean; pendingMigrations: string[]; missingTables: string[]; error: string | null; ok: boolean };
  telegram: {
    enabled: boolean;
    botUsername: string;
    actualUsername: string | null;
    expectedWebhookUrl: string | null;
    webhookUrl: string | null;
    pendingUpdates: number | null;
    lastErrorAtUtc: string | null;
    lastError: string | null;
    cronConfigured: boolean;
    linkedAccounts: number;
    remindersOn: number;
    apiError: string | null;
    issues: SystemIssue[];
  };
  features: { emailVerification: boolean; googleSignIn: boolean; gemini: boolean };
}

/** Umumiy holat: baza yoki Telegram'da muammo bo'lsa — "ogohlantirish". */
export function overallOk(s: SystemStatus): boolean {
  return s.database.ok && s.telegram.issues.length === 0;
}

/** 🟢 ishlayapti · 🟠 muammo · ⚪ ixtiyoriy va o'chiq (muammo emas). */
function Dot({ ok, optional = false }: { ok: boolean; optional?: boolean }) {
  return <span aria-hidden="true">{ok ? '🟢' : optional ? '⚪' : '🟠'}</span>;
}

/**
 * Admin paneldagi "Tizim holati": baza sxemasi (migratsiyalar), Telegram bot
 * (webhook, kutilayotgan xabarlar, oxirgi xato) va asosiy sozlamalar.
 * Server hech qanday sir qaytarmaydi — faqat holat.
 */
export function AdminSystem() {
  const [data, setData] = useState<SystemStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const t = useT(adminSystemMsg);
  const locale = localeOf(useLang().lang);

  const load = useCallback(() => {
    setLoading(true);
    setError('');
    apiJson<SystemStatus>('/api/admin/system')
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : msg(adminSystemMsg).loadFailed))
      .finally(() => setLoading(false));
  }, []);

  useEffect(load, [load]);

  const db = data?.database;
  const tg = data?.telegram;

  return (
    <div className="card stack" data-testid="admin-system">
      <div className="row" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <h3 style={{ margin: 0 }}>
          {t.title} {data && <Dot ok={overallOk(data)} />}
        </h3>
        <button className="btn-link small" onClick={load} disabled={loading}>
          {loading ? t.checking : t.refresh}
        </button>
      </div>

      {error && <p className="error small" role="alert">{error}</p>}
      {data && overallOk(data) && <p className="success small" style={{ margin: 0 }}>✅ {t.allGood}</p>}

      {db && tg && data && (
        <div className="table-wrap">
          <table className="data">
            <tbody>
              <tr>
                <td><Dot ok={db.ok} /> {t.database}</td>
                <td>
                  {db.ok && t.dbOk}
                  {!db.checked && (
                    <>
                      {t.dbUnchecked}
                      {db.error && <div className="muted tiny">{db.error}</div>}
                    </>
                  )}
                  {db.pendingMigrations.length > 0 && <div>{t.pending(db.pendingMigrations.join(', '))}</div>}
                  {db.missingTables.length > 0 && <div>{t.missing(db.missingTables.join(', '))}</div>}
                  {db.checked && !db.ok && <div className="muted tiny"><code>{t.dbFix}</code></div>}
                </td>
              </tr>
              <tr>
                <td><Dot ok={tg.enabled && !tg.apiError} /> {t.telegram}</td>
                <td>
                  {tg.enabled ? `@${tg.actualUsername ?? tg.botUsername}` : t.off}
                  {tg.apiError && <div className="muted tiny">{tg.apiError}</div>}
                  {tg.enabled && <div className="muted tiny">{t.linked(tg.linkedAccounts, tg.remindersOn)}</div>}
                </td>
              </tr>
              {tg.enabled && !tg.apiError && (
                <tr>
                  <td>
                    <Dot ok={!tg.issues.some((i) => i.startsWith('webhook_'))} /> {t.webhook}
                  </td>
                  <td>
                    <span style={{ overflowWrap: 'anywhere' }}>{tg.webhookUrl || '—'}</span>
                    <div className="muted tiny">{t.pendingUpdates(tg.pendingUpdates ?? 0)}</div>
                    {tg.lastError && tg.lastErrorAtUtc && (
                      <div className="muted tiny">{t.lastError(new Date(tg.lastErrorAtUtc).toLocaleString(locale), tg.lastError)}</div>
                    )}
                  </td>
                </tr>
              )}
              <tr>
                <td><Dot ok={tg.cronConfigured} /> {t.reminders}</td>
                <td>{tg.cronConfigured ? t.on : t.off}</td>
              </tr>
              <tr>
                <td><Dot ok={data.features.emailVerification} optional /> {t.emailVerification}</td>
                <td>{data.features.emailVerification ? t.on : t.off}</td>
              </tr>
              <tr>
                <td><Dot ok={data.features.googleSignIn} optional /> {t.google}</td>
                <td>{data.features.googleSignIn ? t.on : t.off}</td>
              </tr>
              <tr>
                <td><Dot ok={data.features.gemini} /> {t.gemini}</td>
                <td>{data.features.gemini ? t.on : t.off}</td>
              </tr>
            </tbody>
          </table>
        </div>
      )}

      {tg && tg.issues.length > 0 && (
        <ul className="small" style={{ margin: 0, paddingLeft: 20 }}>
          {tg.issues.map((i) => (
            <li key={i}>{t.issues[i] ?? i}</li>
          ))}
        </ul>
      )}
    </div>
  );
}
