using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderingToCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilmId",
                table: "CollectionPhotos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Index",
                table: "CollectionPhotos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPhotos_CollectionsId_Index",
                table: "CollectionPhotos",
                columns: new[] { "CollectionsId", "Index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CollectionPhotos_CollectionsId_Index",
                table: "CollectionPhotos");
            migrationBuilder.DropColumn(
                name: "FilmId",
                table: "CollectionPhotos");

            migrationBuilder.DropColumn(
                name: "Index",
                table: "CollectionPhotos");
        }
    }
}
