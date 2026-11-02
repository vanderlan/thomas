/** Paginated response returned by the API list endpoints. */
export interface PagedList<T> {
  items: T[];
  count: number;
}

/** Common pagination/search query of the API list endpoints. */
export interface PageQuery {
  page: number;
  quantity: number;
  query?: string;
}

export const DEFAULT_PAGE_SIZE = 10;
