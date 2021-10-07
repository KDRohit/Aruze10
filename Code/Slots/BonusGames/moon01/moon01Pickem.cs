using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class moon01Pickem : PickingGame<WheelOutcome> 
{
	public UILabel jackpotAmountLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent jackpotAmountLabelWrapperComponent;

	public LabelWrapper jackpotAmountLabelWrapper
	{
		get
		{
			if (_jackpotAmountLabelWrapper == null)
			{
				if (jackpotAmountLabelWrapperComponent != null)
				{
					_jackpotAmountLabelWrapper = jackpotAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotAmountLabelWrapper = new LabelWrapper(jackpotAmountLabel);
				}
			}
			return _jackpotAmountLabelWrapper;
		}
	}
	private LabelWrapper _jackpotAmountLabelWrapper = null;
	
	public UILabel currentPickLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent currentPickLabelWrapperComponent;

	public LabelWrapper currentPickLabelWrapper
	{
		get
		{
			if (_currentPickLabelWrapper == null)
			{
				if (currentPickLabelWrapperComponent != null)
				{
					_currentPickLabelWrapper = currentPickLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_currentPickLabelWrapper = new LabelWrapper(currentPickLabel);
				}
			}
			return _currentPickLabelWrapper;
		}
	}
	private LabelWrapper _currentPickLabelWrapper = null;
	
	public Animator machineAnimator;
	public Animator celebrationAnimator;

	public Animation[] boltAnimations;

	private PickemOutcome pickemOutcome;
	private long jackpotAmount = 0;
	private int pickCount = 0;
	private int totalGroupSelections = 0;
	private int voAudioCount = 0;

	PlayingAudio machineAmbiance;

	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();
		Audio.play("BonusIntroVOMoonpies");
		Audio.switchMusicKeyImmediate(Audio.soundMap("bonus_idle_bg"));
	}

	/// Triggered periodically to draw the users eye
	protected override IEnumerator pickMeAnimCallback()
	{
		if (currentStage == 0)
		{
			yield return StartCoroutine(countPickMeAnimCallback());

		}
		else
		{
			yield return StartCoroutine(valuesPickMeAnimCallback());
		}
	}

	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;

		if (currentStage == 0)
		{
			yield return StartCoroutine(countButtonPressedCoroutine(button));
		}
		else
		{
			yield return StartCoroutine(valueButtonPressedCoroutine(button));
		}

		inputEnabled = true;
	}

	/// Handle what happens when one of the buttons in the count stage is picked
	private IEnumerator countButtonPressedCoroutine(GameObject button)
	{
		removeButtonFromSelectableList(button);
		int pickIndex = getButtonIndex(button);
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);

		Audio.play("PickAMoonPie");

		WheelPick wheelPick = outcome.getNextEntry();
		string[] wheelPieces = wheelPick.bonusGame.Split('_');
		pickemOutcome = new PickemOutcome(SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame));

		// Grab the jackpot value now that we have the pickem outcome
		foreach (JSON paytableGroup in pickemOutcome.paytableGroups)
		{
			if (paytableGroup.getString("group_code", "") == "GIRL")
			{
				// @todo : Fairly sure this needs to be scaled up
				jackpotAmount = paytableGroup.getLong("credits", 0L);
				jackpotAmount *= GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
				jackpotAmountLabelWrapper.text = CreditsEconomy.convertCredits(jackpotAmount);
			}
		}

		pickCount = int.Parse(wheelPieces[2]);
		currentPickLabelWrapper.text = pickCount.ToString();

		if (pickCount == 8)
		{
			Audio.play("SMFivePicks");
		}
		else if (pickCount == 9)
		{
			Audio.play("SMTenPicks");
		}
		else
		{
			Audio.play("SMFifteenPicks");
		}

		pickButtonData.revealNumberLabel.text = Localize.text("{0}_picks", wheelPieces[2]);
		pickButtonData.revealNumberOutlineLabel.text = Localize.text("{0}_picks", wheelPieces[2]);
		pickButtonData.animator.Play("Intro cookie_reveal");

		yield return new TIWaitForSeconds(0.5f);

		int winsIndex = 0;
		for (int i = 0; i < roundButtonList[currentStage].revealNumberList.Length; i++)
		{
			if (winsIndex == wheelPick.winIndex)
			{
				//Debug.Log("Found the matching win, skipping now.");
				winsIndex++;
			}

			if (i != pickIndex)
			{
				Audio.play("reveal_others");
				PickGameButtonData pick = getPickGameButton(i);
				string[] revealWheelPieces = wheelPick.wins[winsIndex].bonusGame.Split('_');

				pick.extraLabel.text = Localize.text("{0}_picks", revealWheelPieces[2]);
				pick.extraOutlineLabel.text = Localize.text("{0}_picks", revealWheelPieces[2]);
				pick.animator.Play("Intro cookie_not Selected");

				winsIndex++;

				yield return new TIWaitForSeconds(0.5f);
			}
		}

		yield return new TIWaitForSeconds(2.0f);
		Audio.switchMusicKeyImmediate("BonusBgMoonpies");
		machineAmbiance = Audio.play("MFMachineAmbience", 1, 0, 0, float.PositiveInfinity);
		Audio.play("MFMachineFirstCookie", 1, 0, 1.0f);
		continueToNextStage();
		foreach (Animation boltAnim in boltAnimations)
		{
			boltAnim.Stop();
		}
		StartCoroutine(startOrStopBolts(true, 1.5f));
	}

	private IEnumerator startOrStopBolts(bool startBolts = true, float timeToDelayAnimation = 0.0f, float animationPlayTime = 0.5f)
	{
		yield return new TIWaitForSeconds(timeToDelayAnimation);

		foreach (Animation boltAnim in boltAnimations)
		{
			boltAnim.Play("bolt Rotation");
		}

		Audio.play("MFCookieMovesFoley");

		yield return new TIWaitForSeconds(animationPlayTime);

		foreach (Animation boltAnim in boltAnimations)
		{
			boltAnim.Stop();
		}
	}

	/// Handle what happens when one of hte buttons in the value stage is picked
	private IEnumerator valueButtonPressedCoroutine(GameObject button)
	{
		PickemPick pick = pickemOutcome.getNextEntry();
		int pickIndex = getButtonIndex(button);
		removeButtonFromSelectableList(getButtonUsingIndexAndRound(pickIndex));
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);

		pickCount--;
		currentPickLabelWrapper.text = pickCount.ToString();

		if (pick.groupId == "GIRL")
		{
			switch (totalGroupSelections)
			{
				case 0:
					pickButtonData.animator.Play("Picking object cookie_reveal marshmellow");
					Audio.play("MFRevealIngredient");
					Audio.play("SMAddALittleMarshmallow", 1, 0, 0.8f);
					yield return new TIWaitForSeconds(1.65f);
					StartCoroutine(startOrStopBolts(true, 1.0f));
					machineAnimator.Play("Machine drop a marshmellow");
					Audio.play("MFMarshmallowFoley");
					break;
				case 1:
					pickButtonData.animator.Play("Picking object cookie_reveal cookieCrust");
					Audio.play("MFRevealIngredient");
					Audio.play("BMIsThatACookieLetsGetStarted", 1, 0, 0.8f);
					yield return new TIWaitForSeconds(1.65f);
					StartCoroutine(startOrStopBolts(true, 1.0f));
					machineAnimator.Play("Machine drop a cookieCrust");
					Audio.play("MFCookieFoley");
					break;
				case 2:
					pickButtonData.animator.Play("Picking object cookie_reveal chocolate");
					Audio.play("MFRevealJackpot");
					Audio.play("CMEverythingBetterCovered", 1, 0, 0.8f);
					yield return new TIWaitForSeconds(1.65f);
					StartCoroutine(startOrStopBolts(true, 1.0f));
					machineAnimator.Play("Machine drop chocolate");
					Audio.play("MFChocolateFoley");
					break;
			}

			totalGroupSelections++;

			if (totalGroupSelections == 3)
			{
				celebrationAnimator.gameObject.SetActive(true);
				celebrationAnimator.Play("jackpot celebrade");
				yield return new TIWaitForSeconds(1.5f);

				Audio.play("MFMoonpieRain");

				yield return new TIWaitForSeconds(1.0f);
				
				StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + jackpotAmount));
				BonusGamePresenter.instance.currentPayout += jackpotAmount;
				celebrationAnimator.gameObject.SetActive(false);
			}
		}
		else
		{
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(pick.credits);
			pickButtonData.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(pick.credits);
			pickButtonData.animator.Play("Picking object cookie_reveal Number");
			Audio.play("PickAMoonPie");

			voAudioCount++;
			if (voAudioCount%2 == 0)
			{
				Audio.play("MFRandomPickVO", 1, 0, 0.6f);
			}

			yield return new TIWaitForSeconds(1.0f);

			StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + pick.credits));
			BonusGamePresenter.instance.currentPayout += pick.credits;
		}

		yield return new TIWaitForSeconds(0.5f);

		if (pickCount == 0)
		{
			while (pickmeButtonList[currentStage].Count > 0)
			{
				// get the next reveal
				PickemPick reveal = pickemOutcome.getNextReveal();
				GameObject nextButton = grabNextButtonAndRemoveIt();
				int revealPickIndex = getButtonIndex(nextButton);
				PickGameButtonData revealPickButtonData = getPickGameButton(revealPickIndex);
				if(!revealWait.isSkipping)
				{
					Audio.play("reveal_others");
				}

				if (reveal.groupId == "GIRL")
				{
					switch (totalGroupSelections)
					{
						case 0:
							revealPickButtonData.animator.Play("Picking object cookie_not selected marshmellow");
							break;
						case 1:
							revealPickButtonData.animator.Play("Picking object cookie_not selected cookieCrust");
							break;
						case 2:
							revealPickButtonData.animator.Play("Picking object cookie_not selected chocolate");
							break;
					}

					totalGroupSelections++;
				}
				else
				{
					revealPickButtonData.extraLabel.text = CreditsEconomy.convertCredits(reveal.credits);
					revealPickButtonData.extraOutlineLabel.text = CreditsEconomy.convertCredits(reveal.credits);
					revealPickButtonData.animator.Play("Picking object cookie_not selected Number");
				}

				yield return StartCoroutine(revealWait.wait(0.25f));
			}

			yield return new TIWaitForSeconds(1.0f);
			Audio.stopSound(machineAmbiance);
			
			BonusGamePresenter.instance.gameEnded();
			Audio.play("BonusSummaryVOMoonpies");
		}
	}

	/// Called to animate a count stage button with a pick me
	protected IEnumerator countPickMeAnimCallback()
	{
		PickGameButtonData countPick = getRandomPickMe();
			
		if (countPick != null)
		{
			countPick.animator.Play("Intro cookie_pick me");
			Audio.play("MoonPiePickMe");
			
			yield return new WaitForSeconds(1.0f);

			if (inputEnabled)
			{
				countPick.animator.Play("Intro cookie_still");
			}
		}
	}

	/// Called to animate a values stage button with a pick me
	protected IEnumerator valuesPickMeAnimCallback()
	{
		PickGameButtonData valuePick = getRandomPickMe();
			
		if (valuePick != null)
		{
			valuePick.animator.Play("Picking object cookie_Pick me");
			Audio.play("MoonPiePickMe");
			
			yield return new WaitForSeconds(1.0f);

			if (inputEnabled)
			{
				valuePick.animator.Play("Picking object cookie_Still");
			}
		}
	}

}

