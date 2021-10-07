using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataDynamicMOTDV2 : MOTDDialogData
{
	public static bool hasShown = false;
	private static List<string> readyTemplates;
	public override bool shouldShow
	{
		get
		{ 
			readyTemplates = DynamicMOTDFeature.instance.getReadyDialogs();
			return DynamicMOTDFeature.instance.isEnabled && readyTemplates.Count > 0;
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;

			if (!DynamicMOTDFeature.instance.isEnabled) 
			{
				reason += " Dynamic MOTD V2 was not enabled ";
			}

			if (readyTemplates != null && readyTemplates.Count == 0)
			{
				reason += " Dynamic MOTD V2 did not have a valid template";
			}

			return reason;
		}
	}

	public override bool show()
	{
		// we cache this in the shouldShow call, but in case some become ready later on we ought to do it here too as heavy as it is.
		readyTemplates = DynamicMOTDFeature.instance.getReadyDialogs();
		if (readyTemplates.Count > 0)
		{
			for (int i = 0; i < readyTemplates.Count; i++)
			{
				DynamicMOTD.showDialog("dynamic_motd_v2", readyTemplates[i]);
			}

			return true;
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("Tried to show dynamic MOTD without a valid template!");
			return false;
		}
	}

	new public static void resetStaticClassData()
	{
		readyTemplates = null;
	}

}
