#!/bin/bash
#
# To use this script, define $launcher_data_destination and $version_data_destination below.
# You must also be setup for non-interactive authentication with the user/host
#

# Configuration variables
# $launcher_data_destination = user@host:dir/
# $version_data_destination = user@host:dir/

# Setup some vars
revision=$(git rev-parse --short HEAD)
bin=bin/
launcher=${bin}launcher-$revision.zip
installer=${bin}Renegade_X_Installer-$revision.msi
json=${bin}version/*.json

# Connect and transfer over the launcher and installer
s3cmd put $launcher $installer $launcher_data_destination

# Connect and transfer over the version files
rsync -av --update $json $version_data_destination
