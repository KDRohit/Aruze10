using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashSaleDealButton : MonoBehaviour
{

    public static FlashSaleDealButton instance;
    [SerializeField] private GameObject tooltipParent;
    public const string REMINDER_TOOLTIP_PREFAB_PATH = "Features/Gift Chest Offer/Prefabs/Gift Chest Offer Tooltip";

    void Start()
    {
        instance = this;
    }

    public void showToolTip()
    {
        AssetBundleManager.load(PostPurchaseChallengeCampaign.REMINDER_TOOLTIP_PREFAB_PATH, assetLoadSuccess, assetLoadFailed);
    }

    private void assetLoadSuccess(string assetPath, Object obj, Dict data = null)
    {
        GameObject tooltip = NGUITools.AddChild(tooltipParent, obj as GameObject, true);
        if (tooltip != null)
        {
            GiftChestOfferTooltip tooltipComponent = tooltip.GetComponent<GiftChestOfferTooltip>();
            if (tooltipComponent != null)
            {
                tooltipComponent.setLabel("Ending Soon!");
            }
        }
    }
    private void assetLoadFailed(string assetPath, Dict data = null)
    {
        Bugsnag.LeaveBreadcrumb("Flash Sale tooltip Asset failed to load: " + assetPath);
#if UNITY_EDITOR
        Debug.LogWarning("Flash Sale tooltip Asset failed to load: " + assetPath);
#endif
    }

}
