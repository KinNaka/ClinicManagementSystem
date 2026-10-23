using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductEditHistoryAndDraftSaleDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SaleDate",
                table: "Sales",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            // Existing drafts have no business date until confirmation.
            migrationBuilder.Sql("UPDATE Sales SET SaleDate = NULL WHERE Status = 'Draft';");
            // The original stock-out records the successful confirmation time.
            migrationBuilder.Sql(@"UPDATE s SET SaleDate = t.ConfirmedAt
                FROM Sales s INNER JOIN (
                    SELECT ReferenceNumber, MIN(TransactionDate) AS ConfirmedAt
                    FROM InventoryTransactions
                    WHERE TransactionType = 'StockOut' AND Reason = N'Xuất kho bán hàng'
                    GROUP BY ReferenceNumber
                ) t ON s.SaleCode = t.ReferenceNumber
                WHERE s.Status = 'Confirmed';");

            migrationBuilder.CreateTable(
                name: "ProductEditHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEditHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEditHistories_ProductId_ChangedAt_Id",
                table: "ProductEditHistories",
                columns: new[] { "ProductId", "ChangedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Sales SET SaleDate = CreatedAt WHERE SaleDate IS NULL;");

            migrationBuilder.DropTable(
                name: "ProductEditHistories");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SaleDate",
                table: "Sales",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}

