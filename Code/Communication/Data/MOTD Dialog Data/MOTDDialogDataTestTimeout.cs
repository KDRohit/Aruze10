using UnityEngine;
using System.Collections;

public class MOTDDialogDataTestTimeout : MOTDDialogData
{
	private const float TIME_TO_WAIT = 31f;

	public MOTDDialogDataTestTimeout()
	{
		this.keyName = "test_timeout_motd";
		this.sortIndex = 0;
		// New MOTD Framework Setup
		this.shouldShowAppEntry = true;
		this.shouldShowRTL = true;
		this.shouldShowVip = true;
		this.shouldShowPreLobby = true;
	}
	
	public override bool shouldShow
	{
		get
		{
			return true;
		}
	}

	public override string noShowReason
	{
		get
		{
			return base.noShowReason;
		}
	}

	public override bool show()
	{
		RoutineRunner.instance.StartCoroutine(waitingRoutine());
		return true;
	}

	private IEnumerator waitingRoutine()
	{
		Debug.LogFormat("MOTDDialogDataTestTimeout.cs -- waitingRoutine -- routine is kicked off!");
		float timeWaited = 0f;
		while (timeWaited < TIME_TO_WAIT)
		{
			timeWaited += 1.0f;
			yield return new WaitForSeconds(1.0f);
		}
		Debug.LogFormat("MOTDDialogDataTestTimeout.cs -- waitingRoutine -- routine has waited {0} seconds, finished!!", timeWaited);
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{

	}
}
