// Created by dylan@hathora.dev

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                Debug.Log("[HathoraServerDeploy.WriteDockerFileAsync] " +
                    "<color=orange>(!)</color> Deleting old Dockerfile...");
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
            //   "-nographics", \
            //   "-mode", "server"
            // ```
            // ^ Notice how "-mode", "server" key:value were in separate csv's but on same line
            string launchArgs = string.Join(
                $", \\{Environment.NewLine}  ", 
                allArgs.Select(arg => arg.Contains(" ") 
                
                    // Split arguments containing a space into separate quoted strings
                    ? $"\"{arg.Replace(" ", "\", \"")}\"" 
                    
                    // Quote arguments without a space
                    : $"\"{arg}\"")
            );


            string dockerStr = $@"############################################################################
# This Dockerfile is auto-generated by {nameof(HathoraDocker)}.cs @ {fileFriendlyShortDateTime}
############################################################################

# Using 'Jammy Jellyfish' Ubuntu LTS
FROM ubuntu:22.04

# Update system and install certificates: Prevents TLS/SSL (https) errs, notably with UnityWebRequests
RUN apt-get update && \
    apt-get install -y ca-certificates && \
    update-ca-certificates

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
    
            Debug.Log($"[GenerateDockerFileStr] Generated string (not yet written): " +
                $"<color=yellow>\n`{dockerStr}`</color> ...");

            return dockerStr;
        }
        
    }
}
