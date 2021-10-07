using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataMFSAskForCredits : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return MainLobby.instance != null && MainLobby.instance.shouldShowMFS;
		}
	}

	public override string noShowReason
	{
		get
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			builder.AppendLine(base.noShowReason);
			if (MainLobby.instance == null)
			{
				builder.AppendLine("The main lobby instance was null.");
			}
			if (!MFSDialog.shouldSurfaceAskForCredits())
			{
				builder.AppendLine("The MFS Dialog has Ask for Credits on cooldown.");
			}
			return builder.ToString();
		}
	}

	public override bool show()
	{
		return MFSDialog.showDialog(MFSDialog.Mode.ASK);
	}

	new public static void resetStaticClassData()
	{
	}
}

