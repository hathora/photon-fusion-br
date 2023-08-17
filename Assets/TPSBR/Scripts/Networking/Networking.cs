#define ENABLE_LOGS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Plugin;
using Fusion.Sockets;
using Hathora.Cloud.Sdk.Api;
using Hathora.Cloud.Sdk.Model;
using Hathora.Core.Scripts.Runtime.Common.Utils;
using Hathora.Core.Scripts.Runtime.Server;
using Hathora.Core.Scripts.Runtime.Server.ApiWrapper;
using Hathora.Core.Scripts.Runtime.Server.Models;
using HathoraPhoton;
using Application = UnityEngine.Application;
using Assert = UnityEngine.Assertions.Assert;
using UnityScene = UnityEngine.SceneManagement.Scene;
using HathoraRegion = Hathora.Cloud.Sdk.Model.Region;

namespace TPSBR
{
	public struct SessionRequest
	{
		public string        UserID;
		public GameMode      GameMode;
		public string        DisplayName;
		public string        SessionName;
		public string        ScenePath;
		public EGameplayType GameplayType;
		public int           MaxPlayers;
		public int           ExtraPeers;
		public string        CustomLobby;
		public string        IPAddress;
		public ushort        Port;
	}

	// This Networking class is complex due to handling of reconnection of extra peers that we used for initial batch testing.
	// We suggest to use standard approach (inheriting from NetworkSceneManagerBase for custom loading
	// functionality and directly starting via NetworkRunner) for your game unless such functionality is needed.
	public class Networking : MonoBehaviour
	{
		private HathoraGetDeployInfoResult hathoraDeployInfo;
		
		private static HathoraServerConfig hathoraServerConfig => 
			HathoraServerMgr.Singleton != null
				? HathoraServerMgr.Singleton.HathoraServerConfig
				: null; 

        public static HathoraRegion HATHORA_FALLBACK_REGION => Region.WashingtonDC;

        /// <summary>ByRef wrapper; passing just `ref` has issues with async/coroutines/tasks</summary>
        private class StartGameArgsContainer
        {
            public StartGameArgs StartGameArgs;
            
            public StartGameArgsContainer(StartGameArgs _args)
            {
                this.StartGameArgs = _args;
            }
        }

        // CONSTANTS

		public const string DISPLAY_NAME_KEY = "name";
		public const string MAP_KEY  = "map";
		public const string TYPE_KEY = "type";
		public const string MODE_KEY = "mode";
		public const string STATUS_SERVER_CLOSED = "Server Closed";

		// PUBLIC MEMBERS

		public string  Status             { get; private set; }
		public string  StatusDescription  { get; private set; }
		public string  ErrorStatus        { get; private set; }

		public bool    HasSession         => _pendingSession != null || _currentSession != null;
		public bool    IsConnecting       => _pendingSession != null || _currentSession.IsConnected == false;
		public bool    IsConnected        => _currentSession != null && _pendingSession == null && _currentSession.IsConnected == true;

		public int     PeerCount          => _currentSession != null ? _currentSession.GamePeers.SafeCount() : 0;

		// PRIVATE MEMBERS

		private Session    _pendingSession;
		private Session    _currentSession;
		private bool       _stopGameOnDisconnect;
		private string     _loadingScene;
		private Coroutine  _coroutine;

		// PUBLIC METHODS

