import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ResultBanner } from '../ResultBanner';

const result = { alias: 'sample', fullUrl: 'https://example.com', shortUrl: 'http://localhost:8080/sample' };

describe('ResultBanner', () => {
  it('copies the link and reports success', async () => {
    const user = userEvent.setup();
    const clipboard = vi.spyOn(navigator.clipboard, 'writeText').mockResolvedValue();
    render(<ResultBanner result={result} onDismiss={vi.fn()} />);
    await user.click(screen.getByRole('button', { name: 'Copy short URL' }));
    expect(clipboard).toHaveBeenCalledWith(result.shortUrl);
    expect(screen.getByRole('button', { name: 'Copy short URL' })).toHaveTextContent('Copied!');
  });

  it('explains clipboard failure without an unhandled rejection', async () => {
    const user = userEvent.setup();
    vi.spyOn(navigator.clipboard, 'writeText').mockRejectedValue(new Error('Denied'));
    render(<ResultBanner result={result} onDismiss={vi.fn()} />);
    await user.click(screen.getByRole('button', { name: 'Copy short URL' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Select and copy the link manually.');
  });
});
