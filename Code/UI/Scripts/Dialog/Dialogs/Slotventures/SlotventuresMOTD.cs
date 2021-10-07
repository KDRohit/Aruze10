using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class SlotventuresMOTD : DialogBase
{
	public enum DialogState { MOTD, EVENT_ENDED, EVENT_COMPLETE, EVENT_RESTART, EVENT_RESTART_LOBBY };

	public GameObject motdModeParent;
	public GameObject eventEndedModeParent;
	public GameObject eventCompleteModeParent;
	public GameObject eventReplayModeParent;

	public ButtonHandler closeButton;
	public ButtonHandler callToAction;

	// The parent of the ends in text, so we can turn it off when time is out.
	public GameObject limitedTimeElements;
	public GameObject timesUpElements;
	public TextMeshPro timeRemainingText;
	public TextMeshPro jackpotText;
	public SlotVenturesReplayConfig replayConfig;
	SlotventuresChallengeCampaign slotventureCampaign = null;

	[SerializeField] private rewardBar jackpotBar;
	[SerializeField] private rewardBar replayJackpotBar;

	[SerializeField] private GameObject cardPackParent;
	private const int COLLECTION_PACK_RARITY = 5;
	
	private const string COLLECTIONS_PREFAB_PATH = "Features/Collections/Prefabs/Collections in SlotVentures Prefabs/Instanced Prefabs/Collections in SlotVentures MOTD Item";

	private static DialogState currentState = DialogState.MOTD;

	public override void init()
	{
		slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as SlotventuresChallengeCampaign;
		
		StatsManager.Instance.LogCount(counterName: "dialog", kingdom:"slotventures_motd", phylum: slotventureCampaign.variant, genus: "click");

		motdModeParent.SetActive(false);
		eventCompleteModeParent.SetActive(false);
		eventEndedModeParent.SetActive(false);
		if (eventReplayModeParent != null)
		{
			eventReplayModeParent.SetActive(false);
		}
		
		limitedTimeElements.SetActive(false);

		switch (currentState)
		{
			case DialogState.EVENT_COMPLETE:
				eventCompleteModeParent.SetActive(true);
				callToAction.gameObject.SetActive(true);
				callToAction.registerEventDelegate(closeClicked);
				closeButton.gameObject.SetActive(true);
				closeButton.registerEventDelegate(closeClicked);
				slotventureCampaign.timerRange.registerLabel(timeRemainingText);
				callToAction.text = Localize.textUpper("ok");
				break;
			case DialogState.EVENT_RESTART_LOBBY:
				StatsManager.Instance.LogCount(counterName: "dialog", kingdom: "slotventures_motd", phylum:slotventureCampaign.variant, genus: "view");
				eventReplayModeParent.SetActive(true);
				slotventureCampaign.timerRange.registerLabel(timeRemainingText);
				jackpotText.gameObject.SetActive(false);
				//turn off default buttons
				callToAction.gameObject.SetActive(false);
				//enable close button
				closeButton.gameObject.SetActive(true);
				closeButton.registerEventDelegate(closeClicked);
				//enable timer
				limitedTimeElements.SetActive(true);

				replayConfig.initWithOneButton(onClickRestartEventFromLobby, CreditsEconomy.convertCredits((slotventureCampaign as ChallengeLobbyCampaign).currentJackpot) , getNextAchievement());
				if (ExperimentWrapper.NetworkAchievement.isInExperiment)
				{
					NetworkAchievementAction.getAchievementsForUser(SlotsPlayer.instance.socialMember, onGetAchievmentData);
				}
				break;
			case DialogState.EVENT_RESTART:
				StatsManager.Instance.LogCount(counterName: "dialog", kingdom: "slotventures_motd", phylum: slotventureCampaign.variant, genus: "view");
				eventReplayModeParent.SetActive(true);
				slotventureCampaign.timerRange.registerLabel(timeRemainingText);
				jackpotText.gameObject.SetActive(false);
				//turn off default buttons
				closeButton.gameObject.SetActive(false);
				callToAction.gameObject.SetActive(false);
				//enable timer
				limitedTimeElements.SetActive(true);
				
				replayConfig.initWithTwoButtons(onCloseWhenRestart, onClickRestartEvent, CreditsEconomy.convertCredits((slotventureCampaign as ChallengeLobbyCampaign).currentJackpot) , getNextAchievement());
				
				if (ExperimentWrapper.NetworkAchievement.isInExperiment)
				{
					NetworkAchievementAction.getAchievementsForUser(SlotsPlayer.instance.socialMember, onGetAchievmentData);
				}
				break;
			case DialogState.MOTD:
				StatsManager.Instance.LogCount(counterName: "dialog", kingdom: "slotventures_motd", phylum:slotventureCampaign.variant, genus: "view");
				Audio.play(SlotventuresLobby.assetData.audioMap[LobbyAssetData.MOTD_OPEN]);
				callToAction.registerEventDelegate(onClickBeginEvent);
				jackpotText.text = CreditsEconomy.convertCredits((slotventureCampaign as ChallengeLobbyCampaign).currentJackpot) ;
				slotventureCampaign.timerRange.registerLabel(timeRemainingText);
				//enable close button
				closeButton.gameObject.SetActive(true);
				closeButton.registerEventDelegate(closeClicked);
				//enable call to action button
				callToAction.gameObject.SetActive(true);
				callToAction.text = Localize.textUpper("play_now");
				//set basic elements active
				motdModeParent.SetActive(true);
				limitedTimeElements.SetActive(true);
				if (Collectables.isActive() && slotventureCampaign.hasCardRewards())
				{
					AssetBundleManager.load(this, COLLECTIONS_PREFAB_PATH, collectionPackLoadSuccess, collectionPackLoadFailed);
				}

				break;
			case DialogState.EVENT_ENDED:
				timeRemainingText.text = Localize.text("event_over");
				//set end mode group active
				eventEndedModeParent.SetActive(true);
				//enable call to action
				callToAction.gameObject.SetActive(true);
				callToAction.registerEventDelegate(onCloseWhenEnded);
				callToAction.text = Localize.textUpper("ok");
				//enable close button
				closeButton.gameObject.SetActive(true);
				closeButton.registerEventDelegate(onCloseWhenEnded);
				break;
			
		}

		if (slotventureCampaign != null && slotventureCampaign.lastMission != null &&
		    slotventureCampaign.lastMission.rewards != null && slotventureCampaign.lastMission.rewards.Count > 0)
		{
			// Display the jack pot. It could be a loot box, or could be coins
			if (jackpotBar != null)
			{
				jackpotBar.showJackPot(slotventureCampaign.lastMission.rewards[0]);
				replayJackpotBar.showJackPot(slotventureCampaign.lastMission.rewards[0]);
			}
		}

		MOTDFramework.markMotdSeen(dialogArgs);
	}
	
	public void onGetAchievmentData(JSON data = null)
	{
		// If somehow this dialog closes before we get the callback, avoid NREs
		if (this != null && gameObject != null && replayConfig != null)
		{
			replayConfig.setupNextAchievement(getNextAchievement());
		}
	}
	
	private Achievement getNextAchievement()
	{
		List<string> orderedAchievements = new List<string>()
		{
			"hir_slot_venture_1",
			"hir_slot_venture_2",
			"hir_slot_venture_3"
		};

		for(int i =0; i<orderedAchievements.Count; ++i)
		{
			Achievement award = NetworkAchievements.getAchievement(orderedAchievements[i]);
			if (award != null && !award.isUnlocked())
			{
				return award;
			}
		}
		return null;
	}

	private void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void onClickRestartEvent(Dict args = null)
	{
		//replay clicked
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "slotventures_motd",
			phylum: slotventureCampaign.variant,
			klass: "slotventures_replay",
			genus: "click"
		);
		//reload the lobby
		SlotventuresLobby lobbyInstance = SlotventuresLobby.instance as SlotventuresLobby;
		lobbyInstance.restart();
		
		Dialog.close();
	}

	private void onClickRestartEventFromLobby(Dict args = null)
	{
		//replay clicked
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "slotventures_motd", 
			phylum: slotventureCampaign.variant,
			klass: "slotventures_replay",
			genus: "click"
		);
		onClickBeginEvent(args);
	}

	private void onClickBeginEvent(Dict args = null)
	{
		Scheduler.addFunction(delegate(Dict a){ DoSomething.now("slotventure");}, null, SchedulerPriority.PriorityType.IMMEDIATE);
		Dialog.close();
	}

	private void onCloseWhenRestart(Dict args = null)
	{
		//call normal close
		onCloseWhenEnded(args);
	}

	private void onCloseWhenEnded(Dict args = null)
	{
		// If we're not in the main lobby, and we're in a game that is part of the current mission or, we're in the slotventures lobby...
		if (MainLobby.instance == null && ((GameState.game != null && slotventureCampaign.currentMission.containsGame(GameState.game.keyName)) || SlotventuresLobby.instance != null))
		{
			// Back to the main lobby, now
			LobbyLoader.lastLobby = LobbyInfo.Type.MAIN;
			NGUIExt.disableAllMouseInput();
			Loading.show(Loading.LoadingTransactionTarget.LOBBY);
			Glb.loadLobby();
			if (Audio.currentMusicPlayer != null && Audio.currentMusicPlayer.isPlaying && Audio.currentMusicPlayer.relativeVolume < 0.01f)
			{
				Audio.switchMusicKeyImmediate("");
			}

			Audio.stopAll();
			Audio.removeDelays();
			Audio.listenerVolume = Audio.maxGlobalVolume;
			Audio.play("return_to_lobby");
		}

		Dialog.close();
	}
	
	private void collectionPackLoadSuccess(string path, Object obj, Dict data = null)
	{
		if (this == null)
		{
			return;
		}
		
		GameObject cardPackPanel = NGUITools.AddChild(cardPackParent, obj as GameObject);
		CollectablePack packHandle = cardPackPanel.GetComponent<CollectablePack>();
		packHandle.initWithForcedRarity(COLLECTION_PACK_RARITY, true);
	}
	
	private void collectionPackLoadFailed(string path, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("SlotventuresMOTD - collection pack load failed");
	}

	private void closeClicked(Dict args = null)
	{
		if (currentState == DialogState.MOTD)
		{
			StatsManager.Instance.LogCount(counterName: "dialog", kingdom: "slotventures_motd",phylum: slotventureCampaign.variant, genus: "close");
		}
		Dialog.close();
	}

	public static bool showDialog(string motdKey = "", DialogState state = DialogState.MOTD)
	{		
		currentState = state;
		Dict dict = Dict.create(D.MOTD_KEY, motdKey, D.THEME, SlotventuresLobby.assetData.themeName);
		Scheduler.addDialog("slotventure_motd", dict);
		return true;
	}

	// Do NOT call -- called by Dialog.close()
	public override void close()
	{
	}
}
