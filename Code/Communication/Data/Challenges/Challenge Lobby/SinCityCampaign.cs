using UnityEngine;
using System.Collections;

public class SinCityCampaign : ChallengeLobbyCampaign
{
	public const string BUNDLE_NAME = "sin_city_strip";

	public SinCityCampaign() : base()
	{
		isForceDisabled = AssetBundleManager.shouldLazyLoadBundle(BUNDLE_NAME);
	}
}