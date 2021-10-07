using UnityEngine;
using System.Collections;

public class pb01Pickem : PickingGame<NewBaseBonusGameOutcome> 
{
	public GameObject vizziniButtonParent;
	public Animator[] vezziniAnimators;
	public GameObject sparkleBoxes;
	public Animator gobletRevealObjLeft;
	public Animator gobletRevealObjRight;
	public UILabelStyle grayedOutStyle;
	private RoundPicks currentRoundPick;
	private int vizziniCount = 0;

	private const string INTRO_VO = "INDecentFellowHateToKillYou";
	private const string PICKME_SWORD_SFX = "SwordPickMePbride";
	private const string PICKME_BOULDER_SFX = "BoulderPickMe";
	private const string PICKME_GOBLET_SFX = "GobletPickMe";
	private const string PICK_SWORD_SFX = "SwordPickASword";
	private const string PICK_BOUDLER_SFX = "BoulderPickABoulder";
	private const string PICK_GOBLET_SFX = "GobletPickAGoblet";

	private const string CARD_PICKME_ANIM = "card_pickme";
	private const string CARD_PICKME_STATIC = "card_static";
	private const string GOBLET_PICKME_ANIM = "goblet_pickme";
	private const string GOBLET_PICKME_STATIC = "goblet_still";

