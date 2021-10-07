using System.Collections.Generic;
using UnityEngine;
using System;

namespace FeatureOrchestrator
{
	public class ServerDataProvider : BaseDataProvider
	{
		public override ProvidableObject provide(FeatureConfig featureConfig, ProvidableObjectConfig providableObjectConfig, JSON json = null, bool logError = true)
		{
			JSON result = setupPropertiesForProvider(providableObjectConfig, json);

			if (!providedObjects.ContainsKey(configToProvide.keyName))
			{
				BaseDataObject instance = getInstance<BaseDataObject>(result);
				//The Data Object class does not exist on the client
				if (instance == null)
				{
					if (logError)
					{
						Debug.LogError("ServerDataProvider could not create an instance of " + configToProvide.keyName);
					}

					return null;
				}
					
				providedObjects.Add(providableObjectConfig.keyName, instance);
			}

			return providedObjects[providableObjectConfig.keyName];
		}
	}
}
