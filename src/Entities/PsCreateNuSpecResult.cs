namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Entities;

public class PsCreateNuSpecResult {
    public string NuSpecFileFullName { get; set; } = "";
    public List<string> Errors { get; set; } = new();
    public List<string> Infos { get; set; } = new();
}