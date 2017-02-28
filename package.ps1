# Copy launcher binaries to /Renegade X Launcher/bin
Remove-Item "bin" -Recurse -ErrorAction Ignore
ROBOCOPY "Renegade X Launcher/bin/Release" "bin" *.dll *.exe *.config /S /XF *.vshost*

# Copy launcher binaries to /UDK_Uncooked/Launcher
ROBOCOPY "Renegade X Launcher/bin/Release" "../UDK_Uncooked/Launcher" *.dll *.exe *.config /S /XF *.vshost*

# Copy patcher binaries to /RXPatch
ROBOCOPY "RXPatch/bin/Release" "../RXPatch" *.dll *.exe *.config /S /XF *.vshost*

# Zip "/Renegade X Launcher/bin" to "/Renegade X Launcher/launcher.zip"
Remove-Item "launcher.zip" -ErrorAction Ignore
Add-Type -Assembly System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory( ( join-path (Get-Location) bin ), ( join-path (Get-Location) launcher.zip ), [System.IO.Compression.CompressionLevel]::Optimal, $false)

# Calculate SHA-256 hash of "/Renegade X Launcher/launcher.zip"
$hash = ( Get-FileHash -Algorithm SHA256 ./launcher.zip ).Hash
echo "SHA256 Hash: $hash"

# Write hash to /Renegade X Launcher/bin/launcher-hash.txt
$hash | Set-Content 'launcher-hash.txt'
