using System.Xml.Linq;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Interfaces;

public interface INuSpecCreator {
    Task<XDocument> CreateNuSpecAsync(string projectFileFullName,
        string organizationUrl, string author, string faviconUrl,
        string checkedOutBranch);
}