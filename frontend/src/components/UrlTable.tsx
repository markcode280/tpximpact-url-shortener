import type { UrlListItem } from '../types/api';

interface UrlTableProps {
  urls: UrlListItem[];
  onDelete: (alias: string) => void;
  deleting?: string | null;
  disabled?: boolean;
}

export function UrlTable({ urls, onDelete, deleting, disabled }: UrlTableProps) {
  if (urls.length === 0) return <p className="empty-state">No shortened URLs yet. Create one above.</p>;

  return (
    <div className="url-table-wrapper">
      <table className="url-table">
        <caption className="sr-only">Shortened URLs, newest first</caption>
        <thead><tr>
          <th scope="col">Short URL / destination</th>
          <th scope="col">Created</th>
          <th scope="col"><span className="sr-only">Actions</span></th>
        </tr></thead>
        <tbody>{urls.map(url => (
          <tr key={url.alias}>
            <td>
              <a className="short-url-link" href={url.shortUrl} target="_blank" rel="noopener noreferrer">
                {url.shortUrl}
              </a>
              <a className="full-url" href={url.fullUrl} target="_blank" rel="noopener noreferrer" title={url.fullUrl}>
                {url.fullUrl}
              </a>
            </td>
            <td><time dateTime={url.createdAt}>{new Date(url.createdAt).toLocaleDateString()}</time></td>
            <td className="actions-cell">
              <button className="btn-danger" aria-label={`Delete ${url.alias}`}
                disabled={disabled || !!deleting} onClick={() => onDelete(url.alias)}>
                {deleting === url.alias ? 'Deleting…' : 'Delete'}
              </button>
            </td>
          </tr>
        ))}</tbody>
      </table>
    </div>
  );
}
