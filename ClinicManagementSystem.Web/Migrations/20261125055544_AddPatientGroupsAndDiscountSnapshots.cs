using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientGroupsAndDiscountSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "Sales",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Amount");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "PatientGroupId",
                table: "Sales",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientGroupName",
                table: "Sales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PatientGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiscountType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientGroupMembers",
                columns: table => new
                {
                    PatientGroupId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientGroupMembers", x => new { x.PatientGroupId, x.PatientId });
                    table.ForeignKey(
                        name: "FK_PatientGroupMembers_PatientGroups_PatientGroupId",
                        column: x => x.PatientGroupId,
                        principalTable: "PatientGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientGroupMembers_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_PatientGroupId",
                table: "Sales",
                column: "PatientGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientGroupMembers_PatientId",
                table: "PatientGroupMembers",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientGroups_Name",
                table: "PatientGroups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_PatientGroups_PatientGroupId",
                table: "Sales",
                column: "PatientGroupId",
                principalTable: "PatientGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_PatientGroups_PatientGroupId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "PatientGroupMembers");

            migrationBuilder.DropTable(
                name: "PatientGroups");

            migrationBuilder.DropIndex(
                name: "IX_Sales_PatientGroupId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PatientGroupId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PatientGroupName",
                table: "Sales");
        }
    }
}
