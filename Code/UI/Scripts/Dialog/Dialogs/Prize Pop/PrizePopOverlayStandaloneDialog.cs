using Com.Scheduler;
using UnityEngine;
using PrizePop;

public class PrizePopOverlayStandaloneDialog : DialogBase
{
    public override void init()
    {
        PrizePopFeature.PrizePopOverlayType type = (PrizePopFeature.PrizePopOverlayType)dialogArgs.getWithDefault(D.TYPE, PrizePopFeature.PrizePopOverlayType.KEEP_SPINNING);
        switch (type)
        {
            case PrizePopFeature.PrizePopOverlayType.KEEP_SPINNING:
                PrizePopDialogOverlay.loadKeepSpinningOverlay(this, overlayLoadSuccess, overlayLoadFailed);
                break;
            case PrizePopFeature.PrizePopOverlayType.BUY_EXTRA_PICKS:
                PrizePopDialogOverlay.loadBuyExtraPicksRewardOverlay(this, overlayLoadSuccess, overlayLoadFailed, Dict.create(D.TYPE, "in_slot"));
                break;
            case PrizePopFeature.PrizePopOverlayType.EVENT_ENDED:
                PrizePopDialogOverlay.loadEventEndedOverlay(this, overlayLoadSuccess, overlayLoadFailed);
                break;
        }
    }
    
    private void overlayLoadSuccess(string path, Object obj, Dict args)
    {
        GameObject overlayObject = NGUITools.AddChild(sizer, obj as GameObject);
        PrizePopDialogOverlay overlay = overlayObject.GetComponent<PrizePopDialogOverlay>();
        overlay.init(null, this, args);
    }

    private void overlayLoadFailed(string path, Dict args)
    {
        Debug.LogWarning("Overlay load failed");
        Dialog.close(this);
    }

    public override void close()
    {
    }

    public static void showDialog(PrizePopFeature.PrizePopOverlayType type)
    {
        Scheduler.addDialog("prize_pop_overlay", Dict.create(D.TYPE, type));
    }
}
