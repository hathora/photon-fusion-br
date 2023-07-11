#!/bin/bash
###################################################################################################
#
# RunWslLinuxServer.sh - Creates 1 LinuxServer (via bash shell) for Photon Fusion BR demo
#  By default, this works with default Unity `HathoraServerConfig` settings in `hathora-fusion-br`.
#  See 'Networking.cs' for the main init.
#
# -------------------------------------------------------------------------------------
# 
# EXPORTED ENV VARS:
# - HATHORA_PROCESS_ID
# - SERVER_IP # Fallback if !HATHORA_PROCESS_ID
# - SERVER_PORT # Fallback if !HATHORA_PROCESS_ID
#
# (!) Get these via Unity's `Environment.GetEnvironmentVariable()`
#
# -------------------------------------------------------------------------------------
#
# ARGS WHEN CALLING THIS SCRIPT, ITSELF
#  -p {hathoraProcessId} # Optional - Gets ip:port from a Hathora Cloud Process Id
#
# -------------------------------------------------------------------------------------
#
# UNITY ARGS: 
# -batchmode runs as a headless (dedicated) server
# -nographics (requires -batchmode) runs without a GUI
# -> More @ https://docs.unity3d.com/Manual/PlayerCommandLineArguments.html 
#
# -------------------------------------------------------------------------------------
#
# PHOTON-FUSION-BR ARGS: 
# -dedicatedServer
# -deathmatch
# -scene GenArea2 // #2 == smallest scene
# -> More @ https://doc.photonengine.com/fusion/current/game-samples/fusion-br/quickstart
#
# -------------------------------------------------------------------------------------
# Created by dylan@hathora.dev @ 7/11/2023
###################################################################################################

# Check for -p HathoraProcessId arg
while getopts ":p:" opt; do
  case ${opt} in
    p )
      HATHORA_PROCESS_ID=$OPTARG
      ;;
    \? )
      echo "Invalid option: $OPTARG" 1>&2
      ;;
    : )
      echo "Option $OPTARG requires an argument" 1>&2
      ;;
  esac
done

export HATHORA_PROCESS_ID

# ====================================================================
# Set local WSL2 server fallbacks, if !HATHORA_PROCESS_ID from -p arg
# Then log results
# ====================================================================

# Arbitrary, but must match Client's port
export SERVER_PORT=7777

# Get the "real" IP of the Wsl2 IP (essentially 'localhost' ported through)
echo "Current directory: $(pwd)"
export LOCAL_SERVER_IP=$(./reveal_wsl_vm_ip.sh)

# Print the output of the Wsl2 IP (essentially `localhost` for WSL2)
clear
echo "Starting dedicated server: \`$LOCAL_SERVER_IP:$SERVER_PORT\`"
echo "HATHORA_PROCESS_ID: \`$HATHORA_PROCESS_ID\`"
echo "-----------------------------"
echo ""

# ====================================================================
# START LINUX SERVER (from Windows -> via wsl2)
# By default, works with default Unity `HathoraServerConfig` settings
# ====================================================================
exe_name="Hathora-Unity-LinuxServer.x86_64"
path_to_linux_server="../../Build-Server/$exe_name"
echo "[$exe_name] Starting instance @ \`$path_to_linux_server\`"

unity_args="-batchmode -nographics"
photon_args="-dedicatedServer \
	-deathmatch \
	-maxPlayers 5 \
	-scene GenArea2 \
	-region us \
	-serverName LocalHeadlessServer \
	-port $SERVER_PORT"

linux_cmd="$path_to_linux_server $unity_args $photon_args"
$linux_cmd
