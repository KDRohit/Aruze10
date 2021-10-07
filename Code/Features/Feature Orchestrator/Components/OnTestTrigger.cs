using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class OnTestTrigger : BaseComponent
	{
		public const string triggerName = "OnTestTrigger";

		public OnTestTrigger(string keyName, JSON json) : base(keyName, json)
		{
			keyName = triggerName;
		}

		public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			Dictionary<string, object> result = base.perform(payload, shouldLog);
			if (result == null)
			{
				result = new Dictionary<string, object>();	
			}
			
			result.Add("out", payload);
			return result;
		}
		
		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new OnTestTrigger(keyname, json);
		}
	}
}
