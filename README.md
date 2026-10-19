# MedLoan.Api

שרת ASP.NET Core (.NET 10) לגמ"ח השאלת ציוד רפואי. הלקוח נמצא ב-repo נפרד, `medloan-client`.

## תיאור המערכת

מנהלי הגמ"ח מנהלים קטלוג של ציוד רפואי: כיסאות גלגלים, הליכונים, קביים, מרכזי חמצן ועוד. הקטלוג מחולק לקטגוריות ולתגיות, ולכל פריט יש עותקים פיזיים עם מספר סידורי. חברים רשומים מתחברים עם JWT ומשאילים עותק זמין. את הציוד מחזירים לגמ"ח, ומנהל מאשר את ההחזרה במערכת. מנהל רואה את כל ההשאלות ומנהל את הפריטים, העותקים, הקטגוריות, התגיות והחברים. בסיס הנתונים הוא PostgreSQL, והגישה אליו דרך EF Core (Code-First).

## המשאב המוגבל והתחרות עליו

המשאב המוגבל הוא **העותק הפיזי** (`ItemCopy`). לכל עותק יכולה להיות לכל היותר השאלה פעילה אחת. התרחיש שצריך למנוע: שני חברים לוחצים "השאל" על העותק האחרון של כיסא גלגלים באותו רגע, ושניהם מקבלים אישור.

### ה-Concurrency token

ב-PostgreSQL אין `rowversion`, ולכן ה-token הוא `Guid Version` שמנוהל ידנית:

- ב-Domain: `public Guid Version { get; set; } = Guid.NewGuid();`, בלי attributes.
- ב-Infrastructure (Fluent API): `builder.Property(c => c.Version).IsConcurrencyToken();`
- ב-`AppDbContext.SaveChangesAsync`: לכל `ItemCopy` במצב `Modified` נוצר `Version` חדש. EF משתמש בערך **המקורי** של `Version` בתנאי ה-WHERE, ובערך החדש ב-SET.

חלופה: `xmin` דרך מאפיין `uint Version` עם `.IsRowVersion()`. ‏`UseXminAsConcurrencyToken()` מיושן.

### הזרימה בהשאלה (`LoanService.CreateAsync`)

1. טוענים את העותק (tracked, כולל `Item`). אם הוא לא קיים: 404.
2. אם העותק לא זמין: Warning בלוג ו-409 עם ההודעה "העותק אינו זמין".
3. אם לחבר יש כבר 3 השאלות פעילות: 400.
4. `copy.CheckOut()` והוספת `Loan`.
5. `SaveChangesAsync` שולח בטרנזקציה אחת:
   ```sql
   UPDATE "ItemCopies" SET ... WHERE "Id" = @id AND "Version" = @old
   INSERT INTO "Loans" ...
   ```
6. אם בינתיים מישהו אחר שינה את העותק, ה-`Version` לא מתאים, ה-UPDATE לא מעדכן אף שורה ו-EF זורק `DbUpdateConcurrencyException`.
   ‏`UnitOfWork.SaveChangesAsync` תופס אותה וזורק `ConcurrencyConflictException`, שמוגדרת ב-Application (כך ה-Service לא מכיר EF).
   ה-Service תופס אותה, כותב **Warning** ללוג ומחזיר **409** עם ההודעה "העותק נתפס על ידי משתמש אחר. נא לנסות שוב."

הבדיקה העסקית (שלב 2) והשמירה (שלב 5) מוגנות יחד: אם מישהו שינה את השורה ביניהן, ה-`Version` לא יתאים.
גם **ההחזרה** מוגנת באותו token: משתי החזרות מקבילות של אותה השאלה, רק אחת עוברת, והשנייה מקבלת 409.

### מה מוכיחה בדיקת שני ה-DbContext

שתי בקשות "במקביל" מהמחשב כמעט תמיד מגיעות בהפרש זמן. הבקשה השנייה קוראת את העותק אחרי שהראשונה כבר נשמרה, ולכן נדחית בשלב 2 ("העותק אינו זמין", 409) ולא דרך ה-`Version`. גם זה 409 נכון.

**ההוכחה שה-`Version` עצמו עובד** היא הבדיקה `ItemCopyConcurrencyTests.SecondSave_WithStaleVersion_Throws`, שמכריחה את שתי הקריאות לקרות לפני השמירה הראשונה:

1. שני `AppDbContext` נפרדים על אותו חיבור SQLite in-memory.
2. שניהם קוראים את אותו עותק, עם אותו `Version`.
3. הראשון משאיל ושומר בהצלחה, וה-`Version` מתחלף.
4. השני משאיל ושומר, ומקבל `DbUpdateConcurrencyException`.

