using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class ProvidableObjectConfig : BaseConfig
	{
		public string providerKeyname { get; private set; }

		public ProvidableObjectConfig(string keyName, string className, Dictionary<string, object> properties, JSON json,
			string providerKeyname) : base(keyName, className, properties, json)
		{
			this.providerKeyname = providerKeyname;
		}
	}
}
