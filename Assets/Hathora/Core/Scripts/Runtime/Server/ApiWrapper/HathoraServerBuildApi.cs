// Created by dylan@hathora.dev

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hathora.Cloud.Sdk.Api;
using Hathora.Cloud.Sdk.Client;
using Hathora.Cloud.Sdk.Model;
using UnityEngine;

namespace Hathora.Core.Scripts.Runtime.Server.ApiWrapper
{
    public class HathoraServerBuildApi : HathoraServerApiBase
    {
        private readonly BuildV1Api buildApi;

        
        /// <summary>
        /// </summary>
        /// <param name="_hathoraServerConfig"></param>
        /// <param name="_hathoraSdkConfig">
        /// Set in base as `HathoraSdkConfig`; potentially null in child.
        /// </param>
        public HathoraServerBuildApi(
            HathoraServerConfig _hathoraServerConfig,
            Configuration _hathoraSdkConfig = null) 
            : base(_hathoraServerConfig, _hathoraSdkConfig)
        {
            Debug.Log("[HathoraServerBuildApi] Initializing API...");
            this.buildApi = new BuildV1Api(base.HathoraSdkConfig);
        }
        
        
        
        #region Server Build Async Hathora SDK Calls
        /// <summary>
        /// Wrapper for `CreateBuildAsync` to request an cloud build (_tarball upload).
        /// </summary>
        /// <param name="_cancelToken"></param>
        /// <returns>Returns Build on success >> Pass this info to RunCloudBuildAsync()</returns>
        public async Task<Build> CreateBuildAsync(CancellationToken _cancelToken = default)
        {
            Build createCloudBuildResult;
            
            try
            {
                createCloudBuildResult = await buildApi.CreateBuildAsync(
                    HathoraServerConfig.HathoraCoreOpts.AppId,
                    _cancelToken);
            }
            catch (ApiException apiException)
            {
                HandleServerApiException(
                    nameof(HathoraServerBuildApi),
                    nameof(CreateBuildAsync), 
                    apiException);
                return null;
            }

            Debug.Log($"[HathoraServerRoomApi.CreateBuildAsync] Success: " +
                $"<color=yellow>createCloudBuildResult: {createCloudBuildResult.ToJson()}</color>");

            return createCloudBuildResult;
        }

        /// <summary>
        /// Wrapper for `RunBuildAsync` to upload the _tarball after calling CreateBuildAsync().
        /// (!) After this is done
        /// </summary>
        /// <param name="_buildId"></param>
        /// <param name="_pathToTarGzBuildFile">Ensure path is normalized</param>
        /// <param name="_cancelToken"></param>
        /// <returns>Returns streamLogs on success</returns>
        public async Task<string> RunCloudBuildAsync(
            double _buildId, 
            string _pathToTarGzBuildFile,
            CancellationToken _cancelToken = default)
        {
            byte[] cloudRunBuildResultLogsStream;
                
            try
            {
                await using FileStream fileStream = new(_pathToTarGzBuildFile, FileMode.Open, FileAccess.Read);
                
                cloudRunBuildResultLogsStream = await buildApi.RunBuildAsync(
                    HathoraServerConfig.HathoraCoreOpts.AppId,
                    _buildId,
                    fileStream,
                    _cancelToken);
            }
            catch (ApiException apiException)
            {
                HandleServerApiException(
                    nameof(HathoraServerBuildApi),
                    nameof(RunCloudBuildAsync), 
                    apiException);
                return null;
            }

            Debug.Log($"[HathoraServerBuildApi.RunCloudBuildAsync] Done - " +
                "to know if success, call buildApi.RunBuild");

            // (!) Unity, by default, truncates logs to 1k chars (including callstack).
            string cloudRunBuildResultLogsStr = Encoding.UTF8.GetString(cloudRunBuildResultLogsStream);
            onRunCloudBuildDone(cloudRunBuildResultLogsStr);
            
            return cloudRunBuildResultLogsStr;  // streamLogs 
        }

        /// <summary>
        /// DONE - not necessarily success. Log stream every 500 lines
        /// (!) Unity, by default, truncates logs to 1k chars (including callstack).
        /// </summary>
        /// <param name="_cloudRunBuildResultLogsStr"></param>
        private static void onRunCloudBuildDone(string _cloudRunBuildResultLogsStr)
        {
            // Split string into lines
            string[] lines = _cloudRunBuildResultLogsStr.Split(new[] 
                { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Group lines into chunks of 500
            const int chunkSize = 500;
            for (int i = 0; i < lines.Length; i += chunkSize)
            {
                IEnumerable<string> chunk = lines.Skip(i).Take(chunkSize);
                string chunkStr = string.Join("\n", chunk);
                Debug.Log($"[HathoraServerBuildApi.onRunCloudBuildDone] result == chunk starting at line {i}: " +
                    $"\n<color=yellow>{chunkStr}</color>");
            }
        }

        /// <summary>
        /// Wrapper for `RunBuildAsync` to upload the _tarball after calling 
        /// </summary>
        /// <param name="_buildId"></param>
        /// <param name="_cancelToken"></param>
        /// <returns>Returns byte[] on success</returns>
        public async Task<Build> GetBuildInfoAsync(
            double _buildId,
            CancellationToken _cancelToken)
        {
            Build getBuildInfoResult;
            
            try
            {
                getBuildInfoResult = await buildApi.GetBuildInfoAsync(
                    HathoraServerConfig.HathoraCoreOpts.AppId,
                    _buildId,
                    _cancelToken);
            }
            catch (ApiException apiException)
            {
                HandleServerApiException(
                    nameof(HathoraServerBuildApi),
                    nameof(GetBuildInfoAsync), 
                    apiException);
                return null;
            }

            bool isSuccess = getBuildInfoResult is { Status: Build.StatusEnum.Succeeded };
            Debug.Log($"[HathoraServerRoomApi.GetBuildInfoAsync] Success? {isSuccess}, " +
                $"<color=yellow>createCloudBuildResult: {getBuildInfoResult.ToJson()}</color>");

            return getBuildInfoResult;
        }
        #endregion // Server Build Async Hathora SDK Calls
    }
}
