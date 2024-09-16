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

Set-Location -Path $psWorkFolder
dotnet new classlib
Remove-Item Class*.cs
dotnet publish

Write-Host $psWorkFolder + "\bin\Release\net8.0\publish\work.dll"

#$source = Get-Content -Path "$csFileFullName"
#$referencedAssemblies = (
#	"System.Xml",
#	"System.Xml.Linq",
#	"System.Xml.XDocument",
#	"System.Xml.XPath",
#	"System.Linq"
#)

# Add-Type -ReferencedAssemblies $referencedAssemblies -TypeDefinition "$source"

# $nuSpecCreator = New-Object -TypeName NuSpecCreator