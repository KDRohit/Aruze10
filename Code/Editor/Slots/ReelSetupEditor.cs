using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(ReelSetup))]
public class ReelSetupEditor : Editor
{
	private ReelSetup currentReelSetup = null;
	private SerializedObject serializedReelSetup = null;
	private SerializedObject serializedReelGame = null;

	public override void OnInspectorGUI()
	{
		currentReelSetup = target as ReelSetup;

		DrawDefaultInspector();

		ReelSetup reelSetup = (ReelSetup)target;
		ReelGame reelGame = reelSetup.GetComponent<ReelGame>();

		if (serializedReelSetup == null)
		{
			serializedReelSetup = new SerializedObject(reelSetup);
		}

		if (serializedReelGame == null)
		{
			serializedReelGame = new SerializedObject(reelGame);
		}

		SerializedProperty payBoxSize = serializedReelGame.FindProperty("payBoxSize");
		Vector2 prevPayBoxSize = payBoxSize.vector2Value;
		EditorGUILayout.PropertyField(payBoxSize, true);
		Vector2 currentPayBoxSize = payBoxSize.vector2Value;

		SerializedProperty paylineScaler = serializedReelGame.FindProperty("paylineScaler");
		float prevPaylineScaler = paylineScaler.floatValue;
		EditorGUILayout.PropertyField(paylineScaler, true);
		float currentPaylineScaler = paylineScaler.floatValue;

		SerializedProperty paylinesGameObjectZOffset = serializedReelGame.FindProperty("activePaylinesGameObjectZOffset");
		float prevPaylinesGameObjectZOffset = paylinesGameObjectZOffset.floatValue;
		EditorGUILayout.PropertyField(paylinesGameObjectZOffset, true);
		float currentPaylinesGameObjectZOffset = paylinesGameObjectZOffset.floatValue;

		SerializedProperty reelRoots = serializedReelGame.FindProperty("reelRoots");
		EditorGUILayout.PropertyField(reelRoots, true);

		SerializedProperty symbolTemplatesProp = serializedReelGame.FindProperty("symbolTemplates");
		EditorGUILayout.PropertyField(symbolTemplatesProp, true);

		SerializedProperty symbolVerticalSpacing = serializedReelGame.FindProperty("symbolVerticalSpacing");
		float prevVerticalSpacing = symbolVerticalSpacing.floatValue;
		EditorGUILayout.PropertyField(symbolVerticalSpacing, true);
		float currentVerticalSpacing = symbolVerticalSpacing.floatValue;

		SerializedProperty playFreespinsInBasegame = serializedReelGame.FindProperty("playFreespinsInBasegame");
		EditorGUILayout.PropertyField(playFreespinsInBasegame, true);

		serializedReelSetup.ApplyModifiedProperties();
		serializedReelSetup.Update();
		serializedReelGame.ApplyModifiedProperties();
		serializedReelGame.Update();

		// ensure we only trigger the paybox rendering if the object is actually in the scene
		if (currentReelSetup.reelGame != null)
		{
			// cycle through all the layers and check if that specific layer wants to render payboxes
			bool isShowingAnyPayboxes = false;
			bool needsToUpdateReelRootInfo = false;
			for (int i = 0; i < currentReelSetup.layerInformation.Length; i++)
			{
				ReelSetup.LayerInformation info = currentReelSetup.layerInformation[i];
				if (info.payBoxInfo.showPayboxes)
				{
					isShowingAnyPayboxes = true;

					if (info.payBoxInfo.payBoxScript == null)
					{
						info.payBoxInfo.payBoxScript = CommonGameObject.instantiate(SkuResources.getObjectFromMegaBundle<GameObject>(ReelSetup.EDITOR_PAYBOX_RESOURCE_PATH)) as GameObject;

						if (!Application.isPlaying && currentReelSetup.reelGame.activePaylinesGameObject == null)
						{
							// we need to create the object that the payboxes will be parented to since the game isn't running, 
							// and clean it up when the payboxes aren't shown anymore
							currentReelSetup.reelGame.createActivePaylinesObject("Editor PayBoxes");
						}

						EditorPayBoxScript editorPayBoxScript = info.payBoxInfo.payBoxScript.GetComponent<EditorPayBoxScript>();
						editorPayBoxScript.init(Color.red, currentReelSetup.reelGame, info);
						needsToUpdateReelRootInfo = true;
					}
					else
					{
						// determine if we need to run an update due to value changes
						if (prevPayBoxSize != currentPayBoxSize || prevPaylineScaler != currentPaylineScaler || prevVerticalSpacing != currentVerticalSpacing)
						{
							EditorPayBoxScript editorPayBoxScript = info.payBoxInfo.payBoxScript.GetComponent<EditorPayBoxScript>();
							editorPayBoxScript.init(Color.red, currentReelSetup.reelGame, info);
							needsToUpdateReelRootInfo = true;
						}

						// this only affects the object that the paylines are parented under, so requires a seperate check
						if (currentPaylinesGameObjectZOffset != prevPaylinesGameObjectZOffset)
						{
							Vector3 currentLocalPos = currentReelSetup.reelGame.activePaylinesGameObject.transform.localPosition;
							currentReelSetup.reelGame.activePaylinesGameObject.transform.localPosition = new Vector3(currentLocalPos.x, currentLocalPos.y, currentPaylinesGameObjectZOffset);
						}
					}
				}
				else
				{
					if (info.payBoxInfo.payBoxScript != null)
					{
						if (Application.isPlaying)
						{
							Destroy(info.payBoxInfo.payBoxScript);
						}
						else
						{
							DestroyImmediate(info.payBoxInfo.payBoxScript);
						}

						info.payBoxInfo.payBoxScript = null;
					}
				}
			}

			if (!isShowingAnyPayboxes && currentReelSetup.reelGame != null)
			{
				currentReelSetup.reelGame.destroyActivePaylinesObject();
			}
		}

		if (GUILayout.Button("Create Flattened Symbols"))
		{
			reelGame.createFlattenedSymbolTemplatesWhileNotRunning();
		}
	}

