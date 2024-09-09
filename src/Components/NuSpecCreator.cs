using System.Xml.Linq;
using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components;

public class NuSpecCreator : INuSpecCreator {
    public Task<XDocument> CreateNuSpecAsync(string repositoryFolder, string projectFileFullName, string dllFullFileName, string organizationUrl, string author, string faviconUrl) {
        throw new NotImplementedException();
    }
}