using UnityEngine;
using System.Collections;
using TMPro;
public class LobbyOptionButtonSlotventure : LobbyOptionButtonChallengeGame
{
	public TextMeshPro rewardAmount;
	public TextMeshPro rewardAmountCompleteState;
	public Animator lobbyCardAnimation;
	public ButtonHandler playNowButton;
	public GameObject cantPlayTooltip;
	public Renderer gameImage;
	public FadeInAnOut fader;
	public GameObject cardPackParent;
	public const string INCOMPLETE = "Incomplete";
	public const string ACTIVE = "Active";
	public const string ACTIVATE = "Activate";
	public const string COMPLETED = "Completed";
	public const string COMPLETE_LOW_PEDESTAL = "Complete Low Pedestal";
	public const string COMPLETED_HIGH_PEDESTAL = "Complete High Pedestal";
	public const string CARD_PACK_ACTIVATE = "Intro";
	public const string CARD_PACK_ACTIVE = "Loop";
	public const string CARD_PACK_INACTIVE = "Outro";

	[System.NonSerialized] public CollectablePack cardPack;

	public Mission mission;

	private LobbyGame game;
	private static ChallengeLobbyCampaign campaign = null;

	private const string COLLECTIONS_PREFAB_PATH = "Features/Collections/Prefabs/Collections in SlotVentures Prefabs/Instanced Prefabs/Collections in SlotVentures Lobby Option Item";

	[SerializeField] rewardBar rewardBar;

	public void init(Mission newMission, ChallengeLobbyCampaign slotventureCampaign, bool shouldAnimate, bool isCurrentObjective, bool isLastKnownObjective)
	{
		// Display the jack pot. It could be a loot box, or could be coins
		if (rewardBar != null && newMission != null && newMission.rewards != null && newMission.rewards.Count > 0)
		{
			rewardBar.showJackPot(newMission.rewards[0]);
		}

		cantPlayTooltip.SetActive(true); // make sure this is on so the coroutine can run.
		StartCoroutine(CommonGameObject.fadeGameObjectToFromCurrent(cantPlayTooltip, 0, 0.1f));

		playNowButton.clearDelegate();
		playNowButton.registerEventDelegate(onClickPlayNow);
		mission = newMission;
		campaign = slotventureCampaign;

		if (mission.rewards.Count > 0)
		{

			for (int i = 0; i < mission.rewards.Count; i++)
			{
				switch (mission.rewards[i].type)
				{
					case MissionReward.RewardType.CREDITS:
						long credits = mission.rewards[i].amount;
						string convertedCredits = CreditsEconomy.convertCredits(credits);
						if (rewardAmount != null)
						{
							rewardAmount.text = convertedCredits;
							rewardAmountCompleteState.text = convertedCredits;
						}

						break;
					
					case MissionReward.RewardType.SLOTVENTURE_CARD_PACK:
					case MissionReward.RewardType.CARD_PACKS:
						string packKey = mission.rewards[i].cardPackKeyName;
						if (Collectables.isActive() && !string.IsNullOrEmpty(packKey))
						{
							//Setup cards stuff here
							AssetBundleManager.load(this, COLLECTIONS_PREFAB_PATH, collectionPackLoadSuccess, collectionPackLoadFailed, Dict.create(D.DATA, packKey, D.OPTION, shouldAnimate, D.OPTION1, isCurrentObjective, D.OPTION2, isLastKnownObjective));
						}
						break;
					
					default:
						Bugsnag.LeaveBreadcrumb("Unsupported slotventures reward for index: " + i);
						break;
				}
			}
		}
		else
		{
			Debug.LogError("LobbyOptionButtonSlotventure:: init - No reward amounts");
			rewardAmount.text = "0";
			rewardAmountCompleteState.text = "0";
		}

		if (!string.IsNullOrEmpty(mission.objectives[0].game))
		{
			game = LobbyGame.find(mission.objectives[0].game);
			game.setDisplayFeatures(LobbyInfo.Type.MAIN);
		}

		if (game != null)
		{
			if (LobbyOption.activeGameOption(game) == null)
			{
				Debug.LogError(string.Format("LobbyOptionButtonSlotventure::Couldn't find option for {0} progress will be impossible...", ((null == mission.objectives  || mission.objectives.Count == 0) ? "" : mission.objectives[0].game)));
				return;
			}
			LobbyOption tempOption = LobbyOption.activeGameOption(game).copy();
			// So that it loads a 1x1
			tempOption.button = this;
			tempOption.isNormal = true;
			tempOption.imageFilename = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, mission.objectives[0].game);
			imageTint = slotventureCampaign.currentMission == mission || mission.isComplete ? Color.white : Color.grey;
			RoutineRunner.instance.StartCoroutine(LobbyOption.setupStandaloneCabinet(this, game, tempOption));
		}
		else
		{
			Debug.LogError("Game was null. should have been " + mission.objectives[0].game);
		}
	}

	public void onClickPlayNow(Dict args = null)
	{
		if (SlotventuresLobby.instance != null)
		{
			SlotventuresLobby svLobby = SlotventuresLobby.instance as SlotventuresLobby;
			if (svLobby != null && svLobby.isPlayingRewardsSequence)
			{
				return; //Don't do anything if we're in the middle of playing the rewards sequence
			}
		}

		if (campaign.state != ChallengeLobbyCampaign.IN_PROGRESS)
		{
			// If we're not in progress, we shouldn't be able to get back into games from here.
			return;
		}
		if (campaign.currentMission == mission || mission.isComplete)
		{
			if (game != null)
			{
				game.askInitialBetOrTryLaunch();
				if (option != null)
				{
					SlotAction.setLaunchDetails("slotventures", option.lobbyPosition);
				}
				else
				{
					Debug.LogErrorFormat("lobby option for game {0} is null", game.keyName);
				}
			}
			else
			{
				Debug.LogError("invalid game in slotventure challenge campaign");
			}
		}
		else
		{
			Audio.play("GameInactiveSlotVenturesCommon");
			fader.init();
		}
	}

	public override void refresh()
	{

	}

	private void collectionPackLoadSuccess(string path, Object obj, Dict data = null)
	{
		if (this == null)
		{
			return;
		}
		
		GameObject cardPackPanel = NGUITools.AddChild(cardPackParent, obj as GameObject);
		CollectablePack packHandle = cardPackPanel.GetComponent<CollectablePack>();
		string packKey = (string) data.getWithDefault(D.DATA, "");
		bool shouldAnimate = (bool) data.getWithDefault(D.OPTION, false);
		bool isCurrentObjective = (bool) data.getWithDefault(D.OPTION1, false);
		bool isLastKnownObjective = (bool) data.getWithDefault(D.OPTION2, false);

		if (packHandle != null)
		{
			packHandle.init(packKey, true);
			if (packHandle.animator != null)
			{
				if ((mission.isComplete && shouldAnimate && isLastKnownObjective) ||
				    (isCurrentObjective && !shouldAnimate))
				{
					packHandle.animator.Play(CARD_PACK_ACTIVE);
				}
			}
		}

		cardPack = packHandle;
	}
	
	private void collectionPackLoadFailed(string path, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("LobbyOptionButtonSlotventure - collection pack load failed");
	}
}
