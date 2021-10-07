using System.Collections;
using System.Collections.Generic;
using System.Text;
using Com.Scheduler;
using UnityEngine;
using TMPro;
using Zynga.Core.Util;


public class GenericLobbyPortal : BottomOverlayButton
{
	protected int gameImageIndex = 0;
	protected List<string> gameKeys = new List<string>();
	protected string newGamePrefID; //id for current "new game"
	protected string prefID;	// id to use for hashcode prefs
	protected int prefHashCode;	// hash value of game keys or campaign name, used to detect changes when showing newIcon banner
	protected ChallengeLobbyCampaign campaign = null;  // set if portal is campaign based

	public GameObject lockElements;
	public TextMeshPro lockLevelLabel;
	public TextMeshPro jackpotLabel;
	public GameObject jackpotParent;
	public Animator jackpotAnimator;
	public string actionId;
	public bool enableIfLocked;

	public bool isWaitingForTextures { get; protected set; }

	protected string filePath;
	protected string fileTabPath = "";
	protected string staticArtPath = "";

	[SerializeField] protected LobbyInfo.Type lobbyType;
	[SerializeField] protected ButtonHandler mainButtonClickedHandler;
	
	protected virtual string FALLBACK_ART_PATH
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/statric_room_card.png"; }
	}

	protected virtual string SIN_CITY_BG_PATH
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/sin_city_room_card.png"; }
	} 

	protected virtual string LOZ_BG_PATH 
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/oz_room_card.png"; }
	}

	protected virtual string MAX_VOLTAGE_BG_PATH
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/max_voltage_room_card.png"; }
	}
	protected virtual string SLOTVENTURES_BG_PATH 
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/slotventures_room_card.png"; }
	}

	protected virtual string SIN_CITY_TAB_BG_PATH 
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/sin_city_room_tab.png"; }
	}
	protected virtual string LOZ_TAB_BG_PATH 
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/oz_room_tab.png"; }
	}
	protected virtual string MAX_VOLTAGE_TAB_BG_PATH 
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/max_voltage_room_tab.png"; }
	}
	protected virtual string SLOTVENTURES_TAB_BG_PATH 
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/slotventures_room_tab.png"; }
	}

	protected virtual string SLOTVENTURES_STATIC_ART_PATH
	{
		get { return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/Room Cards/slotventures_1X1_HIR{0}.png"; }
	}

	public virtual void setup(bool isLocked, bool loadBGImage = true, bool loadTabImage = true)
	{
		isWaitingForTextures = true;

		toolTipController.toggleNewBadge(false);

		prefID = actionId + "_newIcon_hash";
		newGamePrefID = actionId + "_highlight_newIcon_hash";
		hasViewedFeature = EueFeatureUnlocks.hasFeatureBeenSeen(featureKey);

		switch (actionId)
		{
			case "max_voltage_lobby":
				lobbyType = LobbyInfo.Type.MAX_VOLTAGE;
				filePath = MAX_VOLTAGE_BG_PATH;
				fileTabPath = MAX_VOLTAGE_TAB_BG_PATH;
				break;

			case "loz_lobby": //Not key in SCAT data feature deprecated should remove in future
				lobbyType = LobbyInfo.Type.LOZ;
				filePath = LOZ_BG_PATH;
				fileTabPath = LOZ_TAB_BG_PATH;
				break;

			case "sin_city_strip_lobby"://Not key in SCAT data feature deprecated should remove in future
				lobbyType = LobbyInfo.Type.SIN_CITY;
				filePath = SIN_CITY_BG_PATH;
				fileTabPath = SIN_CITY_TAB_BG_PATH;
				break;

			case "slotventure":
				lobbyType = LobbyInfo.Type.SLOTVENTURE;
				filePath = SLOTVENTURES_BG_PATH;

				if (string.IsNullOrEmpty(SlotventuresLobby.assetData.themeName))
				{
					PreferencesBase prefs = SlotsPlayer.getPreferences();
					if (Data.debugMode && !string.IsNullOrEmpty(prefs.GetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE, "")))
					{
						SlotventuresLobby.assetData.themeName = prefs.GetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE).ToLower();
					}
					else if (ExperimentWrapper.Slotventures.isEUE)
					{
						SlotventuresLobby.assetData.themeName = Data.liveData.getString("EUE_SLOTVENTURES_THEME", "").ToLower();
					}
					else
					{
						SlotventuresLobby.assetData.themeName = Data.liveData.getString("SLOTVENTURES_THEME", "").ToLower();
					}

				}

				fileTabPath = string.Format(SLOTVENTURES_TAB_BG_PATH, SlotventuresLobby.assetData.themeName);
				staticArtPath = string.Format(SLOTVENTURES_STATIC_ART_PATH, SlotventuresLobby.assetData.themeName);

				// since slotventures does not cycle through list of games the name of the theme will be the hashcode instead
				prefHashCode = SlotventuresLobby.assetData.themeName.GetStableHashCode();
				break;
		}

		SafeSet.gameObjectActive(lockElements, isLocked);


		setupGameKeyList();
	}

	protected virtual void sortGames(List<LobbyOption> games)
	{
		games.Sort(LobbyOption.sortByOrder);
	}

	protected static int compareByUnlockLevel(LobbyGame x, LobbyGame y)
	{
		if (x == null)
		{
			if (y == null)
			{
				//nulls are equal
				return 0;
			}

			//y is greater
			return -1;
		}
		else if (y == null)
		{
			//x is greater
			return 1;
		}
		else
		{
			return x.unlockLevel.CompareTo(y.unlockLevel);
		}
	}

	private void setupGameKeyList()
	{
		bool shouldSetupFallback = false;
		switch (lobbyType)
		{
			case LobbyInfo.Type.SIN_CITY:
				campaign = CampaignDirector.find(CampaignDirector.SIN_CITY) as ChallengeLobbyCampaign;
				if (campaign != null)
				{
					gameKeys = CampaignDirector.find(CampaignDirector.SIN_CITY).getGameKeys();
				}
				else
				{
					shouldSetupFallback = true;
				}
				break;
			case LobbyInfo.Type.LOZ:
				campaign = CampaignDirector.find(CampaignDirector.LOZ_CHALLENGES) as ChallengeLobbyCampaign;
				if (campaign != null)
				{
					gameKeys = CampaignDirector.find(CampaignDirector.LOZ_CHALLENGES).getGameKeys();
				}
				else
				{
					shouldSetupFallback = true;
				}
				break;
			case LobbyInfo.Type.MAX_VOLTAGE:
				if (jackpotLabel != null && ProgressiveJackpot.maxVoltageJackpot != null)
				{
					ProgressiveJackpot.maxVoltageJackpot.registerLabel(jackpotLabel);

					// Max voltage doesn't have a campaign,so at this point we would have turned thiss off for saftey 
					if (jackpotParent != null)
					{
						jackpotParent.SetActive(true);
					}
				}				

				LobbyInfo lobbyInfo = LobbyInfo.find(LobbyInfo.Type.MAX_VOLTAGE);

				if (lobbyInfo != null)
				{
					foreach (LobbyOption option in lobbyInfo.allLobbyOptions)
					{
						gameKeys.Add(option.game.keyName);
					}
				}
				else
				{
					shouldSetupFallback = true;
				}
				break;

			case LobbyInfo.Type.SLOTVENTURE:
				campaign = CampaignDirector.find(CampaignDirector.SLOTVENTURES) as ChallengeLobbyCampaign;
				break;
		}

		if (shouldSetupFallback)
		{
			setupFallback();
		}

		gameImageIndex = 0;

		refreshNewTag();

		setJackpotLabel();

		if (mainButtonClickedHandler != null)
		{
			mainButtonClickedHandler.registerEventDelegate(enterRoomClicked);
		}
	}

	public void refreshNewTag()
	{
		setGameKeysHashCode();

		//check if we have a special case for this type of lobby
		switch (lobbyType)
		{
			case LobbyInfo.Type.MAX_VOLTAGE:
			{
				if (isLevelLocked())
				{
					toolTipController.toggleNewBadge(false);
					return;
				}
				
				if (!hasViewedFeature)
				{
					toolTipController.toggleNewBadge(!needsToShowUnlockAnimation() && !playingUnlockAnimation);
					return;
				}

				int cachedNewGame = CustomPlayerData.getInt(newGamePrefID,0);
				if (!string.IsNullOrEmpty(LoLa.newMVZGameMotdKey) && LoLa.newMVZGameMotdKey.GetStableHashCode() != cachedNewGame)
				{
					toolTipController.toggleNewBadge(true);
					return;
				}
				break;
			} 
		}

		// if hashcodes don't match show the new banner
		int oldHashCode = CustomPlayerData.getInt(prefID,0);
		toolTipController.toggleNewBadge(oldHashCode != prefHashCode && !isLevelLocked());
	}

	protected virtual void logClick()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "lobby",
			kingdom: "bottom_drawer",
			phylum: "rooms",
			klass: "",
			family: actionId,
			genus: "click"
		);
	}

	public void enterRoomClicked(Dict args)
	{
		if (Input.touchCount > 1 || !Glb.isNothingHappening || Scheduler.hasTask)
		{
			// Prevent responding to multi touch on multiple lobby options.
			// Also ignore if there is something happening, or a task in the Scheduler.
			// This should only happen if two options are trying to be processed at the same time.
			return;
		}

		if (!enableIfLocked && lockElements != null && lockElements.activeSelf)
		{
			// game is locked
			return;
		}

		if (isLevelLocked())
		{
			logLockedClick();
			StartCoroutine(toolTipController.playLockedTooltip());
			return;
		}

		if (!hasViewedFeature)
		{
			logFirstTimeFeatureEntry();
			markFeatureSeen();
		}

		//do stats log
		logClick();
		
		// if newIcon is showing then turn it off and write new hashcode to prefs since they have now seen it

		toolTipController.toggleNewBadge(false);
		CustomPlayerData.setValue(prefID,prefHashCode);

		switch (lobbyType)
		{
			case LobbyInfo.Type.MAX_VOLTAGE:
				{
					int newGame = LoLa.newMVZGameMotdKey == null ? 0 : LoLa.newMVZGameMotdKey.GetStableHashCode();
					CustomPlayerData.setValue(newGamePrefID, newGame);
				}
				break;
		}

		DoSomething.now(actionId);
	}

	protected virtual void setupFallback()
	{

	}

	private void setGameKeysHashCode()
	{
		string gameString = "";
		List<string> games = new List<string>();
		if (gameKeys != null)
		{
			foreach (string gameKey in gameKeys)
			{
				games.Add(gameKey);
			}
		}

		StringBuilder gameStringBuilder = new StringBuilder();
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

	protected void setJackpotLabel()
	{
		if (jackpotLabel != null)
		{
			if (campaign != null)
			{
				jackpotParent.gameObject.SetActive(true);
				jackpotLabel.text = CreditsEconomy.convertCredits(campaign.currentJackpot);
			}
			else if (jackpotParent != null && lobbyType != LobbyInfo.Type.MAX_VOLTAGE)
			{
				// Turn this off if there's no campaign since we'd be showing 00000 otherwise.
				// BUT if it's registered to a progressive, we should check that too (like max voltage)
				jackpotParent.gameObject.SetActive(false);
			}
		}
	}

	protected bool isLevelLocked()
	{
		if (ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			switch (lobbyType)
			{
				case LobbyInfo.Type.MAX_VOLTAGE:
					return !EueFeatureUnlocks.isFeatureUnlocked("max_voltage");
				
				case LobbyInfo.Type.VIP:
					return SlotsPlayer.instance.vipNewLevel < 1;
				
				default:
					return false;
			}
		}

		return false;
	}
}
