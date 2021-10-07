using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class ProviderConfig : BaseConfig
	{
		public ProviderConfig(string keyName, string className, Dictionary<string, object> properties, JSON json) : base(keyName,
			className, properties, json)
		{
			
		}
	}
}
