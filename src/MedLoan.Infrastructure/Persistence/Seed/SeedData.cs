using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Persistence.Seed;

public static class SeedData
{
    // HasData requires fixed values, so nothing here may come from DateTime.UtcNow or Guid.NewGuid().
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "ניידות", Description = "ציוד לעזרה בהליכה ובתנועה" },
            new Category { Id = 2, Name = "נשימה", Description = "ציוד לתמיכה בנשימה" },
            new Category { Id = 3, Name = "מדידה וניטור", Description = "מכשירי מדידה ביתיים" },
            new Category { Id = 4, Name = "רחצה ונוחות", Description = "ציוד לרחצה, שירותים ושינה" });

        modelBuilder.Entity<Item>().HasData(
            Item(1, 1, "כיסא גלגלים מתקפל", "כיסא גלגלים קל משקל שמתקפל לנשיאה ברכב", 60, 500m),
            Item(2, 1, "כיסא גלגלים לילדים", "כיסא גלגלים במידות ילדים", 60, 400m),
            Item(3, 1, "הליכון", "הליכון מתכוונן לגובה", 60, null),
            Item(4, 1, "רולטור (הליכון עם גלגלים)", "הליכון עם ארבעה גלגלים, בלמים ומושב", 60, 200m),
            Item(5, 1, "זוג קביים", "זוג קביים מתכווננים מאלומיניום", 30, null),
            Item(6, 1, "מקל הליכה", "מקל הליכה מתכוונן עם ידית ארגונומית", 30, null),
            Item(7, 2, "מרכז חמצן", "מרכז חמצן ביתי חשמלי", 30, 1000m),
            Item(8, 2, "מכשיר אינהלציה", "מכשיר אינהלציה ביתי עם מסכה", 14, null),
            Item(9, 2, "מכשיר סקשן", "מכשיר שאיבת הפרשות נייד", 30, 300m),
            Item(10, 2, "מכשיר אדים", "מכשיר אדים קרים לחדר", 14, null),
            Item(11, 3, "מד לחץ דם", "מד לחץ דם דיגיטלי לזרוע", 14, null),
            Item(12, 3, "מד סטורציה", "מד ריווי חמצן ודופק לאצבע", 14, null),
            Item(13, 4, "כיסא רחצה", "כיסא רחצה מתכוונן עם רגליות מונעות החלקה", 30, null),
            Item(14, 4, "כיסא שירותים נייד", "כיסא שירותים נייד עם דלי נשלף", 30, null),
            Item(15, 4, "מיטה סיעודית", "מיטה סיעודית חשמלית מתכווננת", 90, 1500m),
            Item(16, 4, "מזרן נגד פצעי לחץ", "מזרן אוויר עם משאבה למניעת פצעי לחץ", 90, 300m));

        modelBuilder.Entity<ItemCopy>().HasData(
            Copy(1, 1, "WCF-001", CopyCondition.Good),

            Copy(2, 2, "WCK-001", CopyCondition.New),
            Copy(3, 2, "WCK-002", CopyCondition.Good),

            Copy(4, 3, "WLK-001", CopyCondition.Good),
            Copy(5, 3, "WLK-002", CopyCondition.Good),
            Copy(6, 3, "WLK-003", CopyCondition.Fair),
            Copy(7, 3, "WLK-004", CopyCondition.New),

            Copy(8, 4, "RLT-001", CopyCondition.New),
            Copy(9, 4, "RLT-002", CopyCondition.Good),
            Copy(10, 4, "RLT-003", CopyCondition.Good),

            Copy(11, 5, "CRT-001", CopyCondition.Good),
            Copy(12, 5, "CRT-002", CopyCondition.Good),
            Copy(13, 5, "CRT-003", CopyCondition.Fair),
            Copy(14, 5, "CRT-004", CopyCondition.Fair),

            Copy(15, 6, "CAN-001", CopyCondition.Good),
            Copy(16, 6, "CAN-002", CopyCondition.New),
            Copy(17, 6, "CAN-003", CopyCondition.Good),

            Copy(18, 7, "OXY-001", CopyCondition.Good),
            Copy(19, 7, "OXY-002", CopyCondition.Good),

            Copy(20, 8, "NEB-001", CopyCondition.New),
            Copy(21, 8, "NEB-002", CopyCondition.Good),
            Copy(22, 8, "NEB-003", CopyCondition.Good),

            Copy(23, 9, "SUC-001", CopyCondition.Good),
            Copy(24, 9, "SUC-002", CopyCondition.New),

            Copy(25, 10, "HUM-001", CopyCondition.Good),
            Copy(26, 10, "HUM-002", CopyCondition.Fair),
            Copy(27, 10, "HUM-003", CopyCondition.Good),

            Copy(28, 11, "BPM-001", CopyCondition.New),
            Copy(29, 11, "BPM-002", CopyCondition.Good),
            Copy(30, 11, "BPM-003", CopyCondition.Good),

            Copy(31, 12, "OXM-001", CopyCondition.New),
            Copy(32, 12, "OXM-002", CopyCondition.New),
            Copy(33, 12, "OXM-003", CopyCondition.Good),
            Copy(34, 12, "OXM-004", CopyCondition.Good),

            Copy(35, 13, "SHC-001", CopyCondition.Good),
            Copy(36, 13, "SHC-002", CopyCondition.Good),
            Copy(37, 13, "SHC-003", CopyCondition.Fair),

            Copy(38, 14, "CMD-001", CopyCondition.Good),
            Copy(39, 14, "CMD-002", CopyCondition.Good),

            Copy(40, 15, "BED-001", CopyCondition.Good),
            Copy(41, 15, "BED-002", CopyCondition.Fair),

            Copy(42, 16, "MAT-001", CopyCondition.New),
            Copy(43, 16, "MAT-002", CopyCondition.Good));

        // Added in the AddTags migration. The item-tag links are seeded in ItemConfiguration (UsingEntity).
        modelBuilder.Entity<Tag>().HasData(
            new Tag { Id = 1, Name = "מתקפל" },
            new Tag { Id = 2, Name = "דורש חשמל" },
            new Tag { Id = 3, Name = "מידת ילדים" },
            new Tag { Id = 4, Name = "נייד" });
    }

    private static Item Item(int id, int categoryId, string name, string description, int maxLoanDays, decimal? deposit) => new()
    {
        Id = id,
        CategoryId = categoryId,
        Name = name,
        Description = description,
        MaxLoanDays = maxLoanDays,
        RequiresDeposit = deposit is not null,
        DepositAmount = deposit,
        CreatedAt = CreatedAt
    };

    // Version is derived from the copy Id so it stays identical across model builds.
    private static ItemCopy Copy(int id, int itemId, string serialNumber, CopyCondition condition) => new()
    {
        Id = id,
        ItemId = itemId,
        SerialNumber = serialNumber,
        Status = CopyStatus.Available,
        Condition = condition,
        Version = new Guid($"00000000-0000-0000-0000-{id:D12}")
    };
}
