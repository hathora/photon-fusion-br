// Created by dylan@hathora.dev

using System;
using System.Threading;
using System.Threading.Tasks;
using Hathora.Cloud.Sdk.Client;
using Hathora.Cloud.Sdk.Model;
using Hathora.Core.Scripts.Runtime.Common.Utils;
using Hathora.Core.Scripts.Runtime.Server.Models;
using UnityEngine;

namespace Hathora.Core.Scripts.Runtime.Server
{
    /// <summary>
    /// Inits and centralizes all Hathora Server [runtime] API wrappers.
    /// 
    /// Unlike HathoraClientMgrBase, we don't need a parent since Server is lower-level
    /// than Client (eg: No UI, Session or net code specific to a platform).
    /// TODO: If this gets more complex, make it an abstract base class; parity with Client.
    /// </summary>
    public class HathoraServerMgr : MonoBehaviour
    {
        #region Vars
        [Header("(!) Top menu: Hathora/ServerConfigFinder")]
        [SerializeField]
        private HathoraServerConfig hathoraServerConfig;
        public HathoraServerConfig HathoraServerConfig
        {
            get {
				#if !UNITY_SERVER && !UNITY_EDITOR
				Debug.LogError("[HathoraServerMgr] (!) Tried to get hathoraServerConfig " +
                    "from Server when NOT a <server || editor>");
				return null;
				#endif // !UNITY_SERVER && !UNITY_EDITOR

                if (hathoraServerConfig == null)
                {
                    Debug.LogError("[HathoraServerMgr.hathoraServerConfig.get] HathoraServerMgr exists, " +
                        "but !HathoraServerConfig -- Did you forget to serialize a config into your scene?");
                }

                return hathoraServerConfig;
            }
        }
        
        [Header("API Wrappers for Hathora SDK")]
        [SerializeField]
        private ServerApiContainer serverApis;
        
        /// <summary>
        /// Get the Hathora Server SDK API wrappers for all Server APIs.
        /// (!) There may be high-level variants of the calls here; check 1st!
        /// </summary>
        public ServerApiContainer ServerApis => serverApis;

        private static HathoraServerMgr _singleton;
        public static HathoraServerMgr Singleton
        {
            get {
                if (_singleton == null)
                {
                    Debug.LogError("[HathoraServerMgr.Singleton.get] " +
                        "!Singleton -- Did you forget to add a `HathoraServerMgr` " +
                        "script to your scene (via a `HathoraManager` prefab?");
                    return null;
                }

                return _singleton;
            }
            private set => _singleton = value;
        }
        
        /// <summary>
        /// (!) This is set async on Awake; check for null.
        /// For the public accessor, `see GetSystemHathoraProcessAsync()`.
        /// </summary>
        private Process systemHathoraProcess;
        #endregion // Vars


        /// <summary>
        /// systemHathoraProcess tries to set async @ Awake, but it could still take some time.
        /// We'll await until != null for 5s before timing out. 
        /// </summary>
        /// <returns></returns>
        public async Task<Process> GetSystemHathoraProcessAsync()
        {
            if (hathoraServerConfig == null)
                return null;

            if (systemHathoraProcess != null)
                return systemHathoraProcess;
            
            // ------------
            // Await up to 5s to become !null =>
            CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(5));
            await HathoraTaskUtils.WaitUntil(() => 
                systemHathoraProcess != null, 
                _cancelToken: cancellationTokenSource.Token);

            while (systemHathoraProcess == null)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                    throw new TimeoutException("[HathoraServerMgr.GetSystemHathoraProcessAsync] Timed out");

                await Task.Delay(
                    TimeSpan.FromMilliseconds(100), 
                    cancellationTokenSource.Token);
            }

            return systemHathoraProcess;
        }        
        
        #region Init
        private void Awake()
        {
            #if !UNITY_SERVER && !UNITY_EDITOR
            Destroy(this);
            return;
            #endif // !UNITY_SERVER

            Debug.Log("[HathoraServerMgr] Awake");
            setSingleton();
            
            // Unlike Client calls, we can init immediately @ Awake
            validateReqs();
            initApis(_hathoraSdkConfig: null); // Base will create this
            
            _ = getHathoraProcessAsync(); // !await
        }

        private void setSingleton()
        {
            if (Singleton != null)
            {
                Debug.LogError("[HathoraServerMgrsetSingleton.] Error: " +
                    "setSingleton: Destroying dupe");
                
                Destroy(gameObject);
                return;
            }

            Singleton = this;
        }

        /// <summary>
        /// Gets the Server process info by a special env var that's
        /// *always* included (automatically) in Hathora deployments.
        /// </summary>
        private async Task getHathoraProcessAsync()
        {
            string HATHORA_PROCESS_ID = Environment.GetEnvironmentVariable("HATHORA_PROCESS_ID");
            
            Debug.Log($"[getHathoraProcessAsync.getHathoraProcessAsync] " +
                $"HATHORA_PROCESS_ID: `{HATHORA_PROCESS_ID}`");
            
            bool hasHathoraProcId = !string.IsNullOrEmpty(HATHORA_PROCESS_ID);
            if (!hasHathoraProcId)
                return;
            
            this.systemHathoraProcess = await serverApis.ServerProcessApi.GetProcessInfoAsync(HATHORA_PROCESS_ID);
        }

        private void validateReqs()
        {
            if (hathoraServerConfig == null)
            {
                Debug.LogError("[HathoraServerMgr] !HathoraServerConfig; " +
                    $"Serialize to {gameObject.name}.{nameof(HathoraServerMgr)}");
            }
        }
        
        /// <summary>
        /// Init all Server [runtime] API wrappers. Uses serialized HathoraServerConfig.
        /// </summary>
        /// <param name="_hathoraSdkConfig">We'll automatically create this, if empty</param>
        private void initApis(Configuration _hathoraSdkConfig = null)
        {
            if (serverApis.ServerProcessApi != null)
                serverApis.ServerProcessApi.Init(hathoraServerConfig, _hathoraSdkConfig);
        }
        #endregion // Init
    }
}
