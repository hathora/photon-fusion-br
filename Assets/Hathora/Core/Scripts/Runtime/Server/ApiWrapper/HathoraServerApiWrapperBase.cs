// Created by dylan@hathora.dev

using Hathora.Cloud.Sdk.Client;
using Hathora.Core.Scripts.Runtime.Common.Models;
using UnityEngine;

namespace Hathora.Core.Scripts.Runtime.Server.ApiWrapper
{
    /// <summary>
    /// This allows the API to view UserConfig (eg: AppId), set session and auth tokens.
    /// Both Client and Server APIs can inherit from this.
    /// Unlike Client API wrappers (since !Mono), we init via Constructor instead of Init().
    /// </summary>
    public abstract class HathoraServerApiWrapperBase : MonoBehaviour, IHathoraApiBase
    {
        public Configuration HathoraSdkConfig { get; set; }
        protected HathoraServerConfig HathoraServerConfig { get; private set; }
        
        // Shortcuts
        public string AppId => HathoraServerConfig.HathoraCoreOpts.AppId;
        

        #region Init
        /// <summary>
        /// Init anytime before calling an API. Server calls use auth token from HathoraServerInfo.
        /// </summary>
        /// <param name="_hathoraServerConfig">Find via Unity editor top menu: Hathora/Configuration</param>
        /// <param name="_hathoraSdkConfig">SDKConfig that we pass to Hathora API calls</param>
        public virtual void Init(
            HathoraServerConfig _hathoraServerConfig,
            Configuration _hathoraSdkConfig = null)
        {
            this.HathoraServerConfig = _hathoraServerConfig;
            this.HathoraSdkConfig = _hathoraSdkConfig ?? GenerateSdkConfig();
        }
        
        public Configuration GenerateSdkConfig() => new()
        {
            AccessToken = HathoraServerConfig.HathoraCoreOpts.DevAuthOpts.DevAuthToken,
        };        
        #endregion // Init
        

        /// <summary>
        /// Server calls use Dev token.
        /// </summary>
        /// <param name="_hathoraServerConfig">
        /// Find via Unity editor top menu: Hathora >> Find Configs
        /// </param>
        /// <param name="_hathoraSdkConfig">
        /// Passed along to base for API calls as `HathoraSdkConfig`; potentially null in children.
        /// </param>
        protected HathoraServerApiWrapperBase(
            HathoraServerConfig _hathoraServerConfig,
            Configuration _hathoraSdkConfig = null)
        {
            this.HathoraServerConfig = _hathoraServerConfig;
            this.HathoraSdkConfig = _hathoraSdkConfig ?? GenerateSdkConfig();
        }

        public void HandleApiException(
            string _className, 
            string _funcName,
            ApiException _apiException)
        {
            UnityEngine.Debug.LogError($"[{_className}.{_funcName}] API Error: " +
                $"{_apiException.ErrorCode} {_apiException.ErrorContent} | {_apiException.Message}");

            throw _apiException;
        }
    }
}
