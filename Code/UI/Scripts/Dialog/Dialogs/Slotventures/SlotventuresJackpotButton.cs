using UnityEngine;
using System.Collections;
using TMPro;
public class SlotventuresJackpotButton : MonoBehaviour
{
	public Animator collectButtonAnimator;
	public ButtonHandler collectButton;
	public Animator jackpotIconAnimation;
	public SlotventuresLobby lobbyReference;
	
	[SerializeField] private GameObject cardPackParent;
	[System.NonSerialized] public CollectablePack cardPack;

	private ChallengeLobbyCampaign slotventureCampaign;
	private const string WIN_STATE = "win";
	private const string IDLE_STATE = "idle";
	private const int OFFSCREEN_TWEEN_LOCATION = -740;
	
	private const string COLLECTIONS_PREFAB_PATH = "Features/Collections/Prefabs/Collections in SlotVentures Prefabs/Instanced Prefabs/Collections in SlotVentures Lobby Option Item";
	private const int ANIMATION_DELAY = 3; // The delay between showing the collect animation and showing the restart dialog

	[SerializeField] rewardBar jackpotBar;

	private bool isALootBoxJackPot = false;
	// Use this for initialization
	void Start()
	{
		slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as ChallengeLobbyCampaign;
		jackpotIconAnimation.Play(IDLE_STATE);
		// Get campaign or get passed campaign and set amount

		if (slotventureCampaign != null && slotventureCampaign.missions != null)
		{
			initRewards();
			collectButton.registerEventDelegate(onClickCollect);
		}
	}

	private void onClickCollect(Dict args = null)
	{
		Audio.play(SlotventuresLobby.assetData.audioMap[LobbyAssetData.COLLECT_JACKPOT]);
		collectButtonAnimator.gameObject.SetActive(false);
		collectButton.unregisterEventDelegate(onClickCollect, true);
		long reward = slotventureCampaign.currentMission.getCreditsReward;
		SlotsPlayer.addFeatureCredits(reward, "sventuresJackpotClick");
		jackpotIconAnimation.Play(WIN_STATE);
		RobustChallengesAction.claimReward(slotventureCampaign.campaignID, onClaimReward);
	}

	private void onClaimReward(JSON data)
	{
		iTween.MoveTo(lobbyReference.progressPanel.gameObject, iTween.Hash("y", OFFSCREEN_TWEEN_LOCATION, "z", 0, "time", 1, "islocal", true, "easetype", iTween.EaseType.linear));
		slotventureCampaign.currentMission.complete();
		slotventureCampaign.state = ChallengeCampaign.COMPLETE;
		slotventureCampaign.timerRange.clearSubtimers();
		CampaignDirector.getProgress(slotventureCampaign.campaignID, onClaimLastReward);	
	}

	private void onClaimLastReward(JSON data)
	{
		if (slotventureCampaign.replayCount < (slotventureCampaign.maxReplayLimit))
		{
			//delay to make sure coin shower finishes
			GameTimerRange replayDelay = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + ANIMATION_DELAY);
			replayDelay.registerFunction(restartCampaign);
			
		}
		else
		{
			//call update and finish
			NGUITools.AddChild(lobbyReference.toasterAnchor, SlotventuresLobby.toasterPrefabEnded);
		}

		
		// Display loot box reward if we have one
		if (isALootBoxJackPot)
		{
			LootBoxFeature.instance.showLootBoxRewardDialog(LootBoxFeature.SOURCE_SLOTVENTURES);
		}
		// Do coin animation only if it is not a lootbox jackpot
		else
		{
			lobbyReference.attractor = NGUITools.AddChild(gameObject, SlotventuresLobby.coinAttractObject);
			lobbyReference.attractor.transform.localPosition = collectButton.gameObject.transform.parent.localPosition;
			lobbyReference.attractor.GetComponentInChildren<particleAttractorSpherical>().target =
				Overlay.instance.top.creditsTMPro.transform;
		}

