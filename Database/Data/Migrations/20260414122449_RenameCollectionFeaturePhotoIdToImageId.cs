using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameCollectionFeaturePhotoIdToImageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collections_Photos_FeaturedPhotoId",
                table: "Collections");

            migrationBuilder.DropIndex(
                name: "IX_Collections_FeaturedPhotoId",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "FeaturedPhotoId",
                table: "Collections");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageId",
                table: "Collections",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Constants.DefaultCollectionImageId);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "Collections");

            migrationBuilder.AddColumn<string>(
                name: "FeaturedPhotoId",
                table: "Collections",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_FeaturedPhotoId",
                table: "Collections",
                column: "FeaturedPhotoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collections_Photos_FeaturedPhotoId",
                table: "Collections",
                column: "FeaturedPhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
