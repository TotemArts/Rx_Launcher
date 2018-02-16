#Cleanup from previous script runs
Remove-Item -Recurse -Force "bin" *>$null
Remove-Item -Recurse -Force "RXPatch-bin" *>$null
Remove-Item -Force "launcher-*.zip" *>$null
Remove-Item -Force "Renegade_X_Installer-*.msi" *>$null
Remove-Item -Force launcher-hash.txt *>$null

# Cleanup the build; check your PATH if this fails
devenv "Renegade X Launcher.sln" /clean

# Make extra sure the installer is deleted
Remove-Item -Recurse -Force "Renegade X Installer/bin/"

# Build the launcher; check your PATH if this fails
devenv "Renegade X Launcher.sln" /build "Release"

# Copy launcher binaries to /bin
Remove-Item "bin" -Recurse -ErrorAction Ignore
Remove-Item "RXPatch-bin" -Recurse -ErrorAction Ignore
ROBOCOPY "Renegade X Launcher/bin/Release" "bin" *.dll *.exe *.config /S /XF *.vshost*

# Copy self updater to /bin
ROBOCOPY "SelfUpdateExecutor/bin/Release" "bin" *.dll *.exe /S /XF *.vshost*

# Copy patcher binaries to /RXPatch-bin
ROBOCOPY "RXPatch/bin/Release" "RXPatch-bin" *.dll *.exe *.config /S /XF *.vshost*

# Get the current git commit hash
$revision = (git rev-parse --short HEAD)

# Copy installer to /
COPY "Renegade X Installer/bin/Release/Renegade_X_Installer.msi" "Renegade_X_Installer-$($revision).msi"

# Zip "/bin" to "/launcher-revision.zip"
Remove-Item "launcher-*.zip" -ErrorAction Ignore
$zipTarget = "launcher-$($revision).zip"
Remove-Item $zipTarget -ErrorAction Ignore
Add-Type -Assembly System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory( ( join-path (Get-Location) bin ), ( join-path (Get-Location) $zipTarget ), [System.IO.Compression.CompressionLevel]::Optimal, $false)

# Calculate SHA-256 hash of "/launcher-revision.zip"
$hash = ( Get-FileHash -Algorithm SHA256 ./$zipTarget ).Hash
echo "SHA256 Hash: $hash"

# Write hash to /bin/launcher-hash.txt
$hash | Set-Content 'launcher-hash.txt'