		public async void StartGame(SessionRequest request)
		{
			#region Hathora
			// #######################################################################
			// Call this early before we create a new Photon `Session` below,
			// (!) We'll replace `request` with the Hathora Lobby `InitConfig`.
			//   If we don't do this, the default CLI `-args` will be set.
			//   Eg: Scene name, lobby name, max players, [...] will all be the same.
			// #######################################################################
			if (request.GameMode is GameMode.Server)
			{
				StatusDescription = "Discovering Hathora Lobby (as Server)";
				
				// Cache the Process, Room and Lobby info => replace the Photon SessionRequest
				this.hathoraDeployInfo = await hathoraServerGetRoomLobbyInfo();
				
				// Earlier, we serialized the SessionRequest @ HathoraMatchmaking.cs (photonHostCreateHathoraLobbyAsync)
				// If we don't replace the request, Photon lobby will show the default -args we provided (maxPlayers, lobbyName)
				request = hathoraDeployInfo.GetLobbyInitConfig<SessionRequest>();
			}
			#endregion // Hathora
			
			
			var session = new Session();

			if (request.ExtraPeers > 0 && NetworkProjectConfig.Global.PeerMode == NetworkProjectConfig.PeerModes.Single)
			{
				Debug.LogError("Cannot start with multiple peers. PeerMode is set to Single.");
				request.ExtraPeers = 0;
			}

			SceneRef sceneRef = SceneUtility.GetBuildIndexByScenePath(request.ScenePath);

			int totalPeers = 1 + request.ExtraPeers;
			session.GamePeers = new GamePeer[totalPeers];

			for (int i = 0; i < totalPeers; i++)
			{
				session.GamePeers[i] = new GamePeer(i)
				{
					UserID   = i == 0 ? request.UserID : $"{request.UserID}.{i}",
					Scene    = sceneRef,
					GameMode = i == 0 ? request.GameMode : GameMode.Client,
					Request  = request,
				};
			}

			session.ConnectionRequested = true;

			_pendingSession       = session;
			_stopGameOnDisconnect = false;

			ErrorStatus = null;

			Log($"StartGame() UserID:{request.UserID} GameMode:{request.GameMode} DisplayName:{request.DisplayName} SessionName:{request.SessionName} ScenePath:{request.ScenePath} GameplayType:{request.GameplayType} MaxPlayers:{request.MaxPlayers} ExtraPeers:{request.ExtraPeers} CustomLobby:{request.CustomLobby}");
		}

		public void StopGame(string errorStatus = null)
		{
			Log($"StopGame()");

			_pendingSession       = null;
			_stopGameOnDisconnect = false;

			if (_currentSession != null)
			{
				_currentSession.ConnectionRequested = false;
			}

			ErrorStatus = errorStatus;
		}

		public void StopGameOnDisconnect()
		{
			Log($"StopGameOnDisconnect()");

			_stopGameOnDisconnect = true;
		}

		public void ClearErrorStatus()
		{
			ErrorStatus = null;
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_loadingScene = Global.Settings.LoadingScene;
		}

		protected void Update()
		{
			if (_pendingSession != null)
			{
				if (_currentSession == null)
				{
					_currentSession = _pendingSession;
					_pendingSession = null;
				}
				else
				{
					// Request end of current session
					_currentSession.ConnectionRequested = false;
				}
			}

			UpdateCurrentSession();

			// Check if current session finished
			if (_coroutine == null && _currentSession != null && _currentSession.IsConnected == false)
			{
				if (_pendingSession == null)
				{
					Log($"Starting LoadMenuCoroutine()");
					// Current session is finished and there is no pending session, let's go to menu
					_coroutine = StartCoroutine(LoadMenuCoroutine());
				}

				_currentSession = null;
			}
		}

		// PRIVATE MEMBERS

		public async void UpdateCurrentSession()
		{
			if (_currentSession == null)
			{
				Status = string.Empty;
				StatusDescription = string.Empty;
				return;
			}

			if (_coroutine != null)
				return;

			var peers = _currentSession.GamePeers;

			if (_stopGameOnDisconnect == true)
			{
				for (int i = 0; i < peers.Length; i++)
				{
					if (_currentSession.ConnectionRequested == true && peers[i].IsConnected == false)
					{
						Log($"Stopping game after disconnect");
						_stopGameOnDisconnect = false;
						StopGame();
						return;
					}
				}
			}

			for (int i = 0; i < peers.Length; i++)
			{
				var peer = peers[i];
				bool isConnected = peer.IsConnected;

				if (_currentSession.ConnectionRequested == true && peer.Loaded == false && isConnected == false && peer.CanConnect == true)
				{
					// First connect or reconnect after failed connect

					Status = peer.WasConnected == false ? "Starting" : "Reconnecting";
					Log($"Starting ConnectPeerCoroutine() - {Status} - Peer {peer.ID}");
					_coroutine = StartCoroutine(ConnectPeerCoroutine(peer));
					return;
				}
				else if (_currentSession.ConnectionRequested == false && (isConnected == true || peer.Loaded == true))
				{
					// Disconnect requested

					Status = "Quitting";
					Log($"Starting DisconnectPeerCoroutine() - {Status} - Peer {peer.ID}");
					_coroutine = StartCoroutine(DisconnectPeerCoroutine(peer));
					return;
				}
				else if (peer.Loaded == true && isConnected == false)
				{
					// Connection lost

					Status = "Connection Lost";
					Log($"Starting DisconnectPeerCoroutine() - {Status} - Peer {peer.ID}");
					_coroutine = StartCoroutine(DisconnectPeerCoroutine(peer));
					return;
				}
			}

			UpdatePeerSwitch(_currentSession.GamePeers);
			ValidateMultiPeers(_currentSession.GamePeers);
		}

