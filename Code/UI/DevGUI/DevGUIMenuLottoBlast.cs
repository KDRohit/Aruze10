using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FeatureOrchestrator;

public class DevGUIMenuLottoBlast : DevGUIMenu
{
    public override void drawGuts()
    {
        if (Orchestrator.instance == null)
        {
            return;
        }
        GUILayout.BeginHorizontal();
		
        GUILayout.Label("Orchestrator: ");
		
        if (GUILayout.Button("Trigger Progress Bar"))
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();
            payload.Add("featureName", "hir_lotto_blast");
            payload.Add("show", "true");
            performOrchestratorDebugStep("OnTestTrigger", payload);
        }
		
        if (GUILayout.Button("Show Mini Game"))
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();
            payload.Add("featureName", "hir_lotto_blast");
            payload.Add("show", "false");
            performOrchestratorDebugStep("OnTestTrigger2", payload);
        }

        GUILayout.EndHorizontal();

        FeatureConfig featureConfig = null;
        if (Orchestrator.instance.allFeatureConfigs.TryGetValue("hir_lotto_blast", out featureConfig))
        {
            string xpProgressKey = "xpProgress";
            ProvidableObjectConfig config = featureConfig.getDataObjectConfigForKey(xpProgressKey);
            if (config != null)
            {
                XPProgressCounter progress = featureConfig.getServerDataProvider().provide(featureConfig, config) as XPProgressCounter;
                if (progress != null && progress.levelData != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Starting Value: " + CommonText.formatNumber(progress.startingValue));
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Current Value: " + CommonText.formatNumber(SlotsPlayer.instance.xp.amount));
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Target Value: " + CommonText.formatNumber(progress.completeValue));
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Complete Level: " + progress.completeLevel);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Levels Left: " + progress.levelsLeftToTarget);
                    GUILayout.EndHorizontal();
                
                    GUILayout.Label("Level Data: ");
                    foreach (KeyValuePair<int, long> levelData in progress.levelData)
                    {
                        GUILayout.Label(levelData.Key +" : "+ levelData.Value);
                    }
                }
            }
        }
    }
    
    private void performOrchestratorDebugStep(string componentKey, Dictionary<string, object> payload)
    {
        FeatureConfig fConfig = Orchestrator.instance.allFeatureConfigs["hir_lotto_blast"];
        ComponentConfig triggerComponentConfig = fConfig.getComponentConfigForKey(componentKey);
        if (triggerComponentConfig != null)
        {
            Orchestrator.instance.performStep(fConfig, payload, triggerComponentConfig.keyName);
        }  
    }
}
