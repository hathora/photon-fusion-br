// Created by dylan@hathora.dev

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hathora.Core.Scripts.Editor.Common;
using Hathora.Core.Scripts.Runtime.Server.Models;
using Debug = UnityEngine.Debug;

namespace Hathora.Core.Scripts.Editor.Server
{
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
        /// <returns>"path/to/DockerFile"</returns>
        public static string GenerateDockerFileStr(HathoraServerPaths _serverPaths)
        {
            string relativePathToBuildDir = $"./{_serverPaths.ExeBuildDirName}"; 
            string fileFriendlyShortDateTime = HathoraEditorUtils.GetFileFriendlyDateTime(DateTime.Now);

            string dockerStr = $@"############################################################################
# This Dockerfile is auto-generated by {nameof(HathoraDocker)}.cs @ {fileFriendlyShortDateTime}
############################################################################

FROM ubuntu

# Copy the server build files into the container, if Dockerfile is @ parent
COPY {relativePathToBuildDir} .

RUN chmod +x ./{_serverPaths.ExeBuildName}

# Run the Linux server in headless mode as a dedicated server
CMD ./{_serverPaths.ExeBuildName} -mode server -batchmode -nographics
";
            
            Debug.Log($"[GenerateDockerFileStr] Generated: <color=yellow>\n" +
                $"`{dockerStr}`</color> ...");

            return dockerStr;
        }
    }
}
