// Created by dylan@hathora.dev

using System.Collections.Generic;
using UnityEngine;

namespace Hathora.Core.Scripts.Runtime.Common.Utils
{
    /// <summary>
    /// Commandline helper - run via `-_mode {server|client|host} -memo {someStr}`.
    /// Unity: Command Line Helper | https://docs-multiplayer.unity3d.com/netcode/current/tutorials/command-line-helper/index.html  
    /// </summary>
    public abstract class HathoraArgHandlerBase : MonoBehaviour
    {
        private void Start() => Init();

        protected virtual void Init()
        {
            Debug.Log($"[HathoraArgHandlerBase] Init");
            
            if (Application.isEditor) 
                return;

            Dictionary<string, string> args = GetCommandlineArgs();

            // -_mode {server|client|host} // Logs and start netcode
            if (args.TryGetValue("-mode", out string mode))
                InitMode(mode);

            // -memo {string} // Show arbitrary text at bottom of screen
            if (args.TryGetValue("-memo", out string memoStr) && !string.IsNullOrEmpty(memoStr))
                InitMemo(memoStr);
        }

        /// <summary>Override me -> Set memoStr in UI</summary>
        /// <param name="_memoStr"></param>
        protected virtual void InitMemo(string _memoStr) =>
            Debug.Log($"[HathoraArgHandler] InitMemo: {_memoStr}");
        
        /// <summary>
        /// - "server" -> StartServer()
        /// - "client" -> StartClient()
        /// - "host" -> StartHost() // server+client together
        /// </summary>
        protected virtual void InitMode(string _mode)
        {
            Debug.Log($"[HathoraArgHandlerBase] InitMode: {_mode}");
            switch (_mode)
            {
                case "server":
                    StartServer();
                    break;
                
                case "client":
                    StartClient();
                    break;
                
                case "host":
                    StartHost();
                    break;
            }
        }

        /// <summary>Both server *and* client.OR, override me</summary>
        protected virtual void StartHost()
        {
            Debug.Log("[HathoraArgHandlerBase] StartHost (server+client)");
            StartServer();
            StartClient();
        }
        
        protected virtual void StartServer() =>
            Debug.Log("[HathoraArgHandlerBase] @ StartServer");

        protected virtual void StartClient() =>
            Debug.Log("[HathoraArgHandlerBase] StartClient");

        
        #region Utils
        protected static Dictionary<string, string> GetCommandlineArgs()
        {
            Dictionary<string, string> argDictionary = new();

            string[] args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLower();

                if (!arg.StartsWith("-"))
                    continue;
                
                string value = i < args.Length - 1 
                    ? args[i + 1].ToLower() 
                    : null;
                
                value = value?.StartsWith("-") ?? false 
                    ? null 
                    : value;

                argDictionary.Add(arg, value);
            }
            return argDictionary;
        }
        #endregion // Utils
    }
}
