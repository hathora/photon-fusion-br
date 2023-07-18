using System.Collections;
using System.Threading.Tasks;
using Fusion;
using Hathora.Core.Scripts.Runtime.Client.Models;
using Hathora.Demos.Shared.Scripts.Client.ClientMgr;
using UnityEngine;

namespace TPSBR.HathoraPhoton
{
    public class HathoraClientPhotonMgr : HathoraClientMgrBase
    {
        public static HathoraClientPhotonMgr Singleton { get; private set; }
        public AuthResult AuthInfo { get; private set; }
        
        
        #region Base implementation
        protected override void OnAwake()
        {
            setSingleton();
        }

        public override async Task<bool> ConnectAsClient()
        {
            _ = await ClientApis.ClientAuthApi.ClientAuthAsync();
            return AuthInfo.IsSuccess;
        }

        public override Task StartServer()
        {
            throw new System.NotImplementedException();
            return Task.CompletedTask;
        }

        public override Task StartClient()
        {
            throw new System.NotImplementedException();
            return Task.CompletedTask;
        }

        public override Task StopHost()
        {
            throw new System.NotImplementedException();
            return Task.CompletedTask;
        }

        public override Task StopServer()
        {
            throw new System.NotImplementedException();
            return Task.CompletedTask;
        }

        public override Task StopClient()
        {
            throw new System.NotImplementedException();
            return Task.CompletedTask;
        }
        #endregion // Base implementation


        /// <summary>
        /// IEnumerator wrapper for async Task ConnectAsClient(). yield return this.
        /// </summary>
        /// <returns></returns>
        public IEnumerator _ConnectAsClient()
        {
            // Coroutine workaround for async/await: Loop the Task until we have a result =>
            bool isDone = false;
            ConnectAsClient().ContinueWith(
                task =>
                {
                    isDone = true;

                    if (task.IsFaulted)
                    {
                        Debug.LogError($"[{nameof(_ConnectAsClient)}] {nameof(ConnectAsClient)} => " +
                            $"failed: {task.Exception}");
                    }
                });
                
            // Coroutine workaround for async/await's `await` =>
            yield return new WaitUntil(() => isDone);
        }
        
        protected virtual void setSingleton()
        {
            if (Singleton != null)
            {
                Debug.LogError("[HathoraClientPhotonMgr]**ERR @ setSingleton: Destroying dupe");
                Destroy(gameObject);
                return;
            }
            
            Singleton = this;
        }
    }
}
