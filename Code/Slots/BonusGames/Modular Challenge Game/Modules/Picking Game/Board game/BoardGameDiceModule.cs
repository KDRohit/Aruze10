using System;
using System.Collections;
using System.Collections.Generic;
using FeatureOrchestrator;
using UnityEngine;

/*
 * This module is responsible for dice roll animations.
 * On every pick, the dice is rolled. The data update sets the panel state- rolls left, purchase prompt etc
 */
public class BoardGameDiceModule : BoardGameModule
{
	private const string ROLL_RESULT_TEXT = "Move {0}!";

	[Tooltip("Animation to play when roll is clicked")]
	[SerializeField] private AnimationListController.AnimationInformationList onRollAnimation;
	
	[Tooltip("Animation to play when landing onto new space")]
	[SerializeField] private AnimationListController.AnimationInformationList onRollSpecialAnimation;
	
	[Tooltip("Animation to reset the dice panel after a roll is complete")]
	[SerializeField] private AnimationListController.AnimationInformationList resetAnimation;
	
	[Tooltip("Animation to prompt user to do an additional roll.")]
	[SerializeField] private AnimationListController.AnimationInformationList rollAgainMessageIntroAnimations;
	
	[Tooltip("Animation to disable additional roll prompt.")]
	[SerializeField] private AnimationListController.AnimationInformationList rollAgainMessageOutroAnimations;
	
	[Tooltip("Animation to show purchase prompt")]
	[SerializeField] private AnimationListController.AnimationInformationList purchasePromptIntroAnimations;
	
	[Tooltip("Animation to hide purchase prompt")]
	[SerializeField] private AnimationListController.AnimationInformationList purchasePromptOutroAnimations;

	[Tooltip("Animation to show purchase prompt")]
	[SerializeField] private AnimationListController.AnimationInformationList noRollsLeftIntroAnimations;
	
	[Tooltip("Animation to show purchase prompt")]
	[SerializeField] private AnimationListController.AnimationInformationList noRollsLeftOutroAnimations;
	
	[Tooltip("Animation to disable roll button when no picks are available at launch of the board")]
	[SerializeField] private AnimationListController.AnimationInformationList disableRollButtonAnimation;
	
	[Tooltip("Animation to disable roll button when event ends")]
	[SerializeField] AnimationListController.AnimationInformationList eventEndedAnimations;
	
	[Tooltip("Animations to set the value on the each die faces")]
	[SerializeField] private BoardGameDiceAnimationInfo[] diceAnimations;

	[Tooltip("Label to show the pick meter value(dice total value)")]
	[SerializeField] private LabelWrapperComponent rollResultLabel;
	
	[Tooltip("How many picks are remaining")]
	[SerializeField] private LabelWrapperComponent pendingRollsLabel;
	
	[Tooltip("To ensure this is hidden when picks are not available")]
	[SerializeField] private GameObject rollButtonParent;
	
	[Tooltip("Button to show sale when running out of rolls")]
	[SerializeField] private ProtonDialogComponentButton purchaseButton;
	
	private int pendingRolls = 0;
	private bool isRollAgainPromptActive = false;
	private bool isNoRollsLeftMessageActive = false;
	private bool isPurchasePromptActive = false;
	
	/// <summary>
	/// Dice animations for each possible outcome
	/// </summary>
	[Serializable]
	private class BoardGameDiceAnimationInfo
	{
		[Tooltip("Result value")]
		public int rollAmount;
		
		[Tooltip("Picks one of these animations to show the dice values. Eg: for 2 dice setup with rollAmount = 4 it can be 1,3 or 3,1. Don't include identical dice(2,2) here")]
		[SerializeField] AnimationListController.RandomizedAnimationInformationLists resultAnimations;
		
		[Tooltip("Animations to show all identical dice picks. Eg: doubles (2,2) or triples (3,3,3) etc. Ignore if using single die.")]
		[SerializeField] AnimationListController.AnimationInformationList identicalDiceAnimations;
		
		public AnimationListController.AnimationInformationList getAnimationList(bool areDiceIdentical)
		{
			if (areDiceIdentical)
			{
				return identicalDiceAnimations;
			}
			else
			{
				// return resultAnimations[Random.Range(0, resultAnimations.Count)];
				return resultAnimations.getRandomAnimationInformationList();
			}
		}
	}
	
	public override bool needsToExecuteOnDataUpdate()
	{
		return true;
	}

