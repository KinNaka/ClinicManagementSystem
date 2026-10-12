using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicManagementSystem.Web.Services;

public sealed class VietnamAddressService
{
    public IReadOnlyList<AddressProvince> Provinces { get; }
    public VietnamAddressService(IWebHostEnvironment environment)
    {
        var json = File.ReadAllText(Path.Combine(environment.ContentRootPath, "Data", "vietnam-addresses.json"));
        Provinces = JsonSerializer.Deserialize<List<AddressProvince>>(json)!;
    }
    public AddressProvince? FindProvince(int? code) => Provinces.FirstOrDefault(p => p.Code == code);
}
public sealed class AddressProvince
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("wards")] public List<AddressWard> Wards { get; set; } = new();
}
public sealed class AddressWard
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}
