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

$baseUrl = "https://raw.githubusercontent.com/aspenlaub/Nuspecumulus/master/src/"
$url = ($baseUrl + "Scripts/NuSpecCreatorSourceFiles.json")
$sourceFiles = (New-Object System.Net.WebClient).DownloadString($url) | ConvertFrom-Json
$workProjId = "Work"
foreach($sourceFile in $sourceFiles) {
	$sourceFileShortName = $sourceFile.SubString($sourceFile.LastIndexOf('/') + 1)
	$fileCopyFullName = $psWorkFolder + "\" + $sourceFileShortName
	if ([System.IO.File]::Exists($fileCopyFullName)) {
		Write-Host ($fileCopyFullName + " exists")
	} else {
		$workProjId = "Work2"
		$url = ($baseUrl + $sourceFile)
		Invoke-WebRequest $url -OutFile $fileCopyFullName
		if ([System.IO.File]::Exists($fileCopyFullName)) {
			Write-Host ($fileCopyFullName + " has now been downloaded")
		} else {
			throw [System.IO.FileNotFoundException]::new("Could not download file " + $sourceFileShortName + " from: " + $url)
		}
	}
}

dotnet new classlib --force -n $workProjId -o $psWorkFolder -lang "C#" -d
Remove-Item ($psWorkFolder + "\Class*.cs")
dotnet publish ($psWorkFolder + "\" + $workProjId + ".csproj")

$workDll = ($psWorkFolder + "\bin\Release\net8.0\publish\" + $workProjId + ".dll")
[System.Reflection.Assembly]::LoadFrom($workDll)

$nuSpecCreator = New-Object -TypeName Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components.NuSpecCreator
$document = $nuSpecCreator.CreateNuSpecAsync($projectFileFullName, $organizationUrl, $author, $faviconUrl, $checkedOutBranch).GetAwaiter().GetResult()
[System.IO.File]::WriteAllText($nuSpecFileFullName, $document.ToString())
