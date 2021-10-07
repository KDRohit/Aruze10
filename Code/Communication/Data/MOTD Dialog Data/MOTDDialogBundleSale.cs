
using Com.HitItRich.Feature.BundleSale;

public class MOTDDialogBundleSale: MOTDDialogData
{
    public override bool shouldShow
    {
        get
        {
            return BundleSaleFeature.instance != null && BundleSaleFeature.instance.canShow();
        }
    }

    public override string noShowReason
    {
        get
        {
            string reason = base.noShowReason;
            if (BundleSaleFeature.instance == null)
            {
                reason += "Player is not in feature and feature is null";
            }
            else if (BundleSaleFeature.instance != null && !BundleSaleFeature.instance.canShow())
            {
                reason += "Player is in cool down or not in bundle sale";
            }
            return reason;
        }
    }

    public override bool show()
    {
        if (BundleSaleFeature.instance != null && BundleSaleFeature.instance.canShow())
        {
            BundleSaleFeature.instance.showDialog();
            return true;
        }
        return false;
    }
}
