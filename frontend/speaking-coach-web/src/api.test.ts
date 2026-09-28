import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AI_TIMEOUT_MS, ApiError, apiFetch, apiJson, DEFAULT_TIMEOUT_MS, isAiPath, networkError, timeoutFor } from './api';
import { getSlowCount, noteServerResponse, resetNetStatus, SLOW_AFTER_MS, subscribeSlow, trackRequest } from './netStatus';

describe('timeouts', () => {
  it('gives AI grading and audio uploads a long timeout', () => {
    expect(isAiPath('/api/speaking/submit')).toBe(true);
    expect(isAiPath('/api/writing/submit?save=false')).toBe(true);
    expect(isAiPath('/api/reading/generate')).toBe(true);
    expect(isAiPath('/api/mock/ielts/speaking')).toBe(true);
    expect(isAiPath('/api/mock/cefr/listening/new?level=B2')).toBe(true);
    expect(isAiPath('/api/talk/123/turn')).toBe(true);
    expect(isAiPath('/api/shadowing/check')).toBe(true);
    expect(timeoutFor('/api/writing/submit')).toBe(AI_TIMEOUT_MS);
    expect(timeoutFor('/api/anything', { method: 'POST', body: new FormData() })).toBe(AI_TIMEOUT_MS);
  });
  it('uses the default for ordinary requests and respects the caller', () => {
    expect(isAiPath('/api/profile')).toBe(false);
    expect(isAiPath('/api/mock/history')).toBe(false);
    expect(isAiPath('/api/mock/status')).toBe(false);
    expect(isAiPath('/api/reading/history')).toBe(false);
    expect(timeoutFor('/api/profile')).toBe(DEFAULT_TIMEOUT_MS);
    expect(timeoutFor('/api/speaking/submit', { timeoutMs: 5000 })).toBe(5000);
  });
});

describe('network errors', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.useRealTimers();
    resetNetStatus();
  });

  it('turns "Failed to fetch" into a human ApiError with status 0', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));
    const err = await apiFetch('/api/profile').catch((e: unknown) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect((err as ApiError).status).toBe(0);
    expect((err as ApiError).code).toBe('network');
    expect((err as ApiError).message).not.toMatch(/Failed to fetch/);
  });

  it('distinguishes offline from a server that does not answer', () => {
    expect(networkError(false, false).code).toBe('offline');
    expect(networkError(true, true).code).toBe('timeout');
    expect(networkError(false, true).code).toBe('network');
  });

  it('aborts after the timeout', async () => {
    vi.useFakeTimers();
    vi.stubGlobal(
      'fetch',
      vi.fn((_url: string, init: RequestInit) =>
        new Promise((_resolve, reject) => {
          init.signal!.addEventListener('abort', () => reject(new DOMException('aborted', 'AbortError')));
        }),
      ),
    );
    const p = apiFetch('/api/profile', { timeoutMs: 1000 }).catch((e: unknown) => e);
    await vi.advanceTimersByTimeAsync(1001);
    const err = (await p) as ApiError;
    expect(err).toBeInstanceOf(ApiError);
    expect(err.status).toBe(0);
  });

  it("rethrows the caller's own abort untouched", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((_url: string, init: RequestInit) =>
        new Promise((_resolve, reject) => {
          init.signal!.addEventListener('abort', () => reject(new DOMException('aborted', 'AbortError')));
        }),
      ),
    );
    const controller = new AbortController();
    const p = apiFetch('/api/profile', { signal: controller.signal }).catch((e: unknown) => e);
    controller.abort();
    const err = (await p) as Error;
    expect(err).not.toBeInstanceOf(ApiError);
    expect(err.name).toBe('AbortError');
  });
});

describe('server errors', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('carries code and retryAfterSeconds for ai_busy', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ error: 'AI band', code: 'ai_busy', retryAfterSeconds: 20 }), { status: 503 }),
      ),
    );
    const err = (await apiJson('/api/writing/submit').catch((e: unknown) => e)) as ApiError;
    expect(err.status).toBe(503);
    expect(err.code).toBe('ai_busy');
    expect(err.retryAfterSeconds).toBe(20);
    expect(err.message).toBe('AI band');
  });

  it('keeps the old two-argument constructor working', () => {
    const e = new ApiError('x', 401);
    expect(e.status).toBe(401);
    expect(e.code).toBeUndefined();
  });

  it('has a friendly message for gateway errors without JSON', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('<html>Bad gateway</html>', { status: 502 })));
    const err = (await apiJson('/api/profile').catch((e: unknown) => e)) as ApiError;
    expect(err.status).toBe(502);
    expect(err.message).not.toMatch(/html/i);
  });
});

describe('slow request signal', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    resetNetStatus();
  });
  afterEach(() => {
    vi.useRealTimers();
    resetNetStatus();
  });

  it('turns on after 4 s and off when the request finishes', () => {
    const seen: number[] = [];
    subscribeSlow(() => seen.push(getSlowCount()));
    const done = trackRequest();
    vi.advanceTimersByTime(SLOW_AFTER_MS - 1);
    expect(getSlowCount()).toBe(0);
    vi.advanceTimersByTime(1);
    expect(getSlowCount()).toBe(1);
    done();
    done(); // ikkinchi chaqiruv hech narsa qilmaydi
    expect(getSlowCount()).toBe(0);
    expect(seen).toEqual([1, 0]);
  });

  it('fast requests never flash the banner', () => {
    const listener = vi.fn();
    subscribeSlow(listener);
    trackRequest()();
    vi.advanceTimersByTime(10_000);
    expect(listener).not.toHaveBeenCalled();
  });

  it('slow AI requests on an awake server are not "waking up"', () => {
    noteServerResponse(Date.now());
    const done = trackRequest(true);
    vi.advanceTimersByTime(SLOW_AFTER_MS + 1);
    expect(getSlowCount()).toBe(0);
    done();
  });
});
