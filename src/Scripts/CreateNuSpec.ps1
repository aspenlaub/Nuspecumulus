using namespace System.Collections.Generic
using namespace System.Text.Json
using namespace System.Threading.Tasks
	
$psWorkFolder = $args[0]
$projectFileFullName = $args[1]
$organizationUrl = $args[2]
$author = $args[3]
$faviconUrl = $args[4]
$checkedOutBranch = $args[5]
$nuSpecFileFullName = $args[6]

dotnet new classlib --force -o $psWorkFolder -lang "C#" -d
Remove-Item ($psWorkFolder + "\Class*.cs")
dotnet publish ($psWorkFolder + "\Work.csproj")

$workDll = ($psWorkFolder + "\bin\Release\net8.0\publish\work.dll")
[System.Reflection.Assembly]::LoadFrom($workDll)

$nuSpecCreator = New-Object -TypeName Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components.NuSpecCreator
$document = $nuSpecCreator.CreateNuSpecAsync($projectFileFullName, $organizationUrl, $author, $faviconUrl, $checkedOutBranch).GetAwaiter().GetResult()
[System.IO.File]::WriteAllText($nuSpecFileFullName, $document.ToString())
