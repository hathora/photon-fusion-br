// Created by dylan@hathora.dev

using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Hathora.Cloud.Sdk.Model;
using Newtonsoft.Json;
using TMPro;
using TPSBR;
using UnityEngine;
using UnityEngine.UI;
using HathoraRegion = Hathora.Cloud.Sdk.Model.Region;
using Assert = UnityEngine.Assertions.Assert;
using Debug = UnityEngine.Debug;

namespace HathoraPhoton
{
    /// <summary>Early logic originally ported over from Photon's Networking.cs</summary>
    public class HathoraMatchmaking : Matchmaking
    {
        #region Serialized
        [Header("Pre-Create")]
        [SerializeField]
        private TextMeshProUGUI hathoraCreateStatusTxt;
        [SerializeField]
        private Button createLobbyBtn;
        
        [Header("Post-Create")]
        [SerializeField]
        private TextMeshProUGUI hathoraCreateDoneStatusTxt;
        [SerializeField]
        private GameObject createSettingsModalPnl;
        #endregion // Serialized
    
    
        private static HathoraRegion HATHORA_FALLBACK_REGION => HathoraRegion.WashingtonDC;
        private static HathoraPhotonClientMgr clientMgr => HathoraPhotonClientMgr.Singleton;

    
        #region Base Overrides
        protected override void OnAwake()
        {
            base.OnAwake();
            Debug.Log("[HathoraMatchmaking] OnAwake");
        }
    
        /// <summary>Host only</summary>
        /// <param name="request"></param>
        public override async void CreateSession(SessionRequest request)
        {
            if (request.GameMode != GameMode.Host)
            {
                // Continue as normal ->
                base.CreateSession(request);
                return;
            }

            if (request.GameMode == GameMode.Host)
            {
                // ##############################################################
                // "Host" Acting as a Hathora Client:
                //
                // 1. Ensure authed; already via HathoraPhotonClientMgr.Awake()
                // 2. Create Lobby (a Room with server browsing capabilities)
                // 3. Wait for Lobby to be created
                // 4. Hide the modal create settings window and show success status
                //
                // You should now be able to see the lobby within the list
                // ##############################################################
                await createHathoraSessionAsync(request);
            }
        }
        #endregion // Base Overrides
    

        /// <summary>High-level, async Task wrapper for CreateSession</summary>
        /// <param name="_request"></param>
        private async Task createHathoraSessionAsync(SessionRequest _request)
        {
            // Photon bypasses the Button interactable check 
            if (!createLobbyBtn.interactable)
                return;
            
            string logPrefix = $"[HathoraMatchmaking.{nameof(createHathoraSessionAsync)}]";
            Debug.Log($"{logPrefix} (as Photon 'Host')");
        
            // Ensure authed; already via HathoraPhotonClientMgr.Awake()
            if (!clientMgr.HathoraClientSession.IsAuthed)
            {
                Debug.Log($"{logPrefix} !IsAuthed");
                return;
            }

            toggleCreateUi(_isCreating: true);
        
            // Create Lobby (a Room with server browsing capabilities)
            Lobby lobby = await photonHostCreateHathoraLobbyAsync(_request);
            Assert.IsNotNull(lobby?.RoomId, $"{logPrefix} Expected Lobby?.RoomId");
            
            // Wait for Lobby to be created
            ConnectionInfoV2 connectionInfo = await clientMgr.GetActiveConnectionInfo(lobby.RoomId);
            // IPAddress ip = await HathoraUtils.ConvertHostToIpAddress(connectionInfo.ExposedPort.Host);

            // Hide the modal create settings window and show success status
            onCreateLobbySuccessUI(lobby);
        }

        private void toggleCreateUi(bool _isCreating)
        {
            hathoraCreateStatusTxt.gameObject.SetActive(_isCreating);
            createLobbyBtn.gameObject.SetActive(!_isCreating);
        }

        /// <summary>
        /// Hide the modal create settings window and show success status
        /// </summary>
        private void onCreateLobbySuccessUI(Lobby _lobby)
        {
            Debug.Log($"[HathoraMatchmaking] onCreateLobbySuccessUI");
        
            hathoraCreateDoneStatusTxt.text = $"Created Lobby: {_lobby.RoomId}";
            hathoraCreateDoneStatusTxt.gameObject.SetActive(true);
            createSettingsModalPnl.SetActive(false);
            
            toggleCreateUi(_isCreating: false);
        }

        /// <summary>
        /// Creates a Hathora Lobby (extended Room) with the given SessionRequest as initConfig.
        /// Photon Region is converted to the closest Hathora Region.
        /// Awaits Lobby creation, then gets the connect info.
        /// This could take a few moments (~5s).
        /// </summary>
        /// <param name="_request"></param>
        /// <returns></returns>
        private async Task<Lobby> photonHostCreateHathoraLobbyAsync(SessionRequest _request)
        {
            _request.UserID      = Context.PlayerData.UserID;
            _request.CustomLobby = GetLobbyName();
        
            // Get the selected Photon Region -> Map to closest Hathora Region
            Region hathoraRegion = getHathoraRegionFromPhoton();
            // string initConfigJsonStr = JsonConvert.SerializeObject(someStatefulConfig); // TODO
        
            string initConfigJsonStr = JsonConvert.SerializeObject(_request);

            Lobby lobby = await clientMgr.CreateLobbyAsync(
                hathoraRegion,
                CreateLobbyRequest.VisibilityEnum.Public,
                initConfigJsonStr);

            Assert.IsNotNull(lobby?.RoomId, "!lobby.RoomId");


            return lobby;
        }
    
        private static Region getHathoraRegionFromPhoton()
        {
            string photonRegionStr = PhotonAppSettings.Instance.AppSettings.FixedRegion;
            bool hasPhotonRegionStr = !string.IsNullOrEmpty(photonRegionStr);

            Region hathoraRegion = hasPhotonRegionStr
                ? (Region)HathoraRegionMap.GetHathoraRegionFromPhoton(photonRegionStr)
                : HATHORA_FALLBACK_REGION;

            return hathoraRegion;
        }
    }
}
