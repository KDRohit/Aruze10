using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MOTDDialogDataWelcomeJourney : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return !WelcomeJourney.instance.isFirstLaunch &&
				WelcomeJourney.shouldShow();
		}
	}

	public override string noShowReason
	{
		get
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			builder.AppendLine(base.noShowReason);
			if (WelcomeJourney.instance.isFirstLaunch)
			{
				builder.AppendLine("This is the very first game launch.");
			}
			builder.AppendLine(WelcomeJourney.instance.noShowReason);
			return builder.ToString();
		}

	}

	public override bool show()
	{
		return SevenDayWelcomeDialog.showDialog(keyName);
	}

	new public static void resetStaticClassData()
	{
	}
}

