using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingCoach.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddShadowingQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Shadowing",
                table: "AiUsages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Shadowing",
                table: "AiUsages");
        }
    }
}
