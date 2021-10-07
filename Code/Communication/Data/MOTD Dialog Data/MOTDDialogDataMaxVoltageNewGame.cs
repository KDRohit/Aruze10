using UnityEngine;
using System.Collections;

public class MOTDDialogDataMaxVoltageNewGame : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return LoLaLobby.main != LoLaLobby.mainEarlyUser && 
			       !string.IsNullOrEmpty(LoLa.newMVZGameMotdKey) &&
				   !isNonProductionReadyGame(LoLa.newMVZGameMotdKey) && 
				   SlotsPlayer.instance.socialMember.experienceLevel >= Glb.MAX_VOLTAGE_MIN_LEVEL;
		}
	}

	private bool isNonProductionReadyGame(string gameKey)
	{
#if ZYNGA_PRODUCTION
		bool nonProductionReadyGameMOTD = false;
		SlotResourceData mapData = SlotResourceMap.getData(gameKey);
		if (mapData == null || mapData.gameStatus == SlotResourceData.GameStatus.NON_PRODUCTION_READY) //Don't show new game MOTDs for games that aren't production ready, or not in the build at all
		{
			nonProductionReadyGameMOTD = true;
			Debug.LogWarning("Suppressing a new game MOTD for a non production ready game.");
		}

		//Only don't want to show non production ready new game MOTDs on production builds
		return nonProductionReadyGameMOTD;
#endif

		return false;
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;

			if (LoLaLobby.main == LoLaLobby.mainEarlyUser)
			{
				reason += "Player in mobile_main_early_user\n";
			}
			if (string.IsNullOrEmpty(LoLa.newMVZGameMotdKey))
			{
				reason += "Missing MVZ new game MOTD string in lola";
			}
			if (isNonProductionReadyGame(LoLa.newMVZGameMotdKey))
			{
				reason += "Attempted to use a non production game";
			}


			if (SlotsPlayer.instance.socialMember.experienceLevel < Glb.MAX_VOLTAGE_MIN_LEVEL)
			{
				reason += "User is too low level. Min level is: " + Glb.MAX_VOLTAGE_MIN_LEVEL.ToString();
			}

			return reason;
		}
	}

	public override bool show()
	{
		return MaxVoltageNewGameDialog.showDialog(LoLa.newMVZGameMotdKey, keyName);
	}

	new public static void resetStaticClassData()
	{
	}
}
