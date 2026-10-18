# MedLoan.Api

שרת ASP.NET Core (.NET 10) לגמ"ח השאלת ציוד רפואי. הלקוח נמצא ב-repo נפרד, `medloan-client`.

## פריסה ל-Render

השרת רץ כ-Web Service מסוג Docker, לפי ה-`Dockerfile` שבשורש ה-repo. בסיס הנתונים הוא Render PostgreSQL.

- ה-image של .NET 10 מאזין לפורט 8080. ה-Dockerfile לא מגדיר `ASPNETCORE_URLS`, כי `ENV` נקבע בזמן הבנייה ולא קורא את `PORT` של Render.
- ה-migrations וה-Seed רצים בעליית השרת, ולכן אין צורך להריץ `dotnet ef` מול Render.
- Render מסיים את ה-HTTPS ב-proxy שלו. השרת סומך על `X-Forwarded-Proto` (`UseForwardedHeaders`) ולא מפנה ל-HTTPS בעצמו.
- Swagger פעיל גם ב-Production, בכתובת `/swagger`.

### משתני סביבה

| שם | ערך |
|---|---|
| `ConnectionStrings__Default` | בפורמט Npgsql (ראו למטה): `Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true` |
| `Jwt__Key` | מחרוזת אקראית ארוכה, שונה מזו של הפיתוח |
| `Cors__AllowedOrigins__0` | כתובת ה-Static Site של הלקוח, למשל `https://<client>.onrender.com` (בלי `/` בסוף) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `PORT` | `8080`. אומר ל-Render לאיזה פורט להפנות את התעבורה |

הערכים האמיתיים נשמרים רק בהגדרות של Render, ולא באף קובץ ב-repo.

### המרת ה-Internal Database URL לפורמט של Npgsql

Render נותן כתובת בפורמט URI, ש-Npgsql לא מקבל:

```
postgresql://<user>:<password>@<host>/<database>
```

מעתיקים כל חלק לשדה המתאים:

```
Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
```

- ב-Internal URL אין פורט, ולכן משתמשים בפורט ברירת המחדל, `5432`.
- אם בסיסמה יש תווים מקודדים, כמו `%40`, מפענחים אותם לתו המקורי (`@`) לפני ההעתקה.

### לפני הדגמה

ב-free tier השרת "נרדם" אחרי זמן בלי בקשות, והבקשה הראשונה אחרי זה איטית (עשרות שניות). לפני הדגמה כדאי לפתוח את Swagger כדי להעיר אותו.

## מגבלות ידועות

- הסיסמאות של משתמשי הדמו (`admin@medloan.local`, `member1@medloan.local`, `member2@medloan.local`) כתובות בקוד וב-README, ולכן הן גלויות גם בפריסה הציבורית. כל מי שיש לו את הכתובת יכול להתחבר כמנהל.
