using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Com.Scheduler;

/**
Represents an option on main lobby menu.
*/

public class LobbyOption : IResetGame
{
	private const string VIP_COMING_SOON_IMAGE_PATH = "";
	private const int MIN_VIP_CABINETS_SIR = 4; // Based on ticket HIR-31965.

	public enum Type
	{
		GAME,
		COMING_SOON,
		VIP_COMING_SOON,
		VIP_EARLY_ACCESS_COMING_SOON, // Treated differently than the normal COMING_SOON. This is where the early access slot would be.
		ACTION
	}

	public string name;
	public LobbyOption.Type type;
	public LobbyGame game = null; // Only non-null if type is "game".
	public string action = ""; // If used, type is ACTION.
	public string imageFilename;
	public string bannerLoadingImageName;
	public Pinned pinned = null; // Only used if the option is pinned.
	public bool isNormal = false;
	public bool isPinned = false;
	public bool defaultBanner = false;
	public Texture bitmap = null;
	public Texture pinnedBitmap = null;
	public int sortOrder = 0;
	public GameObject panel = null;
	public LobbyOptionButton button = null;
	public string localizedText = "";
	public int page = -1;
	public int lobbyPosition = -1;
	
	// only banner actions have image
	public bool isBannerAction => type == Type.ACTION && !string.IsNullOrEmpty(imageFilename);

	public static string freePreviewLocalizationKey = "";	// The localization key to use on the free preview game lobby option if one exists.
	public static string baseUrl = "";		// This is just the base URL from basic data. The lobby-specific URL has the lobby's key appended to this when requested.
	public static string dataVersion = "0";	// The version of the data from baseUrl. We include this in the querystring to avoid caching issues for old versions.

	private static string stuckTask = "";

	// list of actions that are ignored for lobby options in lobby v3
	private const string FILTERED_V3_ACTIONS = "loz_lobby,sin_city_strip_lobby";
	private static readonly List<string> LOBBY_ACTION_1x1_LIST = new List<string> {"max_voltage_lobby", "vip_lobby"};
		
	public static int minSpinCountSorting = 50;
	public bool isProgressive1X2
	{
		get
		{
			return
				game != null &&
				game.isProgressive &&
				isPinned && pinned.shape == Pinned.Shape.BANNER_1X2;
		}
	}
	
	public bool isMysteryGift1X2
	{
		get
		{
			return
				game != null &&
				game.mysteryGiftType == MysteryGiftType.MYSTERY_GIFT &&				
				isPinned && pinned.shape == Pinned.Shape.BANNER_1X2;
		}
	}

	public bool isMysteryGift
	{
		get
		{
			return
				game != null &&
				game.mysteryGiftType == MysteryGiftType.MYSTERY_GIFT;
		}
	}	

	public bool isBigSlice1X2
	{
		get
		{
			return
				game != null &&
				game.mysteryGiftType == MysteryGiftType.BIG_SLICE &&
				isPinned && pinned.shape == Pinned.Shape.BANNER_1X2;
		}
	}
	
	public bool isGiantProgressive1X2
	{
		get
		{
			return (isProgressive1X2 && game.isGiantProgressive);
		}
	}

	public bool isRoyalRush
	{
		get
		{
			return (game != null && game.isRoyalRush && isPinned && pinned.shape == Pinned.Shape.BANNER_1X2);
		}
	}

	public bool isGoldPass
	{
		get
		{
			return (game != null && game.isGoldPassGame);
		}
	}

	public LobbyOption()
	{
	}

	/// Makes a copy of the object to be used as a pinned version of it.
	public LobbyOption copy()
	{
		LobbyOption option = new LobbyOption();
		option.name = name;
		option.type = type;
		option.game = game;
		option.action = action;
		option.imageFilename = imageFilename;
		option.localizedText = localizedText;
		option.sortOrder = sortOrder;
		return option;
	}
	
