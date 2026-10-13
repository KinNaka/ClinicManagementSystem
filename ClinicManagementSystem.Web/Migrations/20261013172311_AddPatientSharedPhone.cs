using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientSharedPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SharedPhoneRelationship",
                table: "Patients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SharedPhoneWithPatientId",
                table: "Patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_SharedPhoneWithPatientId",
                table: "Patients",
                column: "SharedPhoneWithPatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Patients_SharedPhoneWithPatientId",
                table: "Patients",
                column: "SharedPhoneWithPatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Patients_SharedPhoneWithPatientId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_SharedPhoneWithPatientId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "SharedPhoneRelationship",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "SharedPhoneWithPatientId",
                table: "Patients");
        }
    }
}