		/// <summary>
		/// TODO: Convert this to async/await?
		/// </summary>
		/// <param name="peer"></param>
		/// <param name="connectionTimeout"></param>
		/// <param name="loadTimeout"></param>
		/// <returns></returns>
		private IEnumerator ConnectPeerCoroutine(GamePeer peer, float connectionTimeout = 10f, float loadTimeout = 45f)
		{
			peer.Loaded = true;

			if (peer.WasConnected == true)
			{
				peer.ReconnectionTries--;
			}
			else
			{
				peer.ConnectionTries--;
			}

			StatusDescription = "Unloading current scene";

			UnityScene activeScene = SceneManager.GetActiveScene();

			if (IsSameScene(activeScene.path, peer.Request.ScenePath) == false && activeScene.name != _loadingScene)
			{
				Log($"Show loading scene");
				yield return ShowLoadingSceneCoroutine(true);

				var currentScene = activeScene.GetComponent<Scene>();
				if (currentScene != null)
				{
					Log($"Deinitializing Scene");
					currentScene.Deinitialize();
				}

				Log($"Unloading scene {activeScene.name}");
				yield return SceneManager.UnloadSceneAsync(activeScene);
				yield return null;
			}

			float  baseTime  = Time.realtimeSinceStartup;
			float  limitTime = baseTime + connectionTimeout;
			string peerName  = $"{peer.GameMode}#{peer.ID}";

			Log($"Starting {peerName} ...");
			StatusDescription = "Starting network connection";

			yield return null;

			NetworkObjectPool pool = new NetworkObjectPool();

			NetworkRunner runner = Instantiate(Global.Settings.RunnerPrefab);
			runner.name = peerName;

			peer.Runner       = runner;
			peer.SceneManager = runner.GetComponent<NetworkSceneManager>();
			peer.LoadedScene  = default;

			StartGameArgs startGameArgs = new StartGameArgs();
			startGameArgs.GameMode            = peer.GameMode;
			startGameArgs.SessionName         = peer.Request.SessionName;
			startGameArgs.Scene               = peer.Scene;
			startGameArgs.Initialized         = OnGamePeerInitialized;
			startGameArgs.ObjectPool          = pool;
			startGameArgs.CustomLobbyName     = peer.Request.CustomLobby;
			startGameArgs.SceneManager        = peer.SceneManager;
			startGameArgs.DisableClientSessionCreation = true;

			if (peer.Request.MaxPlayers > 0)
				startGameArgs.PlayerCount = peer.Request.MaxPlayers;


            #region Hathora
            // ################################################################################################
            switch (peer.GameMode)
            {
                case GameMode.Server:
                {
	                StatusDescription = "Connecting to Hathora (as Server)";
	                
	                if (hathoraDeployInfo is { HasPort: true })
	                {
		                // Set the Photon SessionProperties (maps to Hathora Lobby InitConfig)
		                startGameArgs.SessionProperties = CreateSessionProperties(peer.Request);
		                
		                // Pass StartGameArgs in a ByRef container, then set the updated changes
		                StartGameArgsContainer startGameArgsByRef = new(startGameArgs);
		                
		                yield return new HathoraTaskUtils.WaitForTaskCompletion(
			                setCustomHathoraServerPublicAddressFromCached(startGameArgsByRef));
		                
		                startGameArgs = startGameArgsByRef.StartGameArgs;
	                }
	                
	                break;
                }
                
                case GameMode.Host:
                {
	                StatusDescription = "Connecting to Hathora (as Host)";
	                throw new NotImplementedException("[Networking.ConnectPeerCoroutine] `Host` should have been handled " +
		                "at HathoraMatchmaking.cs - theoretically, this should never trigger.");
                }

                case GameMode.Client:
                {
	                StatusDescription = "Connecting to Hathora (as Client)";
	                break;
                }
            }
            // ################################################################################################
            #endregion // Hathora
            

            if (peer.Request.IPAddress.HasValue() == true)
			{
				// Sets the StandaloneMgr vals, if exists. For servers, don't set this. --Hathora
				Log($"peer request IP&Port: " + peer.Request.IPAddress + " " + peer.Request.Port);
				startGameArgs.Address = NetAddress.CreateFromIpPort(peer.Request.IPAddress, peer.Request.Port);
			}
			else if (peer.Request.Port > 0)
			{
				// Sets the StandaloneMgr vals, if exists. For servers, don't set this. --Hathora
				Log($"peer request port: " + peer.Request.Port);
				startGameArgs.Address = NetAddress.Any(peer.Request.Port);
			}

			Log($"NetworkRunner.StartGame()");
			var startGameTask = runner.StartGame(startGameArgs);

			while (startGameTask.IsCompleted == false)
			{
				yield return null;

				if (Time.realtimeSinceStartup >= limitTime)
				{
					Debug.LogError(
						$"{peerName} start timeout! IsCompleted: {startGameTask.IsCompleted} IsCanceled: {startGameTask.IsCanceled} IsFaulted: {startGameTask.IsFaulted}");
					break;
				}

				if (_currentSession.ConnectionRequested == false)
				{
					Log($"Stopping coroutine (requested by user)");
					// Stop requested by user
					break;
				}
			}

			if (startGameTask.IsCanceled == true || startGameTask.IsFaulted == true ||
			    startGameTask.IsCompleted == false)
			{
				Debug.LogError($"{peerName} failed to start!");

				Log($"Starting DisconnectPeerCoroutine() - Peer {peer.ID}");
				yield return DisconnectPeerCoroutine(peer);

				_coroutine = null;
				yield break;
			}

			var result = startGameTask.Result;

			Log($"StartGame() Result: {result.ToString()} - Peer {peer.ID}");

			if (result.Ok == false)
			{
				Debug.LogError($"{peerName} failed to start! Result: {result}");

				// Probably incorrect start game parameters, go back to menu immediately
				if (Application.isBatchMode == false)
				{
					StopGame();
				}

				if (peer.WasConnected == true && result.ShutdownReason == ShutdownReason.GameNotFound)
				{
					ErrorStatus = STATUS_SERVER_CLOSED;
				}
				else
				{
					ErrorStatus = StringToLabel(result.ShutdownReason.ToString());
				}

				Log($"Starting DisconnectPeerCoroutine() - Peer {peer.ID}");
				yield return DisconnectPeerCoroutine(peer);

				_coroutine = null;
				yield break;
			}

			limitTime += loadTimeout;

			Log($"Waiting for connection - Peer {peer.ID}");
			StatusDescription = "Waiting for server connection";

			while (peer.IsConnected == false)
			{
				yield return null;

				if (Time.realtimeSinceStartup >= limitTime)
				{
					Debug.LogError(
						$"{peerName} start timeout! IsCloudReady: {runner.IsCloudReady} IsRunning: {runner.IsRunning}");

					Log($"Starting DisconnectPeerCoroutine() - Peer {peer.ID}");
					yield return DisconnectPeerCoroutine(peer);

					_coroutine = null;
					yield break;
				}
			}

			Log($"Loading gameplay scene - Peer {peer.ID}");
			StatusDescription = "Loading gameplay scene";

			while (runner.SimulationUnityScene.IsValid() == false || runner.SimulationUnityScene.isLoaded == false)
			{
				Log($"Waiting for NetworkRunner.SimulationUnityScene - Peer {peer.ID}");
				yield return null;

				if (Time.realtimeSinceStartup >= limitTime)
				{
					Debug.LogError($"{peerName} scene load timeout!");

					Log($"Starting DisconnectPeerCoroutine() - Peer {peer.ID}");
					yield return DisconnectPeerCoroutine(peer);

					_coroutine = null;
					yield break;
				}
			}

			Debug.LogWarning(
				$"{peerName} started on {runner.Simulation.GetLocalAddress()} in {(Time.realtimeSinceStartup - baseTime):0.00}s");

			peer.LoadedScene = runner.SimulationUnityScene;

			if (peer.ID == 0)
			{
				SceneManager.SetActiveScene(peer.LoadedScene);
			}

			StatusDescription = "Waiting for gameplay scene load";

			var scene = peer.SceneManager.GameplayScene;
			while (scene == null)
			{
				Log($"Waiting for GameplayScene - Peer {peer.ID}");

				yield return null;

				scene = peer.SceneManager.GameplayScene;

				if (Time.realtimeSinceStartup >= limitTime)
				{
					Debug.LogError($"{peerName} GameplayScene query timeout!");

					Log($"Starting DisconnectPeerCoroutine() - Peer {peer.ID}");
					yield return DisconnectPeerCoroutine(peer);

					_coroutine = null;
					yield break;
				}
			}

			Log($"Scene.PrepareContext() - Peer {peer.ID}");
			scene.PrepareContext();

			var sceneContext = scene.Context;
			sceneContext.IsVisible = peer.ID == 0;
			sceneContext.HasInput = peer.ID == 0;
			sceneContext.Runner = peer.Runner;
			sceneContext.PeerUserID = peer.UserID;

			peer.Context = sceneContext;
			pool.Context = sceneContext;

			StatusDescription = "Waiting for networked game";

			var networkGame = scene.GetComponentInChildren<NetworkGame>(true);

			while (networkGame.Object == null)
			{
				Log($"Waiting for NetworkGame - Peer {peer.ID}");

				yield return null;

				if (Time.realtimeSinceStartup >= limitTime)
				{
					Debug.LogError($"{peerName} start timeout! Network game not started properly.");

					Log($"Starting DisconnectPeerCoroutine() - Peer {peer.ID}");
					yield return DisconnectPeerCoroutine(peer);

					_coroutine = null;
					yield break;
				}

				if (_currentSession.ConnectionRequested == false)
				{
					// Stop requested by user
					Log(
						$"Starting DisconnectPeerCoroutine() - Connection is not requested anymore - Peer {peer.ID}");
					yield return DisconnectPeerCoroutine(peer);

					_coroutine = null;
					yield break;
				}
			}

			StatusDescription = "Waiting for gameplay load";

			Log($"NetworkGame.Initialize() - Peer {peer.ID}");
			networkGame.Initialize(peer.Request.GameplayType);

			while (scene.Context.GameplayMode == null)
			{
				Log($"Waiting for GameplayMode - Peer {peer.ID}");

				yield return null;

				if (Time.realtimeSinceStartup >= limitTime)
				{
					Debug.LogError($"{peerName} start timeout! Gameplay mode not started properly.");

					Log($"Starting DisconnectPeerCoroutine() - Peer {peer.ID}");
					yield return DisconnectPeerCoroutine(peer);

					_coroutine = null;
					yield break;
				}
			}

			StatusDescription = "Activating scene";

			Log($"Scene.Initialize() - Peer {peer.ID}");
			scene.Initialize();

			Log($"Scene.Activate() - Peer {peer.ID}");
			yield return scene.Activate();

			StatusDescription = "Activating network game";

			Log($"NetworkGame.Activate() - Peer {peer.ID}");
			networkGame.Activate();

			if (SceneManager.GetSceneByName(_loadingScene).IsValid() == true)
			{
				// Wait a little bit for scene activation before showing it
				yield return new WaitForSeconds(1f);

				Log($"Hide loading scene");
				yield return ShowLoadingSceneCoroutine(false);
			}

			if (peer.WasConnected == true)
			{
				peer.ReconnectionTries++;
			}

			peer.WasConnected = true;

			_coroutine = null;

			Log($"ConnectPeerCoroutine() finished");
		}

