
using UnityEngine;
using System.Collections;

/**
Hide all spin panel meters in a game, i.e. real world rewards meter, path to riches meter, and charms meter

//there are better approaches to hiding specific meters.  This script also never re-enables meters
*/
[System.Obsolete]
public class HideSpinPanelMetersUIModule : SlotModule 
{
	// Use this for initialization
	void Start () 
	{
		//SpinPanel.instance.showFeatureUI(false);
	}

	public override bool needsToExecuteOnBonusGameEnded()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEnded() 
	{
		//SpinPanel.instance.showFeatureUI(false);
		yield break;
	}

	public override bool needsToExecuteOnPreBigWin()
	{
		return true;
	}

	public override IEnumerator executeOnPreBigWin()
	{
		//SpinPanel.instance.showFeatureUI(false);
		yield break;
	}
}