	//////// Run through all of the game's that have optimized symbols and redo them
	public static void createOptimizedSymbolsForAllGames()
	{
		// Load the game data.
		loadMockBasicGameData();
		AssetBundleManager.initForTesting(false, false);
		foreach (SlotGameData gameData in SlotGameData.getAll())
		{

			string keyName = gameData.keyName;
			string name = gameData.name;

			SlotResourceData entry = SlotResourceMap.getData(keyName);

			if (entry != null && entry.isUsingOptimizedFlattenedSymbols)
			{
				GameObject baseGame = SlotResourceMap.getSlotPrefabForTesting(keyName);
				if (baseGame != null)
				{
					string path = SlotResourceMap.getSlotPrefabPathForTesting(keyName);
					baseGame.name = baseGame.name.Replace("(Clone)", "");
					runFlattenedSymbolOnGameObject(baseGame, path);
					DestroyImmediate(baseGame);
				}
				List<GameObject> freespinObjects = SlotResourceMap.getFreespinPrefabsForTesting(keyName);
				List<string> freespinObjectsPaths = SlotResourceMap.getFreespinPrefabsPathForTesting(keyName);
				for (int i = 0; i < freespinObjects.Count; i++)
				{
					GameObject go = freespinObjects[i];
					if (go != null)
					{
						go.name = go.name.Replace("(Clone)", ""); 
						string path = freespinObjectsPaths[i];
						runFlattenedSymbolOnGameObject(go, path);
						DestroyImmediate(go);
					}
				}
				string freespinNames = "None";
				if (freespinObjects.Count > 0 && freespinObjects[0] != null)
				{
					freespinNames = freespinObjects[0].name;
				}
				if (baseGame != null)
				{
					Debug.Log("Running createOptimizedSymbolsForAllGames on: " + keyName + " - " + name + 
						"\n Base Game = " + baseGame.name +
						"\n Freespins = " + freespinNames);
				}
				else
				{
					Debug.LogWarning("Nothing found for " + keyName);
				}
				
			}
			break;

		}

		teardownMockBasicGameData();
	}

	private static void runFlattenedSymbolOnGameObject(GameObject reelGameGO, string path)
	{
		ReelGame reelGame = reelGameGO.GetComponent<ReelGame>();
		if (reelGame == null)
		{
			reelGame = reelGameGO.GetComponentInChildren<ReelGame>();
		}
		AssetBundleManager.getProjectRelativePathFromResourcePath("");
		if (reelGame != null && !reelGame.hasAlreadyPreflattendSymbols())
		{
			reelGame.createFlattenedSymbolTemplatesWhileNotRunning();

			// TODO:UNITY2018:nestedprefabs:confirm//old
			// UnityEditor.PrefabUtility.CreatePrefab(
			// 	path, reelGameGO,
			// 	UnityEditor.ReplacePrefabOptions.ConnectToPrefab);
			// TODO:UNITY2018:nestedprefabs:confirm//new
			PrefabUtility.SaveAsPrefabAssetAndConnect(reelGameGO, path, InteractionMode.AutomatedAction);
		}
	}