הבדיקה `UnitOfWork_WhenConcurrencyConflict_ThrowsConcurrencyConflictException` מריצה את אותו תרחיש דרך `UnitOfWork`, ובודקת שהחריגה מתורגמת ל-`ConcurrencyConflictException`.

## ארכיטקטורה

Clean Architecture, עם ארבעה פרויקטים ב-`src` ושני פרויקטי בדיקות ב-`tests`:

```
MedLoan.slnx
├── src/
│   ├── MedLoan.Domain           // ליבה: Entities, Enums, כללי דומיין. אפס תלויות
│   ├── MedLoan.Application      // Use cases: Services, Interfaces (ports), DTOs, Mapping, Result
│   ├── MedLoan.Infrastructure   // EF Core, DbContext, Repositories, Migrations, JWT, Hashing
│   └── MedLoan.Api              // Controllers, Middleware, Program.cs, nlog.config
└── tests/
    ├── MedLoan.Application.Tests
    └── MedLoan.Infrastructure.Tests
```

### התאמה לשכבות שבמסמך הדרישות

| במסמך | אצלי | תפקיד |
|---|---|---|
| Core | Domain + Application (Interfaces, DTOs) | ישויות, ממשקים, DTOs, Enums |
| Data | Infrastructure | DbContext, Repositories, Migrations |
| Service | Application (Services) | לוגיקה עסקית |
| API | Api | Controllers, Middleware, Program.cs |

### כיוון התלויות: הכול מצביע פנימה

```
Api ──► Application ──► Domain
 │            ▲
 └──► Infrastructure   (Infrastructure מממש את ה-interfaces של Application)
```

| פרויקט | מפנה אל |
|---|---|
| Domain | אף אחד |
| Application | Domain |
| Infrastructure | Application, Domain |
| Api | Application, Infrastructure (רק לרישום DI) |

- Application מגדיר "מה צריך" (interfaces), ו-Infrastructure מספק "איך". כך ה-Services נבדקים ב-Moq בלי DB.
- ה-Controllers לא מכירים את ה-DbContext ואת ה-Repositories, רק את ה-Services.

## הרצה מקומית

### דרישות מוקדמות

- .NET 10 SDK
- PostgreSQL 16, בהתקנה מקומית או ב-Docker:
  ```bash
  docker run --name pg -e POSTGRES_PASSWORD=<password> -p 5432:5432 -d postgres:16
  ```

### סודות (User Secrets)

ה-connection string ומפתח ה-JWT לא נמצאים באף קובץ ב-repo. בפיתוח שומרים אותם ב-User Secrets. מריצים מתיקיית השורש של ה-repo:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=MedLoanDb;Username=postgres;Password=<password>" --project src/MedLoan.Api
dotnet user-secrets set "Jwt:Key" "<64+ תווים אקראיים>" --project src/MedLoan.Api
```

`Jwt:Key` חייב להיות באורך 32 תווים לפחות, אחרת השרת לא עולה. ליצירת מפתח אקראי:

```bash
openssl rand -base64 64
```

או ב-PowerShell:

```powershell
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

### הרצה

```bash
dotnet run --project src/MedLoan.Api
```

בעלייה השרת מריץ את ה-migrations ואת ה-Seed בעצמו, ולכן אין צורך להריץ `dotnet ef database update` או ליצור נתונים ידנית.

- השרת: `http://localhost:5062`
- Swagger: `http://localhost:5062/swagger`. לבדיקת endpoints מוגנים: מתחברים דרך `POST /api/auth/login`, ומדביקים את ה-token בכפתור "Authorize".

### הלקוח

הלקוח (React) נמצא ב-repo `medloan-client`, וההוראות להרצה שלו ב-README שלו. השרת מתיר CORS בפיתוח רק ל-`http://localhost:5173`, הכתובת שבה הלקוח נפתח.

## אימות (JWT ו-refresh tokens)

| Method | Route | הרשאה | תשובות |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | 201 + `AuthResponse`, 400, 409 |
| POST | `/api/auth/login` | Anonymous | 200 + `AuthResponse`, 400, 401 |
| POST | `/api/auth/refresh` | Anonymous | 200 + `AuthResponse` חדש, 400, 401 |
| POST | `/api/auth/logout` | Anonymous | 204, גם אם ה-token לא קיים |

