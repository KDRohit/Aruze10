using Com.Initialization;

namespace Com.HitItRich.Feature.BundleSale
{
    public class FeatureDependencyBundleSale : FeatureDependency
    {
        public override void init()
        {
            base.init();
            BundleSaleFeature.instantiateFeature(Data.login.getJSON(BundleSaleFeature.LOGIN_DATA_KEY));
        }
        
        public override bool isSkipped
        {
            get { return Data.hasLoginData && Data.login.getJSON(BundleSaleFeature.LOGIN_DATA_KEY) == null; }
        }

        public override bool canInitialize
        {
            get
            {
                return base.canInitialize &&
                       Data.hasLoginData &&
                       Data.login.getJSON(BundleSaleFeature.LOGIN_DATA_KEY) != null &&
                       ExperimentWrapper.BundleSale.isInExperiment;
            }
        }
    }
}