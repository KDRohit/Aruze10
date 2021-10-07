using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	//The Orchestrator is the entry point into the feature orchestration system
	//This class is responsible for getting the feature configs from the ConfigManager class
	//And also register for the perform component and data updated events from the server 
	public class Orchestrator : IResetGame
	{
		private static Orchestrator _instance = null;
		public const string ORCHESTRATOR_FEATURES_LIVEDATA_KEY = "PROTON_FEATURES";

		public Dictionary<string, FeatureConfig> allFeatureConfigs { get; private set; }
		private JSON featureNames;

		//List of features to display. This can be used by other systems outside the orchestrator,
		//For eg. Buy page to display certain feature specific UI
		public static List<string> activeFeaturesToDisplay = new List<string>();

		private Orchestrator()
		{
		}

		public static Orchestrator instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Orchestrator();
				}

				return _instance;
			}
		}

		public void initialize()
		{
			featureNames = Data.liveData.getJSON(ORCHESTRATOR_FEATURES_LIVEDATA_KEY);
			allFeatureConfigs = new Dictionary<string, FeatureConfig>();
			ConfigManager.setup(featureNames);
			ProvidableObject.buildMapping();
			
			Server.registerEventDelegate("perform_component", onPerformComponentEvent, true);
			Server.registerEventDelegate("data_updated", onDataUpdatedEvent, true);
		}

		public BaseComponent performStep(FeatureConfig featureConfig, Dictionary<string, object> payload, string componentKeyName, bool shouldLog = false)
		{
			ComponentConfig config = featureConfig.getComponentConfigForKey(componentKeyName);
			IProvider p = featureConfig.getComponentProvider();
			BaseComponent component = p.provide(featureConfig, config) as BaseComponent;
			if (component != null)
			{
				Dictionary<string, object> outs = component.perform(payload, shouldLog);
				completePerform(featureConfig, outs, component, shouldLog);
				return component;
			}

			return null;
		}

		public void completePerform(FeatureConfig featureConfig, Dictionary<string, object> outs, BaseComponent component, bool shouldLog = false)
		{
			if (featureConfig == null)
			{
				return;
			}
			
			if (outs != null)
			{
				foreach (KeyValuePair<string, object> outPayload in outs)
				{
					string outString = outPayload.Key;
					string performerKeyname = component.keyName;
					string[] targetKeynames = featureConfig.getOutputsForComponent(performerKeyname, outString);
					if (targetKeynames != null)
					{
						for (int i = 0; i < targetKeynames.Length; i++)
						{
							performStep(featureConfig, outPayload.Value as Dictionary<string, object>,
								targetKeynames[i], shouldLog);
						}
					}
				}
			}
		}

		public void updateFeatureConfigs(string featureName, FeatureConfig config)
		{
			if (allFeatureConfigs == null)
			{
				allFeatureConfigs = new Dictionary<string, FeatureConfig>();
			}

			allFeatureConfigs[featureName] = config;
		}

		private void onPerformComponentEvent(JSON data)
		{
			if (data != null)
			{
				string featureName = data.getString("featurename", "");
				
				if (instance.allFeatureConfigs.TryGetValue(featureName, out FeatureConfig featureConfig))
				{
					string componentName = data.getString("component_keyname", "");
					JSON payloadData = data.getJSON("payload_data");
					bool shouldLog = data.getBool("sample_flow", false);
					performStep(featureConfig, payloadData != null ? payloadData.jsonDict : null, componentName, shouldLog);
				}
			}
		}

		public void onDataUpdatedEvent(JSON data)
		{
			if (data != null)
			{
				string featureName = data.getString("featurename", "");
				
				if (instance.allFeatureConfigs.TryGetValue(featureName, out FeatureConfig featureConfig))
				{
					string dataObjectKey = data.getString("keyname", "");
					JSON payloadData = data.getJSON("data");
					if (payloadData != null)
					{
						ProvidableObjectConfig config = featureConfig.getDataObjectConfigForKey(dataObjectKey);
						if (config != null)
						{
							BaseDataObject dataObject = featureConfig.getServerDataProvider().provide(featureConfig, config, payloadData) as BaseDataObject;
							if (dataObject != null)
							{
								dataObject.updateValue(payloadData);
							}
						}
					}
				}
			}
		}

		public List<BaseComponent> performTrigger(string triggerKey)
		{
			List<BaseComponent> ranComponents = new List<BaseComponent>();
			foreach (FeatureConfig config in allFeatureConfigs.Values)
			{
				if (config.componentConfigs.TryGetValue(triggerKey, out ComponentConfig slotLoadConfig))
				{
					BaseComponent component = performStep(config, slotLoadConfig.properties, triggerKey);
					if (component != null)
					{
						ranComponents.Add(component);
					}
				}
			}
			return ranComponents;
		}
	}
}
