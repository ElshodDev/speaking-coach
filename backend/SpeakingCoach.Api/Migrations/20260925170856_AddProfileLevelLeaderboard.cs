using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingCoach.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileLevelLeaderboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Users",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "B1");

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnLeaderboard",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowOnLeaderboard",
                table: "Users");
        }
    }
}
