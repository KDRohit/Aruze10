using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

public class LOZCampaign : ChallengeLobbyCampaign, IResetGame
{	
	// =============================
	// PUBLIC
	// =============================
	public static Dictionary<string, long> tierCredits = null;

	// =============================
	// CONST
	// =============================
	public const int NUM_TIERS = 3;
	public const string BUNDLE_NAME = "feature_land_of_oz";

	public LOZCampaign() : base()
	{
		isForceDisabled = AssetBundleManager.shouldLazyLoadBundle("land_of_oz");
	}

    public override void init(JSON data)
	{
		base.init(data);
		
		unlockLevel = ExperimentWrapper.LOZChallenges.levelLock;
		
		// adding experiment check, JUST in case server data is bad
		isEnabled = ExperimentWrapper.LOZChallenges.isInExperiment;

		motdPrefix = "loz_challenges";

		tierCredits = new Dictionary<string, long>();
		if (missions != null)
		{
			if (missions.Count > 2)
			{
				if (missions[missions.Count - 3].rewards != null && missions[missions.Count-3].rewards.Count > 0)
				{
					tierCredits.Add("tier1", missions[ missions.Count - 3 ].rewards[ 0 ].amount);	
				}
			}

			if (missions.Count > 1)
			{
				if (missions[missions.Count - 2].rewards != null && missions[missions.Count - 2].rewards.Count > 0)
				{
					tierCredits.Add("tier2", missions[missions.Count - 2].rewards[0].amount);
				}
			}

			if (missions.Count > 0)
			{
				if (missions[missions.Count - 1].rewards != null && missions[missions.Count - 1].rewards.Count > 0)
				{
					tierCredits.Add("tier3", missions[missions.Count - 1].rewards[0].amount);
				}
			}
		}
		
	}

	public override void onProgressUpdate(JSON response)
	{
		base.onProgressUpdate( response );
		
		LobbyOptionButtonChallengeLobby portal = LobbyOptionButtonChallengeLobby.findByCampaign(this.campaignID);
		if (portal != null)
		{
			portal.refresh();
		}
	}

	protected override void showChallengesDialog(ChallengeLobbyCampaign campaign, Mission mission, JSON data, bool didCompleteAll)
	{
		int completedEventIndex = data.getInt("event_index", 0) - 1;
		bool didCompleteTier = completedEventIndex >= missions.Count - NUM_TIERS;
		didCompleteAll = didCompleteAll || didCompleteTier;
		LOZObjectivesDialog.showDialog(campaign, mission, data, true, didCompleteAll);
	}

	protected override void showTypeComplete(List<JSON> completionJson)
	{
		base.showTypeComplete(completionJson);
		Scheduler.addFunction(onTypeComplete);
	}

	protected void onTypeComplete(Dict args)
	{
		Audio.play("ObjectiveTickUpLOOZ");
		Scheduler.removeFunction(onTypeComplete);
	}

	/// <summary>
	///   Returns a mission that contains the specified gamekey
	/// </summary>
	public override Mission findWithGame(string gameKey)
	{
		if (currentMission != null && currentMission.containsGame(gameKey))
		{
			return currentMission;
		}

		for (int i = missions.Count - (NUM_TIERS - tier); --i >= 0; )
		{
			if (missions[i].containsGame(gameKey))
			{
				return missions[i];
			}
		}

		return null;
	}


    /*=========================================================================================
    GETTERS  
    =========================================================================================*/
    // Determines whether the portal lobby option in the main lobby should be unlocked or locked.
    public override bool isPortalUnlocked
    {
        get
        {
            return SlotsPlayer.instance.socialMember.experienceLevel >= unlockLevel;
        }
    }

	// Returns a relative current event index. if the user is above the first tier, it still tracks what their
	// currentEventIndex would have been had it been the normal 5 objective mission
	public int currentRelativeIndex
	{
		get
		{
			if (currentMission != null && currentMission.isComplete)
			{
				Debug.LogWarning("Checking currentRelativeIndex when the current mission is already complete.");
				return 0;
			}
			
			if (currentMission == null || (currentMission as LobbyMission).isFirstTier)
			{
				return currentEventIndex;
			}
			
			// After completing the first tier, all games are unlocked, so they can be played in any order.
			// So, we just need to find the first game in the lobby that doesn't have all of its
			// objectives completed, and return the lobby option index for that game.
			
			LobbyInfo lobby = LobbyInfo.find(LobbyInfo.Type.LOZ);

			if (lobby != null)
			{
				// The options in the unpinnedOptions list are already sorted, so we can iterate straight forward from 0.
				// Check each lobby option's game to see if all objectives have been completed.
				for (int i = 0; i < lobby.unpinnedOptions.Count; i++)
				{
					if (lobby.unpinnedOptions[i].game != null &&
					    !currentMission.isGameObjectivesComplete(lobby.unpinnedOptions[i].game)
					)
					{
						return i;
					}
				}
			}

			// If we got here without finding an incomplete mission, then something isn't right.
			Debug.LogWarning("Couldn't find a game with incomplete objectives in the current mission in LOZ challenge.");
			return 0;
		}
	}

	public override long currentJackpot
	{
		get
		{
			return tierCredits[ "tier" + tier.ToString() ];
		}
	}

	public int tier
	{
		get
		{
			int d = Mathf.Max(currentEventIndex, missions.Count - NUM_TIERS);
			return Mathf.Abs(d - missions.Count + NUM_TIERS + 1);
		}
	}
	
	// Cleanup
	public static void resetStaticClassData()
	{
		tierCredits = null;
	}
}