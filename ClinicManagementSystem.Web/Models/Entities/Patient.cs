using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Models.Entities
{
    public class Patient
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string PatientCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        public long? SharedPhoneWithPatientId { get; set; }
        public Patient? SharedPhoneWithPatient { get; set; }
        [MaxLength(50)]
        public string? SharedPhoneRelationship { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        public int? ProvinceCode { get; set; }
        public int? WardCode { get; set; }
        [StringLength(100)]
        public string? ProvinceName { get; set; }
        [StringLength(100)]
        public string? WardName { get; set; }

        [MaxLength(100)]
        public string? Occupation { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public decimal LegacyTotalSales { get; set; }

        public DateTime? LegacyImportedAt { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
