using FeatureOrchestrator;

/*
Override for special behavior.
*/

public class MOTDDialogDataBoardGameVideo : MOTDDialogData
{
	private const string FEATURE_KEY = "hir_boardgame";
	private const string VIDEO_COMPONENT_KEY = "showVideoDialog";
	private const string FEATURE_DATA_OBJECT_CONFIG_KEY = "availablePicks";
	
	private int currentVersion = -1;
	private int lastSeenVersion = -1;
	public override bool shouldShow
	{
		get
		{
			if (Orchestrator.instance != null)
			{
				if (Orchestrator.instance.allFeatureConfigs.TryGetValue(FEATURE_KEY, out FeatureConfig config))
				{
					lastSeenVersion = CustomPlayerData.getInt(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SEEN_VERSION, 0);
					currentVersion = ExperimentWrapper.BoardGame.experimentData != null? ExperimentWrapper.BoardGame.experimentData.startTime : -1;
					if (currentVersion == lastSeenVersion || currentVersion == -1)
					{
						return false; //Don't show the video if we've already seen it for this run of the event or for some reason startTime isn't in EOS
					}
							
					return config.componentConfigs.ContainsKey(VIDEO_COMPONENT_KEY); //After verify the time, make sure theres the component to show the video
				}
			}
			return false;
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (Orchestrator.instance == null)
			{
				reason += "Orchestrator isn't active\n";
				return reason;
			}
			
			if (Orchestrator.instance.allFeatureConfigs.TryGetValue(FEATURE_KEY, out FeatureConfig config))
			{
				if (!config.componentConfigs.ContainsKey(VIDEO_COMPONENT_KEY))
				{
					reason += "Boardgame config doesn't have a show video component\n";
				}
				
				if (currentVersion == lastSeenVersion)
				{
					reason += "Already saw the video for this event run: " + currentVersion + "\n";
				}
			}
			else
			{
				reason += "Boardgame feature isn't active\n";
			}
			return reason;
		}
	}

	/// <summary>
	/// Returns false when a board game is in progress. This is useful when video motd shows up midway during a board game and we do not want to clear out the token selection.
	/// </summary>
	private bool shouldClearSavedToken
	{
		get
		{
			if (Orchestrator.instance != null)
			{
				if (Orchestrator.instance.allFeatureConfigs.TryGetValue(FEATURE_KEY, out FeatureConfig config))
				{
					ProvidableObjectConfig dataConfig = config.getDataObjectConfigForKey(FEATURE_DATA_OBJECT_CONFIG_KEY);
					if (dataConfig != null)
					{
						if (config.getServerDataProvider().provide(config, dataConfig) is PickByPickClaimableBonusGameOutcome dataObject)
						{
							if (dataObject.currentIndex > 0)
							{
								return false;
							}
						}
					}
				}
			}
			return true;
		}
	}
	public override bool show()
	{
		FeatureConfig config = Orchestrator.instance.allFeatureConfigs[FEATURE_KEY];
		Orchestrator.instance.performStep(config, null, VIDEO_COMPONENT_KEY, true);
		CustomPlayerData.setValue(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SEEN_VERSION, currentVersion);
		if (shouldClearSavedToken)
		{
			CustomPlayerData.setValue(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SELECTED_TOKEN, -1);
		}
		return true;
	}

	new public static void resetStaticClassData()
	{
	}
}

