using System.Collections;
using System.Threading.Tasks;
using Hathora.Core.Scripts.Runtime.Client.Models;
using Hathora.Demos.Shared.Scripts.Client.ClientMgr;
using TPSBR.Hathora.Demos.Shared.Scripts.Client.ClientMgr;
using UnityEngine;

namespace TPSBR.HathoraPhoton
{
    public class HathoraPhotonClientMgr : HathoraClientMgrBase
    {
        public static HathoraPhotonClientMgr Singleton { get; private set; }
        
        
        #region Base implementation
        protected override void OnAwake()
        {
            setSingleton();
        }

        public override async Task<bool> ConnectAsClient()
        {
            _ = await ClientApis.ClientAuthApi.ClientAuthAsync();
            return HathoraClientSession.IsAuthed;
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
