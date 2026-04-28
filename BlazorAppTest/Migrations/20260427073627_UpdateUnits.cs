using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorAppTest.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectStatuses",
                schema: "test");

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomaticArchiving",
                schema: "test",
                table: "StorageUnits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PositionUnits",
                schema: "test",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsMultiple = table.Column<bool>(type: "boolean", nullable: false),
                    OrderNo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionUnits_Units_Id",
                        column: x => x.Id,
                        principalSchema: "test",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PositionUnits",
                schema: "test");

            migrationBuilder.DropColumn(
                name: "IsAutomaticArchiving",
                schema: "test",
                table: "StorageUnits");

            migrationBuilder.CreateTable(
                name: "ProjectStatuses",
                schema: "test",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    ColorHex = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStatuses", x => x.Id);
                });
        }
    }
}
