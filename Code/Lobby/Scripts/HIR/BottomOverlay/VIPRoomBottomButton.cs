using System.Collections;
using System.Collections.Generic;
using System.Text;
using Com.Scheduler;
using UnityEngine;
using Zynga.Core.Util;

public class VIPRoomBottomButton : BottomOverlayButton 
{
	[SerializeField] private VIPRevampCarouselV3 carousel;
	private bool isLocked = false;

	private const string PREF_ID = "vip_newicon_hash";	// id to use for hashcode prefs
	private const string NEW_GAME_PREF_ID = "vip_highlight_newicon_hash"; //id to use for selected new game
	private int prefHashCode;						// hash value of game keys or campaign name, used to detect changes when showing newIcon banner

	public static VIPRoomBottomButton instance;
	protected override void Awake()
	{
		base.Awake();
		sortIndex = 0;
		instance = this;
		init();
	}
    protected override void OnDestroy()
    {
		base.OnDestroy();
		if (EueFeatureUnlocks.instance != null)
		{
			EueFeatureUnlocks.instance.unregisterForFeatureUnlockedEvent("vip_revamp", onVipRoomUnlocked);
		}

		instance = null;
    }

    public void triggerClick() { onClick(null); }
	protected override void onClick(Dict args = null)
	{
		if (!isLocked)
		{
			StatsManager.Instance.LogCount(
				counterName: "bottom_nav",
				kingdom: "vip_room",
				phylum: SlotsPlayer.isFacebookUser ? "fb_connected" : "anonymous",
				genus: "click"
			);

			//turn off new tag
			if (!hasViewedFeature)
			{
				logFirstTimeFeatureEntry();
				markFeatureSeen();
			}

			toolTipController.toggleNewBadge(false);

			//set hash
			setGameKeysHashCode();
			CustomPlayerData.setValue(PREF_ID, prefHashCode);

			//set highlight new game viewed
			if (ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
			{
				LobbyOption option = VIPLobbyHIRRevamp.findLobbyOption(LoLa.vipRevampNewGameKey);
				if (option != null && option.game != null && option.game.keyName != null)
				{
					CustomPlayerData.setValue(NEW_GAME_PREF_ID, option.game.keyName.GetStableHashCode());
				}
			}

			//do vip click
			carousel.onVIPClicked();
		}
		else
		{
			logLockedClick();
			StartCoroutine(toolTipController.playLockedTooltip());
		}
	}

	protected override void init()
	{
		base.init();

		if (!EueFeatureUnlocks.isFeatureUnlocked("vip_revamp"))
		{
			isLocked = true;
			initLevelLock(false);
			toolTipController.toggleNewBadge(false);
			EueFeatureUnlocks.instance.registerForFeatureUnlockedEvent("vip_revamp", onVipRoomUnlocked);
		}
		else
		{
			refreshNewTag();
			if (needsToShowUnlockAnimation())
			{
				showUnlockAnimation();
			}
		}
	}

	private void onVipRoomUnlocked()
	{
		if (unlockData != null)
		{
			unlockData.unlockAnimationSeen = true;
		}
		Scheduler.addTask(new FeatureUnlockTask(Dict.create(D.OBJECT, toolTipController, D.TITLE, "EUE VIP Room unlock task")), SchedulerPriority.PriorityType.BLOCKING);
		refreshNewTag();
		unlockFeature();
		isLocked = false;
	}

	private void setGameKeysHashCode()
	{
		LobbyInfo lobbyInfo = LobbyInfo.find(LobbyInfo.Type.VIP);
		if (ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
		{
			lobbyInfo = LobbyInfo.find(LobbyInfo.Type.VIP_REVAMP);
		}
		string gameString = "";
		List<string> games = new List<string>();
		StringBuilder gameStringBuilder = new StringBuilder();
		if (lobbyInfo != null)
		{
			VIPLevel currentLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
			foreach (LobbyOption option in lobbyInfo.allLobbyOptions)
			{
				if (option == null)
				{
					Debug.LogWarning("Invalid game option in vip lobby");
					continue;
				}

				LobbyGame game = option.game;
				if (game != null && game.vipLevel.levelNumber <= currentLevel.levelNumber)
				{
					games.Add(game.keyName);
				}
			}
		}

		if (games.Count > 0)
		{
			games.Sort();
			for (int i = 0; i < games.Count; ++i)
			{
				if (games[i] == null)
				{
					continue;
				}
				gameStringBuilder.Append(games[i]);
			}

			gameString = gameStringBuilder.ToString();
		}

		if (!string.IsNullOrEmpty(gameString))
		{
			prefHashCode = gameString.GetStableHashCode();
		}
		else
		{
			prefHashCode = 0;
		}
	}

	public void refreshNewTag()
	{
		if (isLocked)
		{
			return;
		}
		
		if (!hasViewedFeature)
		{
			//Always turn this on if we haven't clicked on it yet
			toolTipController.toggleNewBadge(!needsToShowUnlockAnimation());
			return;
		}
		//calculate the hash
		setGameKeysHashCode();

		if (ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
		{
			LobbyOption option = VIPLobbyHIRRevamp.findLobbyOption(LoLa.vipRevampNewGameKey);
			int cachedNewGame = CustomPlayerData.getInt(NEW_GAME_PREF_ID, 0);
			if (option != null && option.game != null && option.game.keyName != null && option.game.keyName.GetStableHashCode() != cachedNewGame)
			{
				//we have a forced highlighted game, tag it as new
				toolTipController.toggleNewBadge(true);
				return;
			}
		}
		
		int oldHashCode = CustomPlayerData.getInt(PREF_ID,0);
		// if hash codes don't match show the new banner
		if (prefHashCode != 0)
		{
			toolTipController.toggleNewBadge(oldHashCode != prefHashCode);
		}
		else
		{
			toolTipController.toggleNewBadge(false);
		}
	}


}
