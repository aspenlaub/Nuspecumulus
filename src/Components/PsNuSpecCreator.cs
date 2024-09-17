using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Entities;
using System.Management.Automation;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.Json;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components;

public static class PsNuSpecCreator {
    public static PsCreateNuSpecResult CreateNuSpec(string projectFileFullName, string organizationUrl, string author, string faviconUrl,
            string checkedOutBranch, bool letPowershellDownloadSourceFiles) {

        var assembly = Assembly.GetExecutingAssembly();
        var psContents = "";
        var workSubFolder = letPowershellDownloadSourceFiles ? "Work2" : "Work";
        var psWorkFolder = Directory.GetCurrentDirectory() + @"\..\" + workSubFolder;
        if (!Directory.Exists(psWorkFolder)) {
            Directory.CreateDirectory(psWorkFolder);
        } else {
            foreach (var fileName in Directory.GetFiles(psWorkFolder, "*.*", SearchOption.AllDirectories)) {
                File.Delete(fileName);
            }
        }

        const string psFileShortName = "CreateNuSpec.ps1";
        var shortNameToQualifiedName = new Dictionary<string, string> {
            { psFileShortName, "Scripts.CreateNuSpec.ps1" },
            { "Work.csproj", "Scripts.Work.csproj.xml" }
        };
        foreach(var shortAndQualifiedName in shortNameToQualifiedName) {
            var fileFullName = $"{psWorkFolder}\\{shortAndQualifiedName.Key}";
            var resourceName = assembly.GetName().Name + "." + shortAndQualifiedName.Value;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) {
                throw new MissingManifestResourceException(resourceName);
            }

            string contents;
            using (var streamReader = new StreamReader(stream, Encoding.UTF8)) {
                contents = streamReader.ReadToEnd();
                if (shortAndQualifiedName.Key == psFileShortName) {
                    psContents = contents;
                }
            }
            File.WriteAllText(fileFullName, contents);
        }

        if (!letPowershellDownloadSourceFiles) {
            const string sourceFilesJsonShortName = "NuSpecCreatorSourceFiles.json";
            var resourceName = assembly.GetName().Name + ".Scripts." + sourceFilesJsonShortName;
            var sourceFiles = new List<string>();
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                if (stream == null) {
                    throw new MissingManifestResourceException(resourceName);
                }
                using var streamReader = new StreamReader(stream, Encoding.UTF8);
                var json = streamReader.ReadToEnd();
                sourceFiles.AddRange(JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>());
            }

            foreach (var sourceFile in sourceFiles) {
                resourceName = assembly.GetName().Name + "." + sourceFile.Replace("/", ".");
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) {
                    throw new MissingManifestResourceException(resourceName);
                }
                using var streamReader = new StreamReader(stream, Encoding.UTF8);
                var sourceFileContents = streamReader.ReadToEnd();
                var sourceFileShortName = sourceFile.Substring(sourceFile.LastIndexOf("/", StringComparison.InvariantCulture) + 1);
                File.WriteAllText($"{psWorkFolder}\\{sourceFileShortName}", sourceFileContents);
            }
        }

        var nuSpecFileFullName = projectFileFullName.Replace(".csproj", ".nuspec");
        var result = new PsCreateNuSpecResult {
            NuSpecFileFullName = nuSpecFileFullName
        };

        var powershell = PowerShell.Create();
        powershell.AddScript(psContents);
        var psParameters = new List<string> {
            psWorkFolder, projectFileFullName, organizationUrl, author, faviconUrl, checkedOutBranch, nuSpecFileFullName
        };
        powershell.AddParameters(psParameters);
        var oneLiner = "& $PSScriptRoot\\" + psFileShortName + " " + string.Join(' ', psParameters.Select(x => '"' + x + '"'));
        File.WriteAllText($"{psWorkFolder}\\oneLiner.ps1", oneLiner);

        powershell.Invoke();
        result.Errors.AddRange(powershell.Streams.Error.Select(e => e.ToString()));
        result.Infos.AddRange(powershell.Streams.Information.Select(e => e.ToString()));
        return result;
    }
}