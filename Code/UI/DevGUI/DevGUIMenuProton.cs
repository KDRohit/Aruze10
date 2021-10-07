using System.Collections.Generic;
using FeatureOrchestrator;
using UnityEngine;

public class DevGUIMenuProton : DevGUIMenu
{
    private static string chosenFeature = "";
    private Dictionary<string, Dictionary<string, object>> payloadDict = new Dictionary<string, Dictionary <string, object>>();
    public override void drawGuts()
    {
        if (Orchestrator.instance == null)
        {
            return;
        }
        
        GUILayout.Label("Active Features: ");
        GUILayout.BeginHorizontal();
        foreach (KeyValuePair<string, FeatureConfig> kvp in Orchestrator.instance.allFeatureConfigs)
        {
            string experimentName = kvp.Key.Substring(ExperimentWrapper.HIR_EXPERIMENTS_PREFIX.Length);
            OrchestratorFeatureExperiment exp = ExperimentManager.GetEosExperiment(experimentName) as OrchestratorFeatureExperiment;
            string version = exp != null ? exp.version : "null";
            if (GUILayout.Button(kvp.Key + " - " + version))
            {
                chosenFeature = kvp.Key;
            }
        }
        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(chosenFeature))
        {
            FeatureConfig config = Orchestrator.instance.allFeatureConfigs[chosenFeature];
            GUILayout.BeginHorizontal();
            GUILayout.Label("Selected: " + chosenFeature);
            GUILayout.EndHorizontal();
            
            drawData(config);
            drawComponents(config);
        }
    }

    private void drawComponents(FeatureConfig config)
    {
        GUILayout.BeginHorizontal();
        int buttonCount = 0;
        GUILayout.Label("Components");
        GUILayout.EndHorizontal();

        foreach (KeyValuePair<string, ComponentConfig> kvp in config.componentConfigs)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(kvp.Key, GUILayout.Width(200)))
            {
                Dictionary<string, object> payload = payloadDict.ContainsKey(kvp.Key) ? payloadDict[kvp.Key] : null;
                Orchestrator.instance.performStep(config, payload, kvp.Key, true);
            }
                
            GUILayout.BeginVertical();
            drawPayloadInputs(kvp.Key, kvp.Value.properties);

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }
    }

    private void drawPayloadInputs(string componentKey, Dictionary<string, object> dict)
    {
        foreach (KeyValuePair<string, object> props in dict)
        {
            if (props.Value is string s && s.FastStartsWith("{payload."))
            {
                GUILayout.BeginHorizontal();
                string payloadKey = s.Substring(9, s.Length - 10);
                GUILayout.Label(payloadKey);

                if (!payloadDict.ContainsKey(componentKey))
                {
                    payloadDict[componentKey] = new Dictionary<string, object>();
                }

                if (!payloadDict[componentKey].ContainsKey(payloadKey))
                {
                    payloadDict[componentKey][payloadKey] = "";
                }

                payloadDict[componentKey][payloadKey] = GUILayout.TextField((string) payloadDict[componentKey][payloadKey]);
                GUILayout.EndHorizontal();
            }

            if (props.Value is Dictionary<string, object> dataDict)
            {
                drawPayloadInputs(componentKey, dataDict);
            }
        }
    }

    private void drawData(FeatureConfig config)
    {
        GUILayout.Label("Data Objects");
        foreach (KeyValuePair<string, DataObjectConfig> kvp in config.dataObjectConfigs)
        {
            ProvidableObject obj = config.getServerDataProvider().provide(config, kvp.Value, kvp.Value.json, false);
            if (obj != null && obj.jsonData != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(obj.keyName);
                GUILayout.TextArea(Zynga.Core.JsonUtil.Json.SerializeHumanReadable(obj.jsonData.jsonDict));
                GUILayout.EndHorizontal();
            }
        }
    }
}