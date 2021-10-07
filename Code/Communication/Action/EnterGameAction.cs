using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
ServerAction class for handling app-related actions.
*/
public class EnterGameAction : ServerAction
{
	private const string ENTER_GAME = "enter_game";

	private string game = "";
	private string currentLobby = "";

	//property names
	private const string GAME_ID = "game";
	private const string LOBBY_KEY = "lobby_key";

	private EnterGameAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// Tells the server that a given app is installed on the current device.
	public static void gameLaunched(string gameId)
	{
		EnterGameAction action = new EnterGameAction(ActionPriority.HIGH, ENTER_GAME);
		action.game = gameId;
		//action.currentLobby = LobbyInfo.typeToString(LobbyLoader.lastLobby, true);
		if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive &&
		    RichPassCampaign.goldGameKeys.Contains(gameId))
		{
			action.currentLobby = LoLaLobby.MOBILE_RICH_PASS;
		}
		else
		{
			LoLaLobby gameLobby = LoLaLobby.findWithGame(gameId);
			if (gameLobby != null)
			{
				action.currentLobby = gameLobby.keyName;
			}	
		}

		// Send it immediately since we will be eagerly waiting for the event to let the player choose a game.
		ServerAction.processPendingActions(true);
	}

	////////////////////////////////////////////////////////////////////////

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(ENTER_GAME, new string[] {GAME_ID, LOBBY_KEY});
			}
			return _propertiesLookup;
		}
	}
	private static Dictionary<string, string[]> _propertiesLookup = null;

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!propertiesLookup.ContainsKey(type))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}

		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
				case GAME_ID:
					appendPropertyJSON(builder, property, game);
					break;
				case LOBBY_KEY:
					if (!string.IsNullOrEmpty(currentLobby) && Data.liveData.getBool("ENABLE_EARLY_USER_LOBBIES", false))
					{
						appendPropertyJSON(builder, property, currentLobby);
					}
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
		_propertiesLookup = null;
	}
}
