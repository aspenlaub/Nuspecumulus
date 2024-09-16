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
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) {
            throw new MissingManifestResourceException(resourceName);
        }
        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        var psContents = streamReader.ReadToEnd();

        // https://stackoverflow.com/questions/24868273/run-a-c-sharp-cs-file-from-a-powershell-script
        // https://stackoverflow.com/questions/527513/execute-powershell-script-from-c-sharp-with-commandline-arguments
        // PowerShell.Create().AddScript()
        var result = new PsCreateNuSpecResult();

        var powershell = PowerShell.Create();
        powershell.AddScript(string.Join(Environment.NewLine, psContents));
        powershell.Invoke();
        result.Errors.AddRange(powershell.Streams.Error.Select(e => e.ToString()));
        result.Infos.AddRange(powershell.Streams.Information.Select(e => e.ToString()));
        return result;
    }
}