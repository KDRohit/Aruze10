using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class to implement different ending animations based on what symbol is featured in the
freespin game.  For instance if the wheel slice needs to play a win animation when it lands.

Original Author: Scott Lepthien
Creation Date: 3/14/2018
*/
public class WheelGameAnimateOnSpinCompleteBasedOnFreespinSymbolModule : WheelGameModule 
{
	[SerializeField] private SymbolWheelAnimations[] symbolWheelAnimations;

	[System.Serializable]
	public class SymbolWheelAnimations
	{
		public string symbolName;
		public AnimationListController.AnimationInformationList animationList;
	}

	private SymbolWheelAnimations currentWheelAnimationEntry = null;

	// Execute when the wheel has completed spinning
	public override bool needsToExecuteOnSpinComplete()
	{
		currentWheelAnimationEntry = getWheelAnimationListEntryForBonusName(BonusGameManager.instance.bonusGameName);

		if (currentWheelAnimationEntry != null && currentWheelAnimationEntry.animationList.Count > 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnSpinComplete()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentWheelAnimationEntry.animationList));
	}

	private SymbolWheelAnimations getWheelAnimationListEntryForBonusName(string bonusName)
	{
		for (int i = 0; i < symbolWheelAnimations.Length; i++)
		{
			if (bonusName.Contains(symbolWheelAnimations[i].symbolName))
			{
				return symbolWheelAnimations[i];
			}
		}

		Debug.LogError("WheelGameAnimateOnSpinCompleteBasedOnFreespinSymbolModule.getWheelAnimationListEntryForBonusName() - Unable to find entry for bonusName = " + bonusName);
		return null;
	}
}
