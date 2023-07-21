// Created by dylan@hathora.dev

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Hathora.Core.Scripts.Editor.Common;
using Hathora.Core.Scripts.Runtime.Server.Models;
using Debug = UnityEngine.Debug;

namespace Hathora.Core.Scripts.Editor.Server
{
    /// <summary>
    /// Dockerfile generation/reading utils.
    /// </summary>
    public static class HathoraDocker
    {
        /// <summary>
        /// Deletes an old one, if exists, to ensure updated paths.
        /// TODO: Use this to customize the Dockerfile without editing directly.
        /// </summary>
        /// <param name="pathToDockerfile"></param>
        /// <param name="dockerfileContent"></param>
        /// <param name="_cancelToken"></param>
        /// <returns>path/to/Dockerfile</returns>
        public static async Task WriteDockerFileAsync(
            string pathToDockerfile, 
            string dockerfileContent,
            CancellationToken _cancelToken = default)
        {
            // TODO: if (!overwriteDockerfile)
            if (File.Exists(pathToDockerfile))
            {
                Debug.LogWarning("[HathoraServerDeploy.WriteDockerFileAsync] " +
                    "Deleting old Dockerfile...");
                File.Delete(pathToDockerfile);
            }
            
            HathoraEditorUtils.ValidateCreateDotHathoraDir();

            try
            {
                await File.WriteAllTextAsync(
                    pathToDockerfile, 
                    dockerfileContent, 
                    _cancelToken);
            }
            catch (Exception e)
            {
                Debug.LogError("[HathoraServerDeploy.WriteDockerFileAsync] " +
                    $"Failed to write Dockerfile to {pathToDockerfile}:\n{e}");

                return;
            }
        }

        /// <returns>isSuccess</returns>
        public static bool OpenDockerfile(HathoraServerPaths _paths)
        {
            try
            {
                Process.Start(new ProcessStartInfo(_paths.PathToDotHathoraDockerfile)
                {
                    // tell the system to use its file association info to open file
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[HathoraDocker.OpenDockerfile] Error: {e.Message}");
                return false; // !isSuccess
            }

            return true; // isSuccess
        }

        /// <summary>
        /// Writes dynamic paths
        /// TODO: Add opts to customize the Dockerfile without editing directly.
        /// </summary>
        /// <param name="_serverPaths"></param>
        /// <param name="_extraLaunchArgs">
        /// - Default Unity args: "-batchmode", "-nographics"
        /// - Default Hathora args: "-mode server" (for use with HathoraArgHandlerBase)
        /// </param>
        /// <returns>"path/to/DockerFile"</returns>
        public static string GenerateDockerFileStr(
            HathoraServerPaths _serverPaths, 
            List<string> _extraLaunchArgs = null)
        {
            string relativePathToBuildDir = $"./{_serverPaths.ExeBuildDirName}"; 
            string fileFriendlyShortDateTime = HathoraEditorUtils.GetFileFriendlyDateTime(DateTime.Now);

            List<string> unityLaunchArgs = new() { "-batchmode", "-nographics" };
            List<string> hathoraLaunchArgs = new() { "-mode server" }; // for use with HathoraArgHandlerBase
            _extraLaunchArgs ??= new List<string>();
            List<string> allArgs = new List<string>{ $"./{_serverPaths.ExeBuildName}" } // 2 spaces for formatting
                .Concat(unityLaunchArgs)
                .Concat(hathoraLaunchArgs)
                .Concat(_extraLaunchArgs)
                .ToList();

            // Combine args into one list, one arg per line with Dockerfile formatting; eg:
            // ```
            //   "-batchmode", \
            //   "-nographics"
            // ```
            string launchArgs = string.Join(
                $", \\{Environment.NewLine}  ", 
                allArgs.Select(arg => $"\"{arg}\""));

            string dockerStr = $@"############################################################################
# This Dockerfile is auto-generated by {nameof(HathoraDocker)}.cs @ {fileFriendlyShortDateTime}
############################################################################

# Using Ubuntu LTS
FROM ubuntu:22.04

# Copy the server build files into the container, if Dockerfile is @ parent
COPY {relativePathToBuildDir} .

# Give execute permission for the script
RUN chmod +x ./{_serverPaths.ExeBuildName}

# Run the Linux server in headless mode as a dedicated server
# Add `-scene <sceneName>` to load a scene before loading the mode
CMD [ \
  {launchArgs} \
]
";
            
            Debug.Log($"[GenerateDockerFileStr] Generated: <color=yellow>\n" +
                $"`{dockerStr}`</color> ...");

            return dockerStr;
        }
        
        #region Photon
        /// <summary>
        /// This will be the default Dockerfile for Photon Fusion, adding `extraLaunchArgs`.
        /// These are all fallbacks: They can be overridden @ Networking.cs, if `GameMode is Server`.
        /// </summary>
        /// <param name="_serverPaths"></param>
        /// <param name="_fallbackGameMode"></param>
        /// <param name="_fallbackMaxPlayers"></param>
        /// <param name="_fallbackSceneName"></param>
        /// <param name="_fallbackPhotonRegion"></param>
        /// <param name="_fallbackServerName"></param>
        /// <param name="_fallbackServerSessionName"></param>
        /// <param name="_fallbackPort"></param>
        /// <returns></returns>
        public static string GeneratePhotonFusionDockerfileStr(
            HathoraServerPaths _serverPaths,
            string _fallbackGameMode = "deathmatch",
            int _fallbackMaxPlayers = 5,
            string _fallbackSceneName = "GenArea2",
            string _fallbackPhotonRegion = "us",
            string _fallbackServerName = "hathoraDeployedServer",
            string _fallbackServerSessionName = "hathoraDeployedServerSession",
            int _fallbackPort = 7777
            )
        {
            // #############################################
            // -dedicatedServer
            // -deathmatch
            // -maxPlayers 5
            // -scene GenArea2
            // -region us
            // -serverName hathoraDeployedServer
            // -sessionName hathoraDeployedServerSession
            // -port 7777
            // #############################################
            List<string> photonArgs = new()
            {
                "-dedicatedServer", // Always, for headless builds
                _fallbackGameMode, // "deathmatch"
                $"-maxPlayers {_fallbackMaxPlayers}", // "5"
                $"-scene {_fallbackSceneName}", // "GenArea2"
                $"-region {_fallbackPhotonRegion}", // "us"
                $"-serverName {_fallbackServerName}", // "hathoraDeployedServer"
                $"-sessionName {_fallbackServerSessionName}", // "hathoraDeployedServerSession"
                $"-port {_fallbackPort}", // "7777"
            };

            return GenerateDockerFileStr(_serverPaths, photonArgs);
        }
        #endregion // Photon
    }
}
