using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This class encompasses 3 games, all which behave exactly the same, save for the differences in the prefab itself.
They select one beard, which reveals a face and the amount they win. The rest are revealed as well. The starting faces can
either be 3, 4, or 5, depending on the amount of scatter wins they got.
*/
public class Wow06LionScatterBonus : PickingGame<WheelOutcome>
{
	public UILabelStyle disabledStyle;			// Test style
	public GameObject revealPrefab;
	public GameObject pickIcon;
	
	private long wonCredits;			// The amount of credits won

	private WheelPick wheelPick;				// Pick extracted from the outcome		

	// Sound constants
	private const string BG_SOUND = "ScatterLionBg";
	private const string VO_SOUND = "ScatterLionVO";
	private const string PICKME_SOUND = "LionPickMe";
	private const string INTRO_POP_SOUND = "RevealSparkly";
	private const string LION_COIN = "LionRevealCoin";
	private const string SCATTER_WIN_VO = "ScatterWinVOChina";
	private const string COINS_REVEAL_SOUND = "RevealOtherCoinsC";

	// Animation constants
	private const string PICKME_ANIMATION = "Picking Object lion 01_PickMe";
	private const string PICKME_STILL = "Picking Object lion 01_still";
	private const string REVEAL_GOAT_ANIMATION = "Picking Object lion 01_reveal goat";
	private const string REVEAL_PIG_ANIMATION = "Picking Object lion 01_reveal pig";
	private const string REVEAL_RABBIT_ANIMATION = "Picking Object lion 01_reveal rabbit";
	private const string REVEAL_GRAY_GOAT_ANIMATION = "Picking Object lion 01_not Selected goat";
	private const string REVEAL_GRAY_PIG_ANIMATION = "Picking Object lion 01_not Selected pig";
	private const string REVEAL_GRAY_RABBIT_ANIMATION = "Picking Object lion 01_not Selected rabbit";

	// Timing constants
	private const float INTRO_POP_IN_WAIT_TIME = 0.5f;
	private const float PICKME_WAIT = 0.75f;
	private const float SHOW_RESULT_WAIT_TIME = 0.5f;
	private const float TIME_BEFORE_START_ANIMATION = 1.25f;
	private const float TIME_BETWEEN_EACH_REVEAL = 0.5f;
	private const float END_GAME_DELAY = 1.0f;
	private const float INITIAL_REVEAL_DELAY = 0.5f;

	
	/**
	Initialize data specific to this game
	*/
	public override void init() 
	{		
		base.init();

		// Kill the input until the intro sequence is done
		inputEnabled = false;

		// Force the correct wings.
		BonusGameManager.instance.wings.forceShowNormalWings(true);

		Audio.play(BG_SOUND);
		Audio.play(VO_SOUND);

		wheelPick = outcome.getNextEntry();
		wonCredits = wheelPick.credits * BonusGameManager.instance.currentMultiplier;

		// We have 3 starting panels, depending on how the user enters the game.
		currentStage = outcome.extraInfo - 3;

		// Switch which stage depending on the extra info provided.
		if (outcome.extraInfo == 3)
		{
			stageObjects[0].SetActive(true);
			stageObjects[1].SetActive(false);
			stageObjects[2].SetActive(false);
		}
		else if (outcome.extraInfo == 4)
		{
			stageObjects[1].SetActive(true);
			stageObjects[0].SetActive(false);
			stageObjects[2].SetActive(false);
		}
		else
		{
			stageObjects[2].SetActive(true);
			stageObjects[0].SetActive(false);
			stageObjects[1].SetActive(false);
		}

		StartCoroutine(beginIntro());
	}

