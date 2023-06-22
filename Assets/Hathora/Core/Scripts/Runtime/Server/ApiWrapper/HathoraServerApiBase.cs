// Created by dylan@hathora.dev

using Hathora.Cloud.Sdk.Client;

namespace Hathora.Core.Scripts.Runtime.Server.ApiWrapper
{
    /// <summary>
    /// This allows the API to view UserConfig (eg: AppId), set session and auth tokens.
    /// Both Client and Server APIs can inherit from this.
    /// Unlike Client API wrappers (since !Mono), we init via Constructor instead of Init().
    /// </summary>
    public abstract class HathoraServerApiBase
    {
        protected Configuration HathoraSdkConfig { get; private set; }
        protected HathoraServerConfig HathoraServerConfig { get; private set; }
        
        // Shortcuts
        protected string AppId => HathoraServerConfig.HathoraCoreOpts.AppId;


        /// <summary>
        /// Server calls use Dev token.
        /// </summary>
        /// <param name="_hathoraServerConfig">
        /// Find via Unity editor top menu: Hathora >> Find Configs
        /// </param>
        /// <param name="_hathoraSdkConfig">
        /// Passed along to base for API calls as `HathoraSdkConfig`; potentially null in children.
        /// </param>
        protected HathoraServerApiBase(
            HathoraServerConfig _hathoraServerConfig,
            Configuration _hathoraSdkConfig = null)
        {
            this.HathoraServerConfig = _hathoraServerConfig;
            this.HathoraSdkConfig = _hathoraSdkConfig ?? GenerateSdkConfig(_hathoraServerConfig);
        }

        protected static void HandleServerApiException(
            string _className, 
            string _funcName,
            ApiException _apiException)
        {
            UnityEngine.Debug.LogError($"[{_className}.{_funcName}] API Error: " +
                $"{_apiException.ErrorCode} {_apiException.ErrorContent} | {_apiException.Message}");

            throw _apiException;
        }

        private static Configuration GenerateSdkConfig(HathoraServerConfig _hathoraClientConfig) => new()
        {
            AccessToken = _hathoraClientConfig.HathoraCoreOpts.DevAuthOpts.DevAuthToken,
        };
        
    }
}