		/// <summary>Sets hathoraDeployInfo</summary>
		private async Task<HathoraGetDeployInfoResult> hathoraServerGetRoomLobbyInfo()
		{
			string errLogPrefix = $"[Networking.{nameof(hathoraServerGetRoomLobbyInfo)}]"; 
			Log(nameof(hathoraServerGetRoomLobbyInfo));
			
			if (HathoraServerMgr.Singleton == null)
			{
				throw new NullReferenceException($"!{nameof(HathoraServerMgr)} - " +
					"Did you serialize this as a component to HathoraManager");
			}
			
			HathoraGetDeployInfoResult deployInfo = await HathoraServerMgr.Singleton.ServerGetDeployedInfoAsync();
			Assert.IsNotNull(deployInfo, $"{errLogPrefix} Expected deployInfo");
			Assert.IsTrue(deployInfo.CheckIsValid(_expectingLobby: true), 
                $"{errLogPrefix} Expected deployInfo.CheckIsValid(expectingLobby)");
				
			SessionRequest sessionReq = deployInfo.GetLobbyInitConfig<SessionRequest>(); // ByVal
			Assert.IsTrue(sessionReq.MaxPlayers > 0, $"{errLogPrefix} Expected sessionReq.MaxPlayers > 0");

			return deployInfo;
		}