	// Build lobby data from the slot resource map data.
	public static void populateAll(Dictionary<string, SlotResourceData> map)
	{
		// We always have a "main" and a "vip" lobby.
		// The experiment variant value for each game determines
		// whether the game appears in main lobby, high lobby, or not at all.
		string shutdown_list = Data.login.getString("shutdown_list", "");
		List<string> gamesUnderMaintenanceList = new List<string>(shutdown_list.Split(','));

		Dictionary<string, LobbyInfo> lobbyInfoLookup = new Dictionary<string, LobbyInfo>()
		{
			{LoLaLobby.MOBILE_MAIN, new LobbyInfo(LobbyInfo.Type.MAIN)},
			{LoLaLobby.MOBILE_RICH_PASS, new LobbyInfo(LobbyInfo.Type.RICH_PASS)},
			{LoLaLobby.MOBILE_VIP, new LobbyInfo(LobbyInfo.Type.VIP)},
			{LoLaLobby.MOBILE_LOZ, new LobbyInfo(LobbyInfo.Type.LOZ)},
			{LoLaLobby.MOBILE_VIP_REVAMP, new LobbyInfo(LobbyInfo.Type.VIP_REVAMP)},
			{LoLaLobby.MOBILE_MAX_VOLTAGE, new LobbyInfo(LobbyInfo.Type.MAX_VOLTAGE)},
			{LoLaLobby.MOBILE_SIN_CITY, new LobbyInfo(LobbyInfo.Type.SIN_CITY)},
			{LoLaLobby.MOBILE_SLOTVENTURE, new LobbyInfo(LobbyInfo.Type.SLOTVENTURE)}
		};

		// replaced main lobby with early user lobby
		if (LoLaLobby.main == LoLaLobby.mainEarlyUser)
		{
			lobbyInfoLookup.Add(LoLaLobby.MOBILE_MAIN_EARLY_USER, new LobbyInfo(LobbyInfo.Type.MAIN));
		}
		
		if (LobbyGame.vipEarlyAccessGame == null)
		{
			// If there is no VIP early access game, then we must create a special
			// coming soon option just for the VIP lobby, which always goes in the
			// first slot of the first page.
			// If we've gotten here, then the VIPLevel.allGames list should have
			// a null in the first index, to account for positioning of all the other options
			// due to this empty one.
			LobbyOption eaOption = new LobbyOption();
			eaOption.type = LobbyOption.Type.VIP_EARLY_ACCESS_COMING_SOON;
			eaOption.imageFilename = VIP_COMING_SOON_IMAGE_PATH;
			eaOption.isNormal = true;
			lobbyInfoLookup[LoLaLobby.MOBILE_VIP].unpinnedOptions.Add(eaOption);
			lobbyInfoLookup[LoLaLobby.MOBILE_VIP].allLobbyOptions.Add(eaOption);
		}
		
		if (LobbyGame.vipEarlyAccessGame != null)
		{
			// Create the VIP Early Access game lobby option separately here,
			// since it may not be defined in the VIP lobby (it probably shouldn't be).
			LobbyOption eaOption = new LobbyOption();
			eaOption.type = LobbyOption.Type.GAME;
			eaOption.game = LobbyGame.vipEarlyAccessGame;
			SlotResourceData data = SlotResourceMap.getData(LobbyGame.vipEarlyAccessGame.keyName);
			if (data != null)
			{
				eaOption.imageFilename = SlotResourceMap.getLobbyImagePath(eaOption.game.groupInfo.keyName, eaOption.game.keyName, "1X2");
			}
			eaOption.isNormal = true;
			lobbyInfoLookup[LoLaLobby.MOBILE_VIP].unpinnedOptions.Add(eaOption);
			lobbyInfoLookup[LoLaLobby.MOBILE_VIP].allLobbyOptions.Add(eaOption);
		}

		EosExperiment priorityExp = null;
		if (!string.IsNullOrEmpty(LoLa.priorityGameKey) && !string.IsNullOrEmpty(LoLa.priorityGameExp))
		{
			//find this game and set it pinned at slot 1
			string expName = LoLa.priorityGameExp;
			if (expName.StartsWith("hir_"))
			{
				expName = expName.Substring("hir_".Length);
			}

			priorityExp = ExperimentManager.GetEosExperiment(expName);
		}

		
		//handle rich pass first
		LoLaLobby richPassLobby = null;
		if (LoLaLobby.all.TryGetValue(LoLaLobby.MOBILE_RICH_PASS, out richPassLobby))
		{
			foreach (LoLaLobbyDisplay disp in richPassLobby.displays)
			{
				disp.lobbyKey = LoLaLobby.shouldUseEarlyUserLobby() ? LoLaLobby.MOBILE_MAIN_EARLY_USER : LoLaLobby.MOBILE_MAIN;
				//remove item from silver games
				if (RichPassCampaign.silverGameKeys.Contains(disp.game.game.keyName))
				{
					RichPassCampaign.silverGameKeys.Remove(disp.game.game.keyName);
				}
				//make it gold
				RichPassCampaign.goldGameKeys.Add(disp.game.game.keyName);
			}
		
			//add games to main lobby.
			if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive)
			{
				LoLaLobby mainLobby = LoLaLobby.all[LoLaLobby.shouldUseEarlyUserLobby() ? LoLaLobby.MOBILE_MAIN_EARLY_USER : LoLaLobby.MOBILE_MAIN];
				mainLobby.displays.InsertRange(0, richPassLobby.displays);
			}	
		}
		
