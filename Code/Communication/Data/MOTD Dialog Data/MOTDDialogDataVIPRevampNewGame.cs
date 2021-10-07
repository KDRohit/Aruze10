using UnityEngine;
using System.Collections;

public class MOTDDialogDataVIPRevampNewGame : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return 	LoLaLobby.main != LoLaLobby.mainEarlyUser && 
			        ExperimentWrapper.VIPLobbyRevamp.isInExperiment &&
					!string.IsNullOrEmpty(LoLa.vipRevampNewGameKey);
		}
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
			if (!ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
			{
				reason += "User is not in the VIP Revamp experiment";
			}
			return reason;
		}
	}

	public override bool show()
	{
		return VIPRevampNewGameDialog.showDialog("vip_new_lobby_game");
	}

	new public static void resetStaticClassData()
	{
	}
}
