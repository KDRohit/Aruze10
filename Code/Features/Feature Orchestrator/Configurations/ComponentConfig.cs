using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class ComponentConfig : ProvidableObjectConfig
	{
		public string owner { get; private set; }

		public JSON outs { get; private set; }
		public bool dev { get; private set; }

		public ComponentConfig(string keyName, string className, string owner, string providerKeyname, JSON outs, Dictionary<string, object> properties, JSON json, bool showInDevPanel) :
			base(keyName, className, properties, json, providerKeyname)
		{
			this.owner = owner;
			this.outs = outs;
			dev = showInDevPanel;
		}
	}
}
