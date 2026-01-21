using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoronelExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceEmailField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceEmail",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceEmail",
                table: "Orders");
        }
    }
}
