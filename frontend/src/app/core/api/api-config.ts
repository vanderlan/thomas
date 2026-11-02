import { InjectionToken } from '@angular/core';

/**
 * Base URL of the Thomas API. By default the app calls a relative "/api" path, proxied to the API
 * by the dev server (proxy.conf.json) or by nginx (Docker), so no CORS is involved.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
