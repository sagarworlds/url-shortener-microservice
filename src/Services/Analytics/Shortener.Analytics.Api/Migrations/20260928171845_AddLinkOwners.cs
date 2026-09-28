using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortener.Analytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "link_owners",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_link_owners", x => x.Code);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "link_owners");
        }
    }
}
