//#define USE_LOCAL_CONFIG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace FeatureOrchestrator
{
	public static class ConfigManager
	{
		private static Dictionary<string, JSON> featuresJSONDict;
		private static List<ProviderConfig> providerConfigs;
		private static List<DataObjectConfig>  dataObjectConfigs;
		private static List<ComponentConfig>  componentConfigs;
		private static bool fetchedS3Config = false;
		private static int processedCount = 0; //The number of features processed. Incremented when either reading from local cache or S3
		private static int experimentValidFeatures = 0; //Number of features that have a valid version from EOS experiment
		
		//Dictionary of featurename => version that are ready on the client
		private static Dictionary<string, string> readyFeaturesDict = null;
		
		//Threshold to break from the sync action coroutine 
		private const float SYNC_ACTION_TIMEOUT_SECONDS = 3f;
		
		delegate BaseConfig NodeParseFunc(JSON json, string featureName);

		public static bool finishedSync { get; private set; }
		public static GenericDelegate onSyncComplete;
		
		private static readonly string[] CAROUSEL_ACTIONS = {"proton_feature_dialog", "proton_feature_video"};

		private static string getDataPath(string featureName, string versionName)
		{
			return string.Format(Application.persistentDataPath + "/{0}_{1}.json", featureName, versionName);
		}

		public static void setup(JSON features)
		{
			if (features != null)
			{
				finishedSync = false;
				readyFeaturesDict = new Dictionary<string, string>();
				featuresJSONDict = new Dictionary<string, JSON>();
				foreach(KeyValuePair<string, object> kvp in features.jsonDict)
				{
					string featureName = kvp.Value as string;
					if (string.IsNullOrEmpty(featureName))
					{
						continue;
					}
					
					//On client the experiment names don't have "hir_" whereas the server has that and that is being sent as the featureName
					string experimentName = featureName.Substring(ExperimentWrapper.HIR_EXPERIMENTS_PREFIX.Length);
					
					//Get the current config version for the feature from EOS
					if (ExperimentManager.EosExperiments.TryGetValue(experimentName, out EosExperiment experiment))
					{
						OrchestratorFeatureExperiment orchestratorFeatureExperiment = experiment as OrchestratorFeatureExperiment;
						if (orchestratorFeatureExperiment != null && orchestratorFeatureExperiment.isInExperiment)
						{
							string version = orchestratorFeatureExperiment.version;
							if (string.IsNullOrEmpty(version))
							{
								Debug.LogError("Empty version found for feature " + featureName);
								continue;
							}
							experimentValidFeatures++;
# if UNITY_EDITOR && USE_LOCAL_CONFIG							
							Orchestrator.instance.updateFeatureConfigs(featureName, getLocalConfig(featureName));
							readyFeaturesDict.Add(featureName, version);
							processedCount++;
#else
							//Check if the config json file is already cached on the client
							//If cached add the featurename, version to the readyFeaturesDict
							//Parse Config and Update the orchestrator config dict 
							
							string path = getDataPath(featureName, version);
							JSON json = null;
							if (File.Exists(path))
							{
								string jsonString = File.ReadAllText(path, System.Text.Encoding.UTF8);
								json = new JSON(jsonString);
								if (json.isValid)
								{
									readyFeaturesDict.Add(featureName, version);
									featuresJSONDict.Add(featureName, json);
									processedCount++;
								}
							}
							else
							{
								//Delete the old cached config files for the feature
								RoutineRunner.instance.StartCoroutine(deleteOldConfigs(featureName));
								//If not cached fetch the json from S3
								getDataFromS3(featureName, version);
							}
#endif
						}
					}
				}
				
				Server.registerEventDelegate("sync_proton_features", onReceivedProtonSync);
				RoutineRunner.instance.StartCoroutine(setFeaturesReadyRoutine());
			}
			else
			{
				finishedSync = true; //Instantly mark this as finished if theres no features to check for
			}
		}

		private static void onReceivedProtonSync(JSON data)
		{
			string[] activeFeatures = data.getStringArray("active_features");
			for (int i = 0; i < activeFeatures.Length; i++)
			{
				if (featuresJSONDict.TryGetValue(activeFeatures[i], out JSON featureJson))
				{
					Orchestrator.instance.updateFeatureConfigs(activeFeatures[i], parseconfig(featureJson, activeFeatures[i]));
				}
				else
				{
					Debug.LogErrorFormat("Client missing config for server active feature {0}", activeFeatures[i]);
				}
			}
			
			checkForInActiveCarousels();
			syncComplete();
			readyFeaturesDict.Clear();
			readyFeaturesDict = null;
			processedCount = 0;
			experimentValidFeatures = 0;
			featuresJSONDict.Clear();
		}

		private static void checkForInActiveCarousels()
		{
			foreach (string featureName in Orchestrator.instance.allFeatureConfigs.Keys)
			{
				for (int i = 0; i < CAROUSEL_ACTIONS.Length; i++)
				{
					CarouselData inactiveAction = CarouselData.findInactiveByAction(string.Format("{0}:{1}", CAROUSEL_ACTIONS[i], featureName));
					if (inactiveAction != null)
					{
						if (inactiveAction.getIsValid())
						{
							inactiveAction.activate();
						}
					}
				}
			}
		}

		private static void syncComplete()
		{
			finishedSync = true;
			if (onSyncComplete != null)
			{
				onSyncComplete.Invoke();
			}
		}

		//This coroutine checks if we have processed reading feature configs either from S3 or local cache
		//and they match with the number of features valid to be processed
		private static IEnumerator setFeaturesReadyRoutine()
		{
			float timeout = 0f;
			//Wait while the processed number of features hasn't yet reached the total experimentValidFeatures
			//or till the timeout
			while (timeout <= SYNC_ACTION_TIMEOUT_SECONDS && experimentValidFeatures > 0 && processedCount < experimentValidFeatures)
			{
				timeout += Time.deltaTime;
				yield return null;
			}

			//Fire the action to let server know the client is ready with the dictionary of featureName => version
			//for the first load this will be empty since the client doesn't  have any cached config
			if (readyFeaturesDict.Count == 0)
			{
				//We don't get a response back if theres no active features. Mark the sync as finished
				syncComplete();
			}
			OrchestratorFeaturesReadyAction.setFeaturesReady(readyFeaturesDict);
		}

		public static IEnumerator deleteOldConfigs(string featureName)
		{
			yield return null;
			
			string[] featureConfigs = Directory.GetFiles(Application.persistentDataPath, featureName + "*.json");
			try
			{
				for (int i = 0; i < featureConfigs.Length; i++)
				{
					File.Delete(featureConfigs[i]);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("Failed to delete feature config file " + e.Message);
			}
		}

		public static void getDataFromS3(string featurename, string version)
		{
			string path = string.Format("Proton/{0}/{1}", featurename, version);
			string url = Data.getFullUrl(path);
			RoutineRunner.instance.StartCoroutine(getData(featurename, version, url));
		}

		private static IEnumerator getData(string featureName, string version, string dataUrl)
		{
			using (UnityWebRequest webRequest2 = UnityWebRequest.Get(dataUrl))
			{
				yield return webRequest2.SendWebRequest();
				if (!webRequest2.isHttpError && !webRequest2.isNetworkError)
				{
					if (webRequest2.downloadHandler != null && webRequest2.downloadHandler.text != null)
					{
						JSON json = new JSON(webRequest2.downloadHandler.text);
						if (json != null)
						{
							//Once valid json is fetched from S3 cache is so if the version hasn't changed in subsequent loads
							//no need to do S3 fetch
							File.WriteAllText(getDataPath(featureName, version), json.ToString(),
								System.Text.Encoding.UTF8);
							if (readyFeaturesDict == null)
							{
								readyFeaturesDict = new Dictionary<string, string>();
							}

							readyFeaturesDict.Add(featureName, version);

							if (featuresJSONDict == null)
							{
								featuresJSONDict = new Dictionary<string, JSON>();
							}

							featuresJSONDict.Add(featureName, json);
						}
					}
					else
					{
						Debug.LogError("Feature Orchestrator - Downlaod finished but handler or text is null" + " - " + dataUrl);
					}
				}
				else
				{
					string error = webRequest2.error != null ? webRequest2.error : "null error";
					Debug.LogError("Feature Orchestrator - network error: " + error +  " - " + dataUrl);
				}
				processedCount++;
			}
		}

		//Used with the USE_LOCAL_CONFIG define to test local config in Unity
		//TODO fallback to config on server if local config not found
		public static FeatureConfig getLocalConfig(string featureName)
		{
			JSON config = null;
			TextAsset configFile = SkuResources.loadSkuSpecificEmbeddedResourceText(string.Format("Data/{0}_config", featureName));
			if (configFile != null && configFile.text != null)
			{
				config = new JSON(configFile.text);
			}
			return parseconfig(config, featureName);
		}

		private static FeatureConfig parseconfig(JSON config, string featureName)
		{
			if (config == null)
			{
				return null;
			}
			componentConfigs = parseConfigData<ComponentConfig>(config, featureName, "componentConfigs", parseComponentConfigItem);
			dataObjectConfigs = parseConfigData<DataObjectConfig>(config, featureName, "dataObjectConfigs", parseDataObjectConfigItem);
			providerConfigs = parseConfigData<ProviderConfig>(config, featureName,"providerConfigs", parseProviderConfigItem);
			return new FeatureConfig(componentConfigs, dataObjectConfigs, providerConfigs);
		}
		
		
		private static List<T> parseConfigData<T>(JSON config, string featureName, string rootNodeName, NodeParseFunc nodeParseFunc) where T : BaseConfig
		{
			List<T> objectConfigs = null;
			JSON[] dataconfigsArray = config.getJsonArray(rootNodeName);
			if (dataconfigsArray != null)
			{
				objectConfigs = new List<T>();
				for (int i = 0; i < dataconfigsArray.Length; i++)
				{
					T objectConfig = nodeParseFunc(dataconfigsArray[i], featureName) as T;
					objectConfigs.Add(objectConfig);
				}

				return objectConfigs;

			}
			
			return null;
		}

		private static BaseConfig parseComponentConfigItem(JSON jsonData, string featureName)
		{
			string keyName = jsonData.getString("keyname", "");
			string owner = jsonData.getString("owner", "");

			string provider = jsonData.getString("provider", "");
			string type = "";

			JSON props = jsonData.getJSON("props");
			Dictionary<string, object> propsDict = props == null ? null : props.jsonDict;
			JSON outs = jsonData.getJSON("outs");
			if (owner == "server")
			{
				type = "ServerComponent";
				if (propsDict == null)
				{
					propsDict = new Dictionary<string, object>();
				}
				propsDict["featureName"] = featureName;
			}
			else
			{
				type = jsonData.getString("type", "");	
			}

			bool showInDevPanel = jsonData.getBool("dev", false);
			
			return new ComponentConfig(keyName, type, owner, provider, outs, propsDict, props, showInDevPanel);
		}

		private static BaseConfig parseDataObjectConfigItem(JSON jsonData,  string featureName)
		{
			string keyName = jsonData.getString("keyname", "");
			string owner = jsonData.getString("owner", "");
			string provider = jsonData.getString("provider", "");
			string configClass = jsonData.getString("type", "");
			JSON props = jsonData.getJSON("props");
			return new DataObjectConfig(keyName, configClass, owner, provider, null, props);
		}

		private static BaseConfig parseProviderConfigItem(JSON jsonData,  string featureName)
		{
			string keyName = jsonData.getString("keyname", "");
			string configClass = jsonData.getString("type", "");
			JSON props = jsonData.getJSON("props");
			Dictionary<string, object> propsDict = props == null ? null : props.jsonDict;
			return new ProviderConfig(keyName, configClass, propsDict, props);
		}

		public static void registerForSyncCompleteDelegate(GenericDelegate function)
		{
			onSyncComplete -= function;
			onSyncComplete += function;
		}

		public static void unregisterForSyncCompleteDelegate(GenericDelegate function)
		{
			onSyncComplete -= function;
		}

	}
}
