using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoronelExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddRUCToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sequential",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RUC",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sequential",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RUC",
                table: "Customers");
        }
    }
}
