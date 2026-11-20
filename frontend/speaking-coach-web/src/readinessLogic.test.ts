import { describe, expect, it } from 'vitest';
import { focusRoute, formatRange, formatScore, pct, statusTone, type Readiness } from './readinessLogic';

const base: Readiness = {
  exam: 'ielts', max: 9, overall: 6, overallLow: 5.5, overallHigh: 6.5, overallLevel: null, target: '7.0', targetValue: 7,
  gap: 1, status: 'behind', daysToExam: 30, focusSkill: 'writing', focusReason: 'weakest', skills: [],
};

describe('readinessLogic', () => {
  it('formats IELTS with one decimal and CEFR as whole points', () => {
    expect(formatScore('ielts', 6)).toBe('6.0');
    expect(formatScore('cefr', 51.4)).toBe('51');
    expect(formatScore('ielts', null)).toBe('—');
  });

  it('range collapses equal ends', () => {
    expect(formatRange('ielts', 5.5, 6.5)).toBe('5.5–6.5');
    expect(formatRange('ielts', 6, 6)).toBe('6.0');
    expect(formatRange('ielts', null, 6)).toBe('');
  });

  it('percent is clamped to the scale', () => {
    expect(pct(4.5, 9)).toBe(50);
    expect(pct(80, 75)).toBe(100);
    expect(pct(null, 9)).toBe(0);
  });

  it('focus route: weakest → practice, missing → that section mock', () => {
    expect(focusRoute(base)).toBe('practice/writing');
    expect(focusRoute({ ...base, focusSkill: 'reading', focusReason: 'missing' })).toBe('mock/reading-academic');
    expect(focusRoute({ ...base, focusSkill: 'listening', focusReason: 'missing' })).toBe('mock/listening');
    expect(focusRoute({ ...base, exam: 'cefr', focusSkill: 'speaking', focusReason: 'missing' })).toBe('mock/cefr-speaking');
    expect(focusRoute({ ...base, exam: null })).toBeNull();
  });

  it('status tone', () => {
    expect(statusTone('on_track')).toBe('good');
    expect(statusTone('close')).toBe('mid');
    expect(statusTone('behind')).toBe('low');
    expect(statusTone('unknown')).toBeNull();
  });
});
