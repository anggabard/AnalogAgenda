using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Data.Migrations;

/// <inheritdoc />
public partial class RemoveKeysTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Keys");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Keys",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                Key = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Keys", x => x.Id);
            });
    }
}

