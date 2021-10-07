using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
This class stores data about how a game is displayed in a particular lobby.
*/

public class LoLaLobbyDisplay
{
	public LoLaGame game = null;		// If used, action is null.
	public LoLaAction action = null;	// If used, game is null.
	public int sortOrder = 0;
	public int pinColumn = 0;
	public string lobbyKey = "";
	// The pinned option data is defined in zRuntime and set in LobbyOption.populateAll().
	// You shouldn't hard code it in the client.
	public bool isNormalOption = true;	// The lobby card will be automatically positioned. All options use this unless overridden by pinned data.
	public bool isPinnedOption = false; // You need to specify which page and slot the lobby card belongs in.
	public int pinnedPage = -1;			// This is 0-based, so page 0 is the first page.
	public int pinnedX = -1;
	public int pinnedY = -1;
	public Pinned.Shape pinnedShape = Pinned.Shape.NOT_SET;
	public string pinnedLobbyImageFilename = "";
	public LoLaGame.Feature feature { get; private set; }
	public LoLaGame.UnlockMode originalUnlockMode = LoLaGame.UnlockMode.UNLOCK_BY_LEVEL;
	public LoLaGame.UnlockMode unlockMode = LoLaGame.UnlockMode.UNLOCK_BY_LEVEL;
	public LoLaGame.UnlockMode fallbackMode = LoLaGame.UnlockMode.UNLOCK_BY_LEVEL;	// Since sneak preview is time-limited, this is the mode to use if time has expired.
	public int vipUnlockLevel = 0;
	
	// Constructor for a game-based option.
	public LoLaLobbyDisplay(JSON json, LoLaGame game)
	{
		feature = LoLaGame.Feature.NONE;
		this.game = game;
		LoLaLobby lobby = processJson(json);

		if (lobby != null)
		{
			if (!lobby.gamesDict.ContainsKey(game.game.keyName))
			{
				lobby.gamesDict.Add(game.game.keyName, this);

				// LoLaLobby.loz will only be non-null if Land Of Oz is active, so no need to check LOZCampaign.isActive again here.
				if (LoLaLobby.eosControlled.ContainsValue(lobby))
				{
					game.game.eosControlledLobby = lobby;
					game.game.groupInfo.clickSound = lobby.getClickOverride() != null ? lobby.getClickOverride() : game.game.groupInfo.clickSound;
				}
			}
			else
			{
				Debug.LogWarning("Trying to add a duplicate game " + game.game.keyName + " into lobby " + lobby.keyName);
			}
		}
	}
	
	// Constructor for an action-based option.
	public LoLaLobbyDisplay(JSON json, LoLaAction action)
	{
		this.action = action;
		processJson(json);
	}

	private LoLaLobby processJson(JSON json)
	{
		lobbyKey = json.getString("lobby_key", "");
		LoLaLobby lobby = LoLaLobby.find(lobbyKey);
		if (lobby == null)
		{
			Debug.LogError("LobbyDisplay Lobby not found: " + json.getString("lobby_key", "") + " for game " + game.game.keyName);
			return null;
		}
		
		lobby.displays.Add(this);

		sortOrder = json.getInt("sort_order", 0);
		pinColumn = json.getInt("pin_column", 0);
		
		if ( pinColumn > 0 && lobby != LoLaLobby.vip) {
			isPinnedOption = true;
			pinnedShape = Pinned.Shape.BANNER_1X2; // set the default pinned shape
		}

		//isNormalOption = !isPinnedOption; //BY 2017-03-21: commenting this out, PMs want 1x1s in for pinned options as well
		isNormalOption = action == null; // do not create normal options for lola action types

		string unlockString = json.getString("unlock_mode", "").ToUpper();
		string featureString = json.getString("extra_feature", "none").ToUpper();
		string fallbackModeString = "";

		if (unlockString.Contains("->"))
		{
			string[] parts = unlockString.Split(new string[] { "->" }, System.StringSplitOptions.RemoveEmptyEntries);
			unlockString = parts[0];
			fallbackModeString = parts[1];
		}

		if (fallbackModeString != "")
		{
			if (System.Enum.IsDefined(typeof(LoLaGame.UnlockMode), fallbackModeString))
			{
				fallbackMode = (LoLaGame.UnlockMode)System.Enum.Parse(typeof(LoLaGame.UnlockMode), fallbackModeString);
			}
		}

		if (game != null && System.Enum.IsDefined(typeof(LoLaGame.Feature), featureString))
		{
			feature = (LoLaGame.Feature) System.Enum.Parse(typeof(LoLaGame.Feature), featureString);
		}
		else if (action != null) {
			feature = LoLaGame.Feature.NONE;
			if (System.Enum.IsDefined(typeof(Pinned.Shape), featureString))
			{
				// For actions, extra_feature indicates the size
				pinnedShape = (Pinned.Shape) System.Enum.Parse(typeof(Pinned.Shape), featureString);
			}
		}
		else
		{
			feature = LoLaGame.Feature.NONE;
			Debug.LogWarningFormat("LoLaGame extra_feature {0} is invalid in data.", featureString);
		}


		if (!string.IsNullOrEmpty(unlockString) && System.Enum.IsDefined(typeof(LoLaGame.UnlockMode), unlockString))
		{
			unlockMode = (LoLaGame.UnlockMode)System.Enum.Parse(typeof(LoLaGame.UnlockMode), unlockString);

			switch (unlockMode)
			{
				case LoLaGame.UnlockMode.UNLOCK_BY_VIP_LEVEL:
					if (game != null)
					{
						vipUnlockLevel = json.getInt( "vip_unlock_level", 0 );
						game.game.vipLevel = VIPLevel.find(vipUnlockLevel);
					}
					break;
				
				case LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS:
					if (game != null)
					{
						game.unlockMode = LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS;
					}
					break;
				
				case LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS:
					if (game != null)
					{
						game.unlockMode = LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS;
					}
					break;
			}
		}

		if (game != null)
		{
			vipUnlockLevel = json.getInt( "vip_unlock_level", 0 );
		}
		
		return lobby;
	}
	
	public static int sortByOrder(LoLaLobbyDisplay a, LoLaLobbyDisplay b)
	{
		return a.sortOrder.CompareTo(b.sortOrder);
	}
}