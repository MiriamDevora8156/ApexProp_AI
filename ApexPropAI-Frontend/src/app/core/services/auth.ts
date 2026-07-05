import { Injectable, inject, signal, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { LoginRequest, LoginResponse, RegisterRequest, User } from '../models/auth';

/**
 * AuthService — ניהול התחברות ו-JWT Token
 *
 * ❌ מה היה שגוי:
 *   private tokenSubject = signal<string | null>(this.getTokenFromStorage());
 *   האתחול הזה רץ לפני ה-constructor, לפני שאפשר לבדוק את הסביבה.
 *   ב-SSR (Node.js) אין window/localStorage → קריסה מיידית.
 *
 * ✅ מה תוקן:
 *   Signal מאותחל תמיד עם null.
 *   הקריאה ל-localStorage קורית רק ב-constructor,
 *   אחרי בדיקת isPlatformBrowser.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly API_URL = 'https://localhost:7215/api';

  // ✅ תמיד null בהתחלה — בטוח לSSR
  private readonly _currentUser = signal<User | null>(null);
  private readonly _token = signal<string | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly token = this._token.asReadonly();

  constructor() {
    // ✅ localStorage נגיש רק בצד Browser
    if (isPlatformBrowser(this.platformId)) {
      const savedToken = localStorage.getItem('auth_token');
      if (savedToken) {
        this._token.set(savedToken);
      }
    }
  }

  login(request: LoginRequest): Observable<{ success: boolean; data: LoginResponse }> {
    return this.http.post<{ success: boolean; data: LoginResponse }>(
      `${this.API_URL}/auth/login`, request
    ).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.saveSession(res.data.accessToken, res.data.user);
        }
      })
    );
  }

  register(request: RegisterRequest): Observable<{ success: boolean; data: LoginResponse }> {
    return this.http.post<{ success: boolean; data: LoginResponse }>(
      `${this.API_URL}/auth/register`, request
    ).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.saveSession(res.data.accessToken, res.data.user);
        }
      })
    );
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('auth_token');
    }
    this._token.set(null);
    this._currentUser.set(null);
  }

  isLoggedIn(): boolean {
    if (this._token()) return true;
    if (isPlatformBrowser(this.platformId)) {
      return !!localStorage.getItem('auth_token');
    }
    return false;
  }

  getToken(): string | null {
    if (this._token()) return this._token();
    if (isPlatformBrowser(this.platformId)) {
      return localStorage.getItem('auth_token');
    }
    return null;
  }

  getCurrentUser(): User | null {
    return this._currentUser();
  }

  private saveSession(token: string, user: User): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('auth_token', token);
    }
    this._token.set(token);
    this._currentUser.set(user);
  }
}
