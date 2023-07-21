
# Hathora<>Photon Fusion BR sample game (Unity NGO + Hathora Cloud)

[![UnityVersion](https://img.shields.io/badge/Unity%20Version:-2021.3%20LTS-57b9d3.svg?logo=unity&color=2196F3)](https://unity.com/releases/editor/qa/lts-releases)
[![HathoraSdkVersion](https://img.shields.io/badge/Hathora%20SDK-1.5.1-57b9d3.svg?logo=none&color=AF64EE)](https://hathora.dev/docs)
![image](https://assetstorev1-prd-cdn.unity3d.com/key-image/44946285-5088-4f57-b51b-a996184da940.webp)
<br><br>

This sample game was started from Photon's [Fusion Battle Royale Sample](https://doc.photonengine.com/fusion/current/game-samples/fusion-br/quickstart)
<br><br>

### Overview

The original [Hathora<>Photon Fusion BR Sample](https://doc.photonengine.com/fusion/current/game-samples/fusion-br/quickstart) was a Photon Fusion full game sample that has added:

* Hathora SDK
* Hathora high-level wrappers
* HathoraServerConfig build, deployment and room mgmt utils

Check it out to learn more about hosting Photon Fusion on Hathora Cloud and accessing the Hathora SDK. We have made a few modifications to make this game easily deployable with a dedicated server on [Hathora Cloud](https://hathora.dev/docs).
<br><br>

---
## Readme Contents and Quick Links
<details open> <summary> Click to expand/collapse contents </summary>

- ### [Getting the Project](#getting-the-project-1)
- ### [Requirements](#requirements-1)
- ### [Troubleshooting](#troubleshooting-1)
  - [Bugs](#bugs)
  - [Documentation](#documentation)

</details>

---
<br>

## Getting the project
### Direct download

 - select `Code` and select the 'Download Zip' option.  Please note that this will download the branch you're currently viewing on Github
<br><br>

## Requirements

This sample game is compatible with the latest Unity Long Term Support (LTS) editor version, currently [2021 LTS](https://unity.com/releases/2021-lts). Please include **Linux Dedicated Server Build Support** in your installation, as well as **Linux Build Support (Mono)**.

**PLEASE NOTE:** You will also need a Photon account with an active Fusion app.

You will also need to have an account created for Hathora Cloud (sign up at: https://console.hathora.dev)
<br><br>

## Steps

1. Photon may automatically handle starting your headless Linux server in "Server" mode using CLI `-args`. See a template at [./hathora/Dockerfile]https://github.com/hathora/photon-fusion-br/blob/main/.hathora/Dockerfile) or [Photon docs](https://doc.photonengine.com/fusion/current/game-samples/fusion-br/quickstart)
3. Use the Hathora Unity plugin to configure, build, and deploy your server on Hathora Cloud
4. Once deployed, create a room in Hathora Cloud via any method:
  - Create via `Menu` scene (as a Client): Click "Create" button at the bottom-right (adds a browsable Lobby)
  - via `Hathora ServerConfig`: Click "Create Room" button in the "Create Room" dropdown group
  - via [Hathora Console](https://console.hathora.dev) in your browser at the top-right
5. Play the `Menu` scene and join the new Room that appeared in a browsable Lobby list.

### Region Mapping

Within the demo, we have included [HathoraRegionMap.cs](https://github.com/hathora/photon-fusion-br/blob/main/Assets/HathoraPhoton/HathoraRegionMap.cs) to map the following Photon<>Hathora regions:

**<< Photon : Hathora >>**
- "us" : Washingington DC
- "usw" : Seattle
- "asia" : Singapore210
- "jp" : Tokyo
- "eu" : Frankfurt
- "sa" : SaoPaulo
- "kr" : Singapore

## Troubleshooting
### Bugs
- Report bugs in the sample game using Github [issues](https://github.com/hathora/photon-fusion-br/issues)
  
### Documentation
- For a deep dive into Hathora Cloud, visit our [documentation site](https://hathora.dev/docs).
- Learn more the [Photon Fusion BR Demo](https://doc.photonengine.com/fusion/current/game-samples/fusion-br).

<br><br>

## Community
For help, questions, advice, or discussions about Netcode for GameObjects and its samples, please join our [Discord Community](https://discord.gg/hathora). We have a growing community of multiplayer developers and our team is very responsive.
<br><br>

## Other samples
### Hathora Unity Plugin
[Hathora Unity Plugin (with FishNet and Mirror demos)](https://github.com/hathora/hathora-unity) is our Unity Plugin that comes with FishNet and Mirror networking demos. It allows you to deploy your game on Hathora Cloud directly from our editor plugin.

### Unity NGO Sample
[@hathora/unity-ngo-sample](https://github.com/hathora/unity-ngo-sample) takes Unity's 2D Space Shooter sample game with *Unity NetCode for Game Objects* (NGO) and modifies it to be easily deployable on Hathora Cloud.
<br><br>
