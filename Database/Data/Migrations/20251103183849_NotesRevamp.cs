using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Data.Migrations
{
    /// <inheritdoc />
    public partial class NotesRevamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Film",
                table: "NoteEntries");

            migrationBuilder.RenameColumn(
                name: "Process",
                table: "NoteEntries",
                newName: "Step");

            migrationBuilder.AddColumn<int>(
                name: "Index",
                table: "NoteEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TemperatureMax",
                table: "NoteEntries",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TemperatureMin",
                table: "NoteEntries",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "NoteEntryOverrides",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NoteEntryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilmCountMin = table.Column<int>(type: "int", nullable: false),
                    FilmCountMax = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<double>(type: "float", nullable: true),
                    Step = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TemperatureMin = table.Column<double>(type: "float", nullable: true),
                    TemperatureMax = table.Column<double>(type: "float", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteEntryOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteEntryOverrides_NoteEntries_NoteEntryId",
                        column: x => x.NoteEntryId,
                        principalTable: "NoteEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoteEntryRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NoteEntryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilmInterval = table.Column<int>(type: "int", nullable: false),
                    TimeIncrement = table.Column<double>(type: "float", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteEntryRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteEntryRules_NoteEntries_NoteEntryId",
                        column: x => x.NoteEntryId,
                        principalTable: "NoteEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoteEntryOverrides_NoteEntryId",
                table: "NoteEntryOverrides",
                column: "NoteEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteEntryRules_NoteEntryId",
                table: "NoteEntryRules",
                column: "NoteEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoteEntryOverrides");

            migrationBuilder.DropTable(
                name: "NoteEntryRules");

            migrationBuilder.DropColumn(
                name: "Index",
                table: "NoteEntries");

            migrationBuilder.DropColumn(
                name: "TemperatureMax",
                table: "NoteEntries");

            migrationBuilder.DropColumn(
                name: "TemperatureMin",
                table: "NoteEntries");

            migrationBuilder.RenameColumn(
                name: "Step",
                table: "NoteEntries",
                newName: "Process");

            migrationBuilder.AddColumn<string>(
                name: "Film",
                table: "NoteEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
