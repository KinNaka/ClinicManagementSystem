using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Web.Models.Entities;

public class ProductEditHistory
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    [MaxLength(100)] public string ProductCode { get; set; } = "";
    [MaxLength(500)] public string ProductName { get; set; } = "";
    [MaxLength(450)] public string? ActorId { get; set; }
    [MaxLength(256)] public string? ActorName { get; set; }
    public DateTime ChangedAt { get; set; }
    [MaxLength(100)] public string FieldName { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
