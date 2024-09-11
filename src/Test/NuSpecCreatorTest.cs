using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide;
using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components;
using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Protch;
using Autofac;
using LibGit2Sharp;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using System.Xml;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using System.Text.Json;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Test;

[TestFixture]
public class NuSpecCreatorTest {
    private static readonly TestTargetFolder PakledTarget = new(nameof(NuSpecCreator), "Pakled");
    private static readonly TestTargetFolder GittyTarget = new(nameof(NuSpecCreator), "Gitty");
    private static readonly IContainer NuclideContainer = new ContainerBuilder()
        .UseGittyTestUtilities().UseProtch().UseNuclideProtchGittyAndPegh("Nuspecumulus", new DummyCsArgumentPrompter()).Build();
    private static IGitUtilities GitUtilities => NuclideContainer.Resolve<IGitUtilities>();
    private static readonly IContainer NuspecumulusContainer = new ContainerBuilder().UseNuspecumulus().Build();

    [OneTimeTearDown]
    public void ClassCleanup() {
        NuclideContainer.Dispose();
        NuspecumulusContainer.Dispose();
    }

    [SetUp]
    public void Initialize() {
        DeleteTargets();
    }

    [TearDown]
    public void Cleanup() {
        DeleteTargets();
    }

    private void DeleteTargets() {
        PakledTarget.Delete();
        GittyTarget.Delete();
    }

    [Test]
    public void NuspecumulusContainerBuilder_CanBuild() {
        var creator = NuspecumulusContainer.Resolve<INuSpecCreator>();
        Assert.That(creator, Is.Not.Null);
    }

    [Test]
    public async Task CanCreateNuSpecForPakled() {
        await CanCreateNuSpecForAsync(PakledTarget);
    }

    [Test]
    public async Task CanCreateNuSpecForGitty() {
        await CanCreateNuSpecForAsync(GittyTarget);
    }

    private async Task CanCreateNuSpecForAsync(TestTargetFolder target) {
        var errorsAndInfos = new ErrorsAndInfos();
        var solutionId = target.SolutionId;
        var url = $"https://github.com/aspenlaub/{solutionId}.git";
        GitUtilities.Clone(url, "master", target.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.That(errorsAndInfos.Errors.Any(), Is.False, errorsAndInfos.ErrorsPlusRelevantInfos());

        NuclideContainer.Resolve<IDotNetCakeInstaller>().InstallOrUpdateGlobalDotNetCakeIfNecessary(errorsAndInfos);
        Assert.That(errorsAndInfos.Errors.Any(), Is.False, errorsAndInfos.ErrorsPlusRelevantInfos());

        NuclideContainer.Resolve<IEmbeddedCakeScriptCopier>().CopyCakeScriptEmbeddedInAssembly(Assembly.GetExecutingAssembly(), BuildCake.Standard,
            target, errorsAndInfos);
        Assert.That(errorsAndInfos.Errors.Any(), Is.False, errorsAndInfos.ErrorsPlusRelevantInfos());

        NuclideContainer.Resolve<ITestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, target,
            "CleanRestorePull", errorsAndInfos);
        Assert.That(errorsAndInfos.Errors.Any(), Is.False, errorsAndInfos.ErrorsPlusRelevantInfos());
        NuclideContainer.Resolve<ITestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, target,
            "ReleaseBuild", errorsAndInfos);
        Assert.That(errorsAndInfos.Errors.Any(), Is.False, errorsAndInfos.ErrorsPlusRelevantInfos());

        var nuclideCreator = NuclideContainer.Resolve<Nuclide.Interfaces.INuSpecCreator>();
        var solutionFileFullName = target.Folder().SubFolder("src").FullName + @"\" + target.SolutionId + ".sln";
        var projectFileFullName = target.Folder().SubFolder("src").FullName + @"\" + target.SolutionId + ".csproj";
        Assert.That(File.Exists(projectFileFullName), Is.True);
        var nuclideDocument = XDocument.Load(projectFileFullName);

        var namespaceManager = new XmlNamespaceManager(new NameTable());
        namespaceManager.AddNamespace("cp", XmlNamespaces.CsProjNamespaceUri);
        namespaceManager.AddNamespace("nu", XmlNamespaces.NuSpecNamespaceUri);

        var targetFrameworkElement = nuclideDocument.XPathSelectElements("./Project/PropertyGroup/TargetFramework", namespaceManager).FirstOrDefault();
        Assert.That(targetFrameworkElement, Is.Not.Null);
        var rootNamespaceElement = nuclideDocument.XPathSelectElements("./Project/PropertyGroup/RootNamespace", namespaceManager).FirstOrDefault();
        Assert.That(rootNamespaceElement, Is.Not.Null);
        var checkedOutBranch = GitUtilities.CheckedOutBranch(target.Folder());
        nuclideDocument = await nuclideCreator.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch,
            new List<string>(), errorsAndInfos);
        Assert.That(nuclideDocument, Is.Not.Null);
        Assert.That(errorsAndInfos.Errors.Any(), Is.False, errorsAndInfos.ErrorsPlusRelevantInfos());
        var areDocumentsEqual = XNode.DeepEquals(nuclideDocument.Root, nuclideDocument.Root);
        Assert.That(areDocumentsEqual, Is.True);

        var developerSettingsSecret = new DeveloperSettingsSecret();
        var developerSettings = await NuclideContainer.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
        Assert.That(developerSettings, Is.Not.Null);

        var versionFile = target.Folder().SubFolder("src").FullName + "\\version.json";
        if (!File.Exists(versionFile)) {
            var version = new Entities.Version { Major = 2, Minor = 4 };
            await File.WriteAllTextAsync(versionFile, JsonSerializer.Serialize(version));
        }
        var sut = NuspecumulusContainer.Resolve<INuSpecCreator>();
        var nuspecumulusDocument = await sut.CreateNuSpecAsync(
            target.Folder().SubFolder("src").FullName + $"\\{solutionId}.csproj",
            developerSettings.GitHubRepositoryUrl,
            developerSettings.Author,
            developerSettings.FaviconUrl,
            checkedOutBranch);

        var configuration = JsonSerializer.Deserialize<Entities.Configuration>(await File.ReadAllTextAsync("settings.json"));
        Assert.That(configuration, Is.Not.Null);
        Assert.That(NormalizeNuspec(nuspecumulusDocument, configuration), Is.EqualTo(NormalizeNuspec(nuclideDocument, configuration)));
    }

    private static string NormalizeNuspec(XDocument nuspec, Entities.Configuration configuration) {
        var nuspecAsString = nuspec.ToString();
        var pos = nuspecAsString.IndexOf(configuration.VersionStartTag, StringComparison.InvariantCulture);
        Assert.That(pos, Is.Positive);
        var pos2 = nuspecAsString.IndexOf(configuration.VersionEndTag, pos + 1, StringComparison.InvariantCulture);
        Assert.That(pos2, Is.Positive);
        Assert.That(pos2, Is.LessThan(pos + configuration.VersionStartTag.Length + 16));
        nuspecAsString = nuspecAsString.Substring(0, pos) + "<version />" + nuspecAsString.Substring(pos2 + 1 + configuration.VersionEndTag.Length);

        return nuspecAsString;
    }
}