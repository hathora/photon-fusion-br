#!/bin/bash
################################################################
#
# reveal_wsl_vm_ip.sh - Reveal the true IP behind a WSL VM. 
#  Since Windows !detects `localhost` (127.0.0.1) on WSL2,
#  this script will reveal the true IP of the WSL VM.
#
# --------------------------------------------------------------
#
# If `localhost` doesn't work for you when testing a localhost`
#   Linux server via WSL2 in Windows, then use this script
#   to get the local IP of the WSL VM.
#
# --------------------------------------------------------------
# Created by dylan@hathora.dev @ 7/11/2023
################################################################

ip addr show eth0 | grep 'inet ' | awk '{ print $2; }' | sed 's/\/.*$//'
