using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class ChallengeLobbyObjectivesDialog : DialogBase
{
	public GameObject achievementsParent;
	public GameObject jackpotParent;
	public ObjectivesGrid objectiveGrid;
	public GameObject closeButton;
	public Animator jackpotAnim;
	public TextMeshPro jackpotLabel;
	public TextMeshPro newJackpotLabel;
	
	protected bool didCompleteThis = false;	// Set to true if showing right after the goals for this event were completed.
	protected bool didCompleteAll = false;	// Set to true if showing right after all the events for the challenge were completed.
	protected bool isShowingJackpot = false;	// Set true when showing the jackpot animation.
	protected bool didSkipJackpot = false;	// Avoid calling the jackpot animation skip code more than once.
	protected JSON completionJSON = null;		// If called from a completion event, this is the data from the event.
	protected Mission mission = null;
	protected List<MissionReward> rewards = new List<MissionReward>();
	protected ChallengeLobbyCampaign campaign = null;

	[SerializeField] protected ClickHandler closeHandler;
	[SerializeField] protected ClickHandler achievementHandler;
	
	public override void init()
	{
		if (closeHandler != null)
		{
			closeHandler.registerEventDelegate(closeClicked);
		}

		if (achievementHandler != null)
		{
			achievementHandler.registerEventDelegate(closeClicked);
		}
		
		didCompleteAll = (bool)dialogArgs.getWithDefault(D.OPTION1, false);
		didCompleteThis = didCompleteAll || (bool)dialogArgs.getWithDefault(D.OPTION, false);
		completionJSON = dialogArgs.getWithDefault(D.DATA, null) as JSON;
		mission = dialogArgs.getWithDefault( D.OPTION2, null ) as Mission;

		campaign = CampaignDirector.findWithGame(GameState.game.keyName) as ChallengeLobbyCampaign;
		
		if (mission != null)
		{
			rewards = mission.rewards;
		}
		else if (GameState.game != null)
		{
			mission = campaign.findWithGame(GameState.game.keyName);
		}
		else
		{
			Debug.LogError("No valid game to display objectives on!");
			Dialog.close();
			return;
		}

		(mission as LobbyMission).refreshLocs();
				
		// Make sure the correct UI is displayed by default.
		objectiveGrid.gameObject.SetActive(true);
		jackpotParent.SetActive(false);
		
		// Get ready to scale up for a smoother transition into this.
		jackpotParent.transform.localScale = new Vector3(0.05f, 0.05f, 1.0f);
		
		if (didCompleteAll)
		{
			jackpotLabel.text = CommonText.formatNumber(campaign.currentJackpot);
		}
		
		if (didCompleteThis)
		{
			LobbyAssetData lobbyAssetData = ChallengeLobby.findAssetDataForCampaign(campaign.campaignID);

			if (lobbyAssetData != null)
			{
				Audio.play(lobbyAssetData.getAudioByKey(LobbyAssetData.ALL_OBJECTIVES_COMPLETE));
			}
		}
		
		closeButton.SetActive(!didCompleteThis);	// Dialog will auto-close, so hide the close button.
		
		objectiveGrid.init(GameState.game, mission);

		if (ChallengeLobbyCampaign.currentCampaign != null)
		{
			StatsManager.Instance.LogCount
			(
				"dialog"
				, ChallengeLobbyCampaign.currentCampaign.campaignID
				, "achievements"
				, ""
				, ""
				, "view"
			);
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("ChallengeLobbyObjectivesDialog: ChallengeLobbyCampaign.currentCampaign is null!");
		}
	}
	
	protected override void onFadeInComplete()
	{
		isShowingJackpot = false;

		base.onFadeInComplete();
		
		objectiveGrid.animateChecks();
		
		if (didCompleteAll)	// Must check this first since didCompleteThis is always true if didCompleteAll is true, but not vice-versa.
		{
			StartCoroutine(showJackpot());
		}
		else if (didCompleteThis)
		{
			// If in this mode, automatically close after a short amount of time (allowing the checkmarks to animate first).
			StartCoroutine(closeAfterDelay());
		}
	}
	
	private IEnumerator closeAfterDelay()
	{
		yield return new WaitForSeconds(2.0f);
		Dialog.close();

		if (!GameState.isMainLobby && didCompleteThis && !didCompleteAll)
		{
			// There should be a game unlocked after this one,
			// if this is the first play-through of LOZ,
			// and it's not the final game in the lobby.
			if (rewards.Count > 0)
			{
				LobbyGame nextGame = LobbyGame.find(rewards[0].game);
			
				if (nextGame != null)
				{
					if (!nextGame.isUnlocked)
					{
						nextGame.xp.isPermanentUnlock = true;
						nextGame.setIsUnlocked();
					}
					GameUnlockedDialog.showDialog(nextGame, null);
				}
			}
		}
	}

	public void Update()
	{
		if (!didCompleteThis)
		{
			AndroidUtil.checkBackButton(closeClicked);
		}
		
		if (isShowingJackpot && didCompleteAll && TouchInput.didTap)
		{
			// Touch anywhere to skip jackpot animation loop and finish it.
			StartCoroutine(skipJackpotAnim());
		}
	}
	
	// When all events are complete, show the final presentation.
	protected virtual IEnumerator showJackpot()
	{		
		// Wait for the checkmark animations to finish before fading out.
		yield return new WaitForSeconds(2.0f);
		
		yield return StartCoroutine(objectiveGrid.fadeOutAchievements());
		
		achievementsParent.SetActive(false);
		jackpotParent.SetActive(true);
		
		iTween.ScaleTo(jackpotParent,
			iTween.Hash(
				"scale", Vector3.one,
				"time", 1.0f,
				"easetype", iTween.EaseType.easeOutQuad
			)
		);

		long jackpotAmount = 100000;
		if (rewards.Count > 0)
		{
			jackpotAmount = rewards[0].amount;
		}

		isShowingJackpot = true;

		// Roll up the jackpot amount.
		yield return StartCoroutine(SlotUtils.rollup(
			start: 0L,
			end: jackpotAmount,
			tmPro: jackpotLabel,
			specificRollupTime: 5.0f,
			rollupOverrideSound: ChallengeLobby.findAssetDataForCampaign(campaign.campaignID).getAudioByKey(LobbyAssetData.JACKPOT_ROLLUP),
			rollupTermOverrideSound: ChallengeLobby.findAssetDataForCampaign(campaign.campaignID).getAudioByKey(LobbyAssetData.JACKPOT_TERM)
		));
		
		StartCoroutine(skipJackpotAnim());
	}
	
	protected virtual IEnumerator skipJackpotAnim()
	{
		if (didSkipJackpot) { yield break; }
			
		didSkipJackpot = true;

		closeButton.SetActive(true); // doesn't go to the final jackpot rollup, show the close button
	}

	// NGUI button callback
	protected void closeClicked(Dict args = null)
	{
		Dialog.close();

		if ( didCompleteThis || didCompleteAll )
		{
			foreach (MissionReward reward in rewards)
			{
				reward.collect("challLobbyClose_" + ChallengeLobby.instance.lobbyAssetData.campaignName);
			}
			
			rewards = null;

			if ( didCompleteAll && campaign.isActive )
			{
				Scheduler.addFunction( LobbyLoader.returnToLobbyAfterDialogCloses );
			}

			if (ChallengeLobbyCampaign.currentCampaign != null)
			{
				StatsManager.Instance.LogCount("dialog", "challenge_type_complete", ChallengeLobbyCampaign.currentCampaign.campaignID);
			}
		}

		if (ChallengeLobbyCampaign.currentCampaign != null)
		{
			StatsManager.Instance.LogCount
			(
				"dialog"
				, ChallengeLobbyCampaign.currentCampaign.campaignID
				, "achievements"
				, ""
				, ""
				, "close"
			);
		}
	}
	
	
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		playCloseSound();
	}
	
	protected override void playOpenSound()
	{
		LobbyAssetData lobbyAssetData = ChallengeLobby.findAssetDataForCampaign(campaign.campaignID);
		Audio.play(lobbyAssetData.getAudioByKey(LobbyAssetData.DIALOG_OPEN));
	}

	public override void playCloseSound()
	{
		LobbyAssetData lobbyAssetData = ChallengeLobby.findAssetDataForCampaign(campaign.campaignID);
		Audio.play(lobbyAssetData.getAudioByKey(LobbyAssetData.DIALOG_CLOSE));
	}
	
	public static void showDialog
	(
		ChallengeLobbyCampaign campaign = null,
		Mission mission = null,
		JSON completionJSON = null,
		bool didCompleteThis = false,
		bool didCompleteAll = false,
		SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.IMMEDIATE
	)
	{
		// Uncomment the next line if you want to always see the jackpot animation for testing.
		//didCompleteAll = true;
		// Uncomment the next line if you want to always test the next game unlock dialog.
		//didCompleteThis = true;
		
		Dict args = Dict.create(
			D.OPTION, didCompleteThis,
			D.OPTION1, didCompleteAll,
			D.OPTION2, mission,
			D.DATA, completionJSON
		);

		if (mission == null || completionJSON == null)
		{
			Debug.LogError("completionJSON == null " +(completionJSON == null));
			Debug.LogError("mission == null " + (mission == null));
		}

		if (campaign == null)
		{
			// find the first campaign that is running with current game
			campaign = CampaignDirector.findWithGame(GameState.game.keyName) as ChallengeLobbyCampaign;
		}
		
		if (campaign != null)
		{
			Scheduler.addDialog(campaign.campaignID + "_dialog", args, priorityType);    // Must show immediately since it's shown during post-outcome processing but before "nothing is happening".
		}
		else
		{
			string gameString = GameState.game != null && !string.IsNullOrEmpty(GameState.game.keyName) ? GameState.game.keyName : "unknown game";
			Debug.LogError("Got a objective complete from a challenge while spinning in " + gameString);
		}
	}
}
