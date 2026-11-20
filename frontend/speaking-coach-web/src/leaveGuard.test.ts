import { afterEach, describe, expect, it } from 'vitest';
import { addLeaveGuard, confirmLeave, guardMessage, isGuarded } from './leaveGuard';

describe('leaveGuard', () => {
  const removers: Array<() => void> = [];
  afterEach(() => {
    removers.splice(0).forEach((r) => r());
  });

  it('allows navigation when no exam is open, without asking', () => {
    let asked = false;
    expect(isGuarded()).toBe(false);
    expect(confirmLeave(() => ((asked = true), false))).toBe(true);
    expect(asked).toBe(false);
  });

  it('asks while an exam is open and blocks when the user declines', () => {
    removers.push(addLeaveGuard('Exam not finished'));
    let seen = '';
    expect(confirmLeave((m) => ((seen = m), false))).toBe(false);
    expect(seen).toBe('Exam not finished');
    expect(isGuarded()).toBe(true);
  });

  it('clears the guard once the user agrees, so the following hashchange does not ask again', () => {
    removers.push(addLeaveGuard('Exam not finished'));
    expect(confirmLeave(() => true)).toBe(true);
    expect(isGuarded()).toBe(false);
    expect(confirmLeave(() => false)).toBe(true);
  });

  it('removing the guard (exam finished or unmounted) lifts it', () => {
    const remove = addLeaveGuard('x');
    remove();
    expect(isGuarded()).toBe(false);
    expect(guardMessage()).toBeNull();
  });

  it('StrictMode double mount: two guards, removing one keeps the other', () => {
    const first = addLeaveGuard('a');
    removers.push(addLeaveGuard('b'));
    first();
    expect(isGuarded()).toBe(true);
    expect(guardMessage()).toBe('b');
  });
});
