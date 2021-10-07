using Com.Rewardables;
using PrizePop;

public class PrizePopDialogOverlayOutOfPicks : PrizePopDialogOverlay
{
    private const string OUT_OF_PICKS_AUDIO_KEY = "OutOfPicksPrizePopCommon";

    public override void init(Rewardable reward, DialogBase parent, Dict overlayArgs)
    {
        base.init(reward, parent, overlayArgs);
        Audio.play(OUT_OF_PICKS_AUDIO_KEY);
    }

    protected override void closeClicked(Dict args = null)
    {
        closeOverlay();
    }

    protected override void ctaClicked(Dict args = null)
    {
        closeOverlay();
    }

    private void closeOverlay()
    {
        StatsPrizePop.logOverlayClose(overlayType);
        if (PrizePopFeature.instance.isEndingSoon())
        {
            Dialog.close(); //Just close immediately since we don't allow purchases near the feature ending
        }
        else
        {
            Destroy(gameObject);
            PrizePopDialog.instance.showBuyMorePicksOverlay(true, "out_of_pops");  
        }
    }
}
