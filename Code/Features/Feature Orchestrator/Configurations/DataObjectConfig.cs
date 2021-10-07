using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class DataObjectConfig : ProvidableObjectConfig
	{
		public string owner { get; private set; }
		
		public DataObjectConfig(string keyName, string className, string owner, string providerKeyname,
			Dictionary<string, object> properties, JSON json) :
			base(keyName, className, properties, json, providerKeyname)
		{
			this.owner = owner;
		}
				
	}
}
