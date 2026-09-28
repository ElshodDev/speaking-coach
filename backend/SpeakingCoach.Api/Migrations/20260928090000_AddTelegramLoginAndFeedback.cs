using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingCoach.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramLoginAndFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignupMethod",
                table: "Users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAtUtc",
                table: "TelegramAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Page = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Lang = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelegramLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NonceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PollTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: true),
                    TgUsername = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TgFirstName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Lang = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IsNewUser = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CreatedAtUtc",
                table: "Feedbacks",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_UserId",
                table: "Feedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLogins_CreatedAtUtc",
                table: "TelegramLogins",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLogins_NonceHash",
                table: "TelegramLogins",
                column: "NonceHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLogins_PollTokenHash",
                table: "TelegramLogins",
                column: "PollTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLogins_UserId",
                table: "TelegramLogins",
                column: "UserId");

            // Mavjud hisoblar qanday ochilgani (taxmin): demo manzil — "demo",
            // Telegram orqali (tg-…@telegram.invalid) — "telegram", paroli bor —
            // "email". Qolganlari (Google yoki kod bilan, bazada Google bog'lanishi
            // saqlanmagan) — NULL: statistika ularni "other" deb ko'rsatadi.
            migrationBuilder.Sql(
                """
                UPDATE "Users" SET "SignupMethod" = CASE
                    WHEN "Email" LIKE '%@demo.speakingcoach.invalid' THEN 'demo'
                    WHEN "Email" LIKE '%@telegram.invalid' THEN 'telegram'
                    WHEN "PasswordHash" <> '' THEN 'email'
                    ELSE NULL
                END
                WHERE "SignupMethod" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "TelegramLogins");

            migrationBuilder.DropColumn(
                name: "SignupMethod",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSeenAtUtc",
                table: "TelegramAccounts");
        }
    }
}
