using UnityEngine;
using System.Collections;

public class DoSomethingNextUnlock : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		LobbyGame nextUnlockGame = LobbyGame.getNextUnlocked(SlotsPlayer.instance.socialMember.experienceLevel);
		if (nextUnlockGame != null)
		{
			DoSomething.now(DoSomething.GAME_PREFIX, nextUnlockGame.keyName);
		}
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return (LobbyGame.getNextUnlocked(SlotsPlayer.instance.socialMember.experienceLevel) != null);
	}
}
