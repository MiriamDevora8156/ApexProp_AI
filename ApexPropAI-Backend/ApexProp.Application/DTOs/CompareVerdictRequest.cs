// CompareVerdictRequest.cs
namespace ApexProp.Application.DTOs;

public class CompareVerdictRequest
{
    public List<int> PropertyIds { get; set; } = new();
    public string Goal { get; set; } = "";
    public string Horizon { get; set; } = "";
    public string Budget { get; set; } = "";
}