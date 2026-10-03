using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorialPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnologyDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "Technologies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "programacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Domain",
                table: "Technologies");
        }
    }
}
