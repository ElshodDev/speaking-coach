// Dars va mavzu matnini kerak bo'lganda yuklash: har biri faqat o'z faylidan (alohida chunk).
// Bir fayldagi boshqa darslar keyin keshdan ochiladi.
import { GRAMMAR_CATALOG, VOCAB_CATALOG } from './catalog';
import type { GrammarLesson, VocabTopic } from './types';

const grammarFiles = {
  grammar: () => import('./grammar').then((m) => m.GRAMMAR),
  grammar2: () => import('./grammar2').then((m) => m.GRAMMAR_MORE),
};

const vocabFiles = {
  vocabTopics: () => import('./vocabTopics').then((m) => m.VOCAB_TOPICS),
  vocabTopics2: () => import('./vocabTopics2').then((m) => m.VOCAB_TOPICS_MORE),
};

/** Grammatika darsi; katalogda yo'q bo'lsa — null. */
export async function loadGrammarLesson(id: string): Promise<GrammarLesson | null> {
  const entry = GRAMMAR_CATALOG.find((g) => g.id === id);
  if (!entry) return null;
  return (await grammarFiles[entry.file]()).find((g) => g.id === id) ?? null;
}

/** Mavzuli lug'at; katalogda yo'q bo'lsa — null. */
export async function loadVocabTopic(id: string): Promise<VocabTopic | null> {
  const entry = VOCAB_CATALOG.find((v) => v.id === id);
  if (!entry) return null;
  return (await vocabFiles[entry.file]()).find((v) => v.id === id) ?? null;
}
