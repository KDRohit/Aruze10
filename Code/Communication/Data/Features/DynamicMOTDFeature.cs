using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.IO;

public class DynamicMOTDFeature : EventFeatureBase
{
	public static DynamicMOTDFeature instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<DynamicMOTDFeature>("dynamic_motd");
		}
	}

	public Dictionary<string, List<string>> templateTexturePaths = new Dictionary<string, List<string>>();

	public Dictionary<string, JSON> validMOTDConfigs = new Dictionary<string, JSON>();
	public Dictionary<string, DialogAudioPack> audioPacks = new Dictionary<string, DialogAudioPack>();

	// Path to where our objects are and some of their file types/names etc. 
	public const string WIDGET_PATH = "Assets/Data/HIR/Bundles/Initialization/Prefabs/Misc/Dynamic MOTD Widgets/";
	public const string BASE_PATH = "Assets/Data/HIR/Bundles/Initialization/Prefabs/Dialogs/Dynamic MOTD/";

	// Widgets go here
	public const string BUTTON_NAME = WIDGET_PATH + "Button.prefab";
	public const string LABEL_NAME = WIDGET_PATH + "Label Body.prefab";
	public const string CLOSE_BUTTON_NAME = WIDGET_PATH + "Close Button.prefab";
	public const string RENDERER_NAME = WIDGET_PATH + "Texture.prefab";
	public const string BOUNDED_BACKGROUND_NAME = WIDGET_PATH + "Bounded Background.prefab";
	public const string FRAME_NAME = WIDGET_PATH + "Dynamic MOTD Frame.prefab";
	public const string TIMER = WIDGET_PATH + "Timer Detached.prefab";
	public const string TIMER_ATTACHED = WIDGET_PATH + "Timer Attached.prefab";
	public const string PAGE_CONTROLLER_PATH = WIDGET_PATH + "Pagination Bar.prefab";

	public const string TEST_DATA_PATH = WIDGET_PATH + "test_dynamic_motd.txt";

	// This is esentially the dialog itself
	public const string BASE_TEMPLATE_NAME = BASE_PATH + "Dynamic MOTD Base.prefab";

	// Names of known background objects to grab
	public const string BOUNDED_BACKGROUND_SPRITE_NAME = "Generic Dialog Panel";

	// Response key
	public const string S3_DATA_RESPONSE_KEY = "dynamic_motd2_data";

	// Configs are stored on a per stage basis so..
	private const string DEVELOPMENT = "development";
	private const string STAGING = "staging";
	private const string PRODUCTION = "production";

	public Dictionary<string, GameObject> cachedObjects;

	public override bool isEnabled
	{
		get
		{
			return ExperimentWrapper.DynamicMotdV2.isInExperiment;
		}
	}

	// Retrieve the LoLa config data from S3.
	public IEnumerator getDataFromS3()
	{
		string knownConfig = PlayerPrefsCache.GetString(Prefs.DYNAMIC_MOTD_CONFIG_VERSION, "");
		string currentConfig = ExperimentWrapper.DynamicMotdV2.config;
		
		string knownVariant = PlayerPrefsCache.GetString(Prefs.DYNAMIC_MOTD_EOS_VARIANT, "");
		string currentVariant = ExperimentWrapper.DynamicMotdV2.variant;
		
		// If this is a new config
		if (knownConfig != currentConfig || knownVariant != currentVariant)
		{
			PlayerPrefsCache.SetString(Prefs.SEEN_DYNAMIC_MOTD_TEMPLATES, "");
			// Try to grab the old one
			if (File.Exists(dynamicMOTDConfigPath))
			{
				// Read the JSON from the file
				string oldConfigJSONString = "";
				oldConfigJSONString = File.ReadAllText(dynamicMOTDConfigPath);
				JSON oldConfigJSON = new JSON(oldConfigJSONString);

				// Grab keys and prepare to grab frame data
				Dictionary<string, JSON> oldFrameData = new Dictionary<string, JSON>();
				List<string> keys = new List<string>();
				if (oldConfigJSON.jsonDict != null)
				{
					keys = oldConfigJSON.getKeyList();
					
					JSON workingJSON;
					for (int i = 0; i < keys.Count; i++)
					{
						// Add the frame data by key to our old frame data dict
						workingJSON = oldConfigJSON.getJSON(keys[i]);
						oldFrameData.Add(keys[i], workingJSON);
					}

					// Go through each image in the old frame data and delete them. Then delete the old config
					checkImagePaths(oldFrameData);
					attemptDelete(dynamicMOTDConfigPath);
				}
			}
		}

		// Cache all of our objects we'll be using to make stuff
		cachedObjects = new Dictionary<string, GameObject>();
		cachedObjects[BUTTON_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(BUTTON_NAME);
		cachedObjects[LABEL_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(LABEL_NAME);
		cachedObjects[CLOSE_BUTTON_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(CLOSE_BUTTON_NAME);
		cachedObjects[RENDERER_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(RENDERER_NAME);
		cachedObjects[BOUNDED_BACKGROUND_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(BOUNDED_BACKGROUND_NAME);
		cachedObjects[BASE_TEMPLATE_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(BASE_TEMPLATE_NAME);
		cachedObjects[TIMER] = SkuResources.getObjectFromMegaBundle<GameObject>(TIMER);
		cachedObjects[PAGE_CONTROLLER_PATH] = SkuResources.getObjectFromMegaBundle<GameObject>(PAGE_CONTROLLER_PATH);
		cachedObjects[TIMER_ATTACHED] = SkuResources.getObjectFromMegaBundle<GameObject>(TIMER_ATTACHED);

		JSON jsonData = null;
		if (!File.Exists(dynamicMOTDConfigPath))
		{
			string currentStage = "";
#if ZYNGA_PRODUCTION
			currentStage = PRODUCTION;
#else
			// We have no way to see if we're on staging, and the path to the data can vary on dev stages, etc
			// So lets try to just grab the path out of the name.
			if (Glb.stageName.Contains("_"))
			{
				currentStage = Glb.stageName.Substring(0, Glb.stageName.IndexOf("_"));
			}
			else
			{
				currentStage = Glb.stageName;

				// weird local testing crap
				if (currentStage == "prod")
				{
					currentStage = PRODUCTION;
				}
			}
#endif
			
			string url = Data.getFullUrl("dynamic_dialogs/" + currentStage + "/variant_config/" + ExperimentWrapper.DynamicMotdV2.config + "_" + ExperimentWrapper.DynamicMotdV2.variant);
			yield return RoutineRunner.instance.StartCoroutine(Server.attemptRequest(url, null, "", S3_DATA_RESPONSE_KEY, false));
			
			jsonData = Server.getResponseData(S3_DATA_RESPONSE_KEY);
			if (jsonData != null)
			{
				File.WriteAllText(dynamicMOTDConfigPath, jsonData.ToString());
			}
			else
			{
				Debug.LogError("URL for Dynamic MOTD v2 config returned null data. This has been known to happen if Glb.switchCdnUrl is false. Glb.switchCdnUrl is currently " + Glb.switchCdnUrl);
			}
		}
		else
		{
			string readData = File.ReadAllText(dynamicMOTDConfigPath);
			jsonData = new JSON(readData);
		}

		if (jsonData == null)
		{
			Debug.LogError("No contents in dynamic motd data request.");
		}
		else
		{
			validMOTDConfigs = new Dictionary<string, JSON>();
			// Check what templates we've seen
			string templatesString = PlayerPrefsCache.GetString(Prefs.SEEN_DYNAMIC_MOTD_TEMPLATES);
			
			List<string> keys = new List<string>();

			// Check to make sure we even have keys to get
			if (jsonData.jsonDict != null)
			{
				keys = jsonData.getKeyList();
			}

			JSON workingJSON;
			for (int i = 0; i < keys.Count; i++)
			{
				workingJSON = jsonData.getJSON(keys[i]);

				int timerStartStamp = workingJSON.getInt("start_time", 0);
				int timerEndStamp = workingJSON.getInt("end_time", 0);
				int maxViews = workingJSON.getInt("max_views", 0);
				int cooldown = workingJSON.getInt("cooldown", 0);

				int currentTimeStamp = GameTimer.currentTime;
				int lastSeen = 0;
				int seenCount = 0;

				string lastSeenString = CommonDataStructures.getRecordElementWithSpecificKey(templatesString.Split(','), keys[i], 1, 3);
				string seenCountString = CommonDataStructures.getRecordElementWithSpecificKey(templatesString.Split(','), keys[i], 2, 3);

				int.TryParse(lastSeenString, out lastSeen);
				int.TryParse(seenCountString, out seenCount);


				bool isAtMaxViews = maxViews != 0 && seenCount >= maxViews;
				bool isOnCooldown = lastSeen != 0 && (currentTimeStamp - lastSeen) < cooldown;

				if (timerStartStamp != 0 && timerEndStamp != 0)
				{
					if (timerStartStamp < currentTimeStamp && timerEndStamp > currentTimeStamp && !isAtMaxViews && !isOnCooldown)
					{
						validMOTDConfigs.Add(keys[i], workingJSON);

						setupAudio(keys[i], workingJSON);
					}
				}
				else if (!isAtMaxViews && !isOnCooldown)
				{
					// No defined start and end time so add it.
					validMOTDConfigs.Add(keys[i], workingJSON);

					setupAudio(keys[i], workingJSON);
				}
			}

			// check player prefs if the key exists
			createAndCacheAssets(validMOTDConfigs);
			PlayerPrefsCache.SetString(Prefs.DYNAMIC_MOTD_CONFIG_VERSION, currentConfig);
			PlayerPrefsCache.SetString(Prefs.DYNAMIC_MOTD_EOS_VARIANT, ExperimentWrapper.DynamicMotdV2.variant);
		}


		// This should be non blocking. In the meantime lets load test data.
		yield break;
	}

	private void setupAudio(string template, JSON data)
	{
		string audioPackKey = data.getString("audio", "dynamic_dialog_default");
		DialogAudioPack  audioPack = new DialogAudioPack(audioPackKey);
		audioPack.addAudio(DialogAudioPack.OK, "Ok");
		audioPack.addAudio(DialogAudioPack.CLOSE, "Close");
		audioPack.addAudio(DialogAudioPack.OPEN, "Open");
		audioPack.addAudio(DialogAudioPack.MUSIC, "Music");
		audioPack.preloadAudio();

		audioPacks.Add(template, audioPack);
	}

	// Looks to see if all the images for a give dynamic dialog are cached and ready to prevent
	// loading times increasing or delays in showing the MOTD
	public List<string> getReadyDialogs()
	{
		List<string> readyTemplateKeys = new List<string>();

		// For each template and its associated paths to its images
		foreach (KeyValuePair<string, List<string>> pair in templateTexturePaths)
		{
			int currentCount = 0;
			if (pair.Value == null)
			{
				continue;
			}

			// For each image path it has
			for (int i = 0; i < pair.Value.Count; i++)
			{
				// if we have the image, count it
				if (DisplayAsset.isTextureDataCachedOnDisk(pair.Value[i]))
				{
					currentCount++;
				}
			}

			// if we have all the images, add the template name to this list so we can look up the paths to download
			if (currentCount == pair.Value.Count)
			{
				readyTemplateKeys.Add(pair.Key);
			}
			else
			{
#if UNITY_WEBGL
				// If it's webGL we can't seem to keep images cached across sessions so allow it through anyway
				readyTemplateKeys.Add(pair.Key);
#endif
			}
		}

		return readyTemplateKeys;
	}

	private void createAndCacheAssets(Dictionary<string, JSON> dict)
	{
		foreach(KeyValuePair<string,JSON> pair in validMOTDConfigs)
		{
			createAndCacheAssets(pair.Key, pair.Value);
		}
	}

	private void checkImagePaths(Dictionary<string, JSON> dict)
	{
		foreach (KeyValuePair<string, JSON> pair in validMOTDConfigs)
		{
			deleteOldImageData(pair.Value);
		}
	}

	public static string dynamicMOTDConfigPath
	{
		get
		{
			return FileCache.path + "/" + "dynamic_motd.txt";
		}
	}


	private static void attemptDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch
		{
			// Nothing to catch here
		}
	}

	// Returns the full path to where the resource would be located in the cache (does not guarantee it exists).
	private static string findCachedUrl(string path)
	{
		return FileCache.path + path.Replace("/", ".");
	}

	private void createAndCacheAssets(string key, JSON data = null)
	{
		// Use i to iterate through all possible frames
		int i = 0;


		if (templateTexturePaths == null)
		{
			templateTexturePaths = new Dictionary<string, List<string>>();
		}

		List<string> paths = new List<string>();
		JSON frameJSON;
		JSON imageJSON;
		string imagePath = "";
		// We don't know how many frames we'll have, but we know they're base 1 so start the parse
		while (data.hasKey("frame_" + i))
		{
			frameJSON = data.getJSON("frame_" + i);
			
			if (frameJSON != null)
			{
				int objectIterator = 0;
				while (frameJSON.hasKey("image_" + objectIterator))
				{
					imageJSON = frameJSON.getJSON("image_" + objectIterator);
	
					imagePath = imageJSON.getString("image_path", "");
					paths.Add(imagePath);
	
					if (!DisplayAsset.isTextureDataCachedOnDisk(imagePath))
					{
						RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTexture(imagePath, onGetMOTDTexture));
					}
	
					objectIterator++;
				}
			}

			i++;
		}

		if (paths.Count > 0)
		{
			templateTexturePaths.Add(key, paths);
		}
	}

	private void onGetMOTDTexture(Texture texture, Dict data)
	{
		// Do nothing, we just wanted this cached.
		// Debug.LogError("Finished caching asset");
	}

	private static void deleteOldImageData(JSON oldConfig)
	{
		string dataPath = "";
		string cachePath = "";

		JSON frameJSON;
		JSON imageJSON;
		string imagePath;
		int i = 0;
		while (oldConfig.hasKey("frame_" + i))
		{
			frameJSON = oldConfig.getJSON("frame_" + i);

			int objectIterator = 0;
			while (frameJSON.hasKey("image_" + objectIterator))
			{
				imageJSON = frameJSON.getJSON("image_" + objectIterator);
				imagePath = imageJSON.getString("image_path", "");
				cachePath = findCachedUrl(imagePath);
				dataPath = cachePath + ".data";

				if (File.Exists(dataPath))
				{
					attemptDelete(dataPath);

				}
				objectIterator++;
			}
			i++;
		}
	}
}

