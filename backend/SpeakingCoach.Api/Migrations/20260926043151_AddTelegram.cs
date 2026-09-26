using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingCoach.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramAccounts",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Lang = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    ReminderHour = table.Column<int>(type: "integer", nullable: true),
                    TzOffsetMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastReminderDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LinkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramAccounts", x => x.ChatId);
                    table.ForeignKey(
                        name: "FK_TelegramAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelegramLinkTokens",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Lang = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    TzOffsetMinutes = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramLinkTokens", x => x.TokenHash);
                    table.ForeignKey(
                        name: "FK_TelegramLinkTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramAccounts_UserId",
                table: "TelegramAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLinkTokens_UserId",
                table: "TelegramLinkTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramAccounts");

            migrationBuilder.DropTable(
                name: "TelegramLinkTokens");
        }
    }
}