	private const float PICKME_DELAY_1 = 0.75f;
	private const float PICKME_DELAY_2 = 1.5f;

	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();
		if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
		{
			BonusGameManager.instance.wings.forceShowChallengeWings(true);
		}
		Audio.play(INTRO_VO, 1, 0, 0.7f);
		currentRoundPick = outcome.roundPicks[currentStage];
	}

	/// Triggered periodically to draw the users eye
	protected override IEnumerator pickMeAnimCallback()
	{	
		PickGameButtonData pickButton = getRandomPickMe();

		if (pickButton != null)
		{
			if (currentStage == 0 || currentStage == 1)
			{			
				pickButton.animator.Play(CARD_PICKME_ANIM);
				if (currentStage == 0)
				{			
					Audio.play(PICKME_SWORD_SFX);
				}
				else
				{
					Audio.play(PICKME_BOULDER_SFX);
				}
			
				yield return new WaitForSeconds(PICKME_DELAY_1);
				if (isButtonAvailableToSelect(pickButton))
				{
					pickButton.animator.Play(CARD_PICKME_STATIC);
				}
			}
			else
			{			
				pickButton.animator.Play(GOBLET_PICKME_ANIM);
				Audio.play(PICKME_GOBLET_SFX);
			
				yield return new WaitForSeconds(PICKME_DELAY_2);
				if (isButtonAvailableToSelect(pickButton))
				{
					pickButton.animator.Play(GOBLET_PICKME_STATIC);
				}
			}
		}
	}

	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;
		switch (currentStage)
		{
			case 0:
				Audio.play(PICK_SWORD_SFX);
				StartCoroutine(stage1ButtonSelectedCoroutine(button));
				break;
			case 1:
				Audio.play(PICK_BOUDLER_SFX);
				StartCoroutine(stage2ButtonSelectedCoroutine(button));
				break;
			case 2:
				Audio.play(PICK_GOBLET_SFX);
				StartCoroutine(stage3ButtonSelectedCoroutine(button));
				break;
		}

		yield return null;
	}

	private IEnumerator stage1ButtonSelectedCoroutine(GameObject button)
	{
		removeButtonFromSelectableList(button);
		int pickIndex = getButtonIndex(button);
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);
		BasePick currentPick = currentRoundPick.getNextEntry();

		GameObject dreadPirateButton = CommonGameObject.findChild(pickButtonData.button, "dread_pirate_roberts");
		GameObject vizziniButton = CommonGameObject.findChild(pickButtonData.button, "vizzini");

		BonusGamePresenter.instance.currentPayout += currentPick.credits;

		if (currentPick.isGameOver)
		{
			dreadPirateButton.SetActive(false);
			vizziniButton.SetActive(true);
			pickButtonData.animator.Play("card_reveal");
			Audio.play("SwordRevealVizzini" + (vizziniCount+1));
			if ((vizziniCount + 1) <= 2)
			{
				Audio.play("RevealVizziniVO", 1, 0, 1);
			}
			else
			{
				Audio.play("VZNeverSicilianDeathOnTheLine", 1, 0, 1.4f);
			}
			yield return new TIWaitForSeconds(0.5f);
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			vezziniAnimators[vizziniCount].Play("vizzini_btn_on");
			Audio.play("XoutEscape");
			vizziniCount++;

			if (vizziniCount == 3)
			{
				yield return StartCoroutine(endStage(true));
			}
			else 
			{
				inputEnabled = true;
			}
		}
		else if (currentPick.canAdvance)
		{
			Audio.play("SwordRevealButtercup");
			Audio.play("WSPleaseUnderstandHighestRespec", 1, 0, 0.7f);
			dreadPirateButton.SetActive(true);
			vizziniButton.SetActive(false);
			pickButtonData.animator.Play("card_reveal");
			yield return new TIWaitForSeconds(0.5f);
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			yield return StartCoroutine(endStage());
			Audio.switchMusicKeyImmediate("BonusBgPbrideV2");
			Audio.play("BoulderPickVO", 1, 0, 1.0f);
			continueToNextStage();
			if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
			{
				BonusGameManager.instance.wings.forceShowSecondaryChallengeWings(true);
			}
			currentRoundPick = outcome.roundPicks[currentStage];
			inputEnabled = true;
		}
		else
		{
			Audio.play("SwordRevealCredit");
			dreadPirateButton.SetActive(false);
			vizziniButton.SetActive(false);
			pickButtonData.animator.Play("card_reveal");
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
			pickButtonData.revealNumberLabel.gameObject.SetActive(true);
			yield return new TIWaitForSeconds(0.5f);
			sparkleBoxes.SetActive(true);
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			sparkleBoxes.SetActive(false);
			inputEnabled = true;
		}
		yield return null;
	}

	private IEnumerator stage2ButtonSelectedCoroutine(GameObject button)
	{
		removeButtonFromSelectableList(button);
		int pickIndex = getButtonIndex(button);
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);
		BasePick currentPick = currentRoundPick.getNextEntry();

		GameObject dreadPirateButton = CommonGameObject.findChild(pickButtonData.button, "dread_pirate_roberts");
		GameObject vizziniButton = CommonGameObject.findChild(pickButtonData.button, "vizzini");

		BonusGamePresenter.instance.currentPayout += currentPick.credits;

		if (currentPick.isGameOver)
		{
			dreadPirateButton.SetActive(false);
			vizziniButton.SetActive(true);
			pickButtonData.animator.Play("card_reveal");
			Audio.play("SwordRevealVizzini" + (vizziniCount+1));
			if ((vizziniCount + 1) <= 2)
			{
				Audio.play("RevealVizziniVO", 1, 0, 1);
			}
			else
			{
				Audio.play("VZNeverSicilianDeathOnTheLine", 1, 0, 1.4f);
			}
			yield return new TIWaitForSeconds(0.5f);
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			vezziniAnimators[vizziniCount].Play("vizzini_btn_on");
			Audio.play("XoutEscape");
			vizziniCount++;

			if (vizziniCount == 3)
			{
				yield return StartCoroutine(endStage(true));
			}
			else 
			{
				inputEnabled = true;
			}
		}
		else if (currentPick.canAdvance)
		{
			dreadPirateButton.SetActive(true);
			vizziniButton.SetActive(false);
			pickButtonData.animator.Play("card_reveal");
			Audio.play("BoulderRevealButtercup");
			Audio.play("WSInMeantimeDreamOfLargeWomen", 1, 0, 0.7f);
			yield return new TIWaitForSeconds(0.5f);
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			yield return StartCoroutine(endStage());
			Audio.switchMusicKeyImmediate("BonusBgPbrideV3");
			continueToNextStage();
			if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
			{
				BonusGameManager.instance.wings.forceShowThirdChallengeWings(true);
			}
			vizziniButtonParent.SetActive(false);
			currentRoundPick = outcome.roundPicks[currentStage];
			StartCoroutine(beginStage3Sequence());
		}
		else
		{
			Audio.play("SwordRevealCredit");
			dreadPirateButton.SetActive(false);
			vizziniButton.SetActive(false);
			pickButtonData.animator.Play("card_reveal");
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
			pickButtonData.revealNumberLabel.gameObject.SetActive(true);
			yield return new TIWaitForSeconds(0.5f);
			sparkleBoxes.SetActive(true);
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			sparkleBoxes.SetActive(false);
			inputEnabled = true;
		}
		yield return null;
	}

	private IEnumerator beginStage3Sequence()
	{
		Audio.play("VZSoDownToYouDownToMe");
		yield return new TIWaitForSeconds(3.5f);
		Audio.play("VZLogic");
		inputEnabled = true;
	}

	private IEnumerator stage3ButtonSelectedCoroutine(GameObject button)
	{
		inputEnabled = false;
		BasePick currentPick = currentRoundPick.getNextEntry();

		int pickIndex = getButtonIndex(button);
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);

		if (pickIndex == 0)
		{
			gobletRevealObjLeft.gameObject.SetActive(true);
		}
		else
		{
			gobletRevealObjRight.gameObject.SetActive(true);
		}

		if (currentPick.credits != 0)
		{
			Audio.play("GobletRevealCredit");
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
			if (pickIndex == 0)
			{
				gobletRevealObjLeft.Play("poison_reveal");
			}
			else
			{
				gobletRevealObjRight.Play("poison_reveal");
			}
			yield return new TIWaitForSeconds(1.5f);
			sparkleBoxes.SetActive(true);
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + currentPick.credits));
			sparkleBoxes.SetActive(false);
		}
		else
		{
			Audio.play("GobletRevealMultiplier");
			pickButtonData.multiplierLabel.text = Localize.text("{0}X", CommonText.formatNumber(currentPick.multiplier));
			if (pickIndex == 0)
			{
				gobletRevealObjLeft.Play("buttercup_reveal");
			}
			else
			{
				gobletRevealObjRight.Play("buttercup_reveal");
			}
			yield return new TIWaitForSeconds(3.0f);
			sparkleBoxes.SetActive(true);
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout * (currentPick.multiplier)));
			sparkleBoxes.SetActive(false);
		}

		yield return new TIWaitForSeconds(2.0f);

		if (currentPick.credits != 0)
		{
			BonusGamePresenter.instance.currentPayout += currentPick.credits;
		}
		else
		{
			BonusGamePresenter.instance.currentPayout *= (currentPick.multiplier);
		}

		// Now do the reveal

		currentPick = currentRoundPick.getNextReveal();
		pickIndex = pickIndex == 0 ? 1 : 0;
		pickButtonData = getPickGameButton(pickIndex);

		if (pickIndex == 0)
		{
			gobletRevealObjLeft.gameObject.SetActive(true);
		}
		else
		{
			gobletRevealObjRight.gameObject.SetActive(true);
		}

		if (currentPick.credits != 0)
		{
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
			if (pickIndex == 0)
			{
				gobletRevealObjLeft.Play("poison_reveal_gray");
			}
			else
			{
				gobletRevealObjRight.Play("poison_reveal_gray");
			}
		}
		else
		{
			pickButtonData.multiplierLabel.text = Localize.text("{0}X", CommonText.formatNumber(currentPick.multiplier));;
			if (pickIndex == 0)
			{
				gobletRevealObjLeft.Play("buttercup_reveal_gray");
			}
			else
			{
				gobletRevealObjRight.Play("buttercup_reveal_gray");
			}
		}

		yield return new TIWaitForSeconds(2.0f);

		Audio.play("BonusSummaryVOPbride");
		BonusGamePresenter.instance.gameEnded();
	}

	private IEnumerator endStage(bool endGame = false)
	{
		revealWait.reset ();
		yield return new TIWaitForSeconds(0.5f);
		BasePick revealPick = currentRoundPick.getNextReveal();
		int revealCount = 0;
		while (revealPick != null)
		{
			revealCount++;
			revealIcon(revealPick);
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
			revealPick = currentRoundPick.getNextReveal();
		}

		if (revealCount > 0)
		{
			yield return new TIWaitForSeconds(2.0f);
		}
		else
		{
			yield return new TIWaitForSeconds(1.0f);
		}

		if (endGame)
		{
			Audio.play("BonusSummaryVOPbride", 1, 0, 2.0f);
			BonusGamePresenter.instance.gameEnded();
		}
	}

	private void revealIcon(BasePick revealPick)
	{
		if (anyButtonsAvailableToSelect())
		{
			GameObject button = grabNextButtonAndRemoveIt(currentStage);
			int pickIndex = getButtonIndex(button);
			PickGameButtonData pickButtonData = getPickGameButton(pickIndex);
			GameObject dreadPirateButton = CommonGameObject.findChild(pickButtonData.button, "dread_pirate_roberts");
			GameObject vizziniButton = CommonGameObject.findChild(pickButtonData.button, "vizzini");
			GameObject dreadPirateButtonGray = CommonGameObject.findChild(pickButtonData.button, "dread_pirate_roberts_gray");
			GameObject vizziniButtonGray = CommonGameObject.findChild(pickButtonData.button, "vizzini_gray");
			dreadPirateButton.SetActive(false);
			vizziniButton.SetActive(false);
			if(!revealWait.isSkipping)
			{
				Audio.play("reveal_others");
			}
			if (revealPick.isGameOver)
			{
				dreadPirateButtonGray.SetActive(false);
				vizziniButtonGray.SetActive(true);
				pickButtonData.animator.Play("card_reveal");
			}
			else if (revealPick.canAdvance)
			{
				dreadPirateButtonGray.SetActive(true);
				vizziniButtonGray.SetActive(false);
				pickButtonData.animator.Play("card_reveal");
			}
			else
			{
				UILabelStyler numberLabelStyler = pickButtonData.revealNumberLabel.GetComponent<UILabelStyler>();
				if (numberLabelStyler != null)
				{
					numberLabelStyler.style = grayedOutStyle;
					numberLabelStyler.updateStyle();
				}
				dreadPirateButtonGray.SetActive(false);
				vizziniButtonGray.SetActive(false);
				pickButtonData.animator.Play("card_reveal");
				pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(revealPick.credits);
				pickButtonData.revealNumberLabel.gameObject.SetActive(true);
			}
		}
	}
}