	private static JSON mockBasicData = null;
	private static JSON mockGlobalData = null;

	private static bool isBasicGameDataLoaded = false;
	private static void loadMockBasicGameData()
	{
		if (isBasicGameDataLoaded)
		{
			return;
		}

		// Get basic data.
		if (Data.basicDataUrl == "none")
		{
			Data.loadConfig();
		}
		WWWForm form = new WWWForm();
		form.AddField("sku_key", CommonEditor.GetBuildSKU(false)); // required to get any data
		form.AddField("client_id", "2"); // required for live data
		WWW www = new WWW(Data.basicDataUrl, form);
		while (!www.isDone) { ; }
		string basicDataJsonString = www.text;
		www.Dispose();
		mockBasicData = new JSON(basicDataJsonString);
		Data.setLiveData(mockBasicData.getJSON("live_data"));

		string miniGlobalUrl = mockBasicData.getString("mini_global_data_url", "BADURL");
		SlotGameData.baseUrl = mockBasicData.getString("game_data_base_url", "BAD_URL");
		SlotGameData.dataVersion = mockBasicData.getString("data_version", "0");
		Glb.staticAssetHosts = mockBasicData.getStringArray("static_asset_hosts");
		Glb.bundleBaseUrl = mockBasicData.getString("bundle_base_url", "");

		// Populate some global data.
		www = new WWW(miniGlobalUrl);
		while (!www.isDone) { ; }
		string globalDataJsonString = Server.decompressResponse(www.text);
		www.Dispose();
		mockGlobalData = new JSON(globalDataJsonString);

		// Populate game data.
		if (LobbyGame.getAll().Count == 0)
		{
			SlotResourceMap.populateAll();
			LobbyGameGroup.populateAll(mockGlobalData.getJsonArray("slots_game_groups"));
			foreach (LobbyGame game in LobbyGame.getAll())
			{
				if (game.keyName == "uitest01")
				{
					continue;
				}
				SlotResourceData slotData = SlotResourceMap.getData(game.keyName);
				if (slotData == null || !slotData.isProductionReady)
				{
					Debug.Log(string.Format("Skipping Game {0} not production ready", game.keyName));
					continue;
				}

				string gameDataUrl = SlotGameData.getDataUrl(game.keyName);
				www = new WWW(gameDataUrl);
				while (!www.isDone) { ; }
				if (www.error != null)
				{
					Debug.LogError(string.Format("{0} for game {1} url {2}", www.error, game.keyName, gameDataUrl));
					www.Dispose();
					continue;
				}
				string gameDataJsonString = Server.decompressResponse(www.text);
				www.Dispose();
				JSON gameData = new JSON(gameDataJsonString);
				BonusGame.populateAll(gameData.getJsonArray("bonus_games_data"));
				BonusGamePaytable.populateAll(gameData.getJsonArray("bonus_game_pay_tables"));
				PayTable.populateAll(gameData.getJsonArray("pay_tables"));
				ReelStrip.populateAll(gameData.getJsonArray("reel_strips"));		// ReelStrips must populate before SlotGameData
				ThresholdLadderGame.populateAll(gameData.getJsonArray("threshold_ladder_games"));
				if (SlotGameData.find(game.keyName) == null)
				{
					SlotGameData.populateGame(gameData);
				}
			}
		}
		if (!SlotResourceMap.isPopulated)
		{
			SlotResourceMap.populateAll();
		}

		isBasicGameDataLoaded = true;
	}

	private static void teardownMockBasicGameData()
	{
		mockBasicData = null;
		mockGlobalData = null;
		SlotGameData.resetStaticClassData();
		LobbyGame.resetStaticClassData();
		LobbyGameGroup.resetStaticClassData();
		BonusGame.resetStaticClassData();
		BonusGamePaytable.resetStaticClassData();
		PayTable.resetStaticClassData();
		ReelStrip.resetStaticClassData();
		ThresholdLadderGame.resetStaticClassData();
		SlotResourceMap.resetStaticClassData();
		// Clear Glb static variables we were messing with.
		Glb.bundleBaseUrl = null;
		Glb.staticAssetHosts = null;
		isBasicGameDataLoaded = false;
		// Destroy the AssetBundleManager game object created by the instance.
		GameObject go = GameObject.Find("AssetBundleManager");
		if (go != null)
		{
			GameObject.DestroyImmediate(go);
		}
		// Destroy the AssetBundleManager instance.
		AssetBundleManager.testDestroyInstance();
	}	
}
