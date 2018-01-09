# Copy launcher binaries to /bin
Remove-Item "bin" -Recurse -ErrorAction Ignore
ROBOCOPY "Renegade X Launcher/bin/Release" "bin" *.dll *.exe *.config /S /XF *.vshost*

# Copy patcher binaries to /RXPatch-bin
ROBOCOPY "RXPatch/bin/Release" "RXPatch-bin" *.dll *.exe *.config /S /XF *.vshost*

# Get the current SVN revision number
$revision = (git rev-parse --short HEAD)

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
