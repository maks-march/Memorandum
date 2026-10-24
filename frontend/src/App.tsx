import { useCallback, useEffect, useRef, useState } from 'react';
import { fetchHealth, type HealthResult } from './api';

const isDev = import.meta.env.DEV;
const proxyTarget = import.meta.env.VITE_API_TARGET ?? 'http://localhost:5000';

export default function App() {
  const [health, setHealth] = useState<HealthResult | null>(null);
  const [loading, setLoading] = useState(false);

  // защита от гонки: применяем только ответ на последний запрос
  const lastRequest = useRef(0);
  const alive = useRef(true);

  useEffect(() => {
    alive.current = true;
    return () => {
      alive.current = false;
    };
  }, []);

  const call = useCallback(async (path: string) => {
    const id = ++lastRequest.current;
    setLoading(true);

    const result = await fetchHealth(path);

    // устаревший ответ (или компонент размонтирован) — не трогаем состояние
    if (!alive.current || id !== lastRequest.current) return;

    setHealth(result);
    setLoading(false);
  }, []);

  // при загрузке страницы сразу спрашиваем реальный API
  useEffect(() => {
    void call('/api/health');
  }, [call]);

  const isMock = health?.ok && health.data.mode === 'mock';

  return (
    <main className="page">
      <header className="head">
        <h1>Memorandum · health</h1>
        <div className="badges">
          <span className={`badge ${isDev ? 'badge-dev' : 'badge-prod'}`}>
            {isDev ? 'DEV (vite)' : 'PROD (static build)'}
          </span>
          {health?.ok && (
            <span className={`badge ${isMock ? 'badge-mock' : 'badge-api'}`}>
              ответ от: {isMock ? 'MOCK' : 'API'}
            </span>
          )}
          {loading && <span className="badge">загрузка…</span>}
        </div>
      </header>

      <section className="card">
        <p className="hint">
          Запросы идут по относительным путям: <code>/api/health</code> и <code>/mock/api/health</code>.
          {isDev ? (
            <>
              {' '}В dev их проксирует Vite на <code>{proxyTarget}</code>.
            </>
          ) : (
            <> В этой сборке запросы обрабатывает сервер, раздающий статику (в проде — nginx).</>
          )}
        </p>

        <div className="actions">
          <button onClick={() => void call('/api/health')} disabled={loading}>
            GET /api/health
          </button>
          <button onClick={() => void call('/mock/api/health')} disabled={loading} className="secondary">
            GET /mock/api/health
          </button>
          <button onClick={() => void call('/mock/api/health?status=500')} disabled={loading} className="secondary">
            mock → 500
          </button>
        </div>
      </section>

      <section className="card">
        <div className="result-head">
          <h2>Результат</h2>
          {health && (
            <span className={health.ok ? 'ok' : 'err'}>
              {health.ok ? `200 OK · ${health.ms} ms` : `ошибка · ${health.ms} ms`}
            </span>
          )}
        </div>

        {!health && <p className="hint">Нет данных.</p>}

        {health && health.ok && (
          <>
            <table className="kv">
              <tbody>
                <tr><th>status</th><td>{health.data.status}</td></tr>
                <tr><th>mode</th><td>{health.data.mode}</td></tr>
                <tr><th>environment</th><td>{health.data.environment}</td></tr>
                <tr><th>database</th><td>{health.data.database}</td></tr>
                <tr><th>version</th><td>{health.data.version}</td></tr>
                <tr><th>machine</th><td>{health.data.machine}</td></tr>
                <tr><th>timeUtc</th><td>{health.data.timeUtc}</td></tr>
              </tbody>
            </table>
            <pre className="json">{JSON.stringify(health.data, null, 2)}</pre>
          </>
        )}

        {health && !health.ok && (
          <>
            <p className="err">{health.error}</p>
            <pre className="json">{health.error}</pre>
          </>
        )}
      </section>
    </main>
  );
}
