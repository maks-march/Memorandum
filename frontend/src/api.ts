export type Health = {
  status: string;
  mode: 'api' | 'mock';
  environment: string;
  database: string;
  version: string;
  machine: string;
  timeUtc: string;
  requestId?: string;
};

export type HealthResult =
  | { ok: true; data: Health; ms: number; url: string }
  | { ok: false; error: string; status?: number; ms: number; url: string };

/** Запрос health по относительному пути — работает и в dev (Vite proxy), и в prod (nginx). */
export async function fetchHealth(path: string, init?: RequestInit): Promise<HealthResult> {
  const started = performance.now();
  const url = path;
  try {
    const res = await fetch(path, {
      headers: { Accept: 'application/json' },
      ...init,
    });
    const ms = Math.round(performance.now() - started);

    if (!res.ok) {
      const body = await res.text();
      return { ok: false, error: `HTTP ${res.status}: ${body.slice(0, 300)}`, status: res.status, ms, url };
    }
    return { ok: true, data: (await res.json()) as Health, ms, url };
  } catch (e) {
    return {
      ok: false,
      error: e instanceof Error ? e.message : String(e),
      ms: Math.round(performance.now() - started),
      url,
    };
  }
}
