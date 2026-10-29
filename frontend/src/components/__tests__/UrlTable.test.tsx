import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { UrlTable } from '../UrlTable';
import type { UrlListItem } from '../../types/api';

const mockUrls: UrlListItem[] = [
  { alias: 'abc', fullUrl: 'https://example.com', shortUrl: 'http://localhost:8080/abc', createdAt: '2026-10-29T10:00:00Z' },
  { alias: 'def', fullUrl: 'https://another.com/path', shortUrl: 'http://localhost:8080/def', createdAt: '2026-10-28T10:00:00Z' },
];

describe('UrlTable', () => {
  it('shows empty state when there are no URLs', () => {
    render(<UrlTable urls={[]} onDelete={vi.fn()} />);
    expect(screen.getByText(/no shortened urls yet/i)).toBeInTheDocument();
  });

  it('renders a row for each URL', () => {
    render(<UrlTable urls={mockUrls} onDelete={vi.fn()} />);
    expect(screen.getByText('http://localhost:8080/abc')).toBeInTheDocument();
    expect(screen.getByText('http://localhost:8080/def')).toBeInTheDocument();
  });

  it('calls onDelete with the alias when delete is clicked', () => {
    const onDelete = vi.fn();
    render(<UrlTable urls={mockUrls} onDelete={onDelete} />);
    fireEvent.click(screen.getAllByRole('button', { name: /delete/i })[0]);
    expect(onDelete).toHaveBeenCalledWith('abc');
  });

  it('disables deletion while a deletion is pending', () => {
    render(<UrlTable urls={mockUrls} onDelete={vi.fn()} deleting="abc" />);
    for (const button of screen.getAllByRole('button')) expect(button).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Delete abc' })).toHaveTextContent('Deleting…');
  });
});
