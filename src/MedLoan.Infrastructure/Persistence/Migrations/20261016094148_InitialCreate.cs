using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MedLoan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MaxLoanDays = table.Column<int>(type: "integer", nullable: false),
                    RequiresDeposit = table.Column<bool>(type: "boolean", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemCopies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Condition = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCopies_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemCopyId = table.Column<int>(type: "integer", nullable: false),
                    MemberId = table.Column<int>(type: "integer", nullable: false),
                    LoanedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_ItemCopies_ItemCopyId",
                        column: x => x.ItemCopyId,
                        principalTable: "ItemCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loans_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "ציוד לעזרה בהליכה ובתנועה", "ניידות" },
                    { 2, "ציוד לתמיכה בנשימה", "נשימה" },
                    { 3, "מכשירי מדידה ביתיים", "מדידה וניטור" },
                    { 4, "ציוד לרחצה, שירותים ושינה", "רחצה ונוחות" }
                });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "DepositAmount", "Description", "Manufacturer", "MaxLoanDays", "Name", "RequiresDeposit" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 500m, "כיסא גלגלים קל משקל שמתקפל לנשיאה ברכב", null, 60, "כיסא גלגלים מתקפל", true },
                    { 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 400m, "כיסא גלגלים במידות ילדים", null, 60, "כיסא גלגלים לילדים", true },
                    { 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "הליכון מתכוונן לגובה", null, 60, "הליכון", false },
                    { 4, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 200m, "הליכון עם ארבעה גלגלים, בלמים ומושב", null, 60, "רולטור (הליכון עם גלגלים)", true },
                    { 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "זוג קביים מתכווננים מאלומיניום", null, 30, "זוג קביים", false },
                    { 6, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "מקל הליכה מתכוונן עם ידית ארגונומית", null, 30, "מקל הליכה", false },
                    { 7, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000m, "מרכז חמצן ביתי חשמלי", null, 30, "מרכז חמצן", true },
                    { 8, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "מכשיר אינהלציה ביתי עם מסכה", null, 14, "מכשיר אינהלציה", false },
                    { 9, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 300m, "מכשיר שאיבת הפרשות נייד", null, 30, "מכשיר סקשן", true },
                    { 10, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "מכשיר אדים קרים לחדר", null, 14, "מכשיר אדים", false },
                    { 11, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "מד לחץ דם דיגיטלי לזרוע", null, 14, "מד לחץ דם", false },
                    { 12, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "מד ריווי חמצן ודופק לאצבע", null, 14, "מד סטורציה", false },
                    { 13, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "כיסא רחצה מתכוונן עם רגליות מונעות החלקה", null, 30, "כיסא רחצה", false },
                    { 14, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "כיסא שירותים נייד עם דלי נשלף", null, 30, "כיסא שירותים נייד", false },
                    { 15, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1500m, "מיטה סיעודית חשמלית מתכווננת", null, 90, "מיטה סיעודית", true },
                    { 16, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 300m, "מזרן אוויר עם משאבה למניעת פצעי לחץ", null, 90, "מזרן נגד פצעי לחץ", true }
                });

            migrationBuilder.InsertData(
                table: "ItemCopies",
                columns: new[] { "Id", "Condition", "ItemId", "Notes", "SerialNumber", "Status", "Version" },
                values: new object[,]
                {
                    { 1, "Good", 1, null, "WCF-001", "Available", new Guid("00000000-0000-0000-0000-000000000001") },
                    { 2, "New", 2, null, "WCK-001", "Available", new Guid("00000000-0000-0000-0000-000000000002") },
                    { 3, "Good", 2, null, "WCK-002", "Available", new Guid("00000000-0000-0000-0000-000000000003") },
                    { 4, "Good", 3, null, "WLK-001", "Available", new Guid("00000000-0000-0000-0000-000000000004") },
                    { 5, "Good", 3, null, "WLK-002", "Available", new Guid("00000000-0000-0000-0000-000000000005") },
                    { 6, "Fair", 3, null, "WLK-003", "Available", new Guid("00000000-0000-0000-0000-000000000006") },
                    { 7, "New", 3, null, "WLK-004", "Available", new Guid("00000000-0000-0000-0000-000000000007") },
                    { 8, "New", 4, null, "RLT-001", "Available", new Guid("00000000-0000-0000-0000-000000000008") },
                    { 9, "Good", 4, null, "RLT-002", "Available", new Guid("00000000-0000-0000-0000-000000000009") },
                    { 10, "Good", 4, null, "RLT-003", "Available", new Guid("00000000-0000-0000-0000-000000000010") },
                    { 11, "Good", 5, null, "CRT-001", "Available", new Guid("00000000-0000-0000-0000-000000000011") },
                    { 12, "Good", 5, null, "CRT-002", "Available", new Guid("00000000-0000-0000-0000-000000000012") },
                    { 13, "Fair", 5, null, "CRT-003", "Available", new Guid("00000000-0000-0000-0000-000000000013") },
                    { 14, "Fair", 5, null, "CRT-004", "Available", new Guid("00000000-0000-0000-0000-000000000014") },
                    { 15, "Good", 6, null, "CAN-001", "Available", new Guid("00000000-0000-0000-0000-000000000015") },
                    { 16, "New", 6, null, "CAN-002", "Available", new Guid("00000000-0000-0000-0000-000000000016") },
                    { 17, "Good", 6, null, "CAN-003", "Available", new Guid("00000000-0000-0000-0000-000000000017") },
                    { 18, "Good", 7, null, "OXY-001", "Available", new Guid("00000000-0000-0000-0000-000000000018") },
                    { 19, "Good", 7, null, "OXY-002", "Available", new Guid("00000000-0000-0000-0000-000000000019") },
                    { 20, "New", 8, null, "NEB-001", "Available", new Guid("00000000-0000-0000-0000-000000000020") },
                    { 21, "Good", 8, null, "NEB-002", "Available", new Guid("00000000-0000-0000-0000-000000000021") },
                    { 22, "Good", 8, null, "NEB-003", "Available", new Guid("00000000-0000-0000-0000-000000000022") },
                    { 23, "Good", 9, null, "SUC-001", "Available", new Guid("00000000-0000-0000-0000-000000000023") },
                    { 24, "New", 9, null, "SUC-002", "Available", new Guid("00000000-0000-0000-0000-000000000024") },
                    { 25, "Good", 10, null, "HUM-001", "Available", new Guid("00000000-0000-0000-0000-000000000025") },
                    { 26, "Fair", 10, null, "HUM-002", "Available", new Guid("00000000-0000-0000-0000-000000000026") },
                    { 27, "Good", 10, null, "HUM-003", "Available", new Guid("00000000-0000-0000-0000-000000000027") },
                    { 28, "New", 11, null, "BPM-001", "Available", new Guid("00000000-0000-0000-0000-000000000028") },
                    { 29, "Good", 11, null, "BPM-002", "Available", new Guid("00000000-0000-0000-0000-000000000029") },
                    { 30, "Good", 11, null, "BPM-003", "Available", new Guid("00000000-0000-0000-0000-000000000030") },
                    { 31, "New", 12, null, "OXM-001", "Available", new Guid("00000000-0000-0000-0000-000000000031") },
                    { 32, "New", 12, null, "OXM-002", "Available", new Guid("00000000-0000-0000-0000-000000000032") },
                    { 33, "Good", 12, null, "OXM-003", "Available", new Guid("00000000-0000-0000-0000-000000000033") },
                    { 34, "Good", 12, null, "OXM-004", "Available", new Guid("00000000-0000-0000-0000-000000000034") },
                    { 35, "Good", 13, null, "SHC-001", "Available", new Guid("00000000-0000-0000-0000-000000000035") },
                    { 36, "Good", 13, null, "SHC-002", "Available", new Guid("00000000-0000-0000-0000-000000000036") },
                    { 37, "Fair", 13, null, "SHC-003", "Available", new Guid("00000000-0000-0000-0000-000000000037") },
                    { 38, "Good", 14, null, "CMD-001", "Available", new Guid("00000000-0000-0000-0000-000000000038") },
                    { 39, "Good", 14, null, "CMD-002", "Available", new Guid("00000000-0000-0000-0000-000000000039") },
                    { 40, "Good", 15, null, "BED-001", "Available", new Guid("00000000-0000-0000-0000-000000000040") },
                    { 41, "Fair", 15, null, "BED-002", "Available", new Guid("00000000-0000-0000-0000-000000000041") },
                    { 42, "New", 16, null, "MAT-001", "Available", new Guid("00000000-0000-0000-0000-000000000042") },
                    { 43, "Good", 16, null, "MAT-002", "Available", new Guid("00000000-0000-0000-0000-000000000043") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCopies_ItemId",
                table: "ItemCopies",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCopies_SerialNumber",
                table: "ItemCopies",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                table: "Items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ItemCopyId",
                table: "Loans",
                column: "ItemCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_MemberId",
                table: "Loans",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_Email",
                table: "Members",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropTable(
                name: "ItemCopies");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
