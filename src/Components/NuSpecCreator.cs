using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components;

public class NuSpecCreator : INuSpecCreator {
    public async Task<XDocument> CreateNuSpecAsync(string repositoryFolder, string projectFileFullName, string dllFullFileName,
            string organizationUrl, string author, string faviconUrl) {
        var document = new XDocument();
        if (!Directory.Exists(repositoryFolder)) {
            throw new FileNotFoundException(repositoryFolder);
        }
        if (!File.Exists(projectFileFullName)) {
            throw new FileNotFoundException(projectFileFullName);
        }
        if (!projectFileFullName.StartsWith(repositoryFolder)) {
            throw new NotSupportedException("Project file must be underneath the repository folder");
        }
        if (!File.Exists(dllFullFileName)) {
            throw new FileNotFoundException(dllFullFileName);
        }
        if (!dllFullFileName.StartsWith(repositoryFolder)) {
            throw new NotSupportedException("Dll must be underneath the repository folder");
        }

        const string csProjNamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";
        const string nuSpecNamespaceUri = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";

        var namespaceManager = new XmlNamespaceManager(new NameTable());
        namespaceManager.AddNamespace("cp", csProjNamespaceUri);
        namespaceManager.AddNamespace("nu", nuSpecNamespaceUri);

        var projectDocument = XDocument.Load(projectFileFullName);
        var targetFrameworkElement = projectDocument.XPathSelectElements("./Project/PropertyGroup/TargetFramework", namespaceManager).FirstOrDefault();
        if (targetFrameworkElement == null) {
            throw new XmlException("Target framework not found");
        }

        /*
        var namespaceSelector = "";
        var targetFramework = targetFrameworkElement.Value;
        */

        return await Task.FromResult(document);
    }
}