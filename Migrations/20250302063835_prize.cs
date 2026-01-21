using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoronelExpress.Migrations
{
    /// <inheritdoc />
    public partial class prize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Prize",
                table: "Promotions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prize",
                table: "Promotions");
        }
    }
}
