using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserSettingsAndMigrateIsSubscribed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsSubscribed = table.Column<bool>(type: "bit", nullable: false),
                    CurrentFilmId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TableView = table.Column<bool>(type: "bit", nullable: false),
                    EntitiesPerPage = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_Films_CurrentFilmId",
                        column: x => x.CurrentFilmId,
                        principalTable: "Films",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_CurrentFilmId",
                table: "UserSettings",
                column: "CurrentFilmId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);

            // Migrate IsSubscribed data from Users to UserSettings
            // Generate 10-character alphanumeric IDs using GUID and substring
            migrationBuilder.Sql(@"
                INSERT INTO UserSettings (Id, UserId, IsSubscribed, TableView, EntitiesPerPage, CreatedDate, UpdatedDate)
                SELECT 
                    UPPER(LEFT(REPLACE(REPLACE(REPLACE(REPLACE(CAST(NEWID() AS VARCHAR(36)), '-', ''), '0', 'A'), '1', 'B'), '2', 'C'), 10)) as Id,
                    u.Id as UserId,
                    u.IsSubscribed,
                    0 as TableView,
                    5 as EntitiesPerPage,
                    GETUTCDATE() as CreatedDate,
                    GETUTCDATE() as UpdatedDate
                FROM Users u
            ");

            migrationBuilder.DropColumn(
                name: "IsSubscribed",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscribed",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
