using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataSoftwareUpdate : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
#if ZYNGA_KINDLE || ZYNGA_IOS || UNITY_EDITOR
			return
				ExperimentWrapper.SoftwareUpdateDialog.isInExperiment &&
				isValidViewCheckCount &&
				isNewerVersionAvailable;
#else
			return false;
#endif
		}
	}
	
	private bool isValidViewCheckCount
	{
		get
		{
			int checkCount = PlayerPrefsCache.GetInt(Prefs.SOFTWARE_UPDATE_CHECK_COUNT, 0);
			int viewCount = PlayerPrefsCache.GetInt(Prefs.SOFTWARE_UPDATE_VIEW_COUNT, 0);
		
			if ((viewCount > 0 && checkCount < 3) || viewCount > 3)
			{
				// If we have shown the dialog before, and it has not been 3 loads yet,
				// Or if we have shown it three times, then don't show it.
		   		checkCount++;
				PlayerPrefsCache.SetInt(Prefs.SOFTWARE_UPDATE_CHECK_COUNT, checkCount);
				return false;
			}	
			return true;
		}
	}
	
	private bool isNewerVersionAvailable
	{
		get
		{
			string os = SystemInfo.operatingSystem;
			string [] osArgs = os.Split(' ');
			if (osArgs.Length > 3)
			{
				if (osArgs[0].Equals("Android") && osArgs[1].Equals("OS"))
				{
					string osVersion = osArgs[2];
					if (compareVersion(osVersion, SoftwareUpdateDialog.minVersion) < 0)
					{
						// If we correctly grabbed a version, AND it is less than the min version
						return true;
					}
				}
			}
			return false;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;

			if (!ExperimentWrapper.SoftwareUpdateDialog.isInExperiment)
			{
				result += "Not in SoftwareUpdateDialog experiment.\n";
			}
			if (!isValidViewCheckCount)
			{
				result += "Have shown the dialog before, and it has not been 3 loads yet, or we have shown it three times.\n";
			}
			if (!isNewerVersionAvailable)
			{
				result += "Already using the latest version.\n";
			}

			return result;
		}
	}

	// Compare two strings of the format "x.y.z"
	private int compareVersion(string versionOne, string versionTwo)
	{
		string[] splitOne = versionOne.Split('.');
		string[] splitTwo = versionTwo.Split('.');
		for (int i = 0; i< Mathf.Min(splitOne.Length, splitTwo.Length); i++)
		{
			if (splitOne[i] == splitTwo[i])
			{
				continue;
			}
			return float.Parse(splitOne[i]).CompareTo(float.Parse(splitTwo[i]));
		}
		return 0;
	}

	public override bool show()
	{
		return SoftwareUpdateDialog.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
