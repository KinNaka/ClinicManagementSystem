using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientAdministrativeAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProvinceCode",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvinceName",
                table: "Patients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WardCode",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WardName",
                table: "Patients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProvinceCode",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ProvinceName",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "WardCode",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "WardName",
                table: "Patients");
        }
    }
}
