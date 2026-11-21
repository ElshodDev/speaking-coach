import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { clearOfflineReview, dayKey, enqueueGrade, flushQueue, loadDue, pendingGrades, saveDue, type OfflineCard } from './offlineReview';

// Vitest Node muhitida localStorage yo'q — oddiy nusxa.
const mem = new Map<string, string>();
(globalThis as { localStorage?: unknown }).localStorage = {
  getItem: (k: string) => mem.get(k) ?? null,
  setItem: (k: string, v: string) => void mem.set(k, v),
  removeItem: (k: string) => void mem.delete(k),
};

const card = (id: string): OfflineCard => ({ id, kind: 'Word', source: null, front: `f${id}`, back: `b${id}`, note: '' });
const D1 = new Date(2026, 10, 21, 9, 0);
const D2 = new Date(2026, 10, 22, 9, 0);

beforeEach(() => mem.clear());
afterEach(() => vi.unstubAllEnvs());

describe('offlineReview', () => {
  it('dayKey: mahalliy sana', () => {
    expect(dayKey(D1)).toBe('2026-11-21');
  });

  it('saqlangan bugungi kartalar qaytadi; boshqa hisobga berilmaydi', () => {
    saveDue('a@x.com', [card('1'), card('2')], D1);
    expect(loadDue('a@x.com', D1)?.map((c) => c.id)).toEqual(['1', '2']);
    expect(loadDue('b@x.com', D1)).toBeNull();
  });

  it('ertasi kuni kartalar ishlatilmaydi (faqat bugungi)', () => {
    saveDue('a@x.com', [card('1')], D1);
    expect(loadDue('a@x.com', D2)).toBeNull();
  });

  it('baholangan karta oflayn ro‘yxatdan chiqadi, navbatga tushadi, takror yozilmaydi', () => {
    saveDue('a@x.com', [card('1'), card('2')], D1);
    expect(enqueueGrade('a@x.com', '1', 2, D1)).toBe(true);
    expect(enqueueGrade('a@x.com', '1', 3, D1)).toBe(true);
    expect(loadDue('a@x.com', D1)?.map((c) => c.id)).toEqual(['2']);
    expect(pendingGrades('a@x.com', D1)).toEqual([{ id: '1', grade: 2 }]);
  });

  it('navbat tartib bilan yuboriladi; offline — to‘xtaydi, qolganlari saqlanadi', async () => {
    saveDue('a@x.com', [card('1'), card('2'), card('3')], D1);
    for (const id of ['1', '2', '3']) enqueueGrade('a@x.com', id, 2, D1);
    const seen: string[] = [];
    const r = await flushQueue(
      'a@x.com',
      async (id) => {
        seen.push(id);
        return id === '3' ? 'offline' : 'ok';
      },
      D1,
    );
    expect(seen).toEqual(['1', '2', '3']);
    expect(r).toEqual({ sent: 2, left: 1 });
    expect(pendingGrades('a@x.com', D1).map((q) => q.id)).toEqual(['3']);
  });

  it('server rad etgan baho (karta o‘chgan) navbatdan chiqadi, sanalmaydi', async () => {
    enqueueGrade('a@x.com', '9', 1, D1);
    const r = await flushQueue('a@x.com', async () => 'drop', D1);
    expect(r).toEqual({ sent: 0, left: 0 });
    expect(pendingGrades('a@x.com', D1)).toEqual([]);
  });

  it('kun o‘tsa kartalar o‘chadi, lekin yuborilmagan baholar yo‘qolmaydi', () => {
    saveDue('a@x.com', [card('1')], D1);
    enqueueGrade('a@x.com', '1', 2, D1);
    expect(loadDue('a@x.com', D2)).toBeNull();
    expect(pendingGrades('a@x.com', D2)).toEqual([{ id: '1', grade: 2 }]);
  });

  it('chiqishda hammasi o‘chadi', () => {
    saveDue('a@x.com', [card('1')], D1);
    enqueueGrade('a@x.com', '1', 2, D1);
    clearOfflineReview();
    expect(loadDue('a@x.com', D1)).toBeNull();
    expect(pendingGrades('a@x.com', D1)).toEqual([]);
  });

  it('boshqa hisobning navbati o‘zgartirilmaydi va yuborilmaydi', () => {
    enqueueGrade('a@x.com', '1', 2, D1);
    expect(pendingGrades('b@x.com', D1)).toEqual([]);
  });

  it('feature flag VITE_OFFLINE_REVIEW=off: hech narsa saqlanmaydi va o‘qilmaydi', () => {
    saveDue('a@x.com', [card('1')], D1);
    vi.stubEnv('VITE_OFFLINE_REVIEW', 'off');
    expect(loadDue('a@x.com', D1)).toBeNull();
    expect(enqueueGrade('a@x.com', '1', 2, D1)).toBe(false);
    saveDue('a@x.com', [card('2')], D1);
    vi.unstubAllEnvs();
    expect(loadDue('a@x.com', D1)?.map((c) => c.id)).toEqual(['1']);
  });
});
