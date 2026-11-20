using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingCoach.Api.Migrations
{
    /// <summary>
    /// Taxalluslar katta-kichik harfsiz noyob: lower("DisplayName") bo'yicha unique indeks.
    /// Kodda tekshiruv bor, lekin parallel saqlashlar uni aylanib o'tardi.
    /// Bazada allaqachon takrorlar bo'lsa — migratsiya YIQILMAYDI, indeks yaratilmaydi
    /// (logga NOTICE): takrorlarni qo'lda tuzatib, migratsiyani qayta qo'llash mumkin.
    /// Model (EF) o'zgarmaydi — indeks ifodali, shuning uchun faqat SQL.
    /// </summary>
    public partial class AddNicknameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "Users"
                        WHERE "DisplayName" IS NOT NULL
                        GROUP BY lower("DisplayName")
                        HAVING count(*) > 1
                    ) THEN
                        RAISE NOTICE 'IX_Users_DisplayName_Lower yaratilmadi: takroriy taxalluslar bor';
                    ELSE
                        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_DisplayName_Lower"
                            ON "Users" (lower("DisplayName"))
                            WHERE "DisplayName" IS NOT NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Users_DisplayName_Lower";""");
        }
    }
}