		GameTimerRange explosionDelay = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + ANIMATION_DELAY);
		explosionDelay.registerFunction(spawnExplosion);
	}

	private void restartCampaign(Dict args = null, GameTimerRange sender = null)
	{
		//call update
		RobustChallengesAction.getCampaignRestartData(slotventureCampaign.campaignID, onCampaignUpdate);
	}

	private void onCampaignUpdate(JSON data)
	{
		JSON clientData = data.getJSON("clientData");
		if (clientData == null)
		{
			Debug.LogError("Invalid client data");
			//call update and finish
			NGUITools.AddChild(lobbyReference.toasterAnchor, SlotventuresLobby.toasterPrefabEnded);
			return;
		}

		//get achievement data

		NetworkAchievementAction.getAchievementsForUser(SlotsPlayer.instance.socialMember, (achievementData)=> {
			
			if (achievementData != null)
			{
				long newScore = achievementData.getLong("achievement_score", 0);
				JSON achievementJSON = achievementData.getJSON("achievements");
				if (achievementJSON != null && SlotsPlayer.instance.socialMember.achievementProgress != null)
				{
					//this will probably update the progress twice, but we can't control the order of the callbacks on the event and we need this to occur before we init the motd
					SlotsPlayer.instance.socialMember.achievementProgress.update(achievementJSON, newScore, SlotsPlayer.instance.socialMember);
					SlotsPlayer.instance.socialMember.setUpdated();
				}
			}
		});
		
		SlotventuresChallengeCampaign slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as SlotventuresChallengeCampaign;
		slotventureCampaign.restart();  //reset to blank slate
		slotventureCampaign.init(clientData); //reinitialize with data
			
		//show the restart button in 1 second just to be sure the achievement udpate goes through
		SlotventuresMOTD.showDialog("", SlotventuresMOTD.DialogState.EVENT_RESTART);
	}

	private void spawnExplosion(Dict args = null, GameTimerRange sender = null)
	{
		// Display coin explosion animation if not a look box jackpot
		if (!isALootBoxJackPot && Overlay.instance.top != null && lobbyReference != null)
		{
			lobbyReference.explosion = NGUITools.AddChild(Overlay.instance.top.creditsTMPro.gameObject, SlotventuresLobby.coinExplosionAnimation);
		}
				
		SlotventuresChallengeCampaign slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as SlotventuresChallengeCampaign;
		slotventureCampaign.dropPackCheck();
	}

	private void initRewards()
	{
		if (slotventureCampaign == null)
		{
			Debug.LogError("SlotventuresJackpotButton.initRewards: slotventureCampaign is null");
		}
			
		string jackpotRewardPackKey = slotventureCampaign.currentJackpotRewardPack;
		if (Collectables.isActive() && !string.IsNullOrEmpty(jackpotRewardPackKey))
		{
			loadCardReward(jackpotRewardPackKey);
		}

		if (slotventureCampaign.lastMission != null && 
		    slotventureCampaign.lastMission.rewards != null && slotventureCampaign.lastMission.rewards.Count > 0)
		{
			// Display the jack pot. It could be a loot box, or could be coins
			if (jackpotBar != null)
			{
				jackpotBar.showJackPot(slotventureCampaign.lastMission.rewards[0]);
			}

			isALootBoxJackPot = slotventureCampaign.lastMission.rewards[0].type == (ChallengeReward.RewardType.LOOT_BOX);
		}
		else
		{
			Debug.LogError("SlotventuresJackpotButton.initRewards: last mission in the slotventureCampaign is null or does not have valid reward");
		}
	}

	public void loadCardReward(string packKeyName)
	{
		AssetBundleManager.load(this, COLLECTIONS_PREFAB_PATH, collectionPackLoadSuccess, collectionPackLoadFailed, Dict.create(D.DATA, packKeyName));
	}
	
	private void collectionPackLoadSuccess(string path, Object obj, Dict data = null)
	{
		if (this == null)
		{
			return;
		}
		
		GameObject cardPackPanel = NGUITools.AddChild(cardPackParent, obj as GameObject);
		CollectablePack packHandle = cardPackPanel.GetComponent<CollectablePack>();
		CollectablePackData packData = null;
		string packKey = (string) data.getWithDefault(D.DATA, "");
		packHandle.init(packKey, true);
		if (slotventureCampaign.currentEventIndex == slotventureCampaign.missions.Count - 1 &&
		    slotventureCampaign.state == ChallengeCampaign.IN_PROGRESS)
		{
			if (packHandle.animator != null)
			{
				packHandle.animator.Play(LobbyOptionButtonSlotventure.CARD_PACK_ACTIVE);
			}
		}

		cardPack = packHandle;
	}
	
	private void collectionPackLoadFailed(string path, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("SlotVenturesJackpotButton - collectionPackLoadFailed");
	}
}
