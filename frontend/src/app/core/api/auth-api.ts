import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthTokens, LoginRequest, RefreshTokenRequest } from '../models/auth';
import { API_BASE_URL } from './api-config';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  login(request: LoginRequest): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.baseUrl}/Auth/Login`, request);
  }

  refresh(request: RefreshTokenRequest): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.baseUrl}/Auth/RefreshToken`, request);
  }
}
