ROBOCOPY "Renegade X Launcher/bin/Release" "bin" *.dll *.exe *.config /S /XF *.vshost*
ROBOCOPY "Renegade X Launcher/bin/Release" "../UDK_Uncooked/Launcher" *.dll *.exe *.config /S /XF *.vshost*
ROBOCOPY "RXPatch/bin/Release" "../RXPatch" *.dll *.exe *.config /S /XF *.vshost*
