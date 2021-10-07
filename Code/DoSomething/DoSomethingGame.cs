using UnityEngine;
using System.Collections;
using Zynga.Core.Util;

public class DoSomethingGame : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		string gameKey = parameter;

		if (LobbyLoader.instance == null) 
		{
			// If we are doing this on a new game launch, then use the autoload functionality.
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetString(Prefs.AUTO_LOAD_GAME_KEY, gameKey);
			prefs.Save();

			// From the PMs, when loading into a game directly, we should treat the user's first
			// MainLobby load as a Return to Lobby.
			MainLobby.isFirstTime = false;
			MainLobby.didLaunchGameSinceLastLobby = true;			
		}
		else
		{
			// Otheriwse use the normal load game flow.
			if (!string.IsNullOrEmpty(gameKey))
			{
				LobbyGame game = LobbyGame.find(gameKey);
				if (game != null)
				{
				
					MOTDFramework.queueCallToAction(gameKey);
				}
				else
				{
					Debug.LogError("DoSomething.gameDelegate: game not found for " + gameKey);
				}
			}
			else
			{
				Debug.LogError("DoSomething.gameDelegate: game key is empty.");
			}
		}
	}
	
	public override bool getIsValidParameter(string parameter)
	{
		return (LobbyOption.activeGameOption(parameter) != null);
	}
}
