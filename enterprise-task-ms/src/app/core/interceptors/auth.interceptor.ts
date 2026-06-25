import { inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';

import {
  API_BASE_URL,
  AUTH_REFRESH_TOKEN_KEY,
  AUTH_STORAGE_KEY,
  AUTH_TOKEN_KEY
} from '../constants/app.constants';

type RefreshResponse = {
  accessToken: string;
  refreshToken: string;
  user: unknown;
};

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const http = inject(HttpClient);
  const token = localStorage.getItem(AUTH_TOKEN_KEY);

  if (!token || !request.url.startsWith(API_BASE_URL)) {
    return next(request);
  }

  const authorizedRequest = request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authorizedRequest).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)
        || error.status !== 401
        || request.url.endsWith('/auth/login')
        || request.url.endsWith('/auth/refresh')) {
        return throwError(() => error);
      }

      const refreshToken = localStorage.getItem(AUTH_REFRESH_TOKEN_KEY);
      if (!refreshToken) {
        return throwError(() => error);
      }

      return http.post<RefreshResponse>(`${API_BASE_URL}/auth/refresh`, { refreshToken }).pipe(
        switchMap((response) => {
          localStorage.setItem(AUTH_TOKEN_KEY, response.accessToken);
          localStorage.setItem(AUTH_REFRESH_TOKEN_KEY, response.refreshToken);
          localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(response.user));

          return next(
            request.clone({
              setHeaders: {
                Authorization: `Bearer ${response.accessToken}`
              }
            })
          );
        }),
        catchError((refreshError: unknown) => {
          localStorage.removeItem(AUTH_TOKEN_KEY);
          localStorage.removeItem(AUTH_REFRESH_TOKEN_KEY);
          localStorage.removeItem(AUTH_STORAGE_KEY);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
