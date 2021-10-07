using UnityEngine;
using System.Collections;

/// <summary>
/// Module for handling sliding of spin panel to free spin panel and back when the free spins are NOT played in the base game.
/// </summary>
public class SpinPanelFreeSpinsSlideModule : SlotModule
{
	public bool slideToFreespins = true;
	public bool slideToBase = true;
	
	public override bool needsToExecuteBeforeFreeSpinsIsShown()
	{
		return true;
	}

	public override IEnumerator executeBeforeFreeSpinsIsShown()
	{
		if (slideToFreespins)
		{
			yield return StartCoroutine(SpinPanel.instance.swapSpinPanels(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Left, 0.5f, false));
		}
		else
		{
			SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.FREE_SPINS);
			SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
		}
	}

	//We don't want to do this transistion unless we are playing the freespins in the base game
	public override bool needsToExecuteOnBonusGameEndedSync()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEndedSync()
	{
		if (slideToBase)
		{
			//Slide bottom panels	
			yield return StartCoroutine(SpinPanel.instance.swapSpinPanels(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Right, 0.5f, false));
		}
		else
		{
			SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
			SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
		}
	}
}

