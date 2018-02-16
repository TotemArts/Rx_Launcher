#!/bin/bash
#
# To use this script, define $destination and $path below.
# You must also be setup for non-interactive authentication with the user/host
#

# Configuration variables
# $launcher_data_destination = user@host:dir/

# Setup some vars
revision=$(git rev-parse --short HEAD)
launcher=launcher-$revision.zip
installer=Renegade_X_Installer-$revision.msi

# Connect and transfer over the launcher and installer
echo destination: $launcher_data_destination
sftp $launcher_data_destination << EOF
put $launcher
put $installer
bye
EOF
