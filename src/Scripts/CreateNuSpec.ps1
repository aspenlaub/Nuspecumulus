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

Write-Host "Downloading required source files"

$baseUrl = "https://raw.githubusercontent.com/aspenlaub/Nuspecumulus/master/src/"
$url = ($baseUrl + "Scripts/NuSpecCreatorSourceFiles.json")
$sourceFiles = (New-Object System.Net.WebClient).DownloadString($url) | ConvertFrom-Json
$workProjId = "Work"
foreach($sourceFile in $sourceFiles) {
	$sourceFileShortName = $sourceFile.SubString($sourceFile.LastIndexOf('/') + 1)
	$sourceFileShortName = $sourceFileShortName.Replace(".csproj.xml", ".csproj")
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

Write-Host "Creating new class library"

if ($workProjId -ne "Work") {
	Rename-Item -Path ($psWorkFolder + "\Work.csproj") -NewName ($workProjId + ".csproj")
}

Write-Host "Publishing project"

dotnet publish ($psWorkFolder + "\" + $workProjId + ".csproj")

Write-Host "Loading dll"

$workDll = ($psWorkFolder + "\bin\Release\net8.0\publish\" + $workProjId + ".dll")
[System.Reflection.Assembly]::LoadFrom($workDll)

Write-Host "Newing NuSpecCreator"

$nuSpecCreator = New-Object -TypeName Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components.NuSpecCreator
$document = $nuSpecCreator.CreateNuSpecAsync($projectFileFullName, $organizationUrl, $author, $faviconUrl, $checkedOutBranch).GetAwaiter().GetResult()
[System.IO.File]::WriteAllText($nuSpecFileFullName, $document.ToString())

Write-Host "NuSpec has been saved"
