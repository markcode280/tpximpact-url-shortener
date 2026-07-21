import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { ShortenForm } from '../ShortenForm';

describe('ShortenForm', () => {
  it('renders the URL input and submit button', () => {
    render(<ShortenForm onSubmit={vi.fn()} />);
    expect(screen.getByLabelText(/long url/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /shorten url/i })).toBeInTheDocument();
  });

  it('shows a validation error when submitted with empty URL', async () => {
    render(<ShortenForm onSubmit={vi.fn()} />);
    fireEvent.click(screen.getByRole('button', { name: /shorten url/i }));
    expect(await screen.findByText(/url is required/i)).toBeInTheDocument();
  });

  it('shows a validation error for an invalid URL', async () => {
    render(<ShortenForm onSubmit={vi.fn()} />);
    await userEvent.type(screen.getByLabelText(/long url/i), 'not-a-url');
    fireEvent.click(screen.getByRole('button', { name: /shorten url/i }));
    expect(await screen.findByText(/valid url/i)).toBeInTheDocument();
  });

  it('shows a validation error for an invalid alias', async () => {
    render(<ShortenForm onSubmit={vi.fn()} />);
    await userEvent.type(screen.getByLabelText(/long url/i), 'https://example.com');
    await userEvent.type(screen.getByLabelText(/custom alias/i), 'bad alias');
    fireEvent.click(screen.getByRole('button', { name: /shorten url/i }));
    expect(await screen.findByText(/letters, numbers, and hyphens/i)).toBeInTheDocument();
  });

  it('calls onSubmit with fullUrl and no alias when alias is empty', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ShortenForm onSubmit={onSubmit} />);
    await userEvent.type(screen.getByLabelText(/long url/i), 'https://example.com');
    fireEvent.click(screen.getByRole('button', { name: /shorten url/i }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('https://example.com', undefined));
  });

  it('calls onSubmit with customAlias when provided', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ShortenForm onSubmit={onSubmit} />);
    await userEvent.type(screen.getByLabelText(/long url/i), 'https://example.com');
    await userEvent.type(screen.getByLabelText(/custom alias/i), 'my-alias');
    fireEvent.click(screen.getByRole('button', { name: /shorten url/i }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('https://example.com', 'my-alias'));
  });

  it('clears the form after successful submission', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ShortenForm onSubmit={onSubmit} />);
    const urlInput = screen.getByLabelText(/long url/i);
    await userEvent.type(urlInput, 'https://example.com');
    fireEvent.click(screen.getByRole('button', { name: /shorten url/i }));
    await waitFor(() => expect(urlInput).toHaveValue(''));
  });
});
