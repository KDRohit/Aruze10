using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controller for PickingGameMultiBadWithMeterModule and PickingGameMultiBadAndCreditsWithMeterModule
*/
[System.Serializable]
public class PickingGameMultiBadWithMeterModuleController
{
	[SerializeField] private List<AnimationListController.AnimationInformationList> advanceMeterAnimations = new List<AnimationListController.AnimationInformationList>();

	[HideInInspector] public int numBadSymbolsFound = 0;	// Tracks how many advance icons the player has revealed, tied to the list of meter animations

	public IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem, GameObject effectsParent)
	{
		if (advanceMeterAnimations.Count != 0 && numBadSymbolsFound < advanceMeterAnimations.Count)
		{
			yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(advanceMeterAnimations[numBadSymbolsFound]));

			numBadSymbolsFound++;
		}

		yield break;
	}
}