		foreach (LoLaLobby lolaLobby in LoLaLobby.all.Values)
		{
			//skip rich pass because we've added it's options to main lobby
			if (lolaLobby.keyName == LoLaLobby.MOBILE_RICH_PASS)
			{
				continue;
			}
			
			foreach (LoLaLobbyDisplay lolaDisplay in lolaLobby.displays)
			{
				SlotResourceData data = null;
				LobbyGame game = null;
				string action = "";
				string gameKey = "";

				LobbyOption option = new LobbyOption();

				if (lolaDisplay.game != null)
				{
					game = lolaDisplay.game.game;
					gameKey = game.keyName;	// Shorthand used in multiple places.
					data = SlotResourceMap.getData(gameKey);
					if (data == null)
					{
						Debug.LogWarning("Could not find SlotResourceData for game " + gameKey);
						continue;
					}

					option.imageFilename = data.lobbyImageFilename;
				}
				else if (lolaDisplay.action != null)
				{
					action = lolaDisplay.action.action;

					// If an action, we need to validate the action instead of a game.
					if (!DoSomething.getIsValidToSurface(action))
					{
						if (action.StartsWith(DoSomething.GAME_PREFIX) && ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
						{
							// This is a special case where we show banners for vip game in the main lobby
							// getIsValidToSurface returned false because vip lobby is not populated yet
							// So we will check if the game exists in the lolalobby data
							string currentAction = action;
							string currentGameId;
							// Separate the gameid from "game:gameid" we dont want to make changes to original string 
							DoSomething.splitActionString(ref currentAction, out currentGameId);

							// now check if the current Game Id is available in vip lobby 
							// Also check if we have the data for it
							LoLaLobby vipLobby;
							if (LoLaLobby.all.TryGetValue(LoLaLobby.MOBILE_VIP_REVAMP, out vipLobby))
							{		
								if (!vipLobby.gamesDict.ContainsKey(currentGameId))
								{
									// skip if game is not found in the vip lobby
									continue;
								}
							}
							else
							{
								// if vip lobby not found, ignore the option
								continue;
							}
						}
						else
						{
							continue;
						}
					}
					else
					{
						if (FILTERED_V3_ACTIONS.Contains(action))
						{
							continue;
						}
					}

					option.type = LobbyOption.Type.ACTION;
					option.action = action;
					option.name = action;
					option.imageFilename = lolaDisplay.action.imagePath;	// Both normal and pinned use the same image path, as defined in LoLa, just in case it's pinned or not.
					lolaDisplay.pinnedLobbyImageFilename = lolaDisplay.action.imagePath;
				}
				else
				{
					Debug.LogWarning("LoLaLobbyDisplay object has no game and no action!");
					continue;
				}

				// early user lobby disabled?
				if (lolaLobby.keyName.Contains("early_user") && !lobbyInfoLookup.ContainsKey(LoLaLobby.MOBILE_MAIN_EARLY_USER))
				{
					continue;
				}

				// Main lobby by default, but may be changed to vipLobby below if it's a game for a different lobby.
				LobbyInfo whichLobby = null;
				
				if (lobbyInfoLookup.TryGetValue(lolaLobby.keyName, out whichLobby))
				{
					if (LoLaLobby.eosControlled.ContainsKey(lolaLobby.keyName) &&
						LoLaLobby.eosControlled[lolaLobby.keyName] == null)
					{
						continue;
					}
				}

				if (whichLobby == null)
				{
					Debug.Log(string.Format("Lobby not supported: {0}", lolaLobby.keyName));
					whichLobby = lobbyInfoLookup[LoLaLobby.MOBILE_MAIN];
				}

				if (action == "")
				{
					// If not an action, validate that it's a game and stuff.
					
					if
					(
						game == null ||
						!game.isActive ||
						game.isVIPEarlyAccess ||
						(game.isDeluxe && !ExperimentWrapper.DeluxeGames.isInExperiment) ||
						(game.xp != null && !game.xp.isVisible) ||
						!game.isAllowedLicense ||
						gamesUnderMaintenanceList.Contains(game.keyName) ||
						!game.isEnabledForLobby
					)
					{
						continue;
					}
					else if (game.eosControlledLobby != null && game.isEOSControlled)
					{
						if (LoLaLobby.find(lolaLobby.keyName) != game.eosControlledLobby)
						{
							// If a game is in an EOS controlled lobby, and we are parsing a different lobby, continue
							continue;
						}
						else
						{
							// currently both the VIP Revamp Lobby, VIP, Max Voltage,  and the LOZ lobby using the same 1X2 images
							option.imageFilename = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName, "1X2");
						}
					}

					// VIP lobby checks for valid vip levels
					if (lolaLobby.keyName.Contains("vip"))
					{
						// It's possible for a non-vip game to be placed in the VIP lobby in LoLa.
						if (game.vipLevel == null)
						{
							Debug.LogWarning("Non-VIP game is in the VIP lobby in LoLa data. Ignoring: " + game.keyName);
							continue;
						}
						else
						{
							option.imageFilename = SlotResourceMap.getLobbyImagePath
							(
								game.groupInfo.keyName
								, game.keyName
								, "1X2"
								, game.isDoubleFreeSpins
							);
						}
					}
					else if (game.vipLevel != null)
					{
						Debug.LogWarning("VIP game is in a non-VIP lobby in LoLa data. Ignoring: " + game.keyName);
						continue;
					}
					// main lobby, basic 1x1
					else if (game.eosControlledLobby == null || !game.isEOSControlled)
					{
						option.imageFilename = data.lobbyImageFilename;
					}

					option.type = LobbyOption.Type.GAME;
					option.game = game;
					option.name = gameKey;

					option.sortOrder = game == LobbyGame.vipEarlyAccessGame ? -1 : lolaDisplay.sortOrder;
				}
			
				/////////////////////////////////////////////////////
				// Set pinned data if applicable for the main lobby
				if (whichLobby == lobbyInfoLookup[LoLaLobby.MOBILE_MAIN] ||
					(lobbyInfoLookup.ContainsKey(LoLaLobby.MOBILE_MAIN_EARLY_USER) && whichLobby == lobbyInfoLookup[LoLaLobby.MOBILE_MAIN_EARLY_USER]))
				{
					setPinnedData(lolaDisplay, game, whichLobby);
				}
				// Done setting pinned data.
				/////////////////////////////////////////////////////
				
				option.isNormal = lolaDisplay.isNormalOption;
				bool isPinned = lolaDisplay.isPinnedOption;
				int pinnedPage = lolaDisplay.pinnedPage;
				int pinnedX = lolaDisplay.pinnedX;
				int pinnedY = lolaDisplay.pinnedY;
				Pinned.Shape pinnedShape = lolaDisplay.pinnedShape;
			
				if (isPinned &&
					pinnedPage > -1 &&
					pinnedX > -1 &&
					pinnedY > -1
					)
				{
					// All the critical properties are specified for a pinned option.
					// Put all the pinned properties under a "pinned" property, so later we know this option is pinned.
				
					if (pinnedY >= MainLobby.MAIN_BUTTON_SPOTS_ROWS_PER_PAGE)
					{
						// Temporary indication that this pinned option has no valid position,
						// so we will find a valid position at the end of the pages after all
						// options have been defined.
						pinnedY = -1;
					}
				
					// Make a copy of the original option to be the pinned option,
					// just in case the option is both pinned and normal.
					LobbyOption pinnedOption = option.copy();
								
					pinnedOption.pinned = new Pinned();
					pinnedOption.isPinned = true;
					pinnedOption.pinned.page = pinnedPage;
					pinnedOption.pinned.x = pinnedX;
					pinnedOption.pinned.y = pinnedY;
					
					if (pinnedShape == Pinned.Shape.NOT_SET) //Pinned multi progressive games use the 1x1 image, which has an empty string name, but have a 1x2 shape so we need to manually set the shape string.
					{
						pinnedOption.pinned.shape = Pinned.Shape.BANNER_1X2;
					}
					else
					{
						pinnedOption.pinned.shape = pinnedShape;
					}
					pinnedOption.pinned.imageFilename = lolaDisplay.pinnedLobbyImageFilename;
				
					if (pinnedY > -1)
					{
						// Add it to the pinned options list.
						whichLobby.addPinnedOption(pinnedOption);
					}
			
					// Add it to the full options list.
					whichLobby.allLobbyOptions.Add(pinnedOption);
				}			
			
				if (option.isNormal)
				{
					// Also add to the normal options list if specified.
					whichLobby.unpinnedOptions.Add(option);
					// Add it to the full options list.
					whichLobby.allLobbyOptions.Add(option);
				}
				
			}
		}
		
