# ApexPropAI — פלטפורמת בינה מלאכותית לניתוח נדל"ן

<div align="center">

![.NET](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular_18-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity_Framework-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![Gemini AI](https://img.shields.io/badge/Gemini_AI-8E75B2?style=for-the-badge&logo=googlegemini&logoColor=white)
![Leaflet](https://img.shields.io/badge/Leaflet-199900?style=for-the-badge&logo=leaflet&logoColor=white)
![TailwindCSS](https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=for-the-badge&logo=tailwind-css&logoColor=white)

**כלי חכם לניתוח נכסי נדל"ן בזמן אמת, מבוסס בינה מלאכותית**

</div>

---

## סטטוס הפרויקט

> **הפרויקט נמצא כרגע בפיתוח פעיל.** חלק מהתכונות המתוארות להלן עשויות להיות חלקיות או בשלבי מימוש.

---

## תוכן עניינים

- [על הפרויקט](#על-הפרויקט)
- [תכונות עיקריות](#תכונות-עיקריות)
- [ארכיטקטורה](#ארכיטקטורה)
- [טכנולוגיות](#טכנולוגיות)
- [מודל הנתונים](#מודל-הנתונים)
- [אבטחה](#אבטחה)
- [התקנה והרצה](#התקנה-והרצה)
- [משתני סביבה](#משתני-סביבה)
- [API Reference](#api-reference)
- [נתוני דמו](#נתוני-דמו)
- [רישיון](#רישיון)

---

## על הפרויקט

**ApexPropAI** הוא כלי מקצועי המיועד לאנשי נדל"ן, משקיעים וקונים פרטיים. המערכת משלבת נתוני נדל"ן בזמן אמת עם ניתוח מבוסס Gemini AI, ומאפשרת קבלת החלטות מושכלת ומהירה בעסקאות נדל"ן.

### זרימת המשתמש

```
הרשמה / התחברות  →  עיון בנכסים על מפה  →  שמירת נכסים מועדפים
                                                      |
                          תחזית מחיר עתידית  ←  השוואה בין נכסים  →  דו"ח מקצועי
```

---

## תכונות עיקריות

| תכונה | תיאור | סטטוס |
|-------|-------|-------|
| מפה אינטראקטיבית | צפייה בנכסים על מפה אמיתית עם ציוני AI | זמין |
| ניתוח AI | ציון כדאיות השקעה מבוסס Gemini AI | זמין |
| השוואת נכסים | השוואה חכמה בין מספר נכסים עם גרפים | זמין |
| תחזית מחיר | ניבוי שווי עתידי לפי סטטיסטיקות אזוריות | זמין |
| נכסים מועדפים | שמירה וניהול נכסים מעניינים | זמין |
| דו"ח מקצועי | ייצוא דו"ח מפורט לכל נכס | בפיתוח |
| התראות שינויים | עדכון אוטומטי על שינויים בנכסים | בפיתוח |

---

## ארכיטקטורה

הפרויקט בנוי על פי עקרונות **Clean Architecture** עם הפרדה מלאה בין שכבות:

```
┌─────────────────────────────────────────────────────────────┐
│                        FRONTEND                             │
│  Angular 18 + Angular Material + Tailwind + Leaflet         │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │   Auth   │ │   Map    │ │ Compare  │ │   AI     │       │
│  │ Login /  │ │Leaflet + │ │ نكسים    │ │ Predict  │       │
│  │ Register │ │ Markers  │ │ + Charts │ │ + Score  │       │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘       │
└─────────────────────┬───────────────────────────────────────┘
                      │ HTTPS + JWT Bearer
┌─────────────────────▼───────────────────────────────────────┐
│                      BACKEND — ASP.NET Core 9               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                    ApexProp.API                      │   │
│  │  Controllers: Auth | Properties | AI | SavedProps   │   │
│  │  Middleware: GlobalExceptionHandler | SecurityHeaders│   │
│  └──────────────────────┬───────────────────────────────┘   │
│  ┌──────────────────────▼───────────────────────────────┐   │
│  │                ApexProp.Application                  │   │
│  │  DTOs | AutoMapper Profiles | FluentValidation       │   │
│  └──────────────────────┬───────────────────────────────┘   │
│  ┌──────────────────────▼───────────────────────────────┐   │
│  │                  ApexProp.Domain                     │   │
│  │  Entities | Interfaces | Models                      │   │
│  └──────────────────────┬───────────────────────────────┘   │
│  ┌──────────────────────▼───────────────────────────────┐   │
│  │               ApexProp.Infrastructure                │   │
│  │  Repositories | Services | AppDbContext | Migrations │   │
│  └──────────────────────┬───────────────────────────────┘   │
└─────────────────────────┼───────────────────────────────────┘
                          │ Entity Framework Core
┌─────────────────────────▼───────────────────────────────────┐
│                    SQL Server LocalDB                       │
│  Properties | Users | PropertyImages | PriceHistories       │
│  Locations  | UserFavoriteProperties                        │
└─────────────────────────────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                    External Services                        │
│  Gemini AI API              OpenStreetMap (Overpass API)    │
│  ניתוח נכסים + תחזית        נקודות עניין סביבתיות           │
└─────────────────────────────────────────────────────────────┘
```

### מבנה תיקיות

```
ApexPropAI-Backend/
├── ApexProp.API/                   # שכבת ה-API
│   ├── Controllers/                # Auth, Properties, AI, SavedProperties
│   ├── Middleware/                 # GlobalExceptionHandler, SecurityHeaders
│   ├── Models/                    # ApiResponse, GeminiResponse
│   ├── appsettings.json
│   └── Program.cs
│
├── ApexProp.Application/           # שכבת האפליקציה
│   ├── DTOs/                       # כל ה-Data Transfer Objects
│   ├── Mappings/                   # AutoMapper Profile
│   └── Validators/                 # FluentValidation
│
├── ApexProp.Domain/                # שכבת הדומיין
│   ├── Entities/                   # Property, User, Location, PriceHistory
│   ├── Interfaces/                 # IPropertyRepository, IUserRepository, IAIScoreService
│   └── Models/                    # PagedRequest, PagedResponse, ExternalLocationResult
│
└── ApexProp.Infrastructure/        # שכבת התשתית
    ├── Data/                       # AppDbContext, SeedData
    ├── Migrations/                 # EF Core Migrations
    ├── Repositories/               # PropertyRepository, UserRepository
    └── Services/                   # AIScoreService, JwtService, PasswordService, OpenStreetMapService
```

---

## טכנולוגיות

### Backend

| טכנולוגיה | גרסה | שימוש |
|-----------|------|-------|
| .NET | 9.0 | פלטפורמת הבסיס |
| ASP.NET Core | 9.0 | Web API |
| Entity Framework Core | 9.0 | ORM — Code First |
| SQL Server LocalDB | — | מסד נתונים |
| AutoMapper | 14.0 | מיפוי בין אובייקטים |
| FluentValidation | 12.1 | ולידציה מתקדמת |
| JWT Bearer | 9.0 | אימות והרשאות |
| Gemini AI | API | ניתוח ותחזית |
| OpenStreetMap | Overpass API | נתונים גיאוגרפיים |
| Polly | 8.6 | Resilience & Retry |

### Frontend

| טכנולוגיה | שימוש |
|-----------|-------|
| Angular 18+ | פריימוורק ראשי עם Standalone Components ו-Signals |
| Angular Material 3 | ספריית UI |
| Tailwind CSS | עיצוב |
| PrimeNG | קומפוננטות מתקדמות |
| Leaflet + MarkerCluster | מפות אינטראקטיביות |
| Chart.js | גרפים וויזואליזציה |
| Phosphor Icons | אייקונים |

---

## מודל הנתונים

```
User ──────────────────────────────── Property
 |                                       |
 | (SavedProperties — רבים לרבים)        ├── PropertyImage (One-to-Many)
 |                                       ├── PriceHistory  (One-to-Many)
 └── UserFavoriteProperties (Join Table) └── Location      (One-to-Many)
```

### טבלאות עיקריות

| טבלה | תיאור |
|------|-------|
| Users | משתמשים עם Role (Admin/User) ו-RefreshToken |
| Properties | נכסים עם ציון AI, EstimatedValue ו-AIAnalysisNotes |
| PropertyImages | תמונות מרובות לכל נכס |
| PriceHistories | היסטוריית מחירים לכל נכס |
| Locations | מיקומים סביבתיים — בתי ספר, תחבורה, פארקים |
| UserFavoriteProperties | קשר רבים-לרבים בין משתמשים לנכסים |

---

## אבטחה

| מנגנון | תיאור |
|--------|-------|
| JWT + Refresh Token | Access Token קצר טווח עם Rotation מלא |
| PBKDF2 + SHA256 | הצפנת סיסמאות עם Salt |
| Security Headers | הגנה מפני XSS, Clickjacking, CSRF |
| Global Exception Handler | לא חושף פרטים רגישים ב-Production |
| FluentValidation | ולידציה מקיפה בכל נקודות הקצה |
| Role-Based Authorization | הפרדה בין Admin ו-User |
| User Secrets | מפתחות API מחוץ לקוד לחלוטין |
| Request Size Limit | הגנה מפני DDoS — מגבלת 10MB לבקשה |

---

## התקנה והרצה

### דרישות מקדימות

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- [Node.js 18+](https://nodejs.org/)
- [Angular CLI](https://angular.io/cli)

### Backend

```bash
# שכפול הפרויקט
git clone <repository-url>
cd ApexPropAI-Backend

# הגדרת User Secrets
cd ApexProp.API
dotnet user-secrets set "Jwt:SecretKey" "your-secret-key-minimum-64-characters"
dotnet user-secrets set "GEMINI_API_KEY" "your-gemini-api-key"

# הרצת Migrations ויצירת DB
dotnet ef database update --project ..\ApexProp.Infrastructure --startup-project .

# הרצת השרת
dotnet run
```

השרת יעלה על:
- **HTTPS:** `https://localhost:7215`
- **HTTP:** `http://localhost:5260`

### Frontend

```bash
cd ApexPropAI-Frontend
npm install
ng serve
```

הפרונטאנד יעלה על: `http://localhost:4200`

---

## משתני סביבה

| משתנה | תיאור | מיקום |
|-------|-------|-------|
| `Jwt:SecretKey` | מפתח חתימת JWT — מינימום 64 תווים | User Secrets |
| `GEMINI_API_KEY` | מפתח Gemini AI API | User Secrets |
| `ConnectionStrings:DefaultConnection` | מחרוזת חיבור ל-SQL Server | appsettings.json |

> **חשוב:** לעולם אל תעלה מפתחות API ל-Git. המפתחות נשמרים באמצעות `dotnet user-secrets` בלבד.

---

## API Reference

### Auth — `/api/auth`

| Method | Endpoint | תיאור | Auth |
|--------|----------|-------|------|
| POST | `/register` | הרשמת משתמש חדש | — |
| POST | `/login` | התחברות | — |
| POST | `/refresh` | רענון Access Token | JWT |
| POST | `/logout` | התנתקות וניקוי Token | JWT |
| GET | `/me` | פרטי המשתמש המחובר | JWT |
| PUT | `/me` | עדכון פרופיל | JWT |
| PATCH | `/{id}/role` | עדכון הרשאה | Admin |
| GET | `/` | כל המשתמשים | Admin |

### Properties — `/api/properties`

| Method | Endpoint | תיאור | Auth |
|--------|----------|-------|------|
| GET | `/` | רשימת כל הנכסים עם Pagination | — |
| GET | `/{id}` | נכס לפי ID | — |
| GET | `/search` | חיפוש וסינון מתקדם | — |
| GET | `/area` | נכסים לפי אזור גיאוגרפי | — |
| POST | `/` | יצירת נכס חדש | JWT |
| PUT | `/{id}` | עדכון נכס | JWT |
| DELETE | `/{id}` | מחיקת נכס | JWT |

### AI — `/api/ai`

| Method | Endpoint | תיאור | Auth |
|--------|----------|-------|------|
| POST | `/analyze/{id}` | ניתוח AI לנכס | JWT |
| POST | `/compare` | השוואת נכסים עם AI | JWT |
| POST | `/predict/{id}` | תחזית מחיר עתידית | JWT |

### Saved Properties — `/api/users/saved-properties`

| Method | Endpoint | תיאור | Auth |
|--------|----------|-------|------|
| GET | `/` | נכסים שמורים של המשתמש | JWT |
| POST | `/{propertyId}` | שמירת נכס | JWT |
| DELETE | `/{propertyId}` | הסרת נכס שמור | JWT |

### מבנה תגובת ה-API

כל תגובה מה-API מגיעה בפורמט אחיד:

```json
{
  "success": true,
  "data": {},
  "message": null,
  "errorCode": null,
  "timestamp": "2025-01-01T00:00:00Z"
}
```

---

## נתוני דמו

המערכת כוללת **SeedData** המכיל כ-100 נכסים בירושלים:

- נכסים בשכונות אמיתיות — תלפיות, גאולה, בית הכרם ועוד
- מיקומים סביבתיים — בתי ספר, תחנות רכבת, פארקים, בתי חולים
- ציוני AI מחושבים מראש

הנתונים נטענים אוטומטית בהרצה הראשונה אם מסד הנתונים ריק.

---

## רישיון

&copy; Devoiry Roberts — All Rights Reserved.

אין להעתיק, לשכפל, להפיץ או לשנות את הקוד ללא אישור מפורש בכתב מהמחבר.

---

## פותח על ידי

פרויקט זה פותח כפרויקט גמר אקדמי.
