using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarshipRegistry.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStarshipIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Starships_Created",
                table: "Starships",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Starships_Name",
                table: "Starships",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Starships_Created",
                table: "Starships");

            migrationBuilder.DropIndex(
                name: "IX_Starships_Name",
                table: "Starships");
        }
    }
}
