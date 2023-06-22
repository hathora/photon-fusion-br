// Created by dylan@hathora.dev

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hathora.Cloud.Sdk.Model;
using Hathora.Core.Scripts.Runtime.Common.Models;
using Hathora.Core.Scripts.Runtime.Common.Utils;
using Hathora.Core.Scripts.Runtime.Server;
using UnityEditor;
using UnityEngine;

namespace Hathora.Core.Scripts.Editor.Server.ConfigStyle.PostAuth
{
    public class HathoraConfigPostAuthBodyDeployUI : HathoraConfigUIBase
    {
        #region Vars
        private HathoraConfigPostAuthBodyDeployAdvUI _advancedDeployUI;
        public static CancellationTokenSource DeployingCancelTokenSrc;

        // Foldouts
        private bool isDeploymentFoldout;
        #endregion // Vars


        #region Init
        public HathoraConfigPostAuthBodyDeployUI(
            HathoraServerConfig _serverConfig, 
            SerializedObject _serializedConfig)
            : base(_serverConfig, _serializedConfig)
        {
            if (!HathoraConfigUI.ENABLE_BODY_STYLE)
                return;

            initDrawUtils();
        }

        /// <summary>
        /// There are modulated parts of the post-auth body.
        /// </summary>
        private void initDrawUtils()
        {
            _advancedDeployUI = new HathoraConfigPostAuthBodyDeployAdvUI(ServerConfig, SerializedConfig);
            
            HathoraServerDeploy.OnZipComplete += onDeployAppStatus_1ZipComplete;
            HathoraServerDeploy.OnBuildReqComplete += onDeployAppStatus_2BuildReqComplete;
            HathoraServerDeploy.OnUploadComplete += onDeployAppStatus_3UploadComplete;
        }
        #endregion // Init
        
        
        #region UI Draw
        public void Draw()
        {
            if (!IsAuthed)
                return; // You should be calling HathoraConfigPreAuthBodyUI.Draw()

            insertDeploymentSettingsFoldout();
        }

        private void insertDeploymentSettingsFoldout()
        {
            isDeploymentFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                isDeploymentFoldout, 
                "Hathora Deployment Configuration");
            
            if (!isDeploymentFoldout)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }
    
            InsertSpace2x();

            insertDeploymentSettingsFoldoutComponents();

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void insertDeploymentSettingsFoldoutComponents()
        {
            insertPlanNameHorizPopupList();
            insertRoomsPerProcessHorizSliderGroup();
            insertContainerPortNumberHorizSliderGroup();
            insertTransportTypeHorizRadioBtnGroup();
            // _advancedDeployUI.Draw();

            bool enableDeployBtn = checkIsReadyToEnableToDeployBtn(); 
            if (enableDeployBtn || HathoraServerDeploy.IsDeploying)
                insertDeployAppHelpbox();
            else
                insertDeployAppHelpboxErr();

            EditorGUI.BeginDisabledGroup(disabled: !enableDeployBtn);
            insertDeployAppBtn(); // !await
            EditorGUI.EndDisabledGroup();
        }

        private void insertRoomsPerProcessHorizSliderGroup()
        {
            int inputInt = base.InsertHorizLabeledConstrainedIntField(
                _labelStr: "Rooms per process",
                _tooltip: "For most Unity multiplayer games, this should be left as 1\n\n" +
                "For some lightweight servers, a single server instance (process) can handle multiple rooms/matches. If your server is built to support this, you can specify the number of rooms to fit on a process before spinning up a fresh instance.\n\n" +
                "Default: 1",
                _val: ServerConfig.HathoraDeployOpts.RoomsPerProcess,
                _minVal: 1,
                _maxVal: 10000,
                _alignPopup: GuiAlign.SmallRight);

            bool isChanged = inputInt != ServerConfig.HathoraDeployOpts.RoomsPerProcess;
            if (isChanged)
                onRoomsPerProcessNumChanged(inputInt);
            
            InsertSpace1x();
        }
        
        private void insertContainerPortNumberHorizSliderGroup()
        {
            int inputInt = base.InsertHorizLabeledConstrainedIntField(
                _labelStr: "Container port number",
                _tooltip: "This is the port your server code is listening on, Hathora will bind to this.\n" +
                "(NOTE: this will be different from the port players/clients connect to - see \"Create Room\")\n\n" +
                "Default: 7777 (<1024 is generally reserved by system)",
                _val: ServerConfig.HathoraDeployOpts.ContainerPortWrapper.PortNumber,
                _minVal: 1024,
                _maxVal: 49151,
                _alignPopup: GuiAlign.SmallRight);

            bool isChanged = inputInt != ServerConfig.HathoraDeployOpts.ContainerPortWrapper.PortNumber;
            if (isChanged)
                onContainerPortNumberNumChanged(inputInt);
            
            InsertSpace1x();
        }
        
