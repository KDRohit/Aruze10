using UnityEngine;
using System.Collections;

/**
Hide just the charms meter for a game, if you want to hide all meters use HideSpinPanelMetersUIModule.cs
*/
public class HideCharmsMeterModule : SlotModule 
{
	// Use this for initialization
	void Start () 
	{
	}

	public override bool needsToExecuteOnBonusGameEnded()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEnded() 
	{
		yield break;
	}

	public override bool needsToExecuteOnPreBigWin()
	{
		return true;
	}

	public override IEnumerator executeOnPreBigWin()
	{
		yield break;
	}
}
