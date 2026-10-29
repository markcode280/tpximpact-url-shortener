import { act, render, renderHook, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';
import { api } from './services/api';
import { useUrlShortener } from './hooks/useUrlShortener';
import type { PagedUrls } from './types/api';

vi.mock('./services/api', () => ({ api: { list: vi.fn(), shorten: vi.fn(), delete: vi.fn() } }));
const item = { alias: 'first', fullUrl: 'https://example.com', shortUrl: 'http://localhost:8080/first', createdAt: '2026-10-29T10:00:00Z' };
const pageOne: PagedUrls = { items: [item], page: 1, pageSize: 10, totalCount: 11, totalPages: 2 };
const pageTwo: PagedUrls = { ...pageOne, items: [{ ...item, alias: 'last', shortUrl: 'http://localhost:8080/last' }], page: 2 };

describe('application behaviour', () => {
  beforeEach(() => vi.resetAllMocks());

  it('shows loading, loads rows and navigates between pages', async () => {
    vi.mocked(api.list).mockResolvedValueOnce(pageOne).mockResolvedValueOnce(pageTwo);
    render(<App />);
    expect(screen.getByRole('status')).toHaveTextContent('Loading');
    expect(await screen.findByRole('link', { name: item.shortUrl })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: 'Next' }));
    expect(await screen.findByRole('link', { name: pageTwo.items[0].shortUrl })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
    expect(api.list).toHaveBeenLastCalledWith(2, 10, expect.any(AbortSignal));
  });

  it('returns to the preceding page after deleting its final row', async () => {
    vi.mocked(api.list).mockResolvedValueOnce(pageOne).mockResolvedValueOnce(pageTwo)
      .mockResolvedValueOnce({ ...pageTwo, items: [], totalCount: 10, totalPages: 1 })
      .mockResolvedValueOnce({ ...pageOne, totalCount: 10, totalPages: 1 });
    vi.mocked(api.delete).mockResolvedValue(undefined);
    render(<App />);
    await screen.findByRole('button', { name: 'Next' });
    await userEvent.click(screen.getByRole('button', { name: 'Next' }));
    await userEvent.click(await screen.findByRole('button', { name: 'Delete last' }));
    expect(await screen.findByRole('button', { name: 'Delete first' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Delete last' })).not.toBeInTheDocument();
    expect(api.delete).toHaveBeenCalledWith('last');
    expect(api.list).toHaveBeenLastCalledWith(1, 10, expect.any(AbortSignal));
  });

  it('leaves the row visible when deletion fails', async () => {
    vi.mocked(api.list).mockResolvedValue(pageOne);
    vi.mocked(api.delete).mockRejectedValue(new Error('Delete failed.'));
    render(<App />);
    await userEvent.click(await screen.findByRole('button', { name: 'Delete first' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Delete failed.');
    expect(screen.getByRole('button', { name: 'Delete first' })).toBeEnabled();
  });

  it('offers retry after a failed load without claiming the database is empty', async () => {
    vi.mocked(api.list).mockRejectedValueOnce(new Error('Unavailable.')).mockResolvedValueOnce(pageOne);
    render(<App />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Unavailable.');
    expect(screen.queryByText(/no shortened urls yet/i)).not.toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Retry loading URLs' }));
    expect(await screen.findByRole('button', { name: 'Delete first' })).toBeInTheDocument();
  });

  it('shows a created URL and returns to the newest page', async () => {
    vi.mocked(api.list).mockResolvedValue(pageOne);
    vi.mocked(api.shorten).mockResolvedValue({ ...item, alias: 'new', shortUrl: 'http://localhost:8080/new' });
    render(<App />);
    await screen.findByRole('button', { name: 'Delete first' });
    await userEvent.type(screen.getByLabelText(/long url/i), 'https://example.com');
    await userEvent.click(screen.getByRole('button', { name: 'Shorten URL' }));
    expect(await screen.findByRole('link', { name: 'http://localhost:8080/new' })).toBeInTheDocument();
    expect(screen.getByLabelText(/long url/i)).toHaveValue('');
    expect(api.list).toHaveBeenLastCalledWith(1, 10, expect.any(AbortSignal));
  });

  it('ignores stale responses after a newer page request', async () => {
    let resolveOld!: (value: PagedUrls) => void;
    vi.mocked(api.list).mockImplementationOnce(() => new Promise(resolve => { resolveOld = resolve; }))
      .mockResolvedValueOnce(pageTwo);
    const { result } = renderHook(() => useUrlShortener());
    await act(async () => { await result.current.loadPage(2); });
    expect(result.current.page).toBe(2);
    await act(async () => { resolveOld(pageOne); });
    await waitFor(() => expect(result.current.page).toBe(2));
    expect(result.current.items[0].alias).toBe('last');
  });
});
