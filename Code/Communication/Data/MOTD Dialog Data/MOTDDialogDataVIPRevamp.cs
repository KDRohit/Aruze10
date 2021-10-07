using UnityEngine;
using System.Collections;

public class MOTDDialogDataVIPRevamp : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return ExperimentWrapper.VIPLobbyRevamp.isInExperiment;
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (!ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
			{
				reason += "User is not in the VIP Revamp experiment";
			}
			return reason;
		}
	}

	public override bool show()
	{
		LobbyOption option = VIPLobbyHIRRevamp.findLobbyOption(LoLa.vipRevampNewGameKey);
		if (option != null)
		{
			return VIPRevampDialog.showDialog(option, "vip_new_lobby");
		}
		else
		{
			Debug.LogError("Invalid lobby revamp option -- aborting dialog");
			return false;
		}

	}

	new public static void resetStaticClassData()
	{
	}
}
