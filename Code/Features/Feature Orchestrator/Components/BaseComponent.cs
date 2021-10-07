using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class BaseComponent : ProvidableObject
	{
		protected bool shouldLog = false;
		protected Dictionary<string, object> payload;

		public BaseComponent(string keyName, JSON json) : base(keyName, json)
		{
		}
		
		public void onClick(string clickEvent)
		{
			Dictionary<string, object> payloadData = new Dictionary<string, object>();
			payloadData.Add(clickEvent, payload);
			FeatureConfig config = Orchestrator.instance.allFeatureConfigs[featureName];
			Orchestrator.instance.completePerform(config, payloadData, this, shouldLog);
		}
		
		public virtual Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			this.shouldLog = shouldLog;
			this.payload = payload;
			if (shouldLog)
			{
				Dictionary<string, string> extraFields = new Dictionary<string, string>();
				extraFields.Add("componentName", keyName);
				SplunkEventManager.createSplunkEvent("Proton", "perform-component", extraFields);
			}
			
			return payload;
		}
		
		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new BaseComponent(keyname, json);
		}
	}
}
