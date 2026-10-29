import { useUrlShortener } from './hooks/useUrlShortener';
import { ShortenForm } from './components/ShortenForm';
import { ResultBanner } from './components/ResultBanner';
import { UrlTable } from './components/UrlTable';

export default function App() {
  const { items, page, pageSize, totalCount, totalPages, loading, listError, error, deleting,
    lastCreated, shorten, deleteUrl, clearLastCreated, loadPage } =
    useUrlShortener();

  return (
    <div className="app">
      <header className="app-header">
        <div className="app-header__inner">
          <div className="logo">
            <span className="logo__mark" aria-hidden="true">⌁</span>
            <h1 className="logo__name">Snip</h1>
          </div>
          <p className="tagline">Long URLs, made short.</p>
        </div>
      </header>

      <main className="app-main">
        <section className="card shorten-card" aria-labelledby="shorten-heading">
          <h2 id="shorten-heading" className="card__title">
            Shorten a URL
          </h2>
          <ShortenForm onSubmit={shorten} disabled={!!deleting} />
        </section>

        {lastCreated && (
          <ResultBanner result={lastCreated} onDismiss={clearLastCreated} />
        )}

        {error && (
          <div className="banner banner--error" role="alert">
            <strong>Error:</strong> {error}
          </div>
        )}

        <section className="card" aria-labelledby="list-heading" aria-busy={loading}>
          <h2 id="list-heading" className="card__title">
            All shortened URLs
            {totalCount > 0 && (
              <span className="badge">{totalCount}</span>
            )}
          </h2>

          {loading && <p className="loading" role="status">Loading URLs…</p>}
          {listError && <div className="banner banner--error" role="alert">
            <p>{listError}</p>
            <button className="btn-secondary" onClick={() => void loadPage(page)}>Retry loading URLs</button>
          </div>}
          {!loading && !listError && <>
            <UrlTable urls={items} onDelete={deleteUrl} deleting={deleting} />
            {totalCount > 0 && <nav className="pagination" aria-label="URL pages">
              <button className="btn-secondary" disabled={page <= 1 || !!deleting}
                onClick={() => void loadPage(page - 1)}>Previous</button>
              <p aria-live="polite">Page {page} of {totalPages} · {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalCount)} of {totalCount}</p>
              <button className="btn-secondary" disabled={page >= totalPages || !!deleting}
                onClick={() => void loadPage(page + 1)}>Next</button>
            </nav>}
          </>}
        </section>
      </main>

      <footer className="app-footer">
        <p>Snip URL Shortener</p>
      </footer>
    </div>
  );
}
