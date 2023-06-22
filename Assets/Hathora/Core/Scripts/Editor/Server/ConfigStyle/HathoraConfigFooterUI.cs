// Created by dylan@hathora.dev

using System;
using System.Text;
using Hathora.Cloud.Sdk.Model;
using Hathora.Core.Scripts.Editor.Common;
using Hathora.Core.Scripts.Editor.Server.ConfigStyle.PostAuth;
using Hathora.Core.Scripts.Runtime.Server;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace Hathora.Core.Scripts.Editor.Server.ConfigStyle
{
    public class HathoraConfigFooterUI : HathoraConfigUIBase
    {
        /// <summary>
        /// For the combo build+deploy btn, we want to use the same deploy func
        /// </summary>
        private readonly HathoraConfigPostAuthBodyDeployUI postAuthBodyDeployUI;
        
        /// <summary>
        /// For the combo build+deploy btn, we want to use the same build func
        /// </summary>
        private readonly HathoraConfigPostAuthBodyBuildUI postAuthBodyBuildUI;
        
        // Scrollable logs
        /// <summary>Useful for debugging/styling without having to build each time</summary>
        private const bool MOCK_BUILD_LOGS = false;
        private Vector2 buildLogsScrollPos = Vector2.zero;
        private bool isBuildLogsFoldoutHeaderOpen = true;
        

        public HathoraConfigFooterUI(
            HathoraServerConfig _serverConfig, 
            SerializedObject _serializedConfig,
            HathoraConfigPostAuthBodyBuildUI _postAuthBodyBuildUI,
            HathoraConfigPostAuthBodyDeployUI _postAuthBodyDeployUI) 
            : base(_serverConfig, _serializedConfig)
        {
            this.postAuthBodyBuildUI = _postAuthBodyBuildUI;
            this.postAuthBodyDeployUI = _postAuthBodyDeployUI;
            
            // HathoraServerDeploy.OnZipComplete += onDeployAppStatus_1ZipComplete;
            // HathoraServerDeploy.OnBuildReqComplete += onDeployAppStatus_2BuildReqComplete;
            // HathoraServerDeploy.OnUploadComplete += onDeployAppStatus_3UploadComplete;
        }

        public void Draw()
        {
            InsertSpace4x();
            
            if (IsAuthed)
                insertPostAuthFooter();
            else
                insertPreAuthFooter();
        }

        private void insertPostAuthFooter()
        {
            bool enableBuildUploadDeployBtn = 
                !HathoraServerDeploy.IsDeploying &&
                ServerConfig.MeetsBuildBtnReqs() && 
                ServerConfig.MeetsDeployBtnReqs(); 
            
            insertBuildUploadDeployHelpbox(_enabled: enableBuildUploadDeployBtn);
            insertBuildUploadDeployBtn(_enabled: enableBuildUploadDeployBtn); // !await
            insertScrollableLogs();
        }

        private void insertScrollableLogs()
        {
            bool hasLastbuildLogsStrb = ServerConfig.LinuxHathoraAutoBuildOpts.HasLastBuildLogsStrb;
            bool hasLastDeployLogsStrb = ServerConfig.HathoraDeployOpts.HasLastDeployLogsStrb;
            if (!hasLastbuildLogsStrb && !hasLastDeployLogsStrb && !MOCK_BUILD_LOGS)
                return;

            if (hasLastbuildLogsStrb || MOCK_BUILD_LOGS)
                insertBuildLogsFoldoutHeader();
            
            if (hasLastDeployLogsStrb)
                InsertLabel(ServerConfig.HathoraDeployOpts.LastDeployLogsStrb.ToString());
        }

        private void insertBuildLogsFoldoutHeader()
        {
            if (!ServerConfig.LinuxHathoraAutoBuildOpts.HasLastBuildLogsStrb)
            {
                if (!MOCK_BUILD_LOGS)
                    return;
                
                // Fake some logs
                appendFakeLogs();
            }

            isBuildLogsFoldoutHeaderOpen = EditorGUILayout.BeginFoldoutHeaderGroup(
                isBuildLogsFoldoutHeaderOpen, 
                "Build Logs");
            
            // USER INPUT >>
            if (!isBuildLogsFoldoutHeaderOpen)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            // Content within the foldout >>
            insertBuildLogsScrollLbl();
            
            EditorGUILayout.EndFoldoutHeaderGroup();
            InsertSpace1x();
        }

        private void insertBuildLogsScrollLbl()
        {
            buildLogsScrollPos = GUILayout.BeginScrollView(
                buildLogsScrollPos,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(150f));
            
            // Content within the scroller >>
            base.BeginPaddedBox();
            InsertLabel(ServerConfig.LinuxHathoraAutoBuildOpts.LastBuildLogsStrb.ToString());
            base.EndPaddedBox();
            
            GUILayout.EndScrollView();
            InsertSpace1x();
        }

        private void appendFakeLogs()
        {
            // Foo 1 ~ 50
            ServerConfig.LinuxHathoraAutoBuildOpts.LastBuildLogsStrb
                .AppendLine("Foo 1").AppendLine("Foo 2").AppendLine("Foo 3")
                .AppendLine("Foo 4").AppendLine("Foo 5").AppendLine("Foo 6")
                .AppendLine("Foo 7").AppendLine("Foo 8").AppendLine("Foo 9")
                .AppendLine("Foo 10").AppendLine("Foo 11").AppendLine("Foo 12")
                .AppendLine("Foo 13").AppendLine("Foo 14").AppendLine("Foo 15")
                .AppendLine("Foo 16").AppendLine("Foo 17").AppendLine("Foo 18")
                .AppendLine("Foo 19").AppendLine("Foo 20").AppendLine("Foo 21")
                .AppendLine("Foo 22").AppendLine("Foo 23").AppendLine("Foo 24")
                .AppendLine("Foo 25").AppendLine("Foo 26").AppendLine("Foo 27")
                .AppendLine("Foo 28").AppendLine("Foo 29").AppendLine("Foo 30")
                .AppendLine("Foo 31").AppendLine("Foo 32").AppendLine("Foo 33")
                .AppendLine("Foo 34").AppendLine("Foo 35").AppendLine("Foo 36")
                .AppendLine("Foo 37").AppendLine("Foo 38").AppendLine("Foo 39")
                .AppendLine("Foo 40").AppendLine("Foo 41").AppendLine("Foo 42")
                .AppendLine("Foo 43").AppendLine("Foo 44").AppendLine("Foo 45")
                .AppendLine("Foo 46").AppendLine("Foo 47").AppendLine("Foo 48")
                .AppendLine("Foo 49").AppendLine("Foo 50");
        }

        private void insertBuildUploadDeployHelpbox(bool _enabled)
        {
            MessageType helpMsgType = _enabled || HathoraServerDeploy.IsDeploying
                ? MessageType.Info 
                : MessageType.Error;
            
            string helpMsg;
            if (_enabled || HathoraServerDeploy.IsDeploying) 
            {
                helpMsg = "This action will create a new server build, upload to Hathora, " +
                    "and create a new deployment version of your application.";
            }
            else
            {
                StringBuilder helpMsgStrb = new StringBuilder("Missing required fields: ")
                    .Append(HathoraConfigPostAuthBodyBuildUI.GetCreateBuildMissingReqsStrb(
                        ServerConfig,
                        _includeMissingReqsFieldsPrefixStr: false));
                // TODO: add validation for deploy fields (i tried, but currently private)

                helpMsg = helpMsgStrb.ToString();
            }
            
            // Post the help box *before* we disable the button so it's easier to see
            EditorGUILayout.HelpBox(helpMsg, helpMsgType);
        }

        private void insertPreAuthFooter()
        {
            InsertHorizontalLine(1.5f, Color.gray, _space: 20);
            InsertCenterLabel("Learn more about Hathora Cloud");
            InsertLinkLabel("Documentation", HathoraEditorUtils.HATHORA_DOCS_URL, _centerAlign: true);
            InsertLinkLabel("Demo Projects", HathoraEditorUtils.HATHORA_DOCS_DEMO_PROJECTS_URL, _centerAlign: true);
        }

        /// <summary>AKA "The Do-All Button"</summary>
        /// <param name="_enabled"></param>
        private async Task insertBuildUploadDeployBtn(bool _enabled)
        {
            EditorGUI.BeginDisabledGroup(disabled: !_enabled);

            string btnLabelStr = HathoraServerDeploy.IsDeploying 
                ? HathoraServerDeploy.GetDeployFriendlyStatus()
                : "Build, Upload & Deploy New Version";

            // USER INPUT >>
            bool clickedBuildUploadDeployBtn = GUILayout.Button(
                btnLabelStr,
                GeneralButtonStyle);
            
            EditorGUI.EndDisabledGroup();

            if (clickedBuildUploadDeployBtn)
                onBuildUploadDeployBtnClick();

            InsertSpace2x();
        }
        

        #region Status Callbacks
        
        private async Task onBuildUploadDeployBtnClick()
        {
            // TODO: Cancel token
            BuildReport buildReport = null;
            try
            {
                buildReport = await postAuthBodyBuildUI.GenerateServerBuildAsync(); // Cached @ ServerConfig
            }
            catch (Exception e)
            {
                Debug.LogError($"[HathoraConfigFooterUI.onBuildUploadDeployBtnClick] " +
                    $"GenerateServerBuildAsync => Error: {e}");
                return;
            }
            
            if (buildReport.summary.result != BuildResult.Succeeded)
                return;
            
            // ------------
            // TODO: Cancel token   
            Deployment deployment = null;
            try
            {
                deployment = await postAuthBodyDeployUI.DeployApp(); // Cached @ ServerConfig
            }
            catch (Exception e)
            {
                Debug.LogError($"[HathoraConfigFooterUI.onBuildUploadDeployBtnClick] " +
                    $"DeployApp => Error: {e}");
                return;
            }
        }
        #endregion Status Callbacks
    }
}