- `AuthResponse` מכיל `token` (access token, ‏15 דקות), `expiresAt`, `refreshToken` (‏7 ימים), `refreshTokenExpiresAt`, `memberId`, `fullName` ו-`role`.
- גוף הבקשה של refresh ושל logout: `{ "refreshToken": "..." }`.
- **rotation:** כל refresh מבטל את ה-refresh token הישן ומחזיר זוג חדש. שימוש חוזר ב-token שבוטל ב-rotation מבטל את כל ה-tokens הפעילים של החבר, ומחזיר 401.
- **logout** מוחק את ה-refresh token מה-DB. refresh אחרי logout מחזיר 401, בלי לבטל את החיבורים האחרים של החבר.
- ב-DB נשמר רק hash (SHA-256) של ה-refresh token. tokens לא נכתבים ללוג.
- חבר שנחסם מקבל 401 כבר בבקשה הבאה, גם עם access token תקף, כי `OnTokenValidated` בודק `IsActive` ב-DB.

### הרשאות

- כל endpoint דורש התחברות (FallbackPolicy), חוץ מהרשמה, התחברות ו-GET של הקטלוג.
- **Member** משאיל (`POST /api/loans`) ורואה את ההשאלות שלו (`GET /api/members/me/loans`).
- **Admin** מנהל קטלוג, עותקים וחברים, רואה את כל ההשאלות, ומאשר החזרות: `PATCH /api/loans/{id}` הוא Admin בלבד, כי רק הגמ"ח יודע שהציוד חזר בפועל. חבר שמנסה להחזיר מקבל 403 ממנגנון ה-Authorization.
- **הרשאה לפי בעלות:** `GET /api/loans/{id}` מותר לבעלים של ההשאלה או ל-Admin. חבר אחר מקבל 403. הבדיקה נעשית ב-`LoanService`, כי policy של role לא יודעת למי שייכת ההשאלה.

## משתמשי דמו

נוצרים ב-Seed בעליית השרת, אם אין עדיין משתמשים.

| Role | Email | Password |
|---|---|---|
| Admin | admin@medloan.local | Admin123! |
| Member | member1@medloan.local | Member123! |
| Member | member2@medloan.local | Member123! |

לפריט "כיסא גלגלים מתקפל" יש עותק יחיד (`WCF-001`). בעזרתו אפשר להדגים 409: מתחברים כ-member1 וכ-member2 בשני דפדפנים, ושניהם לוחצים "השאל".

## הרצת בדיקות

```bash
dotnet test
```

הבדיקות לא צריכות PostgreSQL או סודות.

### MedLoan.Application.Tests

xUnit v3 ו-Moq. בודק את הלוגיקה העסקית ב-Services, עם mock ל-Repositories, ל-`IUnitOfWork`, ל-`ICurrentUser`, ל-`ITokenService` ול-`IPasswordHasher`. ה-AutoMapper אמיתי ונבנה דרך `AddApplication()`, כמו בשרת, והזמן הוא `FakeTimeProvider`.

- `LoanServiceTests`: השאלה של עותק זמין; דחייה של עותק מושאל; 409 כש-`SaveChangesAsync` זורק `ConcurrencyConflictException`; עותק שלא קיים (404); חבר שהגיע למגבלת 3 ההשאלות (400); צפייה בהשאלה על ידי הבעלים, ו-403 לחבר אחר; החזרה על ידי מנהל; 409 בהחזרה כפולה ובהתנגשות בהחזרה.
- `AuthServiceTests`: הרשמה עם תפקיד Member; email שכבר קיים (409); סיסמה שגויה וחבר חסום (401); התחברות תקינה שמחזירה token, וגם refresh token שנשמר רק כ-hash.
  refresh: token תקין מחזיר זוג חדש ומבטל את הישן; token שכבר בוטל (שימוש חוזר) מבטל את כל ה-tokens הפעילים של החבר ומחזיר 401; token שפג וחבר לא פעיל מחזירים 401. logout מוחק את ה-refresh token, ו-refresh אחריו מחזיר 401 בלי לבטל חיבורים אחרים.
- `ItemCopyTests`: `CheckOut` על עותק מושאל זורק `DomainException`.
- `MappingTests`: `AssertConfigurationIsValid()` על הגדרות ה-AutoMapper.

### MedLoan.Infrastructure.Tests

xUnit v3 מול SQLite in-memory, עם `AppDbContext` אמיתי.

