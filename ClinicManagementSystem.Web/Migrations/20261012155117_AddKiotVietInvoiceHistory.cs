using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddKiotVietInvoiceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAt",
                table: "Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ImportedDiscount",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ImportedFileName",
                table: "Sales",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ImportedGoodsTotal",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ImportedSurcharge",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SourcePatientCode",
                table: "Sales",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourcePatientName",
                table: "Sales",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KiotVietInfo",
                table: "SaleDetails",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceProductCode",
                table: "SaleDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceProductName",
                table: "SaleDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUnit",
                table: "SaleDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ImportedDiscount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ImportedFileName",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ImportedGoodsTotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ImportedSurcharge",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SourcePatientCode",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SourcePatientName",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "KiotVietInfo",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "SourceProductCode",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "SourceProductName",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "SourceUnit",
                table: "SaleDetails");
        }
    }
}
