import { describe, expect, it } from 'vitest';
import { overallOk, type SystemStatus } from './AdminSystem';
import { adminSystemMsg } from './locales/adminSystem';

const base: SystemStatus = {
  database: { checked: true, pendingMigrations: [], missingTables: [], error: null, ok: true },
  telegram: {
    enabled: true,
    botUsername: 'SpeakingCoachUzBot',
    actualUsername: 'SpeakingCoachUzBot',
    expectedWebhookUrl: 'https://x/api/telegram/webhook',
    webhookUrl: 'https://x/api/telegram/webhook',
    pendingUpdates: 0,
    lastErrorAtUtc: null,
    lastError: null,
    cronConfigured: true,
    linkedAccounts: 1,
    remindersOn: 1,
    apiError: null,
    issues: [],
  },
  features: { emailVerification: false, googleSignIn: true, gemini: true },
};

describe('overallOk', () => {
  it('is true only when the schema is fine and there are no Telegram issues', () => {
    expect(overallOk(base)).toBe(true);
    expect(overallOk({ ...base, database: { ...base.database, ok: false, missingTables: ['TelegramAccounts'] } })).toBe(false);
    expect(overallOk({ ...base, telegram: { ...base.telegram, issues: ['cron_missing'] } })).toBe(false);
  });

  it('ignores optional features being off', () => {
    expect(overallOk({ ...base, features: { emailVerification: false, googleSignIn: false, gemini: true } })).toBe(true);
  });
});

describe('adminSystemMsg', () => {
  it('has a message for every issue key in every language', () => {
    const keys = Object.keys(adminSystemMsg.uz.issues);
    expect(keys).toHaveLength(9);
    for (const lang of ['ru', 'en'] as const) {
      expect(Object.keys(adminSystemMsg[lang].issues).sort()).toEqual([...keys].sort());
    }
  });
});
