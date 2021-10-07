using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
    public class UpdateActiveFeaturesToDisplayComponent : BaseComponent
    {
        public UpdateActiveFeaturesToDisplayComponent(string keyName, JSON json) : base(keyName, json)
        {
        }
        public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
        {
            Dictionary<string, object> result = base.perform(payload, shouldLog);
            
            //Experiment names dont have the hir_ prefix on client.
            string featureName = jsonData.getString(FEATURE_NAME, "").Substring(ExperimentWrapper.HIR_EXPERIMENTS_PREFIX.Length);;
            bool show = jsonData.getBool("show", false);
            if(show && !Orchestrator.activeFeaturesToDisplay.Contains(featureName))
            {
                Orchestrator.activeFeaturesToDisplay.Add(featureName);
            }
            else if(!show && Orchestrator.activeFeaturesToDisplay.Contains(featureName))
            {
                Orchestrator.activeFeaturesToDisplay.Remove(featureName);
            }

            return result;
        }
        
        public static ProvidableObject createInstance(string keyname, JSON json)
        {
            return new UpdateActiveFeaturesToDisplayComponent(keyname, json);
        }
    }
}
