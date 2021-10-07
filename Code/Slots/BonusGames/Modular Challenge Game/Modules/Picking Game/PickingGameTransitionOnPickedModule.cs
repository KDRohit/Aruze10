using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
	Handles transitioning the pick game to various other game parts like base, snw, etc.
*/
public class PickingGameTransitionOnPickedModule : PickingGameRevealModule
{
	[System.Serializable]
	private class GroupIdToTransitionMap
	{
		public string groupId;
		public TransitionType transitionType = TransitionType.None;
		public string REVEAL_ANIMATION_NAME = "revealCredit";
		public float REVEAL_ANIMATION_DURATION_OVERRIDE = 0.0f;
		public string REVEAL_GRAY_ANIMATION_NAME = "revealCreditGray";
		public float transitionDelay;
	}

	private enum TransitionType
	{
		ToBaseSNW,
		None
	}

	[SerializeField] private List<GroupIdToTransitionMap> groupIdToTransition;
	private Dictionary<string, GroupIdToTransitionMap> groupIdToTransitionMaps = new Dictionary<string, GroupIdToTransitionMap>();
	private GroupIdToTransitionMap selectedTransition;

	public override void Awake()
	{
		foreach (GroupIdToTransitionMap groupIdToTransitionMap in groupIdToTransition)
		{
			groupIdToTransitionMaps.Add(groupIdToTransitionMap.groupId, groupIdToTransitionMap);
		}
	}

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (selectedTransition != null)
		{
			return false;
		}

		selectedTransition = getTransitionForGroupId(pickData.groupId);
		return pickData != null && !string.IsNullOrEmpty(pickData.groupId) && selectedTransition != null;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		//set the credit value within the item and the reveal animation
		pickItem.setRevealAnim(selectedTransition.REVEAL_ANIMATION_NAME, selectedTransition.REVEAL_ANIMATION_DURATION_OVERRIDE);

		yield return StartCoroutine(base.executeOnItemClick(pickItem));
	}

	private GroupIdToTransitionMap getTransitionForGroupId(string groupId)
	{
		if (groupIdToTransitionMaps.TryGetValue(groupId, out selectedTransition))
		{
			return selectedTransition;
		}
		return null;
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		
		if (leftoverOutcome == null || leftover == null)
		{
			yield break;
		}

		GroupIdToTransitionMap leftoverPick = getTransitionForGroupId(leftoverOutcome.groupId);
		leftover.REVEAL_ANIMATION_GRAY = leftoverPick.REVEAL_GRAY_ANIMATION_NAME;

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
	
	// executes when the round is completing, revealing picks that were not chosen
	public override bool needsToExecuteOnRevealRoundEnd()
	{
		return selectedTransition != null;
	}

	public override IEnumerator executeOnRevealRoundEnd(List<PickingGameBasePickItem> leftovers)
	{
		if (selectedTransition.transitionDelay > 0)
		{
			yield return new WaitForSeconds(selectedTransition.transitionDelay);
		}

		switch (selectedTransition.transitionType)
		{
			case TransitionType.None:
				break;
			case TransitionType.ToBaseSNW:
				yield return StartCoroutine(handleTransitionToShowBaseGame());
				break;
		}

		selectedTransition = null;
	}
	
	private IEnumerator handleTransitionToShowBaseGame()
	{
		SlotBaseGame baseGame = ReelGame.activeGame as SlotBaseGame;

		//showGame usually early outs when there aren't hiddenChildren, this ignores
		//that check because in orig012, we overlay the base game without hiding it
		//but need to let the modules know that base game is back in "focus"
		baseGame.showGame(true);
		yield break;
	}
}
