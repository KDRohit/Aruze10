using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataMultiProgressive : MOTDDialogData
{
	public override bool shouldShow
	{
		get 
		{ 
			string key = Data.liveData.getString("CURRENT_MULTI_GAME", "");

			// Make sure the key exists, the game exists, and the game is multiprogressive.
			if (key == "")
			{
				return false;
			}
			
			LobbyGame game = LobbyGame.find(key);
			
			return
				game != null &&
				game.isMultiProgressive;
		}
	}
	

	public override string noShowReason
	{
		get
		{
			string key = Data.liveData.getString("CURRENT_MULTI_GAME", "");

			string result = base.noShowReason;

			if (key == "")
			{
				result += "No LiveData value for CURRENT_MULTI_GAME specified.\n";
			}
			else
			{
				LobbyGame game = LobbyGame.find(key);

				if (game == null)
				{
					result += "Game not found for CURRENT_MULTI_GAME LiveData value " + key + ".\n";
				}
				else if (!game.isMultiProgressive)
				{
					result += "Game specified for CURRENT_MULTI_GAME LiveData value is not a multiprogressive game: " + key + ".\n";
				}
			}

			return result;
		}
	}

	public override bool show()
	{
		return MultiProgressiveMOTD.showDialog();
	}

	new public static void resetStaticClassData()
	{
			
	}
}


