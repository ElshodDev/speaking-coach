using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingCoach.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReviewCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Front = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Back = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IntervalDays = table.Column<double>(type: "double precision", nullable: false),
                    Ease = table.Column<double>(type: "double precision", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    Lapses = table.Column<int>(type: "integer", nullable: false),
                    LastReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewCards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewLogs_ReviewCards_CardId",
                        column: x => x.CardId,
                        principalTable: "ReviewCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewCards_UserId_DueAtUtc",
                table: "ReviewCards",
                columns: new[] { "UserId", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewCards_UserId_Front",
                table: "ReviewCards",
                columns: new[] { "UserId", "Front" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLogs_CardId",
                table: "ReviewLogs",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLogs_UserId_ReviewedAtUtc",
                table: "ReviewLogs",
                columns: new[] { "UserId", "ReviewedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewLogs");

            migrationBuilder.DropTable(
                name: "ReviewCards");
        }
    }
}
