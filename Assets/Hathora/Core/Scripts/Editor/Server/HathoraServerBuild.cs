// Created by dylan@hathora.dev

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hathora.Core.Scripts.Runtime.Server;
using Hathora.Core.Scripts.Runtime.Server.Models;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Hathora.Core.Scripts.Editor.Server
{
    /// <summary>
    /// Contains build + deploy methods for Hathora Server.
    /// Trigger these from HathoraServerConfig ScriptableObject buttons. 
    /// </summary>
    public static class HathoraServerBuild
    {
        /// <summary>
        /// Builds with HathoraServerConfig opts.
        /// </summary>
        /// <param name="_serverConfig">Find via menu `Hathora/Find Server Config(s)`</param>
        /// <param name="_overwriteExistingDockerfile">
        /// Some devs have a custom Dockerfile and don't want a new autogen
        /// </param>
        /// <param name="_cancelToken">This won't cancel the build itself, but things around it.</param>
        /// <returns>isSuccess</returns>
        public static async Task<BuildReport> BuildHathoraLinuxServer(
            HathoraServerConfig _serverConfig,
            bool _overwriteExistingDockerfile,
            CancellationToken _cancelToken = default)
        {
            // Throughout this process, we'll lose focus on the config object.
            UnityEngine.Object previousSelection = Selection.activeObject; // Preserve focus - restore at end
            
            // Prep logs cache
            _serverConfig.LinuxHathoraAutoBuildOpts.LastBuildReport = null;
            StringBuilder strb = _serverConfig.LinuxHathoraAutoBuildOpts.LastBuildLogsStrb;
            strb.Clear();
            strb.AppendLine("Preparing server build...");
            strb.AppendLine($"overwriteExistingDockerfile? {_overwriteExistingDockerfile}");
            strb.AppendLine();
            
            // Set your build options
            HathoraServerPaths configPaths = new(_serverConfig);

            // Create the build directory if it does not exist
            strb.AppendLine($"Cleaning/creating build dir @ path: `{configPaths.PathToBuildDir}` ...")
                .AppendLine();
            
            cleanCreateBuildDir(_serverConfig, configPaths.PathToBuildDir);
            _cancelToken.ThrowIfCancellationRequested();

            BuildPlayerOptions buildPlayerOptions = generateBuildPlayerOptions(
                _serverConfig,
                configPaths.PathToBuildExe);

            // Build the server
            strb.AppendLine("BUILDING now (this may take a while), with opts:")
                .AppendLine("```")
                .AppendLine(getBuildOptsStr(buildPlayerOptions))
                .AppendLine("```")
                .AppendLine();
            
            BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            _cancelToken.ThrowIfCancellationRequested();
            
            // Generate the Dockerfile to `.hathora/`: Paths will be different for each collaborator
            bool genDockerfile = _overwriteExistingDockerfile || !CheckIfDockerfileExists(configPaths);
            if (genDockerfile)
            {
                strb.AppendLine($"Generating Dockerfile to `{configPaths.PathToDotHathoraDockerfile}` ...");
                Debug.Log("[HathoraServerBuild.BuildHathoraLinuxServer] " +
                    "Generating new Dockerfile (if exists: overwriting)...");
                
                string dockerFileContent = HathoraDocker.GenerateDockerFileStr(configPaths);

                strb.AppendLine("```")
                    .AppendLine(dockerFileContent)
                    .AppendLine("```")
                    .AppendLine();

                await HathoraDocker.WriteDockerFileAsync(
                    configPaths.PathToDotHathoraDockerfile,
                    dockerFileContent,
                    _cancelToken);    
            }

            // Did we fail?
            if (buildReport.summary.result != BuildResult.Succeeded)
            {
                Selection.activeObject = previousSelection; // Restore focus
                return buildReport; // fail
            }

            // Open the build directory - this will lose focus of the inspector
            // TODO: Play a small, subtle chime sfx?
            strb.AppendLine("Opening build dir ...");
            Debug.Log("[HathoraServerBuild.BuildHathoraLinuxServer] " +
                $"Build succeeded @ path: `{configPaths.PathToBuildDir}`");
            
            EditorUtility.RevealInFinder(configPaths.PathToBuildExe);
            cacheFinishedBuildReportLogs(_serverConfig, buildReport);

            Selection.activeObject = previousSelection; // Restore focus
            return buildReport;
        }

        private static string getBuildOptsStr(BuildPlayerOptions _buildOpts)
        {
            return $"scenes: `{string.Join("`, `", _buildOpts.scenes)}`\n\n" +
                $"locationPathName: `{_buildOpts.locationPathName}`\n" +
                $"target: `{_buildOpts.target}`\n" +
                $"options: `{_buildOpts.options}`\n" +
                $"standaloneBuildSubtarget `{_buildOpts.subtarget}`";
        }

        private static void cacheFinishedBuildReportLogs(
            HathoraServerConfig _serverConfig, 
            BuildReport _buildReport)
        {
            _serverConfig.LinuxHathoraAutoBuildOpts.LastBuildReport = _buildReport;
            
            TimeSpan totalTime = _buildReport.summary.totalTime;
            _serverConfig.LinuxHathoraAutoBuildOpts.LastBuildLogsStrb
                .AppendLine($"result: {Enum.GetName(typeof(BuildResult), _buildReport.summary.result)}")
                .AppendLine($"totalSize: {_buildReport.summary.totalSize / (1024 * 1024)}MB")
                .AppendLine($"totalTime: {totalTime.Minutes} mins, {totalTime.Seconds} secs")
                .AppendLine($"totalWarnings: {_buildReport.summary.totalWarnings.ToString()}")
                .AppendLine($"totalErrors: {_buildReport.summary.totalErrors.ToString()}")
                .AppendLine()
                .AppendLine("BUILD DONE.")
                .AppendLine();
        }

        /// <summary></summary>
        /// <param name="_paths">Create this from a ServerConfig</param>
        public static bool CheckIfDockerfileExists(HathoraServerPaths _paths) => 
            File.Exists(_paths.PathToDotHathoraDockerfile);

        private static BuildPlayerOptions generateBuildPlayerOptions(
            HathoraServerConfig _serverConfig, 
            string _serverBuildExeFullPath)
        {
            EditorBuildSettingsScene[] scenesInBuildSettings = EditorBuildSettings.scenes; // From build settings
            string[] scenePaths = scenesInBuildSettings?.Select(scene => scene.path).ToArray();
            
            BuildPlayerOptions buildPlayerOpts = new()
            {
                scenes = scenePaths,
                locationPathName = _serverBuildExeFullPath,
                target = BuildTarget.StandaloneLinux64,
                options = _serverConfig.LinuxHathoraAutoBuildOpts.IsDevBuild 
                    ? BuildOptions.Development 
                    : BuildOptions.None,
            };
            
            // Ensure build is a headless Linux server (formerly set via `options = BuildOptions.EnableHeadlessMode`)
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

            return buildPlayerOpts;
        }

        private static void cleanCreateBuildDir(
            HathoraServerConfig _serverConfig,
            string _serverBuildDirPath)
        {
            bool targetBuildDirExists = Directory.Exists(_serverBuildDirPath);

            if (_serverConfig.LinuxHathoraAutoBuildOpts.CleanBuildDir && targetBuildDirExists)
            {
                Debug.Log("[HathoraServerBuild] Found old build dir && " +
                    "_serverConfig.LinuxHathoraAutoBuildOpts.CleanBuildDir: Deleting...");
                Directory.Delete(_serverBuildDirPath, recursive: true);
            }
                
            if (!targetBuildDirExists)
                Directory.CreateDirectory(_serverBuildDirPath);
        }
    }
}
