using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public abstract class BaseDataProvider : IProvider
	{
		protected ProvidableObjectConfig configToProvide = null;
		protected Dictionary<string, ProvidableObject> providedObjects = new Dictionary<string, ProvidableObject>();

		//The provide method is responsible for parsing the config, creating and caching an instance of a ProvidableObject
		public abstract ProvidableObject provide(FeatureConfig featureConfig, ProvidableObjectConfig providableObjectConfig, JSON json = null, bool logError = true);

		protected JSON setupPropertiesForProvider(ProvidableObjectConfig providableObjectConfig, JSON json)
		{
			configToProvide = providableObjectConfig;
            
			if (!(configToProvide is ProvidableObjectConfig))
			{
				Debug.LogError("Provider config not setup");
			}

			if (json != null)
			{
				return json;
			}
			
			return providableObjectConfig.json;
		}

		protected T getInstance<T>(JSON json) where T: ProvidableObject
		{
			return ProvidableObject.createInstance(configToProvide.keyName, configToProvide.className, json) as T;
		}
	}
}
