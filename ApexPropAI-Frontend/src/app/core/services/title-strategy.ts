import { Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

/**
 * AppTitleStrategy — עדכון document.title אוטומטי
 *
 * ❌ מה היה חסר:
 *   כל route הכיל data: { title: '...' } אבל שום דבר לא עדכן
 *   את document.title בפועל. הכרטיסייה בדפדפן תמיד הראתה
 *   את שם ברירת המחדל מ-index.html.
 *
 * ✅ מה זה עושה:
 *   כל פעם שהניווט מסתיים, Angular קורא ל-updateTitle.
 *   אנחנו קוראים את ה-title מה-route data ומעדכנים את הכרטיסייה.
 *
 * נרשם ב-app.config.ts:
 *   { provide: TitleStrategy, useClass: AppTitleStrategy }
 */
@Injectable({ providedIn: 'root' })
export class AppTitleStrategy extends TitleStrategy {
  constructor(private readonly title: Title) {
    super();
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const pageTitle = this.buildTitle(snapshot);
    this.title.setTitle(pageTitle ?? 'ApexProp AI');
  }
}
