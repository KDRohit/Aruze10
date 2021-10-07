using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
    public class OrchestratorFeaturesReadyAction : ServerAction
    {
        private Dictionary<string, string> featureVersions;
        private OrchestratorFeaturesReadyAction(ActionPriority priority, string type) : base(priority, type)
        {
			
        }

        public override void appendSpecificJSON(System.Text.StringBuilder builder)
        {
            appendPropertyJSON(builder, "proton_ready_list", featureVersions);
        }

        public static void setFeaturesReady(Dictionary<string, string> featureVersions)
        {
            OrchestratorFeaturesReadyAction action = new OrchestratorFeaturesReadyAction(ActionPriority.IMMEDIATE, "sync_proton_features");
            action.featureVersions = featureVersions;
        }
    }
}
