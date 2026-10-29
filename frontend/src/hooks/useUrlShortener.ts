import { useState, useCallback, useEffect, useRef } from 'react';
import { api } from '../services/api';
import type { ShortenUrlResponse, PagedUrls } from '../types/api';

const emptyPage: PagedUrls = { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 };

export function useUrlShortener() {
  const [data, setData] = useState<PagedUrls>(emptyPage);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [deleting, setDeleting] = useState<string | null>(null);
  const [lastCreated, setLastCreated] = useState<ShortenUrlResponse | null>(null);
  const activeRequest = useRef<AbortController | null>(null);

  const loadPage = useCallback(async (page: number) => {
    activeRequest.current?.abort();
    const request = new AbortController();
    activeRequest.current = request;
    setLoading(true);
    setListError(null);
    try {
      let result = await api.list(page, 10, request.signal);
      // Deleting the final row, including a concurrent deletion, can remove the current page.
      if (page > Math.max(1, result.totalPages)) {
        result = await api.list(Math.max(1, result.totalPages), 10, request.signal);
      }
      if (!request.signal.aborted) setData(result);
    } catch (e) {
      if (!request.signal.aborted) setListError(e instanceof Error ? e.message : 'Failed to load URLs.');
    } finally {
      if (!request.signal.aborted) setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadPage(1);
    return () => activeRequest.current?.abort();
  }, [loadPage]);

  const shorten = useCallback(async (fullUrl: string, customAlias?: string) => {
    setError(null);
    const result = await api.shorten({ fullUrl, customAlias });
    setLastCreated(result);
    await loadPage(1);
  }, [loadPage]);

  const deleteUrl = useCallback(async (alias: string) => {
    setError(null);
    setDeleting(alias);
    try {
      await api.delete(alias);
      setLastCreated(current => current?.alias === alias ? null : current);
      await loadPage(data.page);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to delete URL.');
    } finally {
      setDeleting(null);
    }
  }, [data.page, loadPage]);

  return {
    ...data, loading, listError, error, deleting, lastCreated, shorten, deleteUrl,
    loadPage, clearLastCreated: () => setLastCreated(null),
  };
}
