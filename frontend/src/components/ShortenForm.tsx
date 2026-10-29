import { useState, type FormEvent } from 'react';

interface ShortenFormProps {
  onSubmit: (fullUrl: string, customAlias?: string) => Promise<void>;
  disabled?: boolean;
}

export function ShortenForm({ onSubmit, disabled }: ShortenFormProps) {
  const [fullUrl, setFullUrl] = useState('');
  const [customAlias, setCustomAlias] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);


  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setValidationError(null);
    const url = fullUrl.trim();
    const alias = customAlias.trim();
    if (!url) { setValidationError('URL is required.'); return; }
    try {
      const parsed = new URL(url);
      if (!['http:', 'https:'].includes(parsed.protocol) || parsed.username || parsed.password || url.length > 2048)
        throw new Error();
    } catch {
      setValidationError('Enter a valid URL using HTTP or HTTPS, without credentials (maximum 2048 characters).');
      return;
    }
    if (alias && !/^[a-zA-Z0-9-]{2,64}$/.test(alias)) {
      setValidationError('Alias may only contain letters, numbers, and hyphens (2–64 characters).');
      return;
    }
    setSubmitting(true);
    try {
      await onSubmit(url, alias || undefined);
      setFullUrl('');
      setCustomAlias('');
    } catch (error) {
      setValidationError(error instanceof Error ? error.message : 'Failed to shorten URL.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form className="shorten-form" onSubmit={handleSubmit} noValidate>
      <div className="field">
        <label htmlFor="fullUrl">Long URL</label>
        <input
          id="fullUrl"
          type="url"
          placeholder="https://example.com/very/long/path"
          value={fullUrl}
          onChange={(e) => setFullUrl(e.target.value)}
          disabled={disabled || submitting}
          aria-required="true"
          required
          aria-describedby={validationError ? 'form-error' : undefined}
        />
      </div>

      <div className="field">
        <label htmlFor="customAlias">
          Custom alias <span className="optional">(optional)</span>
        </label>
        <input
          id="customAlias"
          type="text"
          placeholder="my-custom-alias"
          value={customAlias}
          onChange={(e) => setCustomAlias(e.target.value)}
          disabled={disabled || submitting}
          aria-describedby="alias-hint"
        />
        <small id="alias-hint">2–64 letters, numbers or hyphens. Leave blank for an automatic alias.</small>
      </div>

      {validationError && (
        <p id="form-error" className="error" role="alert">
          {validationError}
        </p>
      )}

      <button type="submit" className="btn-primary" disabled={disabled || submitting}>
        {submitting ? 'Shortening…' : 'Shorten URL'}
      </button>
    </form>
  );
}
