using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchToEarnExperiment : EosExperiment
{

	public int maxViews { get; private set; }
	public int maxAccepts { get; private set; }
	public bool shouldShowDailyBonusCollect { get; private set; }
	public bool shouldShowDailyBonusLobbyButton { get; private set; }
	public bool shouldShowOutOfCredits { get; private set; }
	public bool useUnityAds { get; private set; }
	public WatchToEarnExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		maxViews = getEosVarWithDefault(data, "max_views", 0);
		maxAccepts = getEosVarWithDefault(data, "max_accepts", 0);
		shouldShowDailyBonusCollect = getEosVarWithDefault(data, "surface_point_daily_bonus_collect", false);
		shouldShowDailyBonusLobbyButton = getEosVarWithDefault(data, "surface_point_daily_bonus_lobby", false);
		shouldShowOutOfCredits = getEosVarWithDefault(data, "surface_point_ooc", false);
		useUnityAds = getEosVarWithDefault(data, "use_unity_ads", false);
	}

	public override void reset()
	{
		base.reset();
		maxViews = 0;
		maxAccepts = 0;
		shouldShowDailyBonusCollect = false;
		shouldShowDailyBonusLobbyButton = false;
		shouldShowOutOfCredits = false;
		useUnityAds = false;
	}
}
