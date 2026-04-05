using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarshipRegistry.Migrations
{
    /// <inheritdoc />
    public partial class AddTimestampsToVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Starships",
                table: "Starships");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Starships");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Edited",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Starships",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Starships",
                table: "Starships",
                column: "Url");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Starships",
                table: "Starships");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Edited",
                table: "Vehicles");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Starships",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Starships",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Starships",
                table: "Starships",
                column: "Id");
        }
    }
}
