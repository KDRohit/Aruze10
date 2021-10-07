using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataDoubleFreeSpins : MOTDDialogData
{
	private bool didSearchForGame = false;

	// Returns the first double free spins game in the list that wasn't also the most recently used for the MOTD.
	public LobbyGame gameToUse
	{
		get
		{
			if (!didSearchForGame)
			{
				foreach (LobbyGame game in LobbyGame.doubleFreeSpinGames)
				{
					if (CustomPlayerData.getInt(CustomPlayerData.DOUBLE_FREE_SPINS_MOTD_LAST_SEEN, 0) != game.keyName.GetHashCode())
					{
						_gameToUse = game;
						break;
					}
				}
				didSearchForGame = true;	// Prevent it from searching multiple times if no game is found.
			}
			return _gameToUse;
		}
	}
	private LobbyGame _gameToUse = null;
	
	public override bool shouldShow
	{
		get
		{
		    return (gameToUse != null &&
					gameToUse.isActive &&
					gameToUse.isEnabledForLobby);
		}	
	}
	
	public override bool show()
	{
		if (gameToUse != null)
		{
			return DoubleFreeSpinsMOTD.showDialog(gameToUse.keyName, keyName);
		}
		return false;
	}

	new public static void resetStaticClassData()
	{
	}
}
