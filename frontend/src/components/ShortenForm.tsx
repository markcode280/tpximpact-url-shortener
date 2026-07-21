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
    setSubmitting(true);
    try {
      await onSubmit(fullUrl.trim(), customAlias.trim() || undefined);
      setFullUrl('');
      setCustomAlias('');
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
        />
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
