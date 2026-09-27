using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingCoach.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMockAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BotState",
                table: "TelegramAccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MockTests",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "published");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "MockTests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotState",
                table: "TelegramAccounts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MockTests");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "MockTests");
        }
    }
}
