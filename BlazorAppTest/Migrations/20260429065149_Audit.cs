using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorAppTest.Migrations
{
    /// <inheritdoc />
    public partial class Audit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_ReferenceBase_Id",
                schema: "test",
                table: "Units");

            migrationBuilder.DropTable(
                name: "ReferenceBase",
                schema: "test");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "test",
                table: "Units",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "test",
                table: "Units",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "test",
                table: "Units",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "test",
                table: "Units",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "test",
                table: "Units",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "test",
                table: "Units",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "test",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangesJson = table.Column<string>(type: "jsonb", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Units_DeletedAt",
                schema: "test",
                table: "Units",
                column: "DeletedAt",
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "test");

            migrationBuilder.DropIndex(
                name: "IX_Units_DeletedAt",
                schema: "test",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "test",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "test",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "test",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "test",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "test",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "test",
                table: "Units");

            migrationBuilder.CreateTable(
                name: "ReferenceBase",
                schema: "test",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceBase", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Units_ReferenceBase_Id",
                schema: "test",
                table: "Units",
                column: "Id",
                principalSchema: "test",
                principalTable: "ReferenceBase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
