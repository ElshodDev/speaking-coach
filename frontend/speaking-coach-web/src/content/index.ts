// Barcha tayyor materiallar bitta joyda: asosiy va qo'shimcha to'plamlar birlashtiriladi,
// darslar darajasi bo'yicha tartiblanadi (A2 → C1).
import { GRAMMAR } from './grammar';
import { GRAMMAR_MORE } from './grammar2';
import { LEVELS, type GrammarLesson, type VocabTopic } from './types';
import { VOCAB_TOPICS } from './vocabTopics';
import { VOCAB_TOPICS_MORE } from './vocabTopics2';

export const ALL_GRAMMAR: GrammarLesson[] = [...GRAMMAR, ...GRAMMAR_MORE].sort((a, b) => LEVELS.indexOf(a.level) - LEVELS.indexOf(b.level));
export const ALL_VOCAB_TOPICS: VocabTopic[] = [...VOCAB_TOPICS, ...VOCAB_TOPICS_MORE];
