using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Core.Platform;

public class BasicInfoLoader : IDependencyInitializer
{
	private InitializationManager initManager = null;
	
	public static BasicInfoLoader Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new BasicInfoLoader();
			}
			return _instance;
		}
	}
	private static BasicInfoLoader _instance;
	
	private IEnumerator basicDataLoop()
	{
		// We must populate the DialogTypes before anything else,
		// just in case an error happens and we need to show a dialog with a message.
		DialogType.populateAll();
		
		// Get any Canvas-embedded config/basicdata/playerdata objects (for WebGL)
		getEmbeddedCanvasData();

		yield return RoutineRunner.instance.StartCoroutine(LoadBasicData.getBasicGameData());
		
		if (Data.isBasicDataSet)
		{
			// Only if BasicData is good do we proceed with GlobalData...
			yield return RoutineRunner.instance.StartCoroutine(LoadGlobalData.getGlobalData());
			
			if (Data.isGlobalDataSet)
			{
				Glb.cleanupMemoryAsync();  // global-data loading created a big json dict, then we got rid of all refs to it, so cleanup is apropos now
				StatsManager.Instance.LogLoadTimeStart("BIL_InitComplete");
				if (this.initManager != null)
				{
					this.initManager.InitializationComplete(this);
				}
				else
				{
					Debug.LogError("BasicInfoLoader::initManager is NULL. Could not inform IntializationManager of successful initialization.");
				}
				StatsManager.Instance.LogLoadTimeEnd("BIL_InitComplete");
			}
		}
	}
	
	// Extract any WebGL canvas-based objects that might have been passed to us during WebGL startup
	// (Will set the  Data.canvasBasedxxx  properties as appropriate)
	private void getEmbeddedCanvasData()
	{
		Debug.Log("Girish: In embedded canvas data");
#if UNITY_WEBGL
		string gameInfoString = WebGLFunctions.getCanvasBasedGameData();

		string platform = WebPlatform.PLATFORM_NAME_FACEBOOK;
		if (!string.IsNullOrEmpty(gameInfoString))
		{
			Debug.Log("Found canvas-embedded game data; extracting Config, BasicData, and PlayerData...");
			JSON gameInfo = new JSON(gameInfoString);

			Data.canvasBasedConfig = gameInfo.getJSON("config");
			Debug.Assert( Data.canvasBasedConfig != null, "Error! Missing canvas-based Config");

			Data.canvasBasedBasicData = gameInfo.getJSON("basic_game_data");
			Debug.Assert( Data.canvasBasedBasicData != null, "Error! Missing canvas-based BasicData");

			// Determine webplatform, defaulting to Facebook.
			platform = Data.canvasBasedConfig.getBool("dotcom", false) ? WebPlatform.PLATFORM_NAME_DOTCOM : WebPlatform.PLATFORM_NAME_FACEBOOK;

			// check for config flag. 
			if (Data.canvasBasedConfig != null && Data.canvasBasedConfig.getBool("dotcom", false))
			{
				platform = WebPlatform.PLATFORM_NAME_DOTCOM;
			}

			/*
			// this is currently not working since the client must send which bundles it has to the server
			// and the server does not have this information when creating the compressedPlayerData
			// playerdata string is compressed same as server; with a 'c' prefix
			string compressedPlayerData = gameInfo.getString("playerData", null);
			if (!string.IsNullOrEmpty(compressedPlayerData))
			{
				string decompressedPlayerData = Server.decompressResponse(compressedPlayerData);
				if (!string.IsNullOrEmpty(decompressedPlayerData))
				{
					Data.canvasBasedPlayerData = new JSON(decompressedPlayerData);
				}
				else
				{
					Debug.LogError("Error! canvas-based playerData did not decompress");
				}
			}
			Debug.Assert( Data.canvasBasedPlayerData != null, "Error! Missing canvas-based PlayerData");
			*/

		}

#if UNITY_EDITOR
		// Override to DotCom platform/client in editor.
		bool isForcingWebglDotCom = SlotsPlayer.getPreferences().GetBool(DebugPrefs.FORCE_WEBGL_DOTCOM_MODE, false);
		if (isForcingWebglDotCom)
		{
			platform = WebPlatform.PLATFORM_NAME_DOTCOM;
		}
#endif

		Data.webPlatform = new WebPlatform(platform);
		if (Data.webPlatform.IsDotCom)
		{
			DeviceInfo.InitializeWithCustomImplementation(new DeviceInfoHirDotcom(DeviceInfo.CreateDefault()));
		}
#endif
	}

	#region IDependencyInitializer implementation
	public System.Type[] GetDependencies ()
	{
		return new System.Type[] { typeof(URLStartupManager) };
	}

	public void Initialize (InitializationManager mgr)
	{
		this.initManager = mgr;
		RoutineRunner.instance.StartCoroutine(basicDataLoop());
	}
	
	// short description of this dependency for debugging purposes
	public string description()
	{
		return "BasicInfoLoader";
	}
	#endregion


}
