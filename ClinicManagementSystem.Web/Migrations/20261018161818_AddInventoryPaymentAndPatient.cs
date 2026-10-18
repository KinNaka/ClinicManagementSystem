using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryPaymentAndPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "InventoryTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaidBy",
                table: "InventoryTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PatientId",
                table: "InventoryTransactions",
                type: "bigint",
                nullable: true);

            // Preserve the patient link for existing invoice stock movements, even if the invoice is deleted later.
            migrationBuilder.Sql(@"UPDATE t SET PatientId = s.PatientId
                FROM InventoryTransactions t INNER JOIN Sales s ON t.ReferenceNumber = s.SaleCode
                WHERE t.TransactionType = 'StockOut' AND t.Reason = N'Xuất kho bán hàng';");
            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PatientId",
                table: "InventoryTransactions",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Patients_PatientId",
                table: "InventoryTransactions",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Patients_PatientId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_PatientId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PaidBy",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "InventoryTransactions");
        }
    }
}

