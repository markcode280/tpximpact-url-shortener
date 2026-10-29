export interface ShortenUrlRequest {
  fullUrl: string;
  customAlias?: string;
}

export interface ShortenUrlResponse {
  alias: string;
  fullUrl: string;
  shortUrl: string;
}

export interface UrlListItem {
  alias: string;
  fullUrl: string;
  shortUrl: string;
  createdAt: string;
}

export interface PagedUrls {
  items: UrlListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiError {
  error: string;
}
