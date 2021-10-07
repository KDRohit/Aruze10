using Com.Initialization;

namespace Com.HitItRich.Feature.OOCRebound
{
	public class FeatureDependencyOOCRebound : FeatureDependency
	{
		public override void init()
		{
			base.init();
			OOCReboundFeature.instantiateFeature(Data.login.getJSON(OOCReboundFeature.LOGIN_DATA_KEY));
		}

		/// <inheritdoc/>
		public override bool isSkipped
		{
			get { return Data.hasLoginData && Data.login.getJSON(OOCReboundFeature.LOGIN_DATA_KEY) == null; }
		}

		public override bool canInitialize
		{
			get
			{
				return base.canInitialize &&
				       Data.hasPlayerData &&
				       Data.login.getJSON(OOCReboundFeature.LOGIN_DATA_KEY) != null &&
				       ExperimentWrapper.SpecialOutOfCoins.experimentData.isInExperiment;
			}
		}
	}
    
}
