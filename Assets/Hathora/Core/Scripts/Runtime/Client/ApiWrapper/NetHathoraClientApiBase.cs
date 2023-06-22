// Created by dylan@hathora.dev

using Hathora.Cloud.Sdk.Client;
using Hathora.Core.Scripts.Runtime.Client.Config;
using UnityEngine;

namespace Hathora.Core.Scripts.Runtime.Client.ApiWrapper
{
    /// <summary>
    /// This allows the API to view UserConfig (eg: AppId), set session and auth tokens.
    /// Both Client and Server APIs can inherit from this.
    /// </summary>
    public abstract class NetHathoraClientApiBase : MonoBehaviour
    {
        protected Configuration HathoraSdkConfig { get; private set; }
        protected HathoraClientConfig HathoraClientConfig { get; private set; }


        /// <summary>
        /// Init anytime. Client calls use V1 auth token.
        /// </summary>
        /// <param name="_hathoraClientConfig">Find via Unity editor top menu: Hathora >> Find Configs</param>
        /// <param name="_hathoraSdkConfig">SDKConfig that we pass to Hathora API calls</param>
        public virtual void Init(
            HathoraClientConfig _hathoraClientConfig,
            Configuration _hathoraSdkConfig = null)
        {
            this.HathoraClientConfig = _hathoraClientConfig;
            this.HathoraSdkConfig = _hathoraSdkConfig ?? 
                GenerateSdkConfig(_hathoraClientConfig);
        }
        
        protected static void HandleClientApiException(
            string _className,
            string _funcName,
            ApiException _apiException)
        {
            Debug.LogError($"[{_className}.{_funcName}] API Error: " +
                $"{_apiException.ErrorCode} {_apiException.ErrorContent} | {_apiException.Message}");

            throw _apiException;
        }

        private static Configuration GenerateSdkConfig(
            HathoraClientConfig _hathoraClientConfig) => new();
    }
}
