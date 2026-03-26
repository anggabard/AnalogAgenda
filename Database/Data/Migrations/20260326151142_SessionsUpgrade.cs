using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Data.Migrations
{
    /// <inheritdoc />
    public partial class SessionsUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Index",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Sessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IdeaSessions",
                columns: table => new
                {
                    IdeaId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaSessions", x => new { x.IdeaId, x.SessionId });
                    table.ForeignKey(
                        name: "FK_IdeaSessions_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdeaSessions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Index",
                table: "Sessions",
                column: "Index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdeaSessions_SessionId",
                table: "IdeaSessions",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdeaSessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_Index",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Index",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Sessions");
        }
    }
}
