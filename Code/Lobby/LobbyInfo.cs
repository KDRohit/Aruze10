using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Holds info about a lobby.
*/

public class LobbyInfo : IResetGame
{
	// The different kinds of lobbies available.
	public enum Type
	{
		UNDEFINED,
		MAIN,
		VIP,
		VIP_REVAMP,
		LOZ,
		MAX_VOLTAGE,
		SIN_CITY,
		SLOTVENTURE,
		RICH_PASS
	}
	
	public Type type = Type.UNDEFINED;
	
	public Dictionary<int, List<LobbyOption>> pinnedOptions = new Dictionary<int, List<LobbyOption>>();
	public List<LobbyOption> allLobbyOptions = new List<LobbyOption>();
	public List<LobbyOption> unpinnedOptions = new List<LobbyOption>();
	public HashSet<string> pinnedActions { get; private set; }

	// Stores which spots have pinned options on each page.
	// The Dictionary is indexed on the page's slot position.
	public List<Dictionary<int, bool>> pinnedSpots = new List<Dictionary<int, bool>>();

	// Pre-calculate the first option index on each page, based on the pinned options on previous pages.			
	// Page 0 always starts with option 0.
	public List<int> pageStartingIndexes = new List<int>();
	
	public int pages;	// Number of pages of options in the lobby.

	// Stores the page and x value of the last pinned option,
	// so we can manually pin orphaned 1x1 options after it.
	public int maxPinnedPage = 0;
	public int maxPinnedX = 0;
	
	private static Dictionary<LobbyInfo.Type, LobbyInfo> allLobbies = new Dictionary<LobbyInfo.Type, LobbyInfo>();

	public LobbyInfo(Type type)
	{
		this.type = type;
		pinnedActions = new HashSet<string>();
		if (allLobbies.ContainsKey(type))
		{
			// Clobber already existing lobby of same type, but also complain about it.
			allLobbies[type] = this;
			Debug.LogWarningFormat("LobbyInfo() - Multiple definitions of lobby type {0}", type);
		}
		else
		{
			allLobbies.Add(type, this);
		}
	}

	public void removeLobbyOption(LobbyOption option)
	{
		if (option == null)
		{
			Debug.LogWarning("Trying to remove invalid option from lobby");
			return;
		}
		
		int page = option.page;
		List<LobbyOption> pageOptions;
		if (pinnedOptions != null)
		{
			if (pinnedOptions.TryGetValue(page, out pageOptions))
			{
				int indexToRemove = -1;
				for (int i = 0; i < pageOptions.Count; i++)
				{
					if (pageOptions[i] == null)
					{
						continue;
					}
					
					if (option == pageOptions[i])
					{
						indexToRemove = i;
						break;
					}
				}

				if (indexToRemove >= 0 && indexToRemove < pageOptions.Count)
				{
					pageOptions.RemoveAt(indexToRemove);
				}
			}	
		}

		if (unpinnedOptions != null)
		{
			LobbyOption toBeRemoved = null;
			foreach (LobbyOption op in unpinnedOptions)
			{
				if (op.game != null)
				{
					if (op.game.keyName == option.game.keyName)
					{
						toBeRemoved = op;
						break;
					}
				}
			}

			if (toBeRemoved != null)
			{
				unpinnedOptions.Remove(toBeRemoved);
			}
		}
	}
			
	// Adds a pinned option to this lobby's Dictionary for the given page.
	public void addPinnedOption(LobbyOption option)
	{
		Pinned pinned = option.pinned;	// shorthand
		pinned.spots = new List<Vector2int>();

		for (int y = 0; y < pinned.height; y++)
		{
			for (int x = 0; x < pinned.width; x++)
			{
				if (pinned.page >= maxPinnedPage)
				{
					maxPinnedX = Mathf.Max(maxPinnedX, x + pinned.x);
					maxPinnedPage = Mathf.Max(maxPinnedPage, pinned.page);
				}
				 //if (option.game != null)
				 //{
				 	//Debug.LogWarning(type + " adding pinned spot: " + option.game.name + ", " + pinned.page + ", " + (x + pinned.x) + ", " + (y + pinned.y));
				 //}
				pinned.spots.Add(new Vector2int(x + pinned.x, y + pinned.y));
			}
		}

		if (!pinnedOptions.ContainsKey(pinned.page))
		{
			pinnedOptions.Add(pinned.page, new List<LobbyOption>());
		}
		pinnedOptions[pinned.page].Add(option);

		// Sort the pinned options for this page so that higher ranked options
		// take priority when there is a conflict for pinned space.
		pinnedOptions[pinned.page].Sort(LobbyOption.sortPinned);

		if (!string.IsNullOrEmpty(option.action))
		{
			pinnedActions.Add(option.action);
		}
	}


