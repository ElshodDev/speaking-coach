// Darslar sahifasi uchun yengil katalog: src/content/catalog.ts ni to'liq kontentdan yaratadi.
// Ro'yxat sahifasi faqat katalogni yuklaydi (sarlavha, daraja, qisqa tavsif); dars matni
// faqat dars ochilganda, o'z faylidan yuklanadi. Kontent o'zgarsa: npm run catalog
// (catalog.test.ts katalog eskirganini aniqlaydi — CI qizil bo'ladi).
// Ishga tushirish: node --experimental-strip-types scripts/learn-catalog.mjs
import { writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const LEVELS = ['A2', 'B1', 'B2', 'C1'];
const src = (f) => new URL(`../src/content/${f}`, import.meta.url).href;

const [{ GRAMMAR }, { GRAMMAR_MORE }, { VOCAB_TOPICS }, { VOCAB_TOPICS_MORE }] = await Promise.all([
  import(src('grammar.ts')),
  import(src('grammar2.ts')),
  import(src('vocabTopics.ts')),
  import(src('vocabTopics2.ts')),
]);

// Tartib — content/index.ts dagidek: darajasi bo'yicha (barqaror saralash).
const grammar = [
  ...GRAMMAR.map((g) => ({ g, file: 'grammar' })),
  ...GRAMMAR_MORE.map((g) => ({ g, file: 'grammar2' })),
]
  .sort((a, b) => LEVELS.indexOf(a.g.level) - LEVELS.indexOf(b.g.level))
  .map(({ g, file }) => ({ id: g.id, level: g.level, title: g.title, summary: g.summary, file }));

const vocab = [
  ...VOCAB_TOPICS.map((v) => ({ v, file: 'vocabTopics' })),
  ...VOCAB_TOPICS_MORE.map((v) => ({ v, file: 'vocabTopics2' })),
].map(({ v, file }) => ({ id: v.id, emoji: v.emoji, title: v.title, words: v.words.length, file }));

const out = `// AVTOMATIK YARATILGAN — qo'lda tahrirlamang. Yangilash: npm run catalog
// Darslar ro'yxati uchun yengil katalog (to'liq matn — grammar*.ts / vocabTopics*.ts da, dars ochilganda yuklanadi).
import type { L3, Level } from './types';

export interface GrammarEntry {
  id: string;
  level: Level;
  title: L3;
  summary: L3;
  file: 'grammar' | 'grammar2';
}

export interface VocabEntry {
  id: string;
  emoji: string;
  title: L3;
  words: number;
  file: 'vocabTopics' | 'vocabTopics2';
}

export const GRAMMAR_CATALOG: GrammarEntry[] = ${JSON.stringify(grammar, null, 2)};

export const VOCAB_CATALOG: VocabEntry[] = ${JSON.stringify(vocab, null, 2)};
`;

const target = fileURLToPath(new URL('../src/content/catalog.ts', import.meta.url));
writeFileSync(target, out);
console.log(`catalog.ts: ${grammar.length} lessons, ${vocab.length} topics`);
