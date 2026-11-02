import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ErrorLog, ErrorLogDetail, ErrorLogFilter } from '../models/error-log';
import { PagedList } from '../models/paged-list';
import { API_BASE_URL } from './api-config';

@Injectable({ providedIn: 'root' })
export class ErrorLogsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/ErrorLogs`;

  list(filter: ErrorLogFilter): Observable<PagedList<ErrorLog>> {
    let params = new HttpParams().set('Page', filter.page).set('Quantity', filter.quantity);

    if (filter.projectId) {
      params = params.set('ProjectId', filter.projectId);
    }

    if (filter.status !== null && filter.status !== undefined) {
      params = params.set('Status', filter.status);
    }

    if (filter.query?.trim()) {
      params = params.set('Query', filter.query.trim());
    }

    return this.http.get<PagedList<ErrorLog>>(this.baseUrl, { params });
  }

  get(id: string): Observable<ErrorLogDetail> {
    return this.http.get<ErrorLogDetail>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }

  resolve(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${encodeURIComponent(id)}/Resolve`, null);
  }
}
