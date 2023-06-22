// Created by dylan@hathora.dev

using System.Threading;
using System.Threading.Tasks;
using Hathora.Cloud.Sdk.Api;
using Hathora.Cloud.Sdk.Client;
using Hathora.Cloud.Sdk.Model;
using Hathora.Core.Scripts.Runtime.Client.Config;
using Hathora.Core.Scripts.Runtime.Client.Models;
using UnityEngine;

namespace Hathora.Core.Scripts.Runtime.Client.ApiWrapper
{
    /// <summary>
    /// * Call Init() to pass UserConfig/instances.
    /// * Does not handle UI.
    /// </summary>
    public class NetHathoraClientAuthApi : NetHathoraClientApiBase
    {
        private AuthV1Api authApi;

        
        /// <summary>
        /// </summary>
        /// <param name="_hathoraClientConfig"></param>
        /// <param name="_netSession"></param>
        /// <param name="_hathoraSdkConfig">
        /// Passed along to base for API calls as `HathoraSdkConfig`; potentially null in child.
        /// </param>
        public override void Init(
            HathoraClientConfig _hathoraClientConfig, 
            NetSession _netSession,
            Configuration _hathoraSdkConfig = null)
        {
            Debug.Log("[NetHathoraClientAuthApi] Initializing API...");
            base.Init(_hathoraClientConfig, _netSession, _hathoraSdkConfig);
            this.authApi = new AuthV1Api(base.HathoraSdkConfig);
        }


        #region Client Auth Async Hathora SDK Calls
        /// <param name="_cancelToken"></param>
        /// <returns>Returns AuthResult on success</returns>
        public async Task<AuthResult> ClientAuthAsync(CancellationToken _cancelToken = default)
        {
            LoginResponse anonLoginResult;
            try
            {
                anonLoginResult = await authApi.LoginAnonymousAsync(
                    HathoraClientConfig.AppId, 
                    _cancelToken);
            }
            catch (ApiException apiException)
            {
                HandleClientApiException(
                    nameof(NetHathoraClientAuthApi),
                    nameof(ClientAuthAsync), 
                    apiException);
                return null;
            }

            bool isAuthed = !string.IsNullOrEmpty(anonLoginResult?.Token); 
            Debug.Log($"[NetHathoraClientAuthApi] isAuthed: {isAuthed}, " +
                $"<color=yellow>anonLoginResult: {anonLoginResult.ToJson()}</color>");

            if (!isAuthed)
                return null;
            
            NetSession.InitNetSession(anonLoginResult.Token);
            return new AuthResult(anonLoginResult.Token);
        }
        #endregion // Server Auth Async Hathora SDK Calls
    }
}
