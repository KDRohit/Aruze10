using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class ShowSlotUIPrefab : BaseComponent
	{
		protected Dictionary<string, object> payload;

		private const string PREFAB_PATH_KEY = "prefabPath";
		private const string POSITION_KEY = "position";
		private const string TIMER_KEY = "timePeriod"; //Expected for duration based features. Prevents us from showing UI when loading a slot if the feature ended mid-session

		public InGameFeatureContainer.ScreenPosition location { get; private set; }
		public string prefabPath { get; private set; }
		public string featureName { get; private set; }
		public bool isActive { get; private set; }

		public ShowSlotUIPrefab(string keyName, JSON json) : base(keyName, json)
		{
		}
		
		public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			Dictionary<string, object> result = base.perform(payload, shouldLog);
			
			if (this.payload == null)
			{
				this.payload = new Dictionary<string, object>();
			}
			
			this.payload["gameKey"] = GameState.game != null ? GameState.game.keyName : "";

			location = (InGameFeatureContainer.ScreenPosition) jsonData.getInt(POSITION_KEY, 0);
			prefabPath = jsonData.getString(PREFAB_PATH_KEY, "");
			featureName = jsonData.getString(FEATURE_NAME, "");

			isActive = true;

			if (jsonData.jsonDict.TryGetValue(TIMER_KEY, out object timerObj))
			{
				if (timerObj is TimePeriod timer)
				{
					isActive = timer.durationTimer.isActive;
				}
			}
			
			return result;
		}
		
		public void onClick()
		{
			Dictionary<string, object> payloadData = new Dictionary<string, object>();
			payloadData.Add("onClick", payload);
			FeatureConfig config = Orchestrator.instance.allFeatureConfigs[featureName];
			Orchestrator.instance.completePerform(config, payloadData, this, shouldLog);
		}

		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new ShowSlotUIPrefab(keyname, json);
		}
	}
}