	// Process the options to determine page count and used pinned spots on each page.
	public void organizeOptions()
	{
		List<LobbyOption> optionsToReorganize = new List<LobbyOption>();
		int maxPinnedPage = 0;
		int totalPinnedSpots = 0;
	
		// Clear these collections just in case we're re-processing after dynamically adding or moving pinned options.
		pinnedSpots.Clear();
		pageStartingIndexes.Clear();
	
		// Count up the number of spots the pinned options take up, so we can get an accurate page count.
		foreach (KeyValuePair<int, List<LobbyOption>> kvp in pinnedOptions)
		{
			List<LobbyOption> pinPageOptions = kvp.Value;
			foreach (LobbyOption option in pinPageOptions)
			{
				totalPinnedSpots += option.pinned.spots.Count;
				maxPinnedPage = Mathf.Max(maxPinnedPage, option.pinned.page);
			}
		}
		
		int spotsPerRow = MainLobby.MAIN_BUTTON_SPOTS_PER_ROW;
		int spotsPerPage = MainLobby.MAIN_BUTTON_SPOTS_PER_PAGE;
		
		if (type == Type.VIP || type == Type.VIP_REVAMP || type == Type.MAX_VOLTAGE)
		{
			spotsPerRow = MainLobby.VIP_BUTTON_SPOTS_PER_ROW;
			spotsPerPage = MainLobby.VIP_BUTTON_SPOTS_PER_PAGE;
		}

		spotsPerRow = MainLobbyV3.OPTION_COLUMNS_PER_PAGE;
		spotsPerPage = MainLobbyV3.TOTAL_OPTIONS_PER_PAGE;
	
		pages = Mathf.Max(maxPinnedPage + 1, Mathf.CeilToInt((unpinnedOptions.Count + totalPinnedSpots) / (float)spotsPerPage));
					
		if (type == Type.MAIN)
		{
			// Pad the last page with empty LobbyOption.Type.COMING_SOON slots so we don't get partial-page navigation at the end.
			// Also, even though SIR doesn't use those COMING SOON options, we still need to pad them here just in case there
			// are empty spaces between pinned options, which automatically get removed by the SIR main lobby menu.
			while (unpinnedOptions.Count + totalPinnedSpots < pages * spotsPerPage)
			{
				LobbyOption option = new LobbyOption();
				option.type = LobbyOption.Type.COMING_SOON;
				option.name = "coming soon";
				option.isNormal = true;
				option.sortOrder = 9999999;
				unpinnedOptions.Add(option);
			}
		}
	
		// Must do another loop for each page after determining the page count above.		
		for (int i = 0; i < pages; i++)
		{
			pinnedSpots.Add(new Dictionary<int, bool>());	// Each page has a dictionary, even if there are no pinned options on this page.
		
			if (pinnedOptions.ContainsKey(i))
			{
				List<LobbyOption> mutableList = new List<LobbyOption>(pinnedOptions[i]);
				
				foreach (LobbyOption option in mutableList)
				{
					// Use a temporary list to remember what spots are pinned if all turns out to be good.
					List<int> pinnedSpotsToAdd = new List<int>();
					bool isGood = true;
					
					foreach (Vector2int pinned in option.pinned.spots)
					{
						int posIndex = pinned.x + pinned.y * spotsPerRow;
						
						pinnedSpotsToAdd.Add(posIndex);
					
						if (pinnedSpots[i].ContainsKey(posIndex))
						{
							Debug.LogWarning("The lobby option for " + option.name + " on page " + i + ". is overlapping another pinned option.\n" +
								"Make sure that none of the options are at the same position.\n" +
								"Make sure none of the options overlap (eg if they're supposed to be 1x1, but they're 1x2)."
							);

							optionsToReorganize.Add(option);
							
							// Remove it from its old position in the collection.
							pinnedOptions[option.pinned.page].Remove(option);
							
							isGood = false;
							break;
						}
					}

					if (isGood)
					{
						// Add the new pinned spots to the overall list so we know they're now used.
						foreach (int posIndex in pinnedSpotsToAdd)
						{ 
							//	Debug.LogWarning(keyName + " adding pinned spot to page " + i + ", " + posIndex);
							if (!pinnedSpots[i].ContainsKey(posIndex))
							{
								pinnedSpots[i].Add(posIndex, true);
							}
						}
					}
				}
			}
		}

		// Pre-calculate the first option index on each page, based on the pinned options on previous pages.			
		// Page 0 always starts with option 0.
		pageStartingIndexes.Add(0);

		for (int p = 1; p < pages; p++)
		{
			int pinCount = 0;
		
			for (int i = 0; i < p; i++)
			{
				if (pinnedOptions.ContainsKey(i))
				{
					foreach (LobbyOption option in pinnedOptions[i])
					{
						pinCount += option.pinned.spots.Count;
					}
				}
			}
		
			pageStartingIndexes.Add(spotsPerPage * p - pinCount);
		}

		// Sort the unpinned options.
		unpinnedOptions.Sort(LobbyOption.sortByUnlock);
		
		if (optionsToReorganize.Count > 0)
		{
			// After everything else has been organized, now find a new spot
			// for the pinned options that were overlapping other pinned options.
			// Start with the position to the right of the originally defined position.

			foreach (LobbyOption option in optionsToReorganize)
			{
				int limiter = 0;	// Make sure we don't get into an infinite loop if something goes wrong.
				while (limiter < 1000)
				{
					option.pinned.x++;	// Move it to the right by one column.
					// Validate that the option would not be off the page to the right.
					// If so, then move it to the next page in column 0.
					if (option.pinned.x + option.pinned.width - 1 >= spotsPerRow)
					{
						option.pinned.x = 0;
						option.pinned.page++;

						if (pinnedSpots.Count < option.pinned.page + 1)
						{
							// Each page has a dictionary, even if there are no pinned options on this page.
							pinnedSpots.Add(new Dictionary<int, bool>());
						}
					}
					
					// Add it then validate it, since adding it defines the pinned spots for it.
					addPinnedOption(option);

					// Use a temporary list to remember what spots are pinned if all turns out to be good.
					List<int> pinnedSpotsToAdd = new List<int>();
					
					bool isGood = true;
			
					foreach (Vector2int pinned in option.pinned.spots)
					{
						int posIndex = pinned.x + pinned.y * spotsPerRow;
						pinnedSpotsToAdd.Add(posIndex);
					
						if (pinnedSpots[option.pinned.page].ContainsKey(posIndex))
						{
							// This spot is already taken, so don't bother looking for more overlapping positions.
							isGood = false;
							break;
						}
					}
					
					if (isGood)
					{							
						// Add the new pinned spots to the overall list so we know they're now used.
						foreach (int posIndex in pinnedSpotsToAdd)
						{
							if (!pinnedSpots[option.pinned.page].ContainsKey(posIndex))
							{
								pinnedSpots[option.pinned.page].Add(posIndex, true);
							}
						}
					
						Debug.LogWarning("Successfully repositioned pinned option " + option.name + " to page " + option.pinned.page + ", " + option.pinned.x + ", " + option.pinned.y);

						// If still good after validation, then stop looking.
						break;
					}
					else
					{
						// Not good, so remove the option from the pinned list.
						pinnedOptions[option.pinned.page].Remove(option);
					}
											
					limiter++;
				}
				
			}
			
			organizeOptions();
		}
	}