		/// <summary>
        /// Gets the ip:server from Hathora server `Process` API wrapper.
        /// - On succcess, sets _startGameArgsByRef.
        /// </summary>
        /// <returns>ByRef changes in _startGameArgsContainer</returns>
        private async Task<(IPAddress ip, ushort port)> hathoraServerGetIpAsync()
        {
	        Log(nameof(hathoraServerGetIpAsync));
	        
            // ===============================================================================
            //
            // OBSERVATIONS:
            // - If we completely omit this #region, Photon's discovery protocols will *still*
            //   find/register the server deployed in Hathora and you'll see Room logs when
            //   the player connects, *but* CCU !registers in the Hathora Console (and, thus,
            //   shuts itself [the Processs] down after 5m).
            //
            // ===============================================================================
            //
            // HATHORA FLOW:
            // 1. Serialize HathoraServerConfig @ Photon's GlobalSettingg ScriptableObject
            // 2. Get server ip:port via Hathora server `Process` API wrapper to get processId
            // 3. Convert the host name to IP addresss
            // 4. Set Photon's startGameArgs.CustomPublicAddress
            //
            // ===============================================================================
				
            // Coroutine workaround for async/await: Loop the Task until we have a result =>
            (IPAddress ip, ushort port) ipPort;
            try
            {
                ipPort = await GetHathoraServerIpPortAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(GetHathoraServerIpPortAsync)} failed: {e.Message}");
                throw;
            }

