using UnityEngine;
using System;
using Fusion;
using Hathora.Core.Scripts.Runtime.Server;

namespace TPSBR
{
	[Serializable]
	[CreateAssetMenu(fileName = "GlobalSettings", menuName = "TPSBR/Global Settings")]
	public class GlobalSettings : ScriptableObject
	{
		[Header("Hathora"), Tooltip("Find via top menu: Hathora/ServerConfigFinder (or @ Assets/Hathora)")]
		public HathoraServerConfig  HathoraServerConfig;

		[Header("Photon")]
		public NetworkRunner        RunnerPrefab;
		public string               LoadingScene = "LoadingScene";
		public string               MenuScene = "Menu";
		public bool                 SimulateMobileInput;

		public AgentSettings        Agent;
		public MapSettings          Map;
		public NetworkSettings      Network;
		public OptionsData          DefaultOptions;
	}
}
