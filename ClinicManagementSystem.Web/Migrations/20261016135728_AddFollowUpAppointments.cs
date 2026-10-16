using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUpAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FollowUpAppointments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    SaleId = table.Column<long>(type: "bigint", nullable: true),
                    SaleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MedicationDays = table.Column<int>(type: "int", nullable: false),
                    MedicationStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UseCustomDate = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AttendedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttendedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpAppointments", x => x.Id);
                    table.CheckConstraint("CK_FollowUpAppointment_Date", "[AppointmentDate] >= [MedicationStartDate]");
                    table.CheckConstraint("CK_FollowUpAppointment_Days", "[MedicationDays] BETWEEN 1 AND 3650");
                    table.CheckConstraint("CK_FollowUpAppointment_Status", "[Status] IN ('Pending', 'Attended', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_FollowUpAppointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FollowUpAppointments_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpAppointments_AppointmentDate_Status_Id",
                table: "FollowUpAppointments",
                columns: new[] { "AppointmentDate", "Status", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpAppointments_PatientId",
                table: "FollowUpAppointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpAppointments_SaleId",
                table: "FollowUpAppointments",
                column: "SaleId",
                unique: true,
                filter: "[SaleId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FollowUpAppointments");
        }
    }
}
