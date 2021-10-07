using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This module is used to set an animation to play based on a named transition so that we
// can have different visual effects for different jackpots when the bonus game first loads
// Used games : gen84
// 
// Author : Nick Saito <nsaito@zynga.com>
// Date : Mar 5th 2019
// 
public class BonusGameNamedTransitionAnimation : SlotModule
{
	[Tooltip("list of bonus games that can trigger these animations")]
	[SerializeField] protected List<string> namedTransitions;
	[SerializeField] protected AnimationListController.AnimationInformationList namedTransitionAnimations;
	[SerializeField] protected List<GameObject> objectsToActivate;

	private List<TICoroutine> coroutineList;
	private bool isNameListFormatted = false;

	// Activates animation objects and plays the list of animations
	public override void Awake()
	{
		base.Awake();
		
		formatGameKeyIntoBonusGameNames();

		if (needsToExecuteOnAwake())
		{
			performTransition();
		}
	}

	// check if this bonus game is the same as our namedTransition
	private bool needsToExecuteOnAwake()
	{
		if (GameState.giftedBonus != null)
		{
			// For gifted spins the outcome is not going to be available in the same place
			// as it usually is (or at the same time).  So we need to delay until the game is
			// starting and we can accurately check to see if we need to execute for this gift.
			return false;
		}
		else
		{
			if (BonusGameManager.instance == null || BonusGameManager.instance.outcomes == null)
			{
				return false;
			}

			foreach (KeyValuePair<BonusGameType, BaseBonusGameOutcome> bonusGameOutcomes in BonusGameManager.instance.outcomes)
			{
				if (namedTransitions.Contains(bonusGameOutcomes.Value.bonusGameName))
				{
					return true;
				}
			}
		}

		return false;
	}

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		// If it is a gifted spin, we'll check again here to see if we need to do something
		// because gifted spins can't access outcome data in Awake().
		if (GameState.giftedBonus != null && namedTransitions.Contains(reelGame.freeSpinsOutcomes.bonusGameName))
		{
			performTransition();
		}
	
		return coroutineList != null && coroutineList.Count > 0;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
	}
	
	private void formatGameKeyIntoBonusGameNames()
	{
		if (!isNameListFormatted)
		{
			if (GameState.game != null)
			{
				// Try to auto add the game key, so we can use strings that will work directly in cloned prefabs
				for (int i = 0; i < namedTransitions.Count; i++)
				{
					namedTransitions[i] = string.Format(namedTransitions[i], GameState.game.keyName);
				}
			}

			isNameListFormatted = true;
		}
	}

	private void performTransition()
	{
		foreach (GameObject objectToActivate in objectsToActivate)
		{
			objectToActivate.SetActive(true);
		}

		if (namedTransitionAnimations.Count > 0)
		{
			coroutineList = new List<TICoroutine>();
			coroutineList.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(namedTransitionAnimations)));
		}
	}
}
