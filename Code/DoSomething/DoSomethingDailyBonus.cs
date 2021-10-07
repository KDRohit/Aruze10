using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoSomethingDailyBonus : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		switch (parameter)
		{
			case "timer":
				//Do Nothing
				break;
			
			default:
				goToLobby();
				break;
		}
	}
	
	public override bool shouldCloseInbox(string parameter)
	{
		switch (parameter)
		{
			case "timer":
				return false;
			
			default:
				return true;
		}
	}

	private void goToLobby()
	{
		LobbyInfo.Type targetLobby = LobbyInfo.Type.MAIN;
		if (LobbyLoader.lastLobby == targetLobby && GameState.isMainLobby)
		{
			return;
		}
        
		Overlay.instance.hideShroud();

		// Since this can be called from the canvas, we need to do some validation that it's ok to do something.
		if (!GameState.isMainLobby)
		{
			GameState.pop();
		}

		NGUIExt.disableAllMouseInput();
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
		LobbyLoader.lastLobby = targetLobby;
		//This can be accessed through the game, so it is following the same pattern as existing from the game to go to the main lobby.
		Glb.loadLobby();

		// HIR-6846.  If the slot music is faded out, then stop it
		// to make sure it doesn't play for a second on the way back to the lobby.
		if (Audio.currentMusicPlayer != null && Audio.currentMusicPlayer.isPlaying)
		{
			Audio.switchMusicKeyImmediate("");
		}
		
		Audio.stopAll();
		Audio.removeDelays();
		Audio.listenerVolume = Audio.maxGlobalVolume;
		Audio.play("return_to_lobby");
	}
	
	public override GameTimer getTimer(string parameter)
	{
		if (SlotsPlayer.instance.dailyBonusTimer != null && !SlotsPlayer.instance.dailyBonusTimer.isExpired)
		{
			return SlotsPlayer.instance.dailyBonusTimer;
		}
		return null;
	}

	public override bool getIsValidToSurface(string parameter)
	{
		if (SlotsPlayer.instance.dailyBonusTimer != null)
		{
			switch (parameter)
			{
				case "timer":
					return !SlotsPlayer.instance.dailyBonusTimer.isExpired;
				
				default:
					return SlotsPlayer.instance.dailyBonusTimer.isExpired;
			}
		}

		return false;
	}
}
