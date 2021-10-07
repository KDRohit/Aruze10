using UnityEngine;
using System.Collections;
using Com.Scheduler;

/*
Controls display and functionality of VIP introduction animations.
*/

public abstract class VIPLevelUpDialog : DialogBase
{
	protected bool isFirstTime = false;
	protected VIPLevel level = null;

	// Initialization
	public override void init()
	{
		isFirstTime = !CustomPlayerData.getBool(CustomPlayerData.SHOWN_VIP_WELCOME_DIALOG, false);
		if (isFirstTime)
		{
			CustomPlayerData.setValue(CustomPlayerData.SHOWN_VIP_WELCOME_DIALOG, true);
		}

		int levelToSearch = (int)dialogArgs.getWithDefault(D.NEW_LEVEL, 0);
		if (VIPStatusBoostEvent.isEnabled())
		{
			levelToSearch += VIPStatusBoostEvent.fakeLevel;
		}

		level = VIPLevel.find(levelToSearch);
		
		// Update the isUnlocked status of the games unlocked at every level up to this level,
		// just in case the player jumped up more than one level at a time.
		for (int i = 0; i <= level.levelNumber; i++)
		{
			VIPLevel levelToUnlock = VIPLevel.find(i);
			if (levelToUnlock != null)
			{
				foreach (LobbyGame game in levelToUnlock.games)
				{
					game.setIsUnlocked();
				}

				if (MainLobbyBottomOverlay.instance != null)
				{
					MainLobbyBottomOverlay.instance.refreshUI();
				}
			}
		}
		
		// Also unlock the early access game if it unlocks at this level or below.
		if (LobbyGame.vipEarlyAccessGame != null)
		{
			LobbyGame.vipEarlyAccessGame.setIsUnlocked();
		}
		
		if (GameState.isMainLobby && VIPLobby.instance != null)
		{
			VIPLobby.instance.refreshUI();
		}

		Audio.switchMusicKeyImmediate("idleHighLimitLobby");
	}
			
	public static bool showWelcomeIfNecessary()
	{
		if (!CustomPlayerData.getBool(CustomPlayerData.SHOWN_VIP_WELCOME_DIALOG, false))
		{
			VIPLevelUpDialog.showDialog(SlotsPlayer.instance.vipNewLevel);
			return true;
		}
		return false;
	}
	
	private void closeClicked()
	{
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		playMusicWhenClosing();

		if (VIPStatusBoostEvent.isEnabled() && !VIPStatusBoostEvent.isEnabledByPowerup())
		{
			VIPStatusBoostMOTD.showDialog();
		}
	}
	
	protected void playMusicWhenClosing()
	{
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.playBgMusic();
		}
		else
		{
			MainLobby.playLobbyMusic();
		}
	}
	
	public static void showDialog(int newLevel)
	{
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame)
		{
			//Disabling this dialog while in a royal rush event game
			return;
		}
		
		Scheduler.addDialog("level_up_vip", Dict.create(D.NEW_LEVEL, newLevel));
	}
}

