using FeatureOrchestrator;
using UnityEngine;

public class InGameProtonFeatureDisplay : InGameFeatureDisplay
{
    [SerializeField] protected ButtonHandler button;
    protected ShowSlotUIPrefab parentComponent;

    public override void init(Dict args = null)
    {
        parentComponent = args.getWithDefault(D.DATA, null) as ShowSlotUIPrefab;

        if (button != null)
        {
            button.registerEventDelegate(onButtonClicked, args);    
        }
    }

    protected virtual void onButtonClicked(Dict args = null)
    {
        if (parentComponent != null)
        {
            parentComponent.onClick();
        }
    }
}
