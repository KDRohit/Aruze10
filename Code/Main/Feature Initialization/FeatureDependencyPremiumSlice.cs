using System.Collections;
using System.Collections.Generic;
using Com.Initialization;
using UnityEngine;

public class FeatureDependencyPremiumSlice : FeatureDependency
{
	private const string featureName = "premium_slice";
	/// <inheritdoc/>
	public override void init()
	{
		base.init();
		PremiumSlice.instantiateFeature(Data.login.getJSON(featureName));
		PurchaseFeatureData.populatePremiumSlice();
	}

	/// <inheritdoc/>
	public override bool isSkipped
	{
		get { return Data.hasLoginData && Data.login.getJSON(featureName) == null; }
	}

	public override bool canInitialize
	{
		get
		{
			return base.canInitialize &&
			       Data.hasLoginData &&
			       Data.login.getJSON(featureName) != null &&
			       ExperimentWrapper.PremiumSlice.isInExperiment;
		}
	}
}