	/// Refresh all of the buttons if something from data would change them while they are showing (for instance a setting flag in DevGUI)
	public static void refreshAllLobbyOptionButtons()
	{
		if (Glb.isResetting)
		{
			// Lets not do anything if we are resetting the game.
		    return;
		}		
		processAllLobbyOptions((LobbyOption option) =>
			{
				option.refreshButton();
			});
	}

	// only used by LikelyToLapse feature
	public static void setupAllLobbyOptionButtonsForLTL()
	{
		if (Glb.isResetting)
		{
			// Lets not do anything if we are resetting the game.
		    return;
		}		
		processAllLobbyOptions((LobbyOption option) =>
			{
				if (option.button != null)
					option.button.setupLTL(option);
			});
	}

	// this is currently used by LinkedVIP to update the VIP Lobby when Linked VIP "vip_status" message increases the player VIP level
	public static void updateLobbyOptionsUnlockedState(LobbyInfo.Type lobbyType)
	{
		if (Glb.isResetting)
		{
			// Lets not do anything if we are resetting the game.
			// Note: Glb.isResetting is set to false inside Glb.reinitializeGame() when it is called by
			// DestructionScript just before calling resetStaticClassData() on IResetGame instances and
			// loading STARTUP_LOGIC_SCENE
		    return;
		}
		
		LobbyInfo lobby = LobbyInfo.find(lobbyType);

		if (lobby == null)
		{
			return;
		}

		foreach (LobbyOption option in lobby.allLobbyOptions)
		{
			if(option!=null && option.game!=null)
			{
				option.game.setIsUnlocked();    // for VIP Lobby, this will unlock based on comparing player's vip level to game's
			}
		}
	}

