using UnityEngine;
using System.Collections.Generic;
using Com.Scheduler;

public class ChallengeLobbyCampaign : ChallengeCampaign
{
	// current campaign is set by respective challenge lobby classes (@see ChallengeLobby.setChallengeCampaign)
	public int unlockLevel = 0;
	public bool isPromoUnLocked = false;
	public static ChallengeLobbyCampaign currentCampaign;

	private const string UNLOCK_ENTRY = "challenge_campaign_unlock_entry";

	public override void init(JSON data)
	{
		base.init(data);

		isCampaignValid();

		isPromoUnLocked = data.getBool("entry_locked", false);
		campaignID = data.getString("experiment", "challenge_lobby_campaign");

		Server.registerEventDelegate(UNLOCK_ENTRY, onCampaignUnlocked);
	}

	protected override Mission createMission(JSON data)
	{
		return new LobbyMission(data);
	}

	/*=========================================================================================
	EVENT HANDLING
	=========================================================================================*/
	public override void onCampaignLost( JSON response )
    {
        // does nothing, there's no LOZ ended dialog
    }

	protected virtual void onCampaignUnlocked(JSON data)
	{
		string experiment = data.getString("experiment", "");

		if (experiment == campaignID)
		{
			isPromoUnLocked = true;
			LobbyOptionButtonChallengeLobby portal = LobbyOptionButtonChallengeLobby.findByCampaign(this.campaignID);
			if (portal != null)
			{
				portal.refresh();
			}
		}
	}

	public Mission lastMission
	{
		get
		{
			if (missions != null && missions.Count > 0)
			{
				return missions[missions.Count - 1];	
			}

			return null;
		}
	}

	protected override void showCampaignComplete(List<JSON> completionJSON)
	{
		base.showCampaignComplete(completionJSON);
		
		//get the largest completed event from the list and show that
		int completedEventIndex = -1;
		JSON eventData = null;
		if (completionJSON != null)
		{
			for (int i = 0; i < completionJSON.Count; i++)
			{
				int eventIndex = completionJSON[i].getInt("event_index", 1) - 1;
				if (eventIndex >= completedEventIndex)
				{
					eventData = completionJSON[i];
					completedEventIndex = eventIndex;
				}
			}	
		}
		
		if (completedEventIndex > missions.Count || completedEventIndex < 0)
		{
			Debug.LogError("completed event " + completedEventIndex + ",  out of mission range: "+ missions.Count);
			return;
		}
		
		//show the dialog for the last campaign (unlikely we'd ever get more than one, but ignore the previous ones)
		showChallengesDialog(this, missions[completedEventIndex], eventData, true);
	}
	
	protected override void showMissionComplete(List<JSON> completionJSON)
	{
		
		base.showMissionComplete(completionJSON);
		if (completionJSON != null)
		{
			for (int i = 0; i < completionJSON.Count; i++)
			{
				if (completionJSON == null)
				{
					continue;
				}
				int completedEventIndex = completionJSON[i].getInt("event_index", 1) - 1;	
				if (completedEventIndex == -1)
				{
					// On reset this can be -1 while we wait for the campaign update. Just get out
					continue;
				}
				if (completedEventIndex > missions.Count || completedEventIndex < 0)
				{
					Debug.LogError("completed event " + completedEventIndex + ",  out of mission range: " + missions.Count);
					continue;
				}
		
				showChallengesDialog(this, missions[completedEventIndex], completionJSON[i], completedEventIndex >= missions.Count - 1);
			}
		}
		
		
	}

	protected virtual void showChallengesDialog(ChallengeLobbyCampaign campaign, Mission mission, JSON data, bool didCompleteAll)
	{
		ChallengeLobbyObjectivesDialog.showDialog(campaign, mission, data, true, didCompleteAll);
	}

	// unlock the current game in the campaign, and all the previous games
	protected override void unlockChallengeGame()
	{
		for (int i = 0; i <= currentEventIndex; ++i)
		{
			if (missions[i].objectives.Count > 0)
			{
				// just grab the first objective, all objectives have a game tied to them
				Objective objective = missions[i].objectives[0];

				if (objective.game != null)
				{
					LobbyGame game = LobbyGame.find(objective.game);
					if (game != null && !game.isUnlocked)
					{
						game.xp.isPermanentUnlock = true;
						game.setIsUnlocked();
					}
				}
				else
				{
					Debug.LogError(campaignID + " : No game tied to challenge!");
				}
			}
	
		}
	}
	
	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	public override bool isCampaignValid()
	{
		if (campaignValidState != ChallengeEvalState.NOT_VALIDATED)
		{
			return campaignValidState == ChallengeEvalState.VALID;
		}
		
		foreach (Mission mission in missions)
		{
			// make sure missions have the correct number of objectives
			if (mission.objectives.Count < LobbyMission.OBJECTIVES_PER_GAME)
			{
				campaignValidState = ChallengeEvalState.INVALID;
				return false;
			}
			
			// make sure objectives all have a game tied to it
			foreach (Objective objective in mission.objectives)
			{
				if (string.IsNullOrEmpty(objective.game))
				{
					campaignValidState = ChallengeEvalState.INVALID;
					return false;
				}
			}
		}

		campaignValidState = ChallengeEvalState.VALID;
		
		return campaignValidState == ChallengeEvalState.VALID;
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	public override bool isActive
	{
		get
		{
			return isEnabled &&
			timerRange != null &&
			timerRange.isActive &&
			state != INCOMPLETE && // one key difference from normal campaigns, you can play the games even after the campaign has finished
			lobbyValidState == ChallengeEvalState.VALID &&
			campaignValidState == ChallengeEvalState.VALID; // second key difference is validating the campaign is setup correctly
		}
	}

	public bool isLevelLocked
	{
		get
		{
			return SlotsPlayer.instance.socialMember.experienceLevel < unlockLevel;
		}
	}
	
	public virtual long currentJackpot
	{
		get
		{
			Mission jackpotMission = lastMission;
			if (jackpotMission != null)
			{
				return jackpotMission.rewards[0].amount;	
			}
			return 0;
		}
	}

	public virtual long nextJackpot
	{
		get
		{
			long baseAmount = currentJackpot;
			return baseAmount + System.Convert.ToInt64(baseAmount * (currentCampaign == null ? 0 : currentCampaign.replayRewardRatio));
		}
	}
	
	public string currentJackpotRewardPack
	{
		get
		{
			Mission jackpotMission = lastMission;
			for (int i = 0; i < jackpotMission.rewards.Count; i++)
			{
				if (jackpotMission.rewards[i].type == MissionReward.RewardType.SLOTVENTURE_CARD_PACK)
				{
					return jackpotMission.rewards[i].cardPackKeyName;
				}
			}

			return string.Empty;
		}
	}

	public virtual bool isPortalUnlocked
	{
		get
		{
			return isPromoUnLocked;
		}
	}

	public override string notActiveReason
	{
		get
		{
			if (lobbyValidState == ChallengeEvalState.NOT_VALIDATED)
			{
				return campaignID + " lobby has not been validated yet.";
			}
			return base.notActiveReason;
		}
	}
}