            return ipPort;
        }


		#region Hathora Utils
		/// <summary>
		/// Gets processsId from env var ->   
		/// </summary>
		/// <returns></returns>
		private async Task<(IPAddress ip, ushort port)> GetHathoraServerIpPortAsync()
		{
			string logPrefix = $"[Networking.{nameof(GetHathoraServerIpPortAsync)}]";
			(IPAddress ip, ushort port) ipPort = default;

			Process process = null;
			try
			{
				process = await HathoraServerMgr.Singleton.GetCachedHathoraProcessAsync();
			}
			catch (Exception e)
			{
				Debug.LogError($"{logPrefix} GetCachedSystemHathoraProcess => Error: {e.Message}");
				return ipPort;
			}
			
			Assert.IsNotNull(process?.ExposedPort?.Host, $"{logPrefix} Expected `process.ExposedPort.Host`");
			Assert.IsTrue(process?.ExposedPort?.Port > 0, $"{logPrefix} Expected `process.ExposedPort.Port > 0`");
				
			// Get the IP address from the host name; this can return > 1, but we just want 1st
			ipPort.ip = await HathoraUtils.ConvertHostToIpAddress(process.ExposedPort.Host);
			ipPort.port = (ushort)process.ExposedPort.Port;
			
			return ipPort;
		}

		/// <summary>Validates -> logs -> sets ip:port from cached vals</summary>
		/// <param name="_startGameArgsContainer">Passed ByRef to set `CustomPublicAddress`</param>
		private async Task setCustomHathoraServerPublicAddressFromCached(
			StartGameArgsContainer _startGameArgsContainer)
		{
			// Get info from hathoraDeployInfo cache
			(IPAddress _ip, ushort _port) connectInfo = await hathoraDeployInfo.GetHathoraServerIpPortAsync();
            Log($"[setCustomPublicAddress] ip:port == `{connectInfo._ip}:{connectInfo._port}`");

			_startGameArgsContainer.StartGameArgs.CustomPublicAddress = connectInfo._ip == null
				? NetAddress.Any(connectInfo._port)
				: NetAddress.CreateFromIpPort(connectInfo._ip.ToString(), connectInfo._port);
		}
		#endregion // Hathora Utils
		

