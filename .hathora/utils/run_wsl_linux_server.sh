#!/bin/bash
#################################################################################################
#
# RunWslLinuxServer.sh - Creates 1 LinuxServer (via bash shell) for Photon Fusion BR demo
#  By default, this works with default Unity `HathoraServerConfig` settings in `hathora-fusion-br`
#
# --------------------------------------------------------------------------------
#
# UNITY ARGS: 
# -batchmode runs as a headless (dedicated) server
# -nographics (requires -batchmode) runs without a GUI
# -> More @ https://docs.unity3d.com/Manual/PlayerCommandLineArguments.html 
#
# --------------------------------------------------------------------------------
#
# PHOTON-FUSION-BR ARGS: 
# -dedicatedServer
# -deathmatch
# -scene GenArea2 // #2 == smallest scene
# -> More @ https://doc.photonengine.com/fusion/current/game-samples/fusion-br/quickstart
#
# --------------------------------------------------------------------------------
# Created by dylan@hathora.dev @ 7/11/2023
#################################################################################################

# Print the output of the Wsl2 IP (essentially `localhost` for WSL2)
clear
echo "Starting dedicated server: $(./reveal_wsl_vm_ip.sh)"
echo "-----------------------------"
echo ""

# ====================================================================
# START LINUX SERVER (from Windows -> via wsl2)
# By default, works with default Unity `HathoraServerConfig` settings
# ====================================================================
exe_name="Hathora-Unity-LinuxServer.x86_64"
path_to_linux_server="../../Build-Server/$exe_name"
echo "[$exe_name] Starting instance @ '$path_to_linux_server' ..."

unity_args="-batchmode -nographics"
photon_args="-dedicatedServer \
	-deathmatch \
	-maxPlayers 5 \
	-scene GenArea2 \
	-region us \
	-serverName LocalHeadlessServer \
	-port 7777"

linux_cmd="$path_to_linux_server $unity_args $photon_args"

$linux_cmd
