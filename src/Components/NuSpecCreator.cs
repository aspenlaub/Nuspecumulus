using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components;

public class NuSpecCreator : INuSpecCreator {
    public async Task<XDocument> CreateNuSpecAsync(string repositoryFolder, string projectFileFullName,
            string organizationUrl, string author, string faviconUrl) {
        if (!Directory.Exists(repositoryFolder)) {
            throw new FileNotFoundException(repositoryFolder);
        }
        if (!File.Exists(projectFileFullName)) {
            throw new FileNotFoundException(projectFileFullName);
        }
        if (!projectFileFullName.StartsWith(repositoryFolder)) {
            throw new NotSupportedException("Project file must be underneath the repository folder");
        }

        var configuration = JsonSerializer.Deserialize<Entities.Configuration>(await File.ReadAllTextAsync("settings.json"));
        if (configuration == null) {
            throw new InvalidDataException("Settings file not found or corrupt");
        }

        var namespaceManager = new XmlNamespaceManager(new NameTable());
        namespaceManager.AddNamespace("cp", configuration.CsProjNamespaceUri);
        namespaceManager.AddNamespace("nu", configuration.NuSpecNamespaceUri);
        XNamespace nugetNamespace = configuration.NuSpecNamespaceUri;

        var projectDocument = XDocument.Load(projectFileFullName);
        var targetFrameworkElement = projectDocument.XPathSelectElements("./Project/PropertyGroup/TargetFramework", namespaceManager).FirstOrDefault();
        if (targetFrameworkElement == null) {
            throw new XmlException("Target framework not found");
        }

        var dependencyIdsAndVersions = new Dictionary<string, string>();
        foreach (var element in projectDocument.XPathSelectElements("/Project/ItemGroup/PackageReference", namespaceManager)) {
            var packageId = element.Attribute("Include")?.Value;
            if (string.IsNullOrEmpty(packageId)) {
                continue;
            }

            var packageVersion = element.Attribute("Version")?.Value;
            if (string.IsNullOrEmpty(packageVersion)) {
                packageVersion = element.XPathSelectElement("./Version", namespaceManager)?.Value;
            }

            if (packageVersion == null) { continue; }
            if (dependencyIdsAndVersions.ContainsKey(packageId)) { continue; }

            dependencyIdsAndVersions[packageId] = packageVersion;
        }

        var solutionId = projectFileFullName.Substring(projectFileFullName.LastIndexOf('\\') + 1).Replace(".csproj", "");

        var document = new XDocument();

        var versionFile = projectFileFullName.Substring(0, projectFileFullName.LastIndexOf('\\') + 1) + "version.json";
        if (!File.Exists(versionFile)) {
            return document;
        }
        var version = JsonSerializer.Deserialize<Entities.Version>(await File.ReadAllTextAsync(versionFile));
        if (version == null) {
            return document;
        }
        version.Build = DateTime.UtcNow.Subtract(DateTime.Parse(configuration.FirstBuildDate)).Days;
        version.Revision = (int)Math.Floor(DateTime.UtcNow.Subtract(DateTime.UtcNow.Date).TotalMinutes);

        var docElement = new XElement(nugetNamespace + "package");
        var metaData = ReadMetaData(solutionId, "master", projectDocument, dependencyIdsAndVersions, new List<string>(),
            version, targetFrameworkElement.Value, organizationUrl, author, faviconUrl, namespaceManager, nugetNamespace);
        if (metaData == null) { return document; }

        docElement.Add(metaData);
        var files = Files(projectDocument, namespaceManager, nugetNamespace, configuration);
        if (files == null) { return document; }

        docElement.Add(files);
        document.Add(docElement);

        return await Task.FromResult(document);
    }

    protected XElement? ReadMetaData(string solutionId, string checkedOutBranch, XDocument projectDocument,
            IDictionary<string, string> dependencyIdsAndVersions, IList<string> tags,
            Entities.Version version, string targetFramework, string organizationUrl, string author, string faviconUrl,
            XmlNamespaceManager namespaceManager, XNamespace nugetNamespace) {
        var rootNamespaceElement = projectDocument.XPathSelectElements("./Project/PropertyGroup/RootNamespace", namespaceManager).FirstOrDefault();
        if (rootNamespaceElement == null) { return null; }

        var packageId
            = projectDocument.XPathSelectElements("./Project/PropertyGroup/PackageId", namespaceManager).FirstOrDefault()?.Value
              ?? rootNamespaceElement.Value;

        var element = new XElement(nugetNamespace + @"metadata");
        foreach (var elementName in new[] { @"id", @"title", @"description", @"releaseNotes" }) {
            element.Add(
                new XElement(nugetNamespace + elementName, elementName == @"id" ? packageId : rootNamespaceElement.Value));
        }

        foreach (var elementName in new[] { @"authors", @"owners" }) {
            element.Add(new XElement(nugetNamespace + elementName, author));
        }

        element.Add(new XElement(nugetNamespace + @"projectUrl", organizationUrl + solutionId));
        element.Add(new XElement(nugetNamespace + @"icon", "packageicon.png"));
        element.Add(new XElement(nugetNamespace + @"iconUrl", faviconUrl));
        element.Add(new XElement(nugetNamespace + @"requireLicenseAcceptance", @"false"));
        var year = DateTime.Now.Year;
        element.Add(new XElement(nugetNamespace + @"copyright", $"Copyright {year}"));
        element.Add(new XElement(nugetNamespace + @"version", version));
        tags = tags.Where(t => !t.Contains('<') && !t.Contains('>') && !t.Contains('&') && !t.Contains(' ')).ToList();
        if (tags.Any()) {
            element.Add(new XElement(nugetNamespace + @"tags", string.Join(" ", tags)));
        }

        var dependenciesElement = new XElement(nugetNamespace + @"dependencies");
        element.Add(dependenciesElement);

        var groupElement = new XElement(nugetNamespace + "group", new XAttribute("targetFramework", "net" + TargetFrameworkToLibNetSuffix(targetFramework)));
        dependenciesElement.Add(groupElement);
        dependenciesElement = groupElement;

        foreach (var dependencyElement in dependencyIdsAndVersions.Select(dependencyIdAndVersion
                     => dependencyIdAndVersion.Value == ""
                         ? new XElement(nugetNamespace + @"dependency", new XAttribute("id", dependencyIdAndVersion.Key))
                         : new XElement(nugetNamespace + @"dependency", new XAttribute("id", dependencyIdAndVersion.Key), new XAttribute("version", dependencyIdAndVersion.Value)))) {
            dependenciesElement.Add(dependencyElement);
        }

        return element;
    }

