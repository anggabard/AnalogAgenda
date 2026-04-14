using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateCollectionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FeaturedPhotoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collections_Photos_FeaturedPhotoId",
                        column: x => x.FeaturedPhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CollectionPhotos",
                columns: table => new
                {
                    CollectionsId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PhotosId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionPhotos", x => new { x.CollectionsId, x.PhotosId });
                    table.ForeignKey(
                        name: "FK_CollectionPhotos_Collections_CollectionsId",
                        column: x => x.CollectionsId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionPhotos_Photos_PhotosId",
                        column: x => x.PhotosId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPhotos_PhotosId",
                table: "CollectionPhotos",
                column: "PhotosId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_FeaturedPhotoId",
                table: "Collections",
                column: "FeaturedPhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_IsOpen",
                table: "Collections",
                column: "IsOpen");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_Owner",
                table: "Collections",
                column: "Owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionPhotos");

            migrationBuilder.DropTable(
                name: "Collections");
        }
    }
}
