Param(
	[string]$SourceBranch="master",
	[switch]$DryRun
)

# Constants
$initial_branch = (git symbolic-ref --short -q HEAD)
$version_file = "..\Renegade X Launcher\VersionCheck.cs"
$bin = "bin/"

function UpdateJsonVersion([string]$JsonContent) {
	$Json = $JsonContent | ConvertFrom-Json

	# Build updated values
	$version_number = $Json.launcher.version_number + 1
	$version_name_major = $Json.launcher.version_name.Split(".")[0]
	$version_name_minor = ($Json.launcher.version_name.Split(".")[1] -as [int]) + 1
	$version_name = $version_name_major + "." + $version_name_minor

	# Return Json with updated values
	$Json.launcher.version_number = $version_number
	$Json.launcher.version_name = $version_name
	return $Json
}

function GetFilenameFromPath([string]$Path) {
	# Replace text after the last '/' with filename
	return $Path.Split("/")[-1]
}

function ReplaceFilename([string]$Url, [string]$Path) {
	$Filename = GetFilenameFromPath $Path

	# Replace text after the last '/' with filename
	$UrlPaths = $Url.Split("/")
	$UrlPaths[$UrlPaths.Length - 1] = $Filename
	return [string]::Join("/", $UrlPaths)
}

function GenerateJSON($Json, [string]$Url) {
	# Get Filename from URL
	$Filename = GetFilenameFromPath $Url

	# Fetch JSON at Url and update launcher information
	$TargetJson = ((Invoke-WebRequest -URI $Url).Content | ConvertFrom-Json)
	$TargetJson.launcher = $Json.launcher

	# Write JSON out to disk
	$TargetJson | ConvertTo-Json -Depth 10 | Set-Content ($bin + "version/" + $Filename)
}

# Checkout source branch
git checkout $SourceBranch

# Get new version
$Json = UpdateJsonVersion (Invoke-WebRequest -URI "https://static.renegade-x.com/launcher_data/version/release.json").Content

# Replace version is version file
(Get-Content $version_file).replace('Name = "0.00"', ('Name = "' + $Json.launcher.version_name + '"')).replace("Number = 00", ("Number = " + $Json.launcher.version_number)) | Set-Content $version_file

# Snap a release branch
$release_branch = "release/" + $Json.launcher.version_name
git checkout -b $release_branch
git commit -m ("Set version for release ``" + $Json.launcher.version_name + "``") $version_file

# Push branch to origin
if (!$DryRun) {
	git push origin $release_branch
}

# Package launcher (package.ps1)
.\package.ps1

# Update remaining version data
$revision = (git rev-parse --short HEAD)
$package_zip = ($bin + "launcher-" + $revision + ".zip")
$Json.launcher.patch_url = ReplaceFilename $Json.launcher.patch_url $package_zip
$Json.launcher.patch_hash = ( Get-FileHash -Algorithm SHA256 $package_zip ).Hash

# Generate standard version files
New-Item -ItemType Directory -Path ($bin + "version/")
GenerateJSON $Json "https://static.renegade-x.com/launcher_data/version/release.json"
GenerateJSON $Json "https://static.renegade-x.com/launcher_data/version/beta.json"
GenerateJSON $Json "https://static.renegade-x.com/launcher_data/version/server.json"

# Generate legacy version file
$Json.launcher.version_number = 76
GenerateJSON $Json "https://static.renegade-x.com/launcher_data/version/legacy.json"

# Dry-Run cleanup
if ($DryRun) {
	# Checkout master
	git checkout $initial_branch

	# Cleanup
	git branch -D $release_branch
}