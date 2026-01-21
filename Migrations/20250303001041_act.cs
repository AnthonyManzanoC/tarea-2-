using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoronelExpress.Migrations
{
    /// <inheritdoc />
    public partial class act : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Se agrega la columna como nullable para evitar conflictos con el valor por defecto 0.
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "TermsAcceptances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TermsAcceptances_CustomerId",
                table: "TermsAcceptances",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_TermsAcceptances_Customers_CustomerId",
                table: "TermsAcceptances",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TermsAcceptances_Customers_CustomerId",
                table: "TermsAcceptances");

            migrationBuilder.DropIndex(
                name: "IX_TermsAcceptances_CustomerId",
                table: "TermsAcceptances");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "TermsAcceptances");
        }
    }
}
