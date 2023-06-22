// Created by dylan@hathora.dev

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hathora.Cloud.Sdk.Model;
using Hathora.Core.Scripts.Editor.Common;
using Hathora.Core.Scripts.Runtime.Common.Extensions;
using Hathora.Core.Scripts.Runtime.Server;
using Hathora.Core.Scripts.Runtime.Server.ApiWrapper;
using Hathora.Core.Scripts.Runtime.Server.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hathora.Core.Scripts.Editor.Server.ConfigStyle.PostAuth
{
    public class HathoraConfigPostAuthBodyRoomUI : HathoraConfigUIBase
    {
        #region Vars
        private HathoraConfigPostAuthBodyRoomLobbyUI roomLobbyUI { get; set; }
        public static CancellationTokenSource CreateRoomCancelTokenSrc { get; set; } // TODO
        private const int CREATE_ROOM_TIMEOUT_SECONDS = 30;
        private bool isCreatingRoomAwaitingActiveStatus { get; set; }
        
        // Region lists
        private readonly List<string> displayOptsStrList;
        private readonly List<int> originalIndices;
        
        // Foldouts
        private bool isCreateRoomLobbyFoldout;
        #endregion // Vars


        #region Init
        public HathoraConfigPostAuthBodyRoomUI(
            HathoraServerConfig _serverConfig, 
            SerializedObject _serializedConfig)
            : base(_serverConfig, _serializedConfig)
        {
            if (!HathoraConfigUI.ENABLE_BODY_STYLE)
                return;
            
            initDrawUtils();
            
            // ----------------------
            // REGION LISTS >> We want to be able to sort them, yet still refer to original Enum index
            Dictionary<int, string> regionDisplayMap = 
                Enum.GetValues(typeof(Region))
                    .Cast<Region>()
                    .Select((Region region, int index) => new { region, index })
                    .ToDictionary(x => x.index, x => 
                        x.region.ToString().SplitPascalCase());

            displayOptsStrList = regionDisplayMap.Values.OrderBy(s => s).ToList();
            originalIndices = displayOptsStrList.Select(s => regionDisplayMap.First(kvp => kvp.Value == s).Key).ToList();
        }

        private void initDrawUtils()
        {
            roomLobbyUI = new HathoraConfigPostAuthBodyRoomLobbyUI(ServerConfig, SerializedConfig);
        }
        #endregion // Init
        
        
        #region UI Draw
        public void Draw()
        {
            if (!IsAuthed)
                return; // You should be calling HathoraConfigPreAuthBodyUI.Draw()

            insertCreateRoomOrLobbyFoldout();
        }

        private void insertCreateRoomOrLobbyFoldout()
        {
            isCreateRoomLobbyFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                isCreateRoomLobbyFoldout, 
                "Create Room");
            
            if (!isCreateRoomLobbyFoldout)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }
    
            EditorGUI.indentLevel++;
            InsertSpace2x();

            insertCreateRoomOrLobbyFoldoutComponents();

            EditorGUI.indentLevel--;
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void insertCreateRoomOrLobbyFoldoutComponents()
        {
            insertRegionHorizPopupList();

            bool enableCreateRoomBtn = ServerConfig.MeetsCreateRoomBtnReqs();
            insertCreateRoomLobbyBtnHelpboxOnErr(enableCreateRoomBtn);

            bool showCancelBtn = isCreatingRoomAwaitingActiveStatus && CreateRoomCancelTokenSrc.Token.CanBeCanceled; 
            if (showCancelBtn)
                insertCreateRoomLobbyCancelBtn(CreateRoomCancelTokenSrc);
            else
                insertCreateRoomLobbyBtn(enableCreateRoomBtn);

            bool hasLastRoomInfo = ServerConfig.HathoraLobbyRoomOpts.HasLastCreatedRoomConnection;
            if (hasLastRoomInfo)
                insertLastCreatedRoomInfoGroup();
            
            insertViewLogsMetricsLinkLbl();
        }

        private void insertLastCreatedRoomInfoGroup()
        {
            base.BeginPaddedBox();

            // GUI >>
            try
            {
                insertRoomLastCreatedHeaderLbl();
                insertRoomIdDateHorizGroup();
                insertRoomConnectionInfoBtnGroup();
                insertViewRoomInConsoleLinkLbl();   
            }
            catch (Exception e)
            {
                Debug.LogError($"[HathoraConfigPostAuthBodyRoomUI.insertLastCreatedRoomInfoGroup] " +
                    $"Error (skipping this group so UI can continue to load): {e}");
                throw;
            }

            EndPaddedBox();
        }

        private void insertRoomConnectionInfoBtnGroup()
        {
            EditorGUILayout.BeginHorizontal();

            insertRoomConnectionInfoSelectableLbl();
            insertCopyRoomConnectionInfoBtn();
            
            EditorGUILayout.EndHorizontal();
        }

        private void insertRoomIdDateHorizGroup()
        {
            EditorGUILayout.BeginHorizontal();
            
            insertRoomIdLbl();
            // insertRoomRegionLbl(); // Delayed
            insertRoomCreateDateLbl();
            
            EditorGUILayout.EndHorizontal();
            InsertSpace2x();
        }

        private void insertRoomCreateDateLbl()
        {
            DateTime? createdDateTime = ServerConfig.HathoraLobbyRoomOpts
                .LastCreatedRoomConnection?.Room?.CurrentAllocation?.ScheduledAt;

            string createdDateStr = createdDateTime.HasValue
                ? $"{createdDateTime.Value.ToShortDateString()} {createdDateTime.Value.ToShortTimeString()}"
                : "{Unknown DateTime}";

            string labelStr = $"<b>Created:</b> {createdDateStr}";
            InsertLabel(labelStr, _fontSize: 10);
        }

        private void insertRoomRegionLbl()
        {
            // TODO: Missing Region from LastCreatedRoomConnection - We don't want to assume from ServerConfig
            // InsertLabel("{Region}");
        }

        private void insertRoomConnectionInfoSelectableLbl()
        {
            string connInfoStr = ServerConfig.HathoraLobbyRoomOpts
                .LastCreatedRoomConnection?.GetConnInfoStr();
            
            InsertLabel(
                $"<b>Connection Info:</b> {connInfoStr}", 
                _fontSize: 10,
                _vertCenter: true,
                _selectable: false); // BUG: If true, there's a random indent
        }

        private void insertRoomIdLbl()
        {
            InsertLabel("<b>Room ID:</b> " + ServerConfig.HathoraLobbyRoomOpts.LastCreatedRoomConnection?
                .Room?.RoomId, _fontSize: 10);
            InsertSpace1x();
        }

        private void insertRoomLastCreatedHeaderLbl()
        {
            InsertLabel("<color=white><b>Last Created Room:</b></color>");
            InsertSpace1x();
        }

        private void insertCopyRoomConnectionInfoBtn()
        {
            // USER INPUT >>
            bool clickedRoomCopyConnInfoBtn = InsertLeftGeneralBtn("Copy Connection Info");
            if (clickedRoomCopyConnInfoBtn)
                onCopyRoomConnectionInfoBtnClick();
        }

        private void insertViewRoomInConsoleLinkLbl()
        {
            string appId = ServerConfig.HathoraCoreOpts?.AppId ?? "APP_ID_MISSING"; 
            string consoleAppUrl = $"{HathoraEditorUtils.HATHORA_CONSOLE_APP_BASE_URL}/{appId}";
            string processId = ServerConfig.HathoraLobbyRoomOpts.LastCreatedRoomConnection.Room?
                    .CurrentAllocation?.ProcessId ?? "PROCESS_ID_MISSING"; 
            string appUrl = $"{consoleAppUrl}/process/{processId}";
            
            InsertLinkLabel("View room in Hathora Console", appUrl, _centerAlign:false);
        }

        private void insertCreateRoomLobbyCancelBtn(CancellationTokenSource _cancelTokenSrc)
        {
            string btnLabelStr = $"<color={HathoraEditorUtils.HATHORA_PINK_CANCEL_COLOR_HEX}>" +
                "<b>Cancel</b> (Creating Room...)</color>";

            // USER INPUT >>
            bool clickedCancelBtn = GUILayout.Button(btnLabelStr, GeneralButtonStyle);
            if (clickedCancelBtn)
                onCreateRoomCancelBtnClick(_cancelTokenSrc);
        }

        /// <summary>
        /// Generally used for helpboxes to explain why a button is disabled.
        /// </summary>
        /// <param name="_serverConfig"></param>
        /// <param name="_includeMissingReqsFieldsPrefixStr">Useful if you had a combo of this </param>
        /// <returns></returns>
        public static StringBuilder GetCreateRoomMissingReqsStrb(
            HathoraServerConfig _serverConfig,
            bool _includeMissingReqsFieldsPrefixStr = true)
        {
            StringBuilder helpboxLabelStrb = new(_includeMissingReqsFieldsPrefixStr 
                ? "Missing required fields: " 
                : ""
            );
            
            // (!) Hathora SDK Enums start at index 1 (not 0)
            if (!_serverConfig.HathoraCoreOpts.HasAppId)
                helpboxLabelStrb.Append("`AppId` ");
            
            // (!) This is 0-indexed
            if (_serverConfig.HathoraLobbyRoomOpts.RegionSelectedIndex < 0)
                helpboxLabelStrb.Append("`Region` ");

            return helpboxLabelStrb;
        }
        
        private void insertCreateRoomLobbyBtnHelpboxOnErr(bool _enable)
        {
            if (_enable)
                return;
            
            // Explain why the button is disabled
            StringBuilder helpboxLabelStrb = GetCreateRoomMissingReqsStrb(ServerConfig);
            
            // Post the help box *before* we disable the button so it's easier to see (if toggleable)
            EditorGUILayout.HelpBox(helpboxLabelStrb.ToString(), MessageType.Error);
        }

        private void insertViewLogsMetricsLinkLbl()
        {
            InsertSpace2x();
            
            InsertCenterLabel("View logs and metrics for your active rooms and processes below:");

            string consoleAppUrl = HathoraEditorUtils.HATHORA_CONSOLE_APP_BASE_URL +
                $"/{ServerConfig.HathoraCoreOpts.AppId}";
            
            InsertLinkLabel(
                "Hathora Console",
                consoleAppUrl,
                _centerAlign: true);
            
            InsertSpace1x();
        }

        private void insertCreateRoomLobbyBtn(bool _enable)
        {
            string btnLabelStr = isCreatingRoomAwaitingActiveStatus 
                ? "Creating Room..." 
                : "Create Room";

            EditorGUI.BeginDisabledGroup(disabled: !_enable);
            
            bool clickedCreateRoomLobbyBtn = InsertLeftGeneralBtn(btnLabelStr);

            EditorGUI.EndDisabledGroup();
            InsertSpace1x();

            if (!clickedCreateRoomLobbyBtn)
                return;

            onCreateRoomLobbyBtnClick(); // !await

            // Deployment deployment = await HathoraServerDeploy.DeployToHathoraAsync(ServerConfig);
            // Assert.That(deployment?.BuildId, Is.Not.Null,
            //     "Deployment failed: Check console for details.");
            
            EditorGUI.EndDisabledGroup();
        }

        private void insertRegionHorizPopupList()
        {
            int selectedIndex = ServerConfig.HathoraLobbyRoomOpts.RegionSelectedIndex;

            if (selectedIndex < 0 || selectedIndex >= originalIndices.Count)
            {
                Debug.LogError("[HathoraConfigPostAuthBodyRoomUI.insertRegionHorizPopupList] " + 
                    $"Invalid selected index: {selectedIndex}");
                return;
            }

            // Get display index from original index
            int displaySelectedIndex = originalIndices.IndexOf(selectedIndex);
            
            int newDisplaySelectedIndex = base.InsertHorizLabeledPopupList(
                _labelStr: "Region",
                _tooltip: "Default: `Seattle`",
                _displayOptsStrArr: displayOptsStrList.ToArray(),
                _selectedIndex: displaySelectedIndex,
                GuiAlign.SmallRight);

            bool isNewValidIndex = newDisplaySelectedIndex >= 0 &&
                newDisplaySelectedIndex != displaySelectedIndex &&
                newDisplaySelectedIndex < displayOptsStrList.Count;

            if (isNewValidIndex)
            {
                // Get the original index from display index
                int originalIndex = originalIndices[newDisplaySelectedIndex];
                onSelectedRegionPopupIndexChanged(originalIndex);
            }

            InsertSpace2x();
        }
        #endregion // UI Draw
        
        
        #region Event Logic
        private void onSelectedRegionPopupIndexChanged(int _newSelectedIndex)
        {
            ServerConfig.HathoraLobbyRoomOpts.RegionSelectedIndex = _newSelectedIndex;
            SaveConfigChange(
                nameof(ServerConfig.HathoraLobbyRoomOpts.RegionSelectedIndex), 
                _newSelectedIndex.ToString());
            Debug.Log("[HathoraConfigPostAuthBodyRoomUI.onSelectedRegionPopupIndexChanged] " +
                $"Selected Region: {(Region)ServerConfig.HathoraLobbyRoomOpts.RegionSelectedIndex}" +
                $" (index {_newSelectedIndex})]");
        }
        
        /// <summary>
        /// On cancel, we'll set !isCreatingRoomAwaitingActiveStatus so we can try again.
        /// </summary>
        private async Task onCreateRoomLobbyBtnClick()
        {
            isCreatingRoomAwaitingActiveStatus = true;
            resetLastCreatedRoom(); // Both UI + ServerConfig

            createNewCreateRoomCancelToken();
            HathoraServerRoomApi serverRoomApi = new(ServerConfig);

            (Room room, ConnectionInfoV2 connInfo) roomConnInfoTuple;
            try
            {
                roomConnInfoTuple = await serverRoomApi.CreateRoomAwaitActiveAsync(
                    _cancelToken: CreateRoomCancelTokenSrc.Token);
            }
            catch (Exception e)
            {
                // Could be a TaskCanceledException
                onCreateRoomDone();
                return;
            }
            
            // Parse to helper class containing extra parsing
            // We can also save to ServerConfig as this type
            HathoraCachedRoomConnection roomConnInfo = new(
                roomConnInfoTuple.room, roomConnInfoTuple.connInfo);

            onCreateRoomDone();

            Assert.IsNotNull(roomConnInfo.Room?.RoomId, "!RoomId");
            
            Assert.AreEqual(roomConnInfo.ConnectionInfoV2?.Status, 
                ConnectionInfoV2.StatusEnum.Active,  "Status !Active");

            onCreateRoomSuccess(roomConnInfo);
        }

        /// <summary>
        /// This being null should trigger the UI to auto-hide the info box
        /// </summary>
        private void resetLastCreatedRoom() =>
            ServerConfig.HathoraLobbyRoomOpts.LastCreatedRoomConnection = null;

        private void onCreateRoomSuccess(HathoraCachedRoomConnection _roomConnInfo)
        {
            Debug.Log("[HathoraConfigPostAuthBodyRoomUI] onCreateRoomSuccess");

            // Save to this session ONLY - restarting Unity will reset this
            ServerConfig.HathoraLobbyRoomOpts.LastCreatedRoomConnection = _roomConnInfo;
            
            //// (!) While Rooms last only 5m, don't actually persist this
            // SaveConfigChange(
            //     nameof(ServerConfig.HathoraLobbyRoomOpts.LastCreatedRoomConnection), 
            //     $"RoomId={_room?.RoomId} | ProcessId={_room?.CurrentAllocation.ProcessId}");
        }

        private void onCreateRoomCancelBtnClick(CancellationTokenSource _cancelTokenSrc)
        {
            Debug.Log("[HathoraConfigPostAuthBodyRoomUI] onCreateRoomCancelBtnClick");
            _cancelTokenSrc?.Cancel();
            onCreateRoomDone();
        }

        private void onCreateRoomDone()
        {
            Debug.Log("[HathoraConfigPostAuthBodyRoomUI.onCreateRoomDone] Done (or canceled)");
            isCreatingRoomAwaitingActiveStatus = false;
        }
        
        
        private void onCopyRoomConnectionInfoBtnClick()
        {
            string connectionInfoStr = ServerConfig.HathoraLobbyRoomOpts.LastCreatedRoomConnection?.GetConnInfoStr();
            GUIUtility.systemCopyBuffer = connectionInfoStr; // Copy to clipboard
            
            Debug.Log($"Copied connection info to clipboard: `{connectionInfoStr}`");
        }
        #endregion // Event Logic

        
        #region Utils
        /// <summary>Cancel old, create new</summary>
        private static void createNewCreateRoomCancelToken()
        {
            // Cancel an old op 1st
            if (CreateRoomCancelTokenSrc != null && CreateRoomCancelTokenSrc.Token.CanBeCanceled)
                CreateRoomCancelTokenSrc.Cancel();
 
            CreateRoomCancelTokenSrc = new CancellationTokenSource(
                TimeSpan.FromSeconds(CREATE_ROOM_TIMEOUT_SECONDS));
        }
        #endregion // Utils
    }
}
