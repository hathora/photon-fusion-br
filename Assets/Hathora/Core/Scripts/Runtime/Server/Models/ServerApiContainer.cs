// Created by dylan@hathora.dev

using System;
using Hathora.Core.Scripts.Runtime.Server.ApiWrapper;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hathora.Core.Scripts.Runtime.Server.Models
{
    /// <summary>
    /// Server [runtime] API wrapper container to serialize in HathoraPlayer.
    /// 
    /// -> Have a new Hathora API to add?
    /// 1. Serialize it here, and add to `HathoraClientMgrBase.InitApis()`
    /// 2. Open scene `HathoraManager` GameObj (not prefab)
    /// 3. Add the new script component to HathoraManager.Hathora{Platform}ClientMgr.ClientApis
    /// 4. Add a new script[] to ClientApis container -> drag the script into the serialized field
    /// </summary>
    [Serializable]
    public struct ServerApiContainer
    {
        [Header("Hathora Server [runtime] API wrappers")]
        [SerializeField]
        private HathoraServerAppApi serverAppApi;
        public HathoraServerAppApi ServerAppApi => serverAppApi;
        
        // [SerializeField] // Not generally intended for runtime use
        // private HathoraServerBuildApi serverBuildApi;
        // public HathoraServerBuildApi ServerBuildApi => serverBuildApi;
        //
        // [SerializeField] // Not generally intended for runtime use
        // private HathoraServerDeployApi serverDeployApi;
        // public HathoraServerDeployApi ServerDeployApi => serverDeployApi;
        
        [SerializeField]
        private HathoraServerLobbyApi serverLobbyApi;
        public HathoraServerLobbyApi ServerLobbyApi => serverLobbyApi;
        
        [FormerlySerializedAs("_serverProcessApi")]
        [SerializeField]
        private HathoraServerProcessApi serverProcessApi;
        public HathoraServerProcessApi ServerProcessApi => serverProcessApi;
        
        [SerializeField]
        private HathoraServerRoomApi serverRoomApi;
        public HathoraServerRoomApi ServerRoomApi => serverRoomApi;
        
    }
}
