using UnityEngine;
using System.Collections;

/**
NOTE : This class is deprecated now, and what it does is integreated into the basic functionality of freespins in base
if we ever need to do something other than slide the spin panels we can consider adding hooks to change how you both enter and exit freespins in base
for now I'm going to make this module do nothing and warn people to remove it since it already functions correctly without it
*/
public class SlideSpinPanelsForBasegameFreespinsEnd : SlotModule
{
	//We don't want to do this transistion unless we are playing the freespins in the base game
	public override bool needsToExecuteOnBonusGameEnded()
	{
		Debug.LogWarning("SlideSpinPanelsForBasegameFreespinsEnd.needsToExecuteOnBonusGameEnded() - Remove this module, it isn't needed anymore for freespins in base!");
		return false;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		//Slide bottom panels	
		yield return StartCoroutine(SpinPanel.instance.swapSpinPanels(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Right, 0.5f, false));
	}
}
