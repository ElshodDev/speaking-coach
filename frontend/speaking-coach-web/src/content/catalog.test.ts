import { describe, expect, it } from 'vitest';
import { GRAMMAR_CATALOG, VOCAB_CATALOG } from './catalog';
import { ALL_GRAMMAR, ALL_VOCAB_TOPICS } from './index';
import { loadGrammarLesson, loadVocabTopic } from './load';

// Katalog to'liq kontentdan avtomatik yaratiladi (npm run catalog). Kontent o'zgarib, katalog
// yangilanmasa — shu test yiqiladi va xabar nima qilishni aytadi.
const STALE = 'src/content/catalog.ts eskirgan — `npm run catalog` ni ishga tushiring.';

describe('learn catalog', () => {
  it('lists every grammar lesson in the same order with the same titles and summaries', () => {
    expect(GRAMMAR_CATALOG.map((g) => g.id), STALE).toEqual(ALL_GRAMMAR.map((g) => g.id));
    for (const [i, g] of ALL_GRAMMAR.entries()) {
      const c = GRAMMAR_CATALOG[i];
      expect({ level: c.level, title: c.title, summary: c.summary }, STALE).toEqual({ level: g.level, title: g.title, summary: g.summary });
    }
  });

  it('lists every vocabulary topic with its word count', () => {
    expect(VOCAB_CATALOG.map((v) => [v.id, v.emoji, v.title, v.words]), STALE).toEqual(
      ALL_VOCAB_TOPICS.map((v) => [v.id, v.emoji, v.title, v.words.length]),
    );
  });

  it('loads each lesson and topic from the file the catalog names', async () => {
    for (const g of GRAMMAR_CATALOG) expect((await loadGrammarLesson(g.id))?.id).toBe(g.id);
    for (const v of VOCAB_CATALOG) expect((await loadVocabTopic(v.id))?.words.length).toBe(v.words);
    expect(await loadGrammarLesson('nope')).toBeNull();
    expect(await loadVocabTopic('nope')).toBeNull();
  });
});
