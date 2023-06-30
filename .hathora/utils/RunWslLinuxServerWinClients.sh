#!/bin/bash
##################################################################################
# ABOUT: Creates 1 LinuxServer (via bash shell) for Photon Fusion BR demo
##################################################################################
# ARG LIST: https://docs.unity3d.com/Manual/PlayerCommandLineArguments.html 
# -batchmode is useful, but it's hard to know when to -quit (or need to end task)
# -nographics (combined with -batchmode) runs as headless dedicated server
##################################################################################
# PHOTON FUSION BR CMDS
# -dedicatedServer
# -deathmatch
# -scene GenArea2 # 2 == smallest
#
# More @ https://doc.photonengine.com/fusion/current/game-samples/fusion-br/quickstart
##################################################################################
# Clear the console
clear

# print the output of the last command
echo "Starting dedicated server: $(./RevealWslVmIp.sh)"
echo "-----------------------------"
echo ""
##################################################################################
# START LINUX SERVER (from Windows -> via wsl2)
##################################################################################
exe_name="Hathora-Unity-LinuxServer.x86_64"
path_to_linux_server="../../Build-Server/$exe_name"
echo "Starting $exe_name instance @ '$path_to_linux_server' ..."

linux_cmd="$path_to_linux_server -batchmode -nographics -dedicatedServer -deathmatch -maxPlayers 5 -scene GenArea2 -region us -serverName LocalHeadlessServer -port 7777"
$linux_cmd
