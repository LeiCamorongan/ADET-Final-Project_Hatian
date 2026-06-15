using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hatian.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidDebtKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaidDebtKeys",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidDebtKeys",
                table: "Events");
        }
    }
}
