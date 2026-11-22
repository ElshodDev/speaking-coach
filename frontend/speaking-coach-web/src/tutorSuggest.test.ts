import { describe, expect, it } from 'vitest';
import { mistakesMsg } from './locales/mistakes';
import { tutorMsg } from './locales/tutor';
import { topicFromRoute, tutorTopicRoute } from './TutorSuggest';

describe('tavsiya marshruti', () => {
  it('mavzu marshrutga kodlanadi va qaytib o‘qiladi', () => {
    for (const topic of ['Articles (a/an/the)', 'Past Simple vs Present Perfect', 'Countable and uncountable nouns', 'Oʻtgan zamon']) {
      const route = tutorTopicRoute(topic);
      expect(route.startsWith('tutor/t/')).toBe(true);
      expect(route.slice('tutor/t/'.length)).not.toMatch(/[\s/]/);              // marshrut bo'laklarini buzmaydi
      expect(topicFromRoute(route.slice('tutor/t/'.length))).toBe(topic);
    }
  });

  it('yaroqsiz mavzu marshrutdan olinmaydi', () => {
    expect(topicFromRoute(undefined)).toBeNull();
    expect(topicFromRoute('')).toBeNull();
    expect(topicFromRoute('a')).toBeNull();
    expect(topicFromRoute('%E0%A4%A')).toBeNull();                                 // buzuq kodlash
    expect(topicFromRoute(encodeURIComponent('x'.repeat(81)))).toBeNull();
    expect(topicFromRoute(encodeURIComponent('  Modal   verbs '))).toBe('Modal verbs');
  });

  it('tarjimalar: sabab matni uch tilda, tavsiya turlari uchun nom bor', () => {
    for (const lang of ['uz', 'ru', 'en'] as const) {
      const m = tutorMsg[lang];
      expect(m.suggestWhy('Artikllar', 5, true)).toContain('5');
      expect(m.suggestWhy('Artikllar', 5, false)).not.toBe(m.suggestWhy('Artikllar', 5, true));
      expect(m.suggestTitle).toBeTruthy();
      for (const cat of ['articles', 'prepositions', 'agreement', 'tense', 'plural', 'comparatives', 'word_order']) expect(mistakesMsg[lang].cats[cat]?.name).toBeTruthy();
    }
  });
});
