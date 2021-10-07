using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class PerformComponentAction : ServerAction
	{
		private string featureName = "";
		private string componentKeyName = "";
		private bool shouldLog = false;
		private Dictionary<string, object> payload;
		
		private PerformComponentAction(ActionPriority priority, string type) : base(priority, type)
		{
			
		}

		public override void appendSpecificJSON(System.Text.StringBuilder builder)
		{
			appendPropertyJSON(builder, "featurename", featureName);
			appendPropertyJSON(builder, "component_keyname", componentKeyName);
			appendPropertyJSON(builder, "payload_data", payload);
			appendPropertyJSON(builder, "sample_flow", shouldLog);
		}

		public static void performComponentAction(string featureName, string componentKeyName, bool shouldLog, Dictionary<string, object> payload = null)
		{
			PerformComponentAction action = new PerformComponentAction(ActionPriority.HIGH, "perform_component");
			action.featureName = featureName;
			action.componentKeyName = componentKeyName;
			action.shouldLog = shouldLog;
			if (payload != null)
			{
				replacePayloadObjects(payload);
			}

			action.payload = payload;
		}

		private static void replacePayloadObjects(Dictionary<string, object> payload)
		{
			//Need to replace any dataObjects with the object name from the config and let the server handle grabbing the object itself
			List<KeyValuePair<string, string>> keysToReplace = new List<KeyValuePair<string, string>>();
			foreach (KeyValuePair<string, object> kvp in payload)
			{
				if (kvp.Value is ProvidableObject protonObject)
				{
					keysToReplace.Add(new KeyValuePair<string, string>(kvp.Key, "{" + protonObject.keyName + "}"));
				}
			}

			foreach (KeyValuePair<string, string> kvp in keysToReplace)
			{
				payload[kvp.Key] = kvp.Value;
			}
		}
	}

} 