	delegate void ProcessLobbyOptionDelegate(LobbyOption option);
	private static void processAllLobbyOptions(ProcessLobbyOptionDelegate processLobbyOption)
	{
		if (Glb.isResetting)
		{
			// Lets not do anything if we are resetting the game.
		    return;
		}
		
		foreach (KeyValuePair<LobbyInfo.Type, LobbyInfo> entry in allLobbies)
		{
			foreach (LobbyOption option in entry.Value.allLobbyOptions)
			{
				processLobbyOption(option);
			}
		}
	}

	public static LobbyInfo find(LobbyInfo.Type type)
	{
		LobbyInfo info;
		if (allLobbies.TryGetValue(type, out info))
		{
			return info;
		}

		if (type != Type.UNDEFINED) //Don't log breadcrumb if we didn't find the UNDEFINED lobby. This one will never exist in LOLA
		{
			//Bugsnag.LeaveBreadcrumb("LobbyInfo " + type.ToString() + " Not found");
		}
		
		return null;
	}

	public static LobbyInfo findChallengeLobbyWithGame(string gameKey)
	{
		foreach (KeyValuePair<LobbyInfo.Type, LobbyInfo> lobbyInfo in allLobbies)
		{
			Type type = lobbyInfo.Key;
			if (type != Type.MAIN && type != Type.VIP && type != Type.VIP_REVAMP)
			{
				foreach (LobbyOption option in lobbyInfo.Value.allLobbyOptions)
				{
					if (option.game != null && option.game.keyName == gameKey)
					{
						return lobbyInfo.Value;
					}
				}
			}
		}
		return null;
	}

	public static string typeToString(Type lobbyType, bool useFullName = false)
	{
		string prefix = useFullName ? "mobile_" : "";

		switch(lobbyType)
		{
			case Type.LOZ:
				return prefix + "loz";

			case Type.MAX_VOLTAGE:
				return prefix + "max_voltage";

			case Type.SIN_CITY:
				return prefix + "sin_city";

			case Type.VIP_REVAMP:
			case Type.VIP:
				return prefix + "vip_revamp";

			case Type.SLOTVENTURE:
				return prefix + "slot_venture";
			
			case Type.RICH_PASS:
				return prefix + "gold_pass";

			default:
				if (LoLaLobby.main == LoLaLobby.mainEarlyUser)
				{
					return prefix + "main_early_user";
				}
				return prefix + "main";

		}
	}

	public static string currentTypeToString
	{
		get
		{
			return typeToString(LobbyLoader.lastLobby);
		}
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		allLobbies = new Dictionary<LobbyInfo.Type, LobbyInfo>();
	}

}
