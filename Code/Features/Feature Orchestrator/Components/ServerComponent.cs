using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
    public class ServerComponent : BaseComponent
    {
        public ServerComponent(string keyName, JSON json) : base(keyName, json)
        {
        }
        public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
        {
            Dictionary<string, object> result = base.perform(payload, shouldLog);

            string featureName = jsonData.getString(FEATURE_NAME, "");
            if (!string.IsNullOrEmpty(featureName))
            {
                PerformComponentAction.performComponentAction(featureName, keyName, shouldLog, payload);
            }
            
            return result;
        }
        
        public static ProvidableObject createInstance(string keyname, JSON json)
        {
            return new ServerComponent(keyname, json);
        }
    }
}
