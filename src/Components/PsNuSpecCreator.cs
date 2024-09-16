using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Entities;
using System.Management.Automation;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components;

public static class PsNuSpecCreator {
    public static PsCreateNuSpecResult CreateNuSpec(string projectFileFullName, string organizationUrl, string author, string faviconUrl,
        string checkedOutBranch) {

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetName().Name + ".Scripts.CreateNuSpec.ps1";
        string psContents;
        var psWorkFolder = Directory.GetCurrentDirectory() + @"\..\Work";
        if (!Directory.Exists(psWorkFolder)) {
            Directory.CreateDirectory(psWorkFolder);
        } else {
            foreach (var fileName in Directory.GetFiles(psWorkFolder, "*.*", SearchOption.AllDirectories)) {
                File.Delete(fileName);
            }
        }

        var psFileFullName = psWorkFolder + @"\CreateNuSpec.ps1";
        using (var stream = assembly.GetManifestResourceStream(resourceName)) {
            if (stream == null) {
                throw new MissingManifestResourceException(resourceName);
            }

            using (var streamReader = new StreamReader(stream, Encoding.UTF8)) {
                psContents = streamReader.ReadToEnd();
            }
            File.WriteAllText(psFileFullName, psContents);
        }

        var csFileShortName = "NuSpecCreator.cs";
        resourceName = assembly.GetName().Name + ".Scripts.NuSpecCreatorCopy.cs";
        using (var stream = assembly.GetManifestResourceStream(resourceName)) {
            if (stream == null) {
                throw new MissingManifestResourceException(resourceName);
            }
            using var streamReader = new StreamReader(stream, Encoding.UTF8);
            var csContents = streamReader.ReadToEnd();
            File.WriteAllText($"{psWorkFolder}\\{csFileShortName}", csContents);
        }

        var nuSpecFileFullName = projectFileFullName.Replace(".csproj", ".nuspec");
        // https://stackoverflow.com/questions/24868273/run-a-c-sharp-cs-file-from-a-powershell-script
        // https://stackoverflow.com/questions/527513/execute-powershell-script-from-c-sharp-with-commandline-arguments
        // PowerShell.Create().AddScript()
        var result = new PsCreateNuSpecResult {
            NuSpecFileFullName = nuSpecFileFullName
        };

        var powershell = PowerShell.Create();
        powershell.AddScript(psContents);
        powershell.AddParameters(new List<string> {
            psWorkFolder, csFileShortName, projectFileFullName, organizationUrl, author, faviconUrl, checkedOutBranch, nuSpecFileFullName
        });
        powershell.Invoke();
        result.Errors.AddRange(powershell.Streams.Error.Select(e => e.ToString()));
        result.Infos.AddRange(powershell.Streams.Debug.Select(e => e.ToString()));
        return result;
    }
}