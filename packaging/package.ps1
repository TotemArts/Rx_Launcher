# Constants
$launcher_bin = "bin/launcher"
$patch_bin = "bin/RXPatch"
$bin = "bin/"

# Cleanup from previous script runs
Remove-Item -Recurse -Force $bin *>$null
Remove-Item -Force "launcher-*.zip" *>$null
Remove-Item -Force "Renegade_X_Installer-*.msi" *>$null

# Cleanup the build; check your PATH if this fails
devenv "../Renegade X Launcher.sln" /clean

# Make extra sure the installer is deleted
Remove-Item -Recurse -Force "../Renegade X Installer/bin/"

# Build the launcher; check your PATH if this fails
devenv "../Renegade X Launcher.sln" /build "Release"

# Copy launcher binaries to /bin
ROBOCOPY "../Renegade X Launcher/bin/Release" $launcher_bin *.dll *.exe *.config /S /XF *.vshost*

# Copy self updater to /bin
ROBOCOPY "../SelfUpdateExecutor/bin/Release" $launcher_bin *.dll *.exe /S /XF *.vshost*

# Copy patcher binaries to /RXPatch-bin
ROBOCOPY "../RXPatch/bin/Release" $patch_bin *.dll *.exe *.config /S /XF *.vshost*

# Get the current git commit hash
$revision = (git rev-parse --short HEAD)

# Copy installer to /
COPY "../Renegade X Installer/bin/Release/Renegade_X_Installer.msi" "$($bin)Renegade_X_Installer-$($revision).msi"

# Zip "/bin" to "/launcher-revision.zip"
$zipTarget = "$($bin)launcher-$($revision).zip"
Remove-Item $zipTarget -ErrorAction Ignore
Add-Type -Assembly System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory( ( join-path (Get-Location) $launcher_bin ), ( join-path (Get-Location) $zipTarget ), [System.IO.Compression.CompressionLevel]::Optimal, $false)
