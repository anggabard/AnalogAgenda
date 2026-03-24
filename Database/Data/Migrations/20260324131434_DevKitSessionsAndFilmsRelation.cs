using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Data.Migrations
{
    /// <inheritdoc />
    public partial class DevKitSessionsAndFilmsRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "Ideas",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CostCurrency",
                table: "Films",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "RON");

            migrationBuilder.CreateTable(
                name: "DevKitFilms",
                columns: table => new
                {
                    DevKitId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    FilmId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevKitFilms", x => new { x.DevKitId, x.FilmId });
                    table.ForeignKey(
                        name: "FK_DevKitFilms_DevKits_DevKitId",
                        column: x => x.DevKitId,
                        principalTable: "DevKits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevKitFilms_Films_FilmId",
                        column: x => x.FilmId,
                        principalTable: "Films",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DevKitSessions",
                columns: table => new
                {
                    DevKitId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevKitSessions", x => new { x.DevKitId, x.SessionId });
                    table.ForeignKey(
                        name: "FK_DevKitSessions_DevKits_DevKitId",
                        column: x => x.DevKitId,
                        principalTable: "DevKits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevKitSessions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdeaPhotos",
                columns: table => new
                {
                    IdeaId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PhotoId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaPhotos", x => new { x.IdeaId, x.PhotoId });
                    table.ForeignKey(
                        name: "FK_IdeaPhotos_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdeaPhotos_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevKitFilms_FilmId",
                table: "DevKitFilms",
                column: "FilmId");

            migrationBuilder.CreateIndex(
                name: "IX_DevKitSessions_SessionId",
                table: "DevKitSessions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaPhotos_PhotoId",
                table: "IdeaPhotos",
                column: "PhotoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevKitFilms");

            migrationBuilder.DropTable(
                name: "DevKitSessions");

            migrationBuilder.DropTable(
                name: "IdeaPhotos");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "CostCurrency",
                table: "Films");
        }
    }
}
