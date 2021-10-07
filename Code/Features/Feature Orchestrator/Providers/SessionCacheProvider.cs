using System.Collections.Generic;
using UnityEngine;
using System;

namespace FeatureOrchestrator
{
    public class SessionCacheProvider : BaseDataProvider
    {
        public override ProvidableObject provide(FeatureConfig featureConfig, ProvidableObjectConfig providableObjectConfig, JSON json = null, bool logError = true)
        {
            JSON result = setupPropertiesForProvider(providableObjectConfig, json);
            ExpressionParser parser = new ExpressionParser(featureConfig, providableObjectConfig);
            ProvidableObject instance = null;
            if (!providedObjects.ContainsKey(configToProvide.keyName))
            {
                instance = getInstance<ProvidableObject>(result);
                if (instance == null)
                {
                    if (logError)
                    {
                        Debug.LogError("SessionCacheProvider could not create an instance of " + configToProvide.keyName);
                    }

                    return null;
                }
                providedObjects.Add(configToProvide.keyName, instance);

            }

            //No need to parse ServerComponents
            if (instance != null && !(instance is ServerComponent))
            {
                parser.parse(providedObjects[configToProvide.keyName]);    
            }
            
            return providedObjects[configToProvide.keyName];
        }
    }
}
