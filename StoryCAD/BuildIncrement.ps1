#This will only run on developer machines and not CI/CD Pipelines.
if ($env:CI -eq $null) {
	[xml]$manifest = Get-Content 'Package.appxmanifest'

	$namespace = New-Object System.Xml.XmlNamespaceManager $manifest.NameTable
	$namespace.AddNamespace('default', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')

	$identityNode = $manifest.SelectSingleNode('//default:Identity', $namespace)
	Write-Host $identityNode # Print the value of $identityNode to debug

	$currentVersion = [Version]$identityNode.Version

	# Get current date in yyddd format
	$currentDate = Get-Date

	$newVersion = New-Object Version ($currentVersion.Major, $currentVersion.Minor, $currentVersion.Build, "65535")
	$identityNode.Version = $newVersion.ToString()
	$manifest.Save('Package.appxmanifest')
}
