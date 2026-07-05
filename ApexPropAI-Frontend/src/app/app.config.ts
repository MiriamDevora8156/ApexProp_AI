import { ApplicationConfig } from '@angular/core';
import {
  provideRouter,
  withPreloading,
  PreloadAllModules,
  TitleStrategy
} from '@angular/router';
import {
  provideHttpClient,
  withFetch,
  withInterceptors,
  withXsrfConfiguration
} from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { routes } from './app.routes';
import { jwtInterceptor } from './core/interceptors/jwt';
import { AppTitleStrategy } from './core/services/title-strategy';

/**
 * app.config.ts — תצורת האפליקציה
 *
 * ❌ מה היה שגוי:
 *   1. HTTP_INTERCEPTORS (class-based) לא פועל עם provideHttpClient().
 *      זה המנגנון הישן מ-Angular 14 ומטה. עם provideHttpClient() החדש,
 *      ה-interceptor פשוט לא נקרא — כל בקשה יוצאת ללא JWT token.
 *   2. importProvidersFrom() יובא אבל לא שומש — מיותר.
 *
 * ✅ מה תוקן:
 *   1. withInterceptors([jwtInterceptor]) — כך רושמים functional interceptors.
 *   2. הוסרו imports מיותרים.
 *   3. TitleStrategy — מעדכן את document.title לפי route data.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    // Router עם preloading לביצועים טובים יותר
    provideRouter(
      routes,
      withPreloading(PreloadAllModules)
    ),

    // HttpClient עם:
    // 1. JWT interceptor פונקציונלי (זה מה שתיקן את הבעיה)
    // 2. XSRF Protection
    provideHttpClient(
      withFetch(),
      withInterceptors([jwtInterceptor]),
      withXsrfConfiguration({
        cookieName: 'XSRF-TOKEN',
        headerName: 'X-XSRF-TOKEN'
      })
    ),

    // עדכון document.title אוטומטי לפי route data.title
    { provide: TitleStrategy, useClass: AppTitleStrategy },

    // אנימציות (נדרש ל-PrimeNG)
    provideAnimations()
  ]
};
