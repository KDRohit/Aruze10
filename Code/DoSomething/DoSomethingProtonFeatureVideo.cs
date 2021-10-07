using System.Collections.Generic;
using FeatureOrchestrator;
using PrizePop;
using UnityEngine;

public class DoSomethingProtonFeatureVideo : DoSomethingAction
{
    private const string VIDEO_CAROUSEL_COMPONENT_KEY = "onCarouselClick";
    private const string DIALOG_OUTS_KEY = "showVideoDialog";

    public override bool getIsValidToSurface(string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            Debug.LogWarning("Proton carousel card without a parameter is not supported. Add feature name as parameter in carousel admin page.");
            return false;
        }
        
        if (Orchestrator.instance.allFeatureConfigs.TryGetValue(parameter, out FeatureConfig config))
        {
            if (config.componentConfigs.TryGetValue(VIDEO_CAROUSEL_COMPONENT_KEY, out ComponentConfig component))
            {
                return component.outs.hasKey(DIALOG_OUTS_KEY);
            }
        }

        return false;
    }

    public override void doAction(string parameter)
    {
        FeatureConfig config = Orchestrator.instance.allFeatureConfigs[parameter];
        Dictionary<string, object> payload = new Dictionary<string, object>();
        payload[DIALOG_OUTS_KEY] = null;
        Orchestrator.instance.performStep(config, payload, VIDEO_CAROUSEL_COMPONENT_KEY, true);
    }
}
