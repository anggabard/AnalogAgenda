using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateExposureDatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExposureDates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilmId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExposureDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExposureDates_Films_FilmId",
                        column: x => x.FilmId,
                        principalTable: "Films",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExposureDates_FilmId",
                table: "ExposureDates",
                column: "FilmId");

            // Migrate data from Films.ExposureDates JSON column to ExposureDates table
            // This uses SQL Server's JSON functions to parse the JSON array
            // Generate 8-character alphanumeric IDs using GUID and substring
            migrationBuilder.Sql(@"
                INSERT INTO ExposureDates (Id, FilmId, Date, Description, CreatedDate, UpdatedDate)
                SELECT 
                    UPPER(LEFT(REPLACE(REPLACE(REPLACE(REPLACE(CAST(NEWID() AS VARCHAR(36)), '-', ''), '0', 'A'), '1', 'B'), '2', 'C'), 8)) as Id,
                    f.Id as FilmId,
                    CAST(JSON_VALUE(ed.value, '$.Date') AS date) as Date,
                    ISNULL(JSON_VALUE(ed.value, '$.Description'), '') as Description,
                    GETUTCDATE() as CreatedDate,
                    GETUTCDATE() as UpdatedDate
                FROM Films f
                CROSS APPLY OPENJSON(f.ExposureDates) ed
                WHERE f.ExposureDates IS NOT NULL 
                    AND f.ExposureDates != ''
                    AND ISJSON(f.ExposureDates) = 1
                    AND JSON_VALUE(ed.value, '$.Date') IS NOT NULL
            ");

            migrationBuilder.DropTable(
                name: "Keys");

            migrationBuilder.DropColumn(
                name: "ExposureDates",
                table: "Films");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExposureDates");

            migrationBuilder.AddColumn<string>(
                name: "ExposureDates",
                table: "Films",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Keys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keys", x => x.Id);
                });
        }
    }
}