        private void insertTransportTypeHorizRadioBtnGroup()
        {
            int selectedIndex = ServerConfig.HathoraDeployOpts.TransportTypeSelectedIndex;
            
            // Get list of string names from PlanName Enum members. Set UPPER.
            List<string> displayOptsStrList = GetStrListOfEnumMemberKeys<TransportType>(
                EnumListOpts.AllCaps,
                _prependDummyIndex0Str: "<Choose a Transport Type>");

            int newSelectedIndex = base.InsertHorizLabeledPopupList(
                _labelStr: "Transport Type",
                _tooltip: 
                    "Default: `UDP` (Fastest; although less reliable) " +
                    "(!) For now, all transports override to UDP",
                _displayOptsStrArr: displayOptsStrList.ToArray(),
                _selectedIndex: selectedIndex,
                GuiAlign.SmallRight);

            bool isNewValidIndex = selectedIndex >= 0 &&
                newSelectedIndex != selectedIndex &&
                selectedIndex < displayOptsStrList.Count;

            if (isNewValidIndex)
                onSelectedTransportTypePopupIndexChanged(newSelectedIndex);
            
            InsertSpace2x();
        }

        private static string getPlanNameListWithExtraInfo(PlanName _planName)
        {
            switch (_planName)
            {
                default:
                case PlanName.Tiny:
                    return $"{nameof(PlanName.Tiny)} (Shared core, 1GB)";
                
                case PlanName.Small:
                    return $"{nameof(PlanName.Small)} (1 core, 2GB)";
                
                case PlanName.Medium:
                    return $"{nameof(PlanName.Medium)} (2 cores, 4GB)";
                
                case PlanName.Large:
                    return $"{nameof(PlanName.Large)} (4 cores, 8GB)"; 
            }
        }

        private void insertPlanNameHorizPopupList()
        {
            int selectedIndex = ServerConfig.HathoraDeployOpts.PlanNameSelectedIndex;
            
            // Get list of string names from PlanName Enum members - with extra info.
            // The index order is !modified.
            List<string> displayOptsStrArr = GetDisplayOptsStrArrFromEnum<PlanName>(
                _prependDummyIndex0Str: "<Choose a Plan>");

            int newSelectedIndex = base.InsertHorizLabeledPopupList(
                _labelStr: "Plan Size",
                _tooltip: "Determines amount of resources your server instances has access to\n\n" +
                "Tiny - Shared Core, 1GB Memory\n" +
                "Small - 1 Core, 2GB Memory\n" +
                "Medium - 2 Cores, 4GB Memory\n" +
                "Large - 4 Cores, 8GB Memory\n\n" +
                "Default: `Tiny`",
                _displayOptsStrArr: displayOptsStrArr.ToArray(),
                _selectedIndex: selectedIndex,
                GuiAlign.SmallRight);

            bool isNewValidIndex = selectedIndex >= 0 &&
                newSelectedIndex != selectedIndex &&
                selectedIndex < displayOptsStrArr.Count;

            if (isNewValidIndex)
                onSelectedPlanNamePopupIndexChanged(newSelectedIndex);
            
            string appUrl = "https://hathora.dev/docs/pricing-billing";
            InsertLinkLabel("See pricing details", appUrl, _centerAlign:false);
            
            InsertSpace2x();
        }
        
        private void insertDeployAppHelpbox()
        {
            InsertSpace2x();
            
            const MessageType helpMsgType = MessageType.Info;
            const string helpMsg = "This action will create a new deployment version of your application. " +
                "New rooms will be created with this version of your server, existing rooms will be unaffected.";

            // Post the help box *before* we disable the button so it's easier to see (if toggleable)
            EditorGUILayout.HelpBox(helpMsg, helpMsgType);
        }
        
        private void insertDeployAppHelpboxErr()
        {
            InsertSpace2x();

            // (!) Hathora SDK Enums start at index 1 (not 0)
            StringBuilder helpboxLabelStrb = new("Missing required fields: ");
            if (!ServerConfig.HathoraCoreOpts.HasAppId)
                helpboxLabelStrb.Append("`AppId` ");
            
            if (ServerConfig.HathoraDeployOpts.PlanNameSelectedIndex < 1)
                helpboxLabelStrb.Append("`Plan Size` ");
            
            if (ServerConfig.HathoraDeployOpts.RoomsPerProcess < 1)
                helpboxLabelStrb.Append("`Rooms per Process`,");
            
            if (ServerConfig.HathoraDeployOpts.ContainerPortWrapper.PortNumber < 1)
                helpboxLabelStrb.Append("`Container Port Number` ");
            
            if (ServerConfig.HathoraDeployOpts.TransportTypeSelectedIndex < 1)
                helpboxLabelStrb.Append("`Transport Type`");

            // Post the help box *before* we disable the button so it's easier to see (if toggleable)
            EditorGUILayout.HelpBox(helpboxLabelStrb.ToString(), MessageType.Error);
        }

