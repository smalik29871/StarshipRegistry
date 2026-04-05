using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarshipRegistry.Migrations
{
    /// <inheritdoc />
    public partial class AddUrlToStarship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Starships",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "Starships");
        }
    }
}
