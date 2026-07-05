import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * app.routes.server.ts — הגדרת Render Mode לכל Route
 *
 * ❌ מה היה שגוי:
 *   `{ path: '**', renderMode: RenderMode.Prerender }`
 *
 *   Prerender = Angular בונה HTML סטטי מראש בזמן build.
 *   הבעיה: `/property/:id` הוא route דינמי — Angular לא יודע
 *   בזמן build אילו IDs קיימים. זה גרם לשגיאת build:
 *   "Cannot prerender routes with dynamic parameters without providing params"
 *
 * ✅ מה תוקן:
 *   - דף הכניסה `/` → Prerender: אין data דינמי, תמיד אותו HTML.
 *     מהיר יותר, טוב לSEO.
 *
 *   - דף הבית `/home` → Server: תלוי בהתחברות ו-data מה-API.
 *     חייב להיות מוגש חי מה-server.
 *
 *   - דף נכס `/property/:id` → Server: כל ID שונה, data מה-API.
 *     לא ניתן לprerender ללא ידיעת כל IDים מראש.
 *
 *   - Wildcard `**` → Server: כל route לא ידוע מוגש מה-server.
 */
export const serverRoutes: ServerRoute[] = [
  {
    // דף Login: סטטי לחלוטין — מצוין ל-Prerender
    path: '',
    renderMode: RenderMode.Prerender
  },
  {
    // דף הבית: תלוי בAPI → Server rendering
    path: 'home',
    renderMode: RenderMode.Server
  },
  {
    // דף נכס: פרמטר דינמי :id → חייב Server rendering
    path: 'property/:id',
    renderMode: RenderMode.Server
  },
  {
    // ברירת מחדל: כל route אחר → Server
    path: '**',
    renderMode: RenderMode.Server
  }
];
