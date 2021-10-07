using UnityEngine;
using System.Collections;
using System;


/**
Early Access Dialog
**/

public abstract class EarlyAccessDialog : DialogBase
{
	public override void init()
	{
		MOTDFramework.markMotdSeen(dialogArgs);
        
        if (LobbyGame.vipEarlyAccessGame != null)
        {
			PlayerPrefsCache.SetString(Prefs.EARLY_ACCESS_RECENT, LobbyGame.vipEarlyAccessGame.keyName);
			PlayerPrefsCache.Save();
			
		    StatsManager.Instance.LogCount("dialog", "new_game_early", "", "", LobbyGame.vipEarlyAccessGame.keyName, "view");
        }
	}
		
	public void clickClose()
	{
		Dialog.close();
	}
	
	public void clickVIPRoom()
	{
		Dialog.close();
		// Use the scripting system so that the VIP room navigation actually happens after all dialogs are closed.
		DoSomething.now("vip_room");
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
         if ( LobbyGame.vipEarlyAccessGame != null)
         {
		    StatsManager.Instance.LogCount("dialog", "new_game_early", "", "", LobbyGame.vipEarlyAccessGame.keyName, "close");
         }
	}

	public static bool showDialog(string motdKey = "")
	{
		Dict args = Dict.create(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey
		);
		
		string filename = SlotResourceMap.getLobbyImagePath(LobbyGame.vipEarlyAccessGame.groupInfo.keyName, LobbyGame.vipEarlyAccessGame.keyName);

		Dialog.instance.showDialogAfterDownloadingTextures("early_access",nonMappedBundledTextures:new string[]{filename}, args:args);
		return true;
	}
}
