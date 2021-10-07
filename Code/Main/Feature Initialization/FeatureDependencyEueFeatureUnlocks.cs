using Com.Initialization;
using UnityEngine;


public class FeatureDependencyEueFeatureUnlocks : FeatureDependency
{
	/// <inheritdoc/>
	public override void init()
	{
		base.init();
		EueFeatureUnlocks.instantiateFeature(Data.login);
	}

	/// <inheritdoc/>
	public override bool isSkipped
	{
		get { return Data.hasLoginData && !Data.login.hasKey("eue_unlock_data"); }
	}

	public override bool canInitialize
	{
		get
		{
			return base.canInitialize &&
			       Data.hasPlayerData &&
			       Data.login.hasKey("eue_unlock_data") &&
			       ExperimentWrapper.EUEFeatureUnlocks.isInExperiment;
		}
	}
}

