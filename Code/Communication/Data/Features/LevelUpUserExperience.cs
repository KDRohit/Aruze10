using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class LevelUpUserExperienceFeature : FeatureBase
{
	// Data comes in from login data, after that we'll probably have to cache it somehow.
	public long nextLevelMaxBetAmount = 0L;
	
	public static LevelUpUserExperienceFeature instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<LevelUpUserExperienceFeature>("level_up_user_experience");
		}
	}
	
#region FEATURE_BASE_OVERRIDES
	protected override void initializeWithData(JSON data)
	{
		// Read data from login data. Store special levels or whatever
		nextLevelMaxBetAmount = data.getLong("max_bet", 0);
	}
	
	public override bool isEnabled
	{
		get
		{
			// Add other conditions as needed
			return ExperimentWrapper.RepriceLevelUpSequence.isInExperiment && ExperimentWrapper.RepriceLevelUpSequence.isToasterEnabled;
		}
	}
	
#endregion
}
