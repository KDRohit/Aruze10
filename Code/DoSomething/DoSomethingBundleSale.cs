
using Com.HitItRich.Feature.BundleSale;

public class DoSomethingBundleSale: DoSomethingAction
{
    public override void doAction(string parameter)
    {
        if(BundleSaleFeature.instance != null)
        {
            BundleSaleFeature.instance.showDialog();
        }		
    }
    
    public override GameTimer getTimer(string parameter)
    {
        if ( BundleSaleFeature.instance != null && BundleSaleFeature.instance.isTimerVisible && BundleSaleFeature.instance.getSaleTimer() != null && BundleSaleFeature.instance.getSaleTimer().endTimer != null)
        {
            return BundleSaleFeature.instance.getSaleTimer().endTimer;
        }
        return null;
    }

    public override bool getIsValidToSurface(string parameter)
    {
        return BundleSaleFeature.instance != null && BundleSaleFeature.instance.canShow();
    }
}