		private IEnumerator DisconnectPeerCoroutine(GamePeer peer)
		{
			StatusDescription = "Disconnecting from server";

			UnityScene gameplayScene = default;

			try
			{
				if (peer.Runner != null)
				{
					// Possible exception when runner tries to read config
					gameplayScene = peer.Runner.SimulationUnityScene;

					// Close and hide the room
					if (peer.Runner.IsServer == true && peer.Runner.SessionInfo != null)
					{
						Log($"Closing the room");
						peer.Runner.SessionInfo.IsOpen = false;
						peer.Runner.SessionInfo.IsVisible = false;
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}

			if (gameplayScene.IsValid() == false)
			{
				gameplayScene = peer.LoadedScene;
			}

			if (gameplayScene.IsValid() == true)
			{
				Scene scene = gameplayScene.GetComponent<Scene>(true);
				if (scene != null)
				{
					try
					{
						Log($"Deinitializing Scene");
						scene.Deinitialize();
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}

			Task shutdownTask = null;

			if (peer.Runner != null)
			{
				Debug.LogWarning($"Shutdown {peer.Runner.name} ...");

				try
				{
					shutdownTask = peer.Runner.Shutdown(true);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}

			Log($"Show loading scene");
			yield return ShowLoadingSceneCoroutine(true);

			if (shutdownTask != null)
			{
				float operationTimeout = 10.0f;
				while (operationTimeout > 0.0f && shutdownTask.IsCompleted == false)
				{
					yield return null;
					operationTimeout -= Time.unscaledDeltaTime;
				}
			}

			StatusDescription = "Unloading gameplay scene";

			yield return null;

			if (gameplayScene.IsValid() == true)
			{
				Debug.LogWarning($"Unloading scene {gameplayScene.name}");

				yield return SceneManager.UnloadSceneAsync(gameplayScene);
				yield return null;
			}

			peer.Loaded       = default;
			peer.Runner       = default;
			peer.SceneManager = default;
			peer.LoadedScene  = default;

			_coroutine = null;

			Log($"DisconnectPeerCoroutine() finished");
		}

		private IEnumerator ShowLoadingSceneCoroutine(bool show, float additionalTime = 1f)
		{
			var loadingScene = SceneManager.GetSceneByName(_loadingScene);

			if (loadingScene.IsValid() == false)
			{
				yield return SceneManager.LoadSceneAsync(_loadingScene, LoadSceneMode.Additive);
				loadingScene = SceneManager.GetSceneByName(_loadingScene);
			}

			if (show == false && additionalTime > 0f)
			{
				// Wait additional time till fade out starts
				yield return new WaitForSeconds(additionalTime);
			}

			yield return null;

			var loadingSceneObject = loadingScene.GetComponent<LoadingScene>();
			if (loadingSceneObject != null)
			{
				if (show == true)
				{
					loadingSceneObject.FadeIn();
				}
				else
				{
					loadingSceneObject.FadeOut();
				}

				while (loadingSceneObject.IsFading == true)
					yield return null;
			}

			if (show == true && additionalTime > 0f)
			{
				// Wait additional time after fade in
				yield return new WaitForSeconds(additionalTime);
			}

			if (show == false)
			{
				yield return SceneManager.UnloadSceneAsync(loadingScene);
			}
		}

		private IEnumerator LoadMenuCoroutine()
		{
			string menuSceneName = Global.Settings.MenuScene;

			if (SceneManager.sceneCount == 1 && SceneManager.GetSceneAt(0).name == menuSceneName)
			{
				_coroutine = null;
				yield break;
			}

			StatusDescription = "Unloading gameplay scenes";

			yield return ShowLoadingSceneCoroutine(true);

			for (int i = SceneManager.sceneCount - 1; i >= 0; --i)
			{
				var scene = SceneManager.GetSceneAt(i);

				if (scene.name != _loadingScene)
				{
					yield return SceneManager.UnloadSceneAsync(scene);
				}
			}

			StatusDescription = "Loading menu scene";
			yield return null;

			yield return SceneManager.LoadSceneAsync(menuSceneName, LoadSceneMode.Additive);
			yield return ShowLoadingSceneCoroutine(false);

			SceneManager.SetActiveScene(SceneManager.GetSceneByName(menuSceneName));

			_coroutine = null;
		}

		private void OnGamePeerInitialized(NetworkRunner runner)
		{
			if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Multiple)
				return;

			Camera camera = runner.SimulationUnityScene.FindMainCamera();
			if (camera != null)
			{
				camera.gameObject.SetActive(false);
			}

			EventSystem eventSystem = runner.SimulationUnityScene.GetComponent<EventSystem>(true);
			if (eventSystem != null)
			{
				eventSystem.gameObject.SetActive(false);
			}
		}

		private void UpdatePeerSwitch(GamePeer[] peers)
		{
			int  newID      = -1;
			bool showOthers = false;

			bool canSwitchPeer = Application.isEditor == true ? true : Keyboard.current.leftCtrlKey.isPressed == true && Keyboard.current.leftShiftKey.isPressed == true;
			if (canSwitchPeer == true)
			{
				if (Keyboard.current.numpad1Key.wasPressedThisFrame == true)
				{
					newID = 0;
				}
				else if (Keyboard.current.numpad2Key.wasPressedThisFrame == true)
				{
					newID = 1;
				}
				else if (Keyboard.current.numpad3Key.wasPressedThisFrame == true)
				{
					newID = 2;
				}
				else if (Keyboard.current.numpad4Key.wasPressedThisFrame == true)
				{
					newID = 0;
					showOthers = true;
				}
				else if (Keyboard.current.numpad5Key.wasPressedThisFrame == true)
				{
					newID = 1;
					showOthers = true;
				}
				else if (Keyboard.current.numpad6Key.wasPressedThisFrame == true)
				{
					newID = 2;
					showOthers = true;
				}
			}

			if (newID >= 0 && newID < peers.Length)
			{
				for (int i = 0; i < peers.Length; i++)
				{
					GamePeer peer = peers[i];

					peer.Context.HasInput = peer.ID == newID;
					peer.Context.IsVisible = peer.ID == newID || showOthers == true;
				}
			}
		}

		private void ValidateMultiPeers(GamePeer[] peers)
		{
			if (peers.SafeCount() <= 0)
				return;

			int inputPeer = -1;
			int visibilityPeer = -1;

			for (int i = 0; i < peers.Length; i++)
			{
				GamePeer peer = peers[i];

				if (peer.Context == null)
					continue;

				if (peer.Context.HasInput)
				{
					if (inputPeer >= 0)
					{
						Debug.Log($"Multiple peers with input is not allowed, turning off input for peer {peer.ID}");
						peer.Context.HasInput = false;
					}
					else
					{
						inputPeer = peer.ID;
					}
				}

				if (peer.Context.IsVisible == true && visibilityPeer < 0)
				{
					visibilityPeer = peer.ID;
				}
			}

			if (peers[0].Context != null)
			{
				if (inputPeer < 0)
				{
					Debug.Log($"No input peer, turning on input for peer {peers[0].ID}");
					peers[0].Context.HasInput = true;
				}

				if (visibilityPeer < 0)
				{
					Debug.Log($"No visible peer, turning on visibility for peer {peers[0].ID}");
					peers[0].Context.IsVisible = true;
				}
			}
		}

		private Dictionary<string, SessionProperty> CreateSessionProperties(SessionRequest request)
		{
			var dictionary = new Dictionary<string, SessionProperty>();

			dictionary[DISPLAY_NAME_KEY] = request.DisplayName;
			dictionary[MAP_KEY]          = Global.Settings.Map.GetMapIndexFromScenePath(request.ScenePath);
			dictionary[TYPE_KEY]         = (int)request.GameplayType;
			dictionary[MODE_KEY]         = (int)request.GameMode;

			return dictionary;
		}

		[System.Diagnostics.Conditional("ENABLE_LOGS")]
		private void Log(string message)
		{
			Debug.Log($"[{Time.realtimeSinceStartup:F3}][{Time.frameCount}] Networking({GetInstanceID()}): {message}\n");
		}

		private static string StringToLabel(string myString)
		{
			var label = System.Text.RegularExpressions.Regex.Replace(myString, "(?<=[A-Z])(?=[A-Z][a-z])", " ");
			label = System.Text.RegularExpressions.Regex.Replace(label, "(?<=[^A-Z])(?=[A-Z])", " ");

			return label;
		}

		private static bool IsSameScene(string assetPath, string scenePath)
		{
			return assetPath == $"Assets/{scenePath}.unity";
		}

		// HELPERS

		private sealed class GamePeer
		{
			public int                         ID;
			public SceneRef                    Scene;
			public SceneContext                Context;
			public GameMode                    GameMode;
			public NetworkRunner               Runner;
			public NetworkSceneManager         SceneManager;
			public UnityScene                  LoadedScene;
			public string                      UserID;
			public SessionRequest              Request;
			public int                         ConnectionTries   = 3;
			public int                         ReconnectionTries = 1;

			public bool                        Loaded;
			public bool                        WasConnected;
			public bool                        CanConnect => WasConnected == true ? ReconnectionTries > 0 : ConnectionTries > 0;

			public bool IsConnected
			{
				get
				{
					if (Runner == null)
						return false;

					if (Request.GameMode == GameMode.Single)
						return true;

					if (Runner.IsCloudReady == false || Runner.IsRunning == false)
						return false;

					return GameMode == GameMode.Client ? Runner.IsConnectedToServer : true;
				}
			}

			public GamePeer(int id)
			{
				ID = id;
			}
		}

		private class Session
		{
			public bool       ConnectionRequested;
			public GamePeer[] GamePeers;

			public bool IsConnected
			{
				get
				{
					if (GamePeers.SafeCount() == 0)
						return false;

					for (int i = 0; i < GamePeers.Length; i++)
					{
						if (GamePeers[i].IsConnected == false)
							return false;
					}

					return true;
				}
			}
		}
	}
}
