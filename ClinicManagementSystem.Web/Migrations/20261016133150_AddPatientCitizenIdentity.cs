using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientCitizenIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CitizenIdIssueDate",
                table: "Patients",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CitizenIdNumber",
                table: "Patients",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CitizenIdIssueDate",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CitizenIdNumber",
                table: "Patients");
        }
    }
}
