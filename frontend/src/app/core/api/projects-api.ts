import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedList, PageQuery } from '../models/paged-list';
import {
  Project,
  ProjectCreateRequest,
  ProjectUpdateRequest,
  SyncProjectErrorsResponse,
} from '../models/project';
import { API_BASE_URL } from './api-config';

@Injectable({ providedIn: 'root' })
export class ProjectsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/Projects`;

  list(query: PageQuery): Observable<PagedList<Project>> {
    let params = new HttpParams().set('Page', query.page).set('Quantity', query.quantity);

    if (query.query?.trim()) {
      params = params.set('Query', query.query.trim());
    }

    return this.http.get<PagedList<Project>>(this.baseUrl, { params });
  }

  get(id: string): Observable<Project> {
    return this.http.get<Project>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }

  create(request: ProjectCreateRequest): Observable<Project> {
    return this.http.post<Project>(this.baseUrl, request);
  }

  update(id: string, request: ProjectUpdateRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${encodeURIComponent(id)}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }

  /** Runs, on demand, the same synchronization executed by the background job. */
  syncErrors(id: string): Observable<SyncProjectErrorsResponse> {
    return this.http.post<SyncProjectErrorsResponse>(
      `${this.baseUrl}/${encodeURIComponent(id)}/Errors/Sync`,
      null,
    );
  }
}