        /// <summary>
        /// TODO: Add cancel btn
        /// </summary>
        private async Task insertDeployAppBtn()
        {
            string btnLabelStr = HathoraServerDeploy.IsDeploying 
                ? HathoraServerDeploy.GetDeployFriendlyStatus()
                : "Deploy Application";
            
            bool clickedDeployBtn = InsertLeftGeneralBtn(btnLabelStr);
            InsertSpace1x();
            
            if (!clickedDeployBtn)
                return;

            onClickedDeployAppBtnClick(); // !await
        } 
        #endregion // UI Draw

        
        #region Event Logic
        private void onSelectedPlanNamePopupIndexChanged(int _newSelectedIndex)
        {
            ServerConfig.HathoraDeployOpts.PlanNameSelectedIndex = _newSelectedIndex;
            SaveConfigChange(
                nameof(ServerConfig.HathoraDeployOpts.PlanNameSelectedIndex), 
                _newSelectedIndex.ToString());
        }
        
        private void onSelectedTransportTypePopupIndexChanged(int _newSelectedIndex)
        {
            ServerConfig.HathoraDeployOpts.TransportTypeSelectedIndex = _newSelectedIndex;
            SaveConfigChange(
                nameof(ServerConfig.HathoraDeployOpts.TransportTypeSelectedIndex), 
                _newSelectedIndex.ToString());
        }

        private void onRoomsPerProcessNumChanged(int _inputInt)
        {
            ServerConfig.HathoraDeployOpts.RoomsPerProcess = _inputInt;
            SaveConfigChange(
                nameof(ServerConfig.HathoraDeployOpts.RoomsPerProcess), 
                _inputInt.ToString());
        }
        
        private void onContainerPortNumberNumChanged(int _inputInt)
        {
            ServerConfig.HathoraDeployOpts.ContainerPortWrapper.PortNumber = _inputInt;
            SaveConfigChange(
                nameof(ServerConfig.HathoraDeployOpts.ContainerPortWrapper.PortNumber), 
                _inputInt.ToString());
        }

        private async Task onClickedDeployAppBtnClick() => 
            await DeployApp();

        /// <summary>
        /// Optionally sub to events:
        /// - OnZipComplete
        /// - OnBuildReqComplete
        /// - OnUploadComplete
        /// </summary>
        /// <returns></returns>
        public async Task<Deployment> DeployApp()
        {
            DeployingCancelTokenSrc = new CancellationTokenSource();

            Deployment deployment = await HathoraServerDeploy.DeployToHathoraAsync(
                ServerConfig,
                DeployingCancelTokenSrc.Token);

            bool isSuccess = deployment?.DeploymentId > 0;
            if (isSuccess)
                onDeployAppSuccess(deployment);
            else
                onDeployAppFail();
            
            return deployment;
        }

        private void onDeployAppFail()
        {
            throw new NotImplementedException("TODO onDeployAppFail");
        }

        /// <summary>Step 1 of 4</summary>
        private void onDeployAppStatus_1ZipComplete()
        {
            Debug.Log("[HathoraConfigPostAuthBodyDeployUI] <color=yellow>" +
                "onDeployAppStatus_1ZipComplete</color>");
        }
        
        /// <summary>Step 2 of 4</summary>
        private void onDeployAppStatus_2BuildReqComplete(Build _build)
        {
            Debug.Log("[HathoraConfigPostAuthBodyDeployUI] <color=yellow>" +
                "onDeployAppStatus_2BuildReqComplete</color>");
            // TODO
        }
        
        /// <summary>Step 3 of 4</summary>
        private void onDeployAppStatus_3UploadComplete()
        {
            Debug.Log("[HathoraConfigPostAuthBodyDeployUI] <color=yellow>" +
                "onDeployAppStatus_3UploadComplete</color>");
            // TODO
        }

        /// <summary>
        /// Cache last successful Deployment for the session
        /// </summary>
        /// <param name="_deployment"></param>
        private void onDeployAppSuccess(Deployment _deployment)
        {
            ServerConfig.HathoraDeployOpts.LastDeployment = 
                new DeploymentWrapper(_deployment);
        }
        #endregion // Event Logic
        
        
        #region Utils
        /// <summary>
        /// (!) Hathora SDK Enums starts at index 1; not 0: Care of indexes
        /// </summary>
        /// <returns></returns>
        private bool checkIsReadyToEnableToDeployBtn() =>
            !HathoraServerDeploy.IsDeploying &&
            ServerConfig.HathoraDeployOpts.PlanNameSelectedIndex >= HathoraUtils.SDK_ENUM_STARTING_INDEX &&
            ServerConfig.HathoraDeployOpts.RoomsPerProcess > 0 &&
            ServerConfig.HathoraDeployOpts.ContainerPortWrapper.PortNumber > 0 &&
            ServerConfig.HathoraDeployOpts.TransportTypeSelectedIndex >= HathoraUtils.SDK_ENUM_STARTING_INDEX;

        #endregion //Utils
    }
}