		foreach (KeyValuePair<string, LobbyInfo> entry in lobbyInfoLookup)
		{
			entry.Value.organizeOptions();
		}
	}

	protected static void setPinnedData(LoLaLobbyDisplay lolaDisplay, LobbyGame game, LobbyInfo lobbyInfo)
	{
		string pinnedOptionData = "";
		// In LoLa, any game can be pinned now, and special feature games aren't necessarily pinned.
		// So, all we have to do is see if there is a pin column to determine pinning.
		if (lolaDisplay.isPinnedOption)
		{
			int columns = MainLobbyV3.OPTION_COLUMNS_PER_PAGE;
			
			// Since pinColumn data is 1-based instead of 0-based, we must subtract one for this formula.
			// Then a one back on for the page number, since that is also 1-based.
			lolaDisplay.pinnedPage = Mathf.FloorToInt((float)(lolaDisplay.pinColumn - 1) / columns);
			// The column on the page is 0-based here, so subtract one before getting the modulus.
			lolaDisplay.pinnedX = (lolaDisplay.pinColumn - 1) % columns;
			// always 0 for now
			lolaDisplay.pinnedY = 0; 

			if (game != null)
			{
				if (game.isMultiProgressive)
				{
					// Multi-progressive options only have room for the 1X1 option, which is done by passing "" for the shape argument.
					lolaDisplay.pinnedShape = Pinned.Shape.NOT_SET;// force not set for above reason
				}
								
				lolaDisplay.pinnedLobbyImageFilename = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName, Pinned.getFilePathPostFix(lolaDisplay.pinnedShape));

				if (game.xp.isSkuGameUnlock)
				{
					// Special image filename formatting for xpromo game thingy.");
					lolaDisplay.pinnedLobbyImageFilename = lolaDisplay.pinnedLobbyImageFilename.Replace(
						game.keyName + "_1X2.jpg", 
						string.Format(LobbyOptionButtonLearnMore.FILENAME_FORMAT, game.xp.xpromoTarget, game.keyName, 1));
				}
			}
			else if (lolaDisplay.action != null && LOBBY_ACTION_1x1_LIST.Contains(lolaDisplay.action.action))
			{
				lolaDisplay.pinnedShape = Pinned.Shape.STANDARD_1X1;
				
				if (lobbyInfo.pinnedOptions.TryGetValue(lolaDisplay.pinnedPage, out List<LobbyOption> pinnedOptionsOnPage))
				{
					//Check if we have 2 1x1s on the same spot then scoot the second one down
					for (int i = 0; i < pinnedOptionsOnPage.Count; i++)
					{
						if (pinnedOptionsOnPage[i].pinned.x == lolaDisplay.pinnedX &&
						    pinnedOptionsOnPage[i].pinned.y == lolaDisplay.pinnedY &&
						    pinnedOptionsOnPage[i].pinned.shape == Pinned.Shape.STANDARD_1X1 &&
						    lolaDisplay.pinnedShape == Pinned.Shape.STANDARD_1X1)
						{
							lolaDisplay.pinnedY++;
						}
					}
				}
			}
		}
	}
		
	// Returns an option for the gameKey that exists and is active.
	public static LobbyOption activeGameOption(string gameKey)
	{
		LobbyGame game = LobbyGame.find(gameKey);
		
		if (game == null)
		{
			Debug.LogWarning("LobbyOption.activeGameOption: Game key not found: " + gameKey);
			return null;
		}
		
		return activeGameOption(game);
	}
	
	public static LobbyOption activeGameOption(LobbyGame game)
	{
		if (game == null)
		{
			Debug.LogWarning("LobbyOption.activeGameOption: game argument is null" );
			return null;
		}

		var values = LobbyInfo.Type.GetValues(typeof(LobbyInfo.Type));
		foreach ( LobbyInfo.Type type in values )
		{
			LobbyOption option = activeGameOptionInLobby(game, type);

			if ( option != null )
			{
				return option;
			}
		}
		
		return null;
	}
	
	/// Checks a specific lobby for existence of an active game.
	/// whichLobby is "main" or "vip".
	public static LobbyOption activeGameOptionInLobby(LobbyGame game, LobbyInfo.Type whichLobby)
	{
		LobbyInfo lobby = LobbyInfo.find(whichLobby);
		
		if (lobby == null)
		{
			return null;
		}
		
		foreach (LobbyOption option in lobby.allLobbyOptions)
		{
			if (option.game == game)
			{
				return option;
			}
		}
		return null;
	}

	// Helper function for getting the spin count for a LobbyOption.
	public int sortOrderSpinCount
	{
		get
		{
			int spinCount = 0;
			if (game != null && game.xp != null && game.isUnlocked && game.xp.spinCount >= minSpinCountSorting)
			{
				spinCount = game.xp.spinCount;
			}

			return spinCount;
		}
	}
	
	// Sorting method for Sort() call. This is only done for unpinned options.
	public static int sortByUnlock(LobbyOption a, LobbyOption b)
	{
		// move gold games to the end of	
		if (a.isGoldPass && !b.isGoldPass)
		{
			return -1;
		}
		if (b.isGoldPass && !a.isGoldPass)
		{
			return 1;
		}
		
		int aSort = a.sortOrderUnlock;
		int bSort = b.sortOrderUnlock;
		
		if (aSort == bSort)
		{
			// If both sorts are the same based on unlock level,
			// fall back to sort order as defined in SlotResourceMap.
			aSort = a.sortOrder;
			bSort = b.sortOrder;
		}
		
		return aSort.CompareTo(bSort);
	}

	public static int sortByOrder(LobbyOption a, LobbyOption b)
	{
		int aSort = a.sortOrder;
		int bSort = b.sortOrder;
				
		return aSort.CompareTo(bSort);
	}
	
	// The new VIP lobby needs to sort the options since it doesn't use pinned options.
	public static int sortVIPOptions(LobbyOption a, LobbyOption b)
	{
		return a.sortOrderVIP.CompareTo(b.sortOrderVIP);
	}
	
	public int sortOrderVIP
	{
		get
		{
			if (type == LobbyOption.Type.VIP_COMING_SOON)
			{
				// Make sure these are sorted last.
				return 100000;
			}
			
			if (game == null || game.vipLevel == null)
			{
				// Shouldn't happen if properly sorting on a list of LobbyOptions that we already know are VIP.
				return 0;
			}
			
			int order = game.vipLevel.levelNumber;
			
			// Use LoLa sort order as a tie breaker for games that unlock within the same level.
			LoLaLobbyDisplay display = ExperimentWrapper.VIPLobbyRevamp.isInExperiment && LoLaLobby.vipRevamp != null ? 
				LoLaLobby.vipRevamp.findGame(game.keyName) : LoLaLobby.vip.findGame(game.keyName);
			if (display != null)
			{
				order = order * 10 + display.sortOrder;
			}
			
			return order;
		}
	}
	
	// Get the sort order of the pinned option, as defined in LoLa.
	public int sortOrderPinned
	{
		get
		{
			return pinned.page * 10 + pinned.x;
		}
	}
	
	// Sorting method for Sort() call. This is only done for pinned options.
	// This doesn't actually sort them since pinned options are pinned to certain spots,
	// but it gives priority to options if multiple options are pinned to the same spot.
	// Options with lesser priority are forced to find the next available spot.
	public static int sortPinned(LobbyOption a, LobbyOption b)
	{
		return a.sortOrderPinned.CompareTo(b.sortOrderPinned);
	}

	
	// Returns the value used for sorting unpinned options only
	// (except for SIR, where we treat pinned as unpinned too, for manual sorting hack).
	// Priority is:
	// 1. Unlocked games, in order by unlock level (so latest unlocks are first).
	// 2. Locked games, in order by unlock level (so the next unlock is first).
	public int sortOrderUnlock
	{
		get
		{
			if (type == LobbyOption.Type.COMING_SOON)
			{
				// The empty LobbyOption.Type.COMING_SOON options are always last since they just pad the last page.
				return 9999999;
			}
			
			if (game == null)
			{
				// This really shouldn't happen since we should only have unpinned options that are games or COMING_SOON.
				return 0;
			}
			
			if (game.isChallengeLobbyGame)
			{
				// If a Land of Oz game, sort by LoLa sort order.
				LoLaLobbyDisplay display = LoLaLobby.findGameInEOS(game.keyName);
				if (display != null)
				{
					return display.sortOrder;
				}
			}

			const int BASE_SORT_LOCKED = 100000;
			int sort = 0;
			
			// MRCC -- All 1x1's should now be sorted by unlock level only, regardless of whether they are already unlocked.
			sort = BASE_SORT_LOCKED + game.unlockLevel;
			
			return sort;
		}
	}

	public static IEnumerator setupStandaloneCabinet(LobbyOptionButtonActive optionButton, LobbyGame gameInfo, LobbyOption option = null)
	{
		if (option == null)
		{
			 option = LobbyOption.activeGameOption(gameInfo);
		}
		
		if (option == null)
		{
			if (gameInfo == null)
			{
				Debug.LogWarning("Null gameInfo passed in for setupStandaloneCabinet()");
			}
			else
			{
				Debug.LogWarning("No lobby option found for setupStandaloneCabinet()");
			}
			yield break;
		}
		
		bool isSneakPreview = false;

		if (option.game != null)
		{
			isSneakPreview = option.game.isSneakPreview;
			option.game.isSneakPreview = false;	// Temporary set to false to prevent the sneak preview icon from showing on a standalone cabinet.
		}
		
		optionButton.setup(option, 0, 0, 0);

		if (option != null)
		{
			yield return RoutineRunner.instance.StartCoroutine(optionButton.option.loadImages());	// The image should be loaded already by now, but just in case.
			optionButton.setImage();
		}
		else
		{
			Debug.LogWarning("LobbyOption:setupStandaloneCabinet - The created option was null...may want to check your game info..");
		}
		
		// Set it back to what it was.
		if (option.game != null)
		{
			option.game.isSneakPreview = isSneakPreview;
		}		
	}

	// Loads the images for the option, and sets them on the visible button if it exists.
	public IEnumerator loadImages()
	{
		if (type == LobbyOption.Type.COMING_SOON)
		{
			// No image to load for this type.
			yield break;
		}
					
		// Wait one frame to let the button property get assigned before starting to load images.
		yield return null;

		Dict data = null;
		LobbyOptionButtonActive activeButton = button as LobbyOptionButtonActive;
	
		if (activeButton != null && activeButton.image != null)
		{
			// If the option is visible, show a loading indicator.
			data = Dict.create(D.IMAGE_TRANSFORM, activeButton.image.transform);
			
			// since banner images are relatively larger, they might take some time to download. 
			// Hence we use the default image as the placeholder until the actual images are loaded
			if (isBannerAction && !string.IsNullOrEmpty(bannerLoadingImageName))
			{
				Texture2D image = SkuResources.getObjectFromMegaBundle<Texture2D>(bannerLoadingImageName);
				if (image != null)
				{
					optionTextureLoaded(image, data);
					activeButton.setImage();
				}
				// clear the bitmaps to allow it to load actual images
				bitmap = pinnedBitmap = null;
			}
		}

		if ((isNormal || (game != null && game.isMultiProgressive)) &&
			!string.IsNullOrEmpty(imageFilename) &&
			(imageFilename.FastEndsWith(".jpg") || imageFilename.FastEndsWith(".png")))
		{
			yield return RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(imageFilename, optionTextureLoaded, data, skipBundleMapping:true, pathExtension:".png"));
		}
		
		if ((isPinned && (game == null || !game.isMultiProgressive)) &&
			pinned != null &&
			!string.IsNullOrEmpty(pinned.imageFilename) &&
		    (imageFilename.FastEndsWith(".jpg") || imageFilename.FastEndsWith(".png")))
		{
			if (isBannerAction && defaultBanner)
			{
				// keep the default loading image
				yield break;
			}
			else
			{
				yield return RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(
					pinned.imageFilename, optionTextureLoaded, data,
					skipBundleMapping: pinned.shape == Pinned.Shape.NOT_SET, pathExtension: ".png"));
			}
		}
		
		// This is a hack fix because lobby images are not showing up unless they load
		// immediately from the cache. This is because the optionTextureLoaded() callback
		// below is called after the activeButton.setImage() below - somehow.
		// When we have time to figure out why, we should fix it - until then:
		//#warning Hacked fix for DisplayAsset timing issue.
		while (bitmap == null && pinnedBitmap == null)
		{
			yield return null;
		}
		
		// Re-check the button property again after the images have finished loading.
		activeButton = button as LobbyOptionButtonActive;

		if (activeButton != null)
		{
			// If the option is visible, set the loaded image to it immediately.
			activeButton.setImage();
		}
	}

	// An option image has finished loading, so use it.
	private void optionTextureLoaded(Texture2D tex, Dict data)
	{
		if (tex != null)
		{
			if (isPinned && pinned != null)
			{
				pinnedBitmap = tex;
			}
			else
			{
				bitmap = tex;
			}
		}
	}
	
	// This option has been clicked in a lobby.
	public void click() 
	{
		if (Input.touchCount > 1 || !Glb.isNothingHappening || (Scheduler.hasTask && Scheduler.hasTaskCanExecute))
		{
			// Prevent responding to multi touch on multiple lobby options.
			// Also ignore if there is something happening, or a task in the Scheduler.
			// This should only happen if two options are trying to be processed at the same time.
			if (Scheduler.hasTask)
			{
				//Task can't execute but is blocking click.  This is not a temporary problem and will persist
				//Log the name of the task that is stuck and blocking the client
				SchedulerTask task = Scheduler.getNextTask();
				if (task == null)
				{
					if (stuckTask != "null")
					{
						stuckTask = "null";
						string taskInfo = Scheduler.getCurrentTaskInfo();
						SchedulerTask blockingTask = Scheduler.getNextBlockingTask();
						if (!string.IsNullOrEmpty(taskInfo))
						{
							Debug.LogError("Current task has failed to execute: " + System.Environment.NewLine + taskInfo);
						}
						else if (blockingTask != null)
						{
							StringBuilder sb = new StringBuilder();
							sb.Append("A blocking task has failed to execute");
							sb.AppendLine(":");
							sb.Append(blockingTask.ToString());
							Debug.LogError(sb.ToString());
						}
						else if (Dialog.isTransitioning)
						{
							if (Dialog.instance.isClosing)
							{
								Debug.LogError("Scheduler cannot run because dialog" +  Dialog.instance.lastclosedDialogKey + " is attempting to close");
							}
							else if (Dialog.instance.isOpening)
							{
								Debug.LogError("Scheduler cannot run because dialog" +  Dialog.instance.lastOpenedDialogKey + " is attempting to open");	
							}
							else
							{
								Debug.LogError("Scheduler cannot run because a dialog is transitioning");
							}
						}
						else
						{
							Debug.LogError("A null blocking task has failed to execute");
						}

						//Manually try running the scheduler if we have a stuck item or else we have to rely on some other source triggering run()
						Scheduler.run();
						click();
					}
				}
				else
				{
					string taskData = task.ToString();
					if (stuckTask != taskData)
					{
						stuckTask = taskData;
						Debug.LogError(stuckTask);
						
						//Manually try running the scheduler if we have a stuck item or else we have to rely on some other source triggering run()
						Scheduler.run();
						click();
					}
				}
			}
			return;
		}

		switch (type)
		{
			case Type.ACTION:
				DoSomething.now(action);
				break;
			
			case Type.GAME:
				if (game == null)
				{
					// Should never happen, but checking in case.
					Debug.LogError("LobbyOption.type is GAME but the game property is null.");
					return;
				}
				
				if (MainLobby.hirV3 != null)
				{
					MainLobby.hirV3.storeCurrentPageIndex();
				}

				// check if wager data isn't setup yet for this game
				if (!SlotsWagerSets.doesGameHaveWagerSet(game.keyName))
				{
					string errorMsg = "Missing Wager Set for: " + game.keyName + ". Disable \"Use new wager system\" from DevGUI Main tab to load this game.";
					Debug.LogError(errorMsg);
					GenericDialog.showDialog(
							Dict.create(
								D.TITLE, Localize.text("error"),
								D.MESSAGE, errorMsg,
								D.REASON, "lobby-option-missing-wager-set"
							),
							SchedulerPriority.PriorityType.IMMEDIATE
						);
					return;
				}

				SlotAction.setLaunchDetails("lobby_option", lobbyPosition);
				game.askInitialBetOrTryLaunch();
				break;
		}
	}

	/// Handle a refresh of hte button if a data configuration changes while the lobby is visible
	public void refreshButton()
	{
		if (button != null)
		{
			button.refresh();
		}
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		freePreviewLocalizationKey = "";
		baseUrl = "";
		dataVersion = "0";
		minSpinCountSorting = 50;
	}
}
