using System.Xml.Linq;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Interfaces;

public interface INuSpecCreator {
    Task<XDocument> CreateNuSpecAsync(string repositoryFolder, string projectFileFullName, string dllFullFileName,
        string organizationUrl, string author, string faviconUrl);
}