	public override void executeOnDataUpdate(PickByPickClaimableBonusGameOutcome data)
	{
		pendingRolls = data.availablePickCount;
		pendingRollsLabel.text = CommonText.formatNumber(pendingRolls);
		StartCoroutine(setupDicePanel());
	}

	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return true;
	}

	// What happens when roll is clicked.
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		if (isRollAgainPromptActive)
		{
			isRollAgainPromptActive = false;
			yield return
				StartCoroutine(AnimationListController.playListOfAnimationInformation(rollAgainMessageOutroAnimations));
		}

		ModularChallengeGameOutcomeEntry pickedOutcomeEntry = pickingVariantParent.getCurrentPickOutcome();
		rollResultLabel.text = Localize.text(ROLL_RESULT_TEXT, CommonText.formatNumber(pickedOutcomeEntry.meterValue));
		
		yield return StartCoroutine(playRollAnimation(pickedOutcomeEntry.meterValue, pickedOutcomeEntry.additonalPicks > 0,  boardGameVariantParent.willTokenLandOnNewSpace(pickedOutcomeEntry.meterValue)));
	}
	
	public override bool needsToExecuteOnAdvancePick()
	{
		return true;
	}

	// Check if additional roll is available
	public override IEnumerator executeOnAdvancePick()
	{
		ModularChallengeGameOutcomeEntry previousPickOutcomeEntry = pickingVariantParent.getPreviousPickOutcome(); 
		// if the picked item had additional pick, we show roll again prompt
		// Also do not show it if the pick is a jackpot (last pick)
		if (previousPickOutcomeEntry.additonalPicks > 0 && !previousPickOutcomeEntry.isJackpot)
		{
			isRollAgainPromptActive = true;
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rollAgainMessageIntroAnimations));
		}
	}

	private IEnumerator setupDicePanel()
	{
		if (boardGameVariantParent.isGameExpired)
		{
			if (isNoRollsLeftMessageActive)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(noRollsLeftOutroAnimations));
			}
			
			if (isPurchasePromptActive)
			{
				isPurchasePromptActive = false;
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(purchasePromptOutroAnimations));
			}
			
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(eventEndedAnimations));
		}
		else if (pendingRolls > 0)
		{
			if (isNoRollsLeftMessageActive)
			{
				// Rolls were finished, but now new rolls are available (after purchase etc)
				// turn off no rolls left message and purchase prompt if available.
				isNoRollsLeftMessageActive = false;
				
				List<TICoroutine> coroutines = new List<TICoroutine>();
				coroutines.Add(StartCoroutine(
					AnimationListController.playListOfAnimationInformation(noRollsLeftOutroAnimations)));
				if (isPurchasePromptActive)
				{
					isPurchasePromptActive = false;
					coroutines.Add(StartCoroutine(
						AnimationListController.playListOfAnimationInformation(purchasePromptOutroAnimations)));
				}
				yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));
			}

			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(resetAnimation));
		}
		else
		{
			if (!isNoRollsLeftMessageActive || rollButtonParent.activeSelf)
			{
				isNoRollsLeftMessageActive = true;
				rollResultLabel.text = "";
				yield return StartCoroutine(
					AnimationListController.playListOfAnimationInformation(noRollsLeftIntroAnimations));
				if (boardGameVariantParent.isPurchaseOfferAvailable)
				{
					isPurchasePromptActive = true;
					// show purchase prompt
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(purchasePromptIntroAnimations));
					purchaseButton.onClick();
				}
			}
		}
	}

	private BoardGameDiceAnimationInfo getBoardGameDiceAnimationInformation(int rollAmount)
	{
		foreach (BoardGameDiceAnimationInfo boardGameDiceAnimation in diceAnimations)
		{
			if (boardGameDiceAnimation.rollAmount == rollAmount)
			{
				return boardGameDiceAnimation;
			}
		}
		
		Debug.LogError("No animations defined for " + rollAmount);
		return null;
	}

	private IEnumerator playRollAnimation(int rollAmount, bool isDouble, bool shouldPlaySpecialAnimation)
	{
		List<TICoroutine> coroutineList = new List<TICoroutine>();

		BoardGameDiceAnimationInfo diceAnimationInformation = getBoardGameDiceAnimationInformation(rollAmount);
		if (diceAnimationInformation != null)
		{
			coroutineList.Add(StartCoroutine(AnimationListController
				.playListOfAnimationInformation(diceAnimationInformation.getAnimationList(isDouble))));

			if (shouldPlaySpecialAnimation)
			{
				coroutineList.Add(
					StartCoroutine(AnimationListController.playListOfAnimationInformation(onRollSpecialAnimation)));
			}
			else
			{
				coroutineList.Add(
					StartCoroutine(AnimationListController.playListOfAnimationInformation(onRollAnimation)));
			}

			yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
	}
}