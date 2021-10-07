using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class specific to handling reveals for PickGameBonusGameSelectionModule where the player gets to pick a bonus
which was not pre-determined by an outcome from the server.

Original Author: Scott Lepthien
Creation Date: June 16, 2017
*/
public class PickingGameBonusGameSelectionItem : PickingGameBasePickItemAccessor 
{
	[SerializeField] protected string _bonusGameScatKey = "";
	public string bonusGameScatKey
	{
		get { return _bonusGameScatKey; }
	}
	[SerializeField] protected SlotResourceMap.FreeSpinTypeEnum _freeSpinPrefabType = SlotResourceMap.FreeSpinTypeEnum.DEFAULT;
	public SlotResourceMap.FreeSpinTypeEnum freeSpinPrefabType
	{
		get { return _freeSpinPrefabType; }
	}
	[SerializeField] protected AnimationListController.AnimationInformationList selectionAnimationList; // animations played after the player makes a selection

	public IEnumerator playSelectionAnimationList()
	{
		yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(selectionAnimationList));
	}
}
