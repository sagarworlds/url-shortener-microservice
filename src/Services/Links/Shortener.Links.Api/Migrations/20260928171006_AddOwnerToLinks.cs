using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortener.Links.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerToLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "links",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "links");
        }
    }
}
