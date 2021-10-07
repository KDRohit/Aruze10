using PrizePop;

public class DoSomethingPrizePop : DoSomethingAction
{
    // Start is called before the first frame update
    public override bool getIsValidToSurface(string parameter)
    {
        switch (parameter)
        {
            case "video":
                if (string.IsNullOrEmpty(ExperimentWrapper.PrizePop.videoSummaryPath))
                {
                    return false;
                }

                break;
        }

        return PrizePopFeature.instance != null && PrizePopFeature.instance.isEnabled;
    }

    public override void doAction(string parameter)
    {
        switch (parameter)
        {
            case "video":
                PrizePopFeature.instance.showVideo(false);
                break;
        }
    }
}
