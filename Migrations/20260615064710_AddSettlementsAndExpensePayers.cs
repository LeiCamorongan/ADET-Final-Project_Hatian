using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hatian.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementsAndExpensePayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountOwed",
                table: "ExpenseSplits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ExpenseSplits",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ExpensePayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpensePayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpensePayers_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExpensePayers_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DebtorParticipantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditorParticipantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settlements_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Settlements_Participants_CreditorParticipantId",
                        column: x => x.CreditorParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Settlements_Participants_DebtorParticipantId",
                        column: x => x.DebtorParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayers_ExpenseId",
                table: "ExpensePayers",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayers_ParticipantId",
                table: "ExpensePayers",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_CreditorParticipantId",
                table: "Settlements",
                column: "CreditorParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_DebtorParticipantId",
                table: "Settlements",
                column: "DebtorParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_EventId",
                table: "Settlements",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpensePayers");

            migrationBuilder.DropTable(
                name: "Settlements");

            migrationBuilder.DropColumn(
                name: "AmountOwed",
                table: "ExpenseSplits");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ExpenseSplits");
        }
    }
}
