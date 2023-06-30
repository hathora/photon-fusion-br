// Created by dylan@hathora.dev

using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Hathora.Cloud.Sdk.Api;
using Hathora.Cloud.Sdk.Client;
using Hathora.Cloud.Sdk.Model;
using Hathora.Core.Scripts.Runtime.Client.Config;
using UnityEngine;

namespace Hathora.Core.Scripts.Runtime.Client.ApiWrapper
{
    /// <summary>
    /// * Call Init() to pass UserConfig/instances.
    /// * Does not handle UI.
    /// * Does not handle Session caching.
    /// </summary>
    public class NetHathoraClientProcessApi : NetHathoraClientApiBase
    {
        private ProcessesV1Api processesApi;

        /// <summary>
        /// </summary>
        /// <param name="_hathoraClientConfig"></param>
        /// <param name="_hathoraSdkConfig">
        /// Passed along to base for API calls as `HathoraSdkConfig`; potentially null in child.
        /// </param>
        public override void Init(
            HathoraClientConfig _hathoraClientConfig, 
            Configuration _hathoraSdkConfig = null)
        {
            Debug.Log("[NetHathoraClientProcessesApi] Initializing API...");
            base.Init(_hathoraClientConfig, _hathoraSdkConfig);
            this.processesApi = new ProcessesV1Api(base.HathoraSdkConfig);
        }


        #region Client Process Async Hathora SDK Calls
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Gets process connection info, like ip:port.
        /// (!) We'll poll until we have an `Active` Status: Be sure to await!
        /// </summary>
        /// <param name="processId">Get this from env variable HATHORA_PROCESS_ID</param>
        /// <param name="pollIntervalSecs"></param>
        /// <param name="pollTimeoutSecs"></param>
        /// <param name="_cancelToken"></param>
        /// <returns>Process on success</returns>
        public async Task<Process> ClientGetProcessInfoAsync(
            string processId, 
            int pollIntervalSecs = 1, 
            int pollTimeoutSecs = 15,
            CancellationToken _cancelToken = default)
        {
            // Poll until we get the `Active` status.
            int pollSecondsTicked; // Duration to be logged later
            Process processInfoResponse = null;
            
            for (pollSecondsTicked = 0; pollSecondsTicked < pollTimeoutSecs; pollSecondsTicked++)
            {
                _cancelToken.ThrowIfCancellationRequested();
                
                try
                {
                    Debug.Log("Try GetProcessInfoAsync(), appId: " + HathoraClientConfig.AppId + ", ProcessId: " +
                              processId);
                    processInfoResponse = await processesApi.GetProcessInfoAsync(
                        HathoraClientConfig.AppId, 
                        processId,
                        _cancelToken);
                }
                catch(ApiException apiException)
                {
                    HandleClientApiException(
                        nameof(NetHathoraClientProcessApi),
                        nameof(ClientGetProcessInfoAsync), 
                        apiException);
                    return null; // fail
                }

                
                if (processInfoResponse.ExposedPort != null)
                    break;
                
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSecs), _cancelToken);
            }

            // -----------------------------------------
            // We're done polling -- success or timeout?
            if (processInfoResponse?.ExposedPort == null)
            {
                Debug.LogError("[NetHathoraClientAuthApi.ClientGetConnectionInfoAsync] " +
                    "Error: Timed out");
                return null;
            }

            // Success
            Debug.Log($"[NetHathoraClientProcessesApi.ClientGetConnectionInfoAsync] Success " +
                $"(after {pollSecondsTicked}s polling): <color=yellow>" +
                $"connectionInfoResponse: {processInfoResponse.ToJson()}</color>");

            return processInfoResponse;
        }
        #endregion // Client Process Async Hathora SDK Calls
    }
}
