import { describe, expect, it } from 'vitest';
import { micPrecheck, micProblemOf, type MicEnv } from './micErrors';
import { clearDraft, DRAFTS_KEY, draftKey, latestDraft, loadDraft, MAX_DRAFTS, saveDraft, DRAFT_TTL_MS } from './writingDraft';
import { feedbackPage } from './Footer';

const ok: MicEnv = { secure: true, hasGetUserMedia: true, hasMediaRecorder: true };
const err = (name: string) => Object.assign(new Error(name), { name });

describe('microphone problems', () => {
  it('maps DOMException names to specific problems', () => {
    expect(micProblemOf(err('NotAllowedError'), ok)).toBe('denied');
    expect(micProblemOf(err('NotFoundError'), ok)).toBe('notFound');
    expect(micProblemOf(err('NotReadableError'), ok)).toBe('busy');
    expect(micProblemOf(err('Weird'), ok)).toBe('other');
    expect(micProblemOf('nonsense', ok)).toBe('other');
  });
  it('detects insecure pages and missing APIs before asking', () => {
    expect(micPrecheck(ok)).toBeNull();
    expect(micPrecheck({ ...ok, secure: false })).toBe('insecure');
    expect(micPrecheck({ ...ok, hasMediaRecorder: false })).toBe('unsupported');
    expect(micProblemOf(err('NotAllowedError'), { ...ok, hasGetUserMedia: false })).toBe('unsupported');
  });
});

function memoryStorage() {
  const m = new Map<string, string>();
  return {
    getItem: (k: string) => m.get(k) ?? null,
    setItem: (k: string, v: string) => void m.set(k, v),
    removeItem: (k: string) => void m.delete(k),
    raw: m,
  };
}

describe('writing drafts', () => {
  const topicA = { id: 'w-1', text: 'Describe your town', category: 'paragraph' };
  const topicB = { text: 'AI topic about cities', category: 'essay-opinion', ai: true };

  it('saves, restores per topic and clears', () => {
    const s = memoryStorage();
    saveDraft(topicA, 'My town is small.', 1000, s);
    saveDraft(topicB, 'Cities are big.', 2000, s);
    expect(loadDraft(topicA, 3000, s)?.text).toBe('My town is small.');
    expect(latestDraft(3000, s)?.topic).toEqual(topicB);
    clearDraft(topicB, 3000, s);
    expect(latestDraft(3000, s)?.topic).toEqual(topicA);
    saveDraft(topicA, '   ', 4000, s); // bo'sh matn — o'chiriladi
    expect(latestDraft(4000, s)).toBeNull();
    expect(s.raw.has(DRAFTS_KEY)).toBe(false);
  });

  it('keys bank topics by id and AI topics by text', () => {
    expect(draftKey(topicA)).toBe('id:w-1');
    expect(draftKey(topicB)).toBe(draftKey({ ...topicB }));
    expect(draftKey(topicB)).not.toBe(draftKey({ ...topicB, text: 'Other' }));
  });

  it('forgets old drafts and keeps at most MAX_DRAFTS', () => {
    const s = memoryStorage();
    for (let i = 0; i < MAX_DRAFTS + 3; i++) saveDraft({ id: `t${i}`, text: `T${i}`, category: 'c' }, `text ${i}`, 1000 + i, s);
    expect(Object.keys(JSON.parse(s.getItem(DRAFTS_KEY)!))).toHaveLength(MAX_DRAFTS);
    expect(loadDraft({ id: 't0', text: 'T0', category: 'c' }, 2000, s)).toBeNull();
    expect(latestDraft(1000 + DRAFT_TTL_MS + 100, s)).toBeNull();
  });

  it('survives broken or blocked storage', () => {
    const s = memoryStorage();
    s.setItem(DRAFTS_KEY, '{not json');
    expect(latestDraft(0, s)).toBeNull();
    const blocked = {
      getItem: () => {
        throw new Error('blocked');
      },
      setItem: () => {
        throw new Error('blocked');
      },
      removeItem: () => undefined,
    };
    expect(() => saveDraft(topicA, 'x', 0, blocked)).not.toThrow();
    expect(latestDraft(0, null)).toBeNull();
  });
});

describe('feedback page', () => {
  it('sends the route but not query parameters or Telegram data', () => {
    expect(feedbackPage({ pathname: '/', hash: '#/practice/writing' })).toBe('/#/practice/writing');
    expect(feedbackPage({ pathname: '/', hash: '#/reset?token=secret' })).toBe('/#/reset');
    expect(feedbackPage({ pathname: '/', hash: '#tgWebAppData=abc' })).toBe('/');
  });
});
