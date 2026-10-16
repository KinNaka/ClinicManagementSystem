using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Models.Entities;

public class FollowUpAppointment
{
    public long Id { get; set; }
    public long PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public long? SaleId { get; set; }
    public Sale? Sale { get; set; }
    [MaxLength(50)] public string SaleCode { get; set; } = "";
    public int MedicationDays { get; set; }
    public DateOnly MedicationStartDate { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public bool UseCustomDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string CreatedBy { get; set; } = "";
    [MaxLength(256)] public string UpdatedBy { get; set; } = "";
    public DateTime? AttendedAtUtc { get; set; }
    [MaxLength(256)] public string? AttendedBy { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
