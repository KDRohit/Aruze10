using Com.Initialization;

namespace PrizePop
{
	public class FeatureDependencyPrizePop : FeatureDependency
	{
		/// <inheritdoc/>
		public override void init()
		{
			base.init();
			PrizePopFeature.instantiateFeature(Data.login.getJSON(PrizePopFeature.LoginDataKey));
		}

		/// <inheritdoc/>
		public override bool isSkipped
		{
			get { return Data.hasLoginData && Data.login.getJSON(PrizePopFeature.LoginDataKey) == null; }
		}

		public override bool canInitialize
		{
			get
			{
				return base.canInitialize &&
				       Data.hasPlayerData &&
				       Data.login.getJSON(PrizePopFeature.LoginDataKey) != null &&
				       ExperimentWrapper.PrizePop.isInExperiment;
			}
		}
	}
}