	// This is the popping in sequence at the beginning of the game.
	private IEnumerator beginIntro()
	{
		// Set them all to the zero scale.
		for (int i = 0; i < outcome.extraInfo;i++)
		{
			PickGameButtonData pickButtonData = getPickGameButton(i);
			pickButtonData.go.transform.localScale = new Vector3(0,0,0);
		}

		// Now scale them back to the correct size
		for (int i = 0; i < outcome.extraInfo;i++)
		{
			PickGameButtonData pickButtonData = getPickGameButton(i);
			iTween.ScaleTo(pickButtonData.go, iTween.Hash("scale", new Vector3(1,1,1), "time", INTRO_POP_IN_WAIT_TIME, "easetype", iTween.EaseType.easeOutSine));
			Audio.play(INTRO_POP_SOUND);
			yield return new TIWaitForSeconds(INTRO_POP_IN_WAIT_TIME);
		}

		// Let's re-enable the intro
		inputEnabled = true;
	}

	/// Triggered periodically to draw the users eye
	protected override IEnumerator pickMeAnimCallback()
	{
		PickGameButtonData pickButton = getRandomPickMe();
			
		if (pickButton != null)
		{
			pickButton.animator.Play(PICKME_ANIMATION);
			Audio.play(PICKME_SOUND);
			
			yield return new WaitForSeconds(PICKME_WAIT);
			if (isButtonAvailableToSelect(pickButton))
			{
				pickButton.animator.Play(PICKME_STILL);
			}
		}
	}
	
	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;
		
		Audio.play(LION_COIN);
		Audio.play(SCATTER_WIN_VO, 1, 0, 0.8f);

		removeButtonFromSelectableList(button);
		int pickIndex = getButtonIndex(button);

		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);
		pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.credits);
		pickButtonData.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(wheelPick.credits);
		pickButtonData.multiplierLabel.text = CreditsEconomy.convertCredits(wheelPick.credits);

		// The extra data is still using the older SATC data *facepalm*
		if (wheelPick.extraData == "Mr.Big")
		{
			pickButtonData.animator.Play(REVEAL_GOAT_ANIMATION);
		}
		else if (wheelPick.extraData == "Samantha")
		{
			pickButtonData.animator.Play(REVEAL_PIG_ANIMATION);
		}
		else
		{
			pickButtonData.animator.Play(REVEAL_RABBIT_ANIMATION);
		}

		yield return new TIWaitForSeconds(INITIAL_REVEAL_DELAY);

		// Now we do the regular reveals.
		WheelPick wheelReveal;
		for (int i = 0; i < wheelPick.wins.Count; i++)
		{
			wheelReveal = wheelPick.wins[i];
			if (wheelReveal.winIndex != wheelPick.winID)
			{
				Audio.play(COINS_REVEAL_SOUND);
				GameObject nextButton = grabNextButtonAndRemoveIt(currentStage);
				pickIndex = getButtonIndex(nextButton);
				pickButtonData = getPickGameButton(pickIndex);
				pickButtonData.extraLabel.text = CreditsEconomy.convertCredits(wheelReveal.credits);
				pickButtonData.extraOutlineLabel.text = CreditsEconomy.convertCredits(wheelReveal.credits);
				if (wheelReveal.extraData == "Mr.Big")
				{
					pickButtonData.animator.Play(REVEAL_GRAY_GOAT_ANIMATION);
				}
				else if (wheelReveal.extraData == "Samantha")
				{
					pickButtonData.animator.Play(REVEAL_GRAY_PIG_ANIMATION);
				}
				else
				{
					pickButtonData.animator.Play(REVEAL_GRAY_RABBIT_ANIMATION);
				}
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_EACH_REVEAL));
			}
		}

		yield return new TIWaitForSeconds(END_GAME_DELAY);

		StartCoroutine(showResults());
	}
	
	// Show the results of clicking an icon.
	private IEnumerator showResults()
	{
		if (BonusGamePresenter.HasBonusGameIdentifier())
			SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());

		yield return new TIWaitForSeconds(SHOW_RESULT_WAIT_TIME);

		BonusGamePresenter.instance.currentPayout = wonCredits;
		BonusGamePresenter.instance.gameEnded();		
	}
	
}