- `ItemCopyConcurrencyTests`: בדיקת שני ה-DbContext (ראו [מה מוכיחה בדיקת שני ה-DbContext](#מה-מוכיחה-בדיקת-שני-ה-dbcontext)), וגם אותו תרחיש דרך `UnitOfWork`.
- `JwtTokenServiceTests`: פענוח token שנוצר ובדיקה שה-claims של המזהה וה-role נמצאים בו.

## מגבלות ידועות

- **access token קצר (15 דקות) ו-refresh token (7 ימים, עם rotation).** את ה-access token עצמו אי אפשר לבטל עד שיפוג, ולכן הוא קצר.
- **חבר שנחסם נחסם מיד.** `OnTokenValidated` בודק `IsActive` ב-DB בכל בקשה, ו-refresh נכשל לחבר חסום.
- **logout** מוחק את ה-refresh token בשרת. access token שכבר ניתן נשאר תקף עד 15 דקות, כי אין רשימה שחורה ל-access tokens.
- **שני refresh מקבילים עם אותו token** עלולים שניהם להצליח, כי אין concurrency token על `RefreshToken`. tokens שפגו לא נמחקים אוטומטית מה-DB.
- **מגבלת 3 ההשאלות לא מוגנת מפני בקשות בו-זמניות.** שתי השאלות של אותו חבר על שני עותקים שונים באותו רגע עלולות לעבור את הבדיקה. ה-`Version` מגן על העותק, לא על ספירת ההשאלות של החבר. פתרון אפשרי: concurrency token גם על `Member`, או טרנזקציה ברמת Serializable.
- **AutoMapper 14.0.0.** זו הגרסה החינמית האחרונה (MIT, בלי מפתח רישיון), ולכן לא מעדכנים ל-15 ומעלה. לגרסה יש פרצת DoS ידועה (רקורסיה במבנה עמוק). ההגנה היא `MaxDepth(5)` על כל המיפויים, ב-`AddApplication()`. הסיכון נמוך ממילא, כי ממפים רק ישויות מה-DB ולא קלט מהמשתמש.
- **סיסמאות הדמו גלויות.** הסיסמאות של משתמשי הדמו (`admin@medloan.local`, `member1@medloan.local`, `member2@medloan.local`) כתובות בקוד וב-README, ולכן הן גלויות גם בפריסה הציבורית. כל מי שיש לו את הכתובת יכול להתחבר כמנהל.
- **בסיס הנתונים ב-Render הוא בתוכנית החינמית,** ותוקפו מוגבל. כשהתוקף פג, Render מוחק את בסיס הנתונים, אלא אם עוברים לתוכנית בתשלום.
- **ERROR בלוג בעלייה הראשונה מול DB ריק.** EF רושם ERROR על טבלת `__EFMigrationsHistory` שעוד לא קיימת, וזו התנהגות רגילה של EF: מיד אחרי זה ה-migrations רצים והשרת עולה.

## קישורים לפריסה

- אתר (לקוח): https://medloan-client.onrender.com
- שרת: https://medloan-server.onrender.com
- Swagger: https://medloan-server.onrender.com/swagger

בגישה הראשונה אחרי זמן בלי שימוש, הטעינה יכולה לקחת עד דקה (ראו [לפני הדגמה](#לפני-הדגמה)).

## פריסה ל-Render

השרת רץ כ-Web Service מסוג Docker, לפי ה-`Dockerfile` שבשורש ה-repo. בסיס הנתונים הוא Render PostgreSQL.

- ה-image של .NET 10 מאזין לפורט 8080. ה-Dockerfile לא מגדיר `ASPNETCORE_URLS`, כי `ENV` נקבע בזמן הבנייה ולא קורא את `PORT` של Render.
- ה-migrations וה-Seed רצים בעליית השרת, ולכן אין צורך להריץ `dotnet ef` מול Render.
- Render מסיים את ה-HTTPS ב-proxy שלו. השרת סומך על `X-Forwarded-Proto` (`UseForwardedHeaders`) ולא מפנה ל-HTTPS בעצמו.
- Swagger פעיל גם ב-Production, בכתובת `/swagger`.

### משתני סביבה

| שם | ערך |
|---|---|
| `ConnectionStrings__Default` | בפורמט Npgsql (ראו למטה): `Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Prefer;Trust Server Certificate=true` |
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
Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Prefer;Trust Server Certificate=true
```

- ב-Internal URL אין פורט, ולכן משתמשים בפורט ברירת המחדל, `5432`.
- אם בסיסמה יש תווים מקודדים, כמו `%40`, מפענחים אותם לתו המקורי (`@`) לפני ההעתקה.

### לפני הדגמה

ב-free tier השרת "נרדם" אחרי זמן בלי בקשות, והבקשה הראשונה אחרי זה איטית (עשרות שניות). לפני הדגמה כדאי לפתוח את Swagger כדי להעיר אותו.
