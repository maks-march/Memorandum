import { useCallback, useEffect, useState } from 'react';
import { fetchHealth, type HealthResult } from './api';

const isDev = import.meta.env.DEV;

export default function App() {
  const [health, setHealth] = useState<HealthResult | null>(null);
  const [loading, setLoading] = useState(false);

  const call = useCallback(async (path: string) => {
    setLoading(true);
    setHealth(await fetchHealth(path));
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
            {isDev ? 'DEV (vite)' : 'PROD (nginx)'}
          </span>
          {health?.ok && (
            <span className={`badge ${isMock ? 'badge-mock' : 'badge-api'}`}>
              ответ от: {health.data.mode === 'mock' ? 'MOCK' : 'API'}
            </span>
          )}
          {loading && <span className="badge">загрузка…</span>}
        </div>
      </header>

      <section className="card">
        <p className="hint">
          Запросы идут по относительным путям: <code>/api/health</code> и <code>/mock/api/health</code>.
          В dev их проксирует Vite на <code>{import.meta.env.VITE_API_TARGET ?? 'http://localhost:5000'}</code>,
          в prod — nginx на контейнер <code>api</code>.
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
