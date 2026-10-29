import { useEffect, useState } from 'react';
import type { ShortenUrlResponse } from '../types/api';

interface ResultBannerProps {
  result: ShortenUrlResponse;
  onDismiss: () => void;
}

export function ResultBanner({ result, onDismiss }: ResultBannerProps) {
  const [copied, setCopied] = useState(false);
  const [copyError, setCopyError] = useState(false);

  useEffect(() => { setCopied(false); setCopyError(false); }, [result.shortUrl]);
  useEffect(() => {
    if (!copied) return;
    const timeout = setTimeout(() => setCopied(false), 2000);
    return () => clearTimeout(timeout);
  }, [copied]);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(result.shortUrl);
      setCopied(true);
      setCopyError(false);
    } catch { setCopyError(true); }
  };

  return (
    <div className="result-banner" role="status">
      <div className="result-banner__inner">
        <div className="result-banner__content">
          <span className="result-banner__label">Your short URL</span>
          <a
            href={result.shortUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="result-banner__url"
          >
            {result.shortUrl}
          </a>
          {copyError && <span role="alert">Could not copy. Select and copy the link manually.</span>}
        </div>
        <div className="result-banner__actions">
          <button className="btn-secondary" onClick={copy} aria-label="Copy short URL">
            {copied ? 'Copied!' : 'Copy'}
          </button>
          <button className="btn-ghost" onClick={onDismiss} aria-label="Dismiss">
            ✕
          </button>
        </div>
      </div>
    </div>
  );
}
