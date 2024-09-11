namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Entities;

public class Configuration {
    public string CsProjNamespaceUri { get; init; } = "";
    public string NuSpecNamespaceUri { get; init; } = "";
    public string FirstBuildDate { get; init; } = "";
    public string[] ContentIncludeFolders { get; init; } = new string[0];
    public string VersionStartTag { get; init; } = "";
    public string VersionEndTag { get; init; } = "";
}