    private static string TargetFrameworkElementToLibNetSuffix(XElement targetFrameworkElement) {
        return TargetFrameworkToLibNetSuffix(targetFrameworkElement.Value);
    }

    private static string TargetFrameworkToLibNetSuffix(string targetFramework) {
        var targetNetFramework = targetFramework.StartsWith("net") ? targetFramework.Substring(3) : targetFramework;
        var libNetSuffix = targetFramework.StartsWith('v')
            ? targetFramework.Replace("v", "").Replace(".", "")
            : targetNetFramework;
        if (libNetSuffix.Contains("-")) {
            libNetSuffix = libNetSuffix.Substring(0, libNetSuffix.IndexOf("-", StringComparison.InvariantCulture));
        }
        return libNetSuffix;
    }

    protected XElement? Files(XDocument projectDocument, XmlNamespaceManager namespaceManager, XNamespace nugetNamespace, Entities.Configuration configuration) {
        var rootNamespaceElement = projectDocument.XPathSelectElements("./Project/PropertyGroup/RootNamespace", namespaceManager).FirstOrDefault();
        if (rootNamespaceElement == null) {
            return null;
        }

        var outputPathElement = projectDocument.XPathSelectElements("./Project/PropertyGroup/OutputPath", namespaceManager).SingleOrDefault(ParentIsReleasePropertyGroup);
        var outputPath = outputPathElement == null ? @"bin\Release\" : outputPathElement.Value;

        var targetFrameworkElement = projectDocument.XPathSelectElements("./Project/PropertyGroup/TargetFrameworkVersion", namespaceManager).FirstOrDefault()
                                     ?? projectDocument.XPathSelectElements("./Project/PropertyGroup/TargetFramework", namespaceManager).FirstOrDefault();
        if (targetFrameworkElement == null) {
            return null;
        }

        var filesElement = new XElement(nugetNamespace + @"files");
        var topLevelNamespace = rootNamespaceElement.Value;
        if (!topLevelNamespace.Contains('.')) {
            return null;
        }

        topLevelNamespace = topLevelNamespace.Substring(0, topLevelNamespace.IndexOf('.'));
        foreach (var fileElement in new[] { @"dll", @"pdb" }.Select(extension
                     => new XElement(nugetNamespace + @"file",
                         new XAttribute(@"src", outputPath + topLevelNamespace + ".*." + extension),
                         new XAttribute(@"exclude", string.Join(";", outputPath + @"*.Test*.*", outputPath + @"*.exe", outputPath + @"ref\*.*")),
                         new XAttribute(@"target", @"lib\net" + TargetFrameworkElementToLibNetSuffix(targetFrameworkElement))))) {
            filesElement.Add(fileElement);
        }

        filesElement.Add(new XElement(nugetNamespace + @"file",
            new XAttribute(@"src", outputPath + "packageicon.png"),
            new XAttribute(@"target", "")));

        var foldersToPack = projectDocument.XPathSelectElements("./Project/ItemGroup/Content", namespaceManager)
            .Where(x => IncludesFileToPack(x, configuration))
            .Select(x => IncludeAttributeValue(x, configuration))
            .Select(f => f.Substring(0, f.LastIndexOf('\\')))
            .Distinct().ToList();
        foreach (var folderToPack in foldersToPack) {
            var target = folderToPack;
            if (folderToPack.StartsWith("lib")) {
                target = @"lib\net" + TargetFrameworkElementToLibNetSuffix(targetFrameworkElement) + target.Substring(3);
            }
            filesElement.Add(new XElement(nugetNamespace + @"file",
                new XAttribute(@"src", outputPath + folderToPack + @"\*.*"),
                new XAttribute(@"exclude", ""),
                new XAttribute(@"target", target)));
        }

        return filesElement;
    }

    private static string? IncludeAttributeValue(XElement contentElement, Entities.Configuration configuration) {
        var attribute = contentElement.Attributes().FirstOrDefault(a => a.Name == "Include");
        return Array.Exists(configuration.ContentIncludeFolders, folder => attribute?.Value.StartsWith(folder) == true) ? attribute?.Value : null;
    }

    private static bool IncludesFileToPack(XElement contentElement, Entities.Configuration configuration) {
        return !string.IsNullOrWhiteSpace(IncludeAttributeValue(contentElement, configuration));
    }

    private static bool ParentIsReleasePropertyGroup(XElement e) {
        return e.Parent?.Attributes("Condition").Any(v => v.Value.Contains("Release")) == true;
    }
}