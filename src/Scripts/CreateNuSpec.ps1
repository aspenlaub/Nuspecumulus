using namespace System.Collections.Generic
using namespace System.Text.Json
using namespace System.Threading.Tasks
	
$psWorkFolder = $args[0]
$csFileShortName = $args[1]
$projectFileFullName = $args[2]
$organizationUrl = $args[3]
$author = $args[4]
$faviconUrl = $args[5]
$checkedOutBranch = $args[6]
$nuSpecFileFullName = $args[7]

dotnet new classlib --force -o $psWorkFolder -lang "C#" -d
Remove-Item ($psWorkFolder + "\Class*.cs")
dotnet publish ($psWorkFolder + "\Work.csproj")

$workDll = ($psWorkFolder + "\bin\Release\net8.0\publish\work.dll")
[System.Reflection.Assembly]::LoadFrom($workDll)

$nuSpecCreator = New-Object -TypeName Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components.NuSpecCreator
$document = $nuSpecCreator.CreateNuSpecAsync($projectFileFullName, $organizationUrl, $author, $faviconUrl, $checkedOutBranch).GetAwaiter().GetResult()
[System.IO.File]::WriteAllText($nuSpecFileFullName, $document.ToString())
