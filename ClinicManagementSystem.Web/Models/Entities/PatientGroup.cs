using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Models.Entities;

public sealed class PatientGroup
{
    public long Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(10)] public string DiscountType { get; set; } = "Amount";
    public decimal DiscountValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<PatientGroupMember> Members { get; set; } = new List<PatientGroupMember>();
}

public sealed class PatientGroupMember
{
    public long PatientGroupId { get; set; }
    public PatientGroup PatientGroup { get; set; } = null!;
    public long PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
}
