using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Pickem class for the phantom pick game */

public class Com06Pickem : PickingGame<WheelOutcome>
{

	public Animator logoTransition;
	public Animator textMessage;
	public Animator map;
	public Animator celebration;
	public UILabel roundText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent roundTextWrapperComponent;

	public LabelWrapper roundTextWrapper
	{
		get
		{
			if (_roundTextWrapper == null)
			{
				if (roundTextWrapperComponent != null)
				{
					_roundTextWrapper = roundTextWrapperComponent.labelWrapper;
				}
				else
				{
					_roundTextWrapper = new LabelWrapper(roundText);
				}
			}
			return _roundTextWrapper;
		}
	}
	private LabelWrapper _roundTextWrapper = null;
	
	public UILabel roundTextOutline2;	// To be removed when prefabs are updated.
	public LabelWrapperComponent roundTextOutlineWrapperComponent;

	public LabelWrapper roundTextOutlineWrapper
	{
		get
		{
			if (_roundTextOutlineWrapper == null)
			{
				if (roundTextOutlineWrapperComponent != null)
				{
					_roundTextOutlineWrapper = roundTextOutlineWrapperComponent.labelWrapper;
				}
				else
				{
					_roundTextOutlineWrapper = new LabelWrapper(roundTextOutline2);
				}
			}
			return _roundTextOutlineWrapper;
		}
	}
	private LabelWrapper _roundTextOutlineWrapper = null;
	
	public GameObject helpMessage1;
	public GameObject helpMessage2;
	public GameObject trailEndPosition;

	private bool winAll = false;
	private int currentWinAllID = -1;

	//private const float TIME_BETWEEN_REVEALS = 0.25f;

	//private SkippableWait revealWait = new SkippableWait();			//Handles skippable reveals

	/// Handle initialization stuff for the game
	public override void init()
	{
		Audio.play("BonusIntroPhantom");
		Audio.play("BonusBgPhantom", 1, 0, 1.65f);
		Audio.play("BonusIntroVOPhantom");
		base.init();
		textMessage.Play("Text message_Down");
		roundTextWrapper.text = Localize.text("round_{0}", currentStage+1);
		roundTextOutlineWrapper.text = Localize.text("round_{0}", currentStage+1);
	}


	/// Triggered periodically to draw the users eye
	protected override IEnumerator pickMeAnimCallback()
	{		
		PickGameButtonData pickButton = getRandomPickMe();
		string prefixName = pickButton.button.name.Split('_')[0];
		string pickMeAnimName = prefixName + "_PickMe";
		string pickMeStillName = prefixName + "_Still";
			
		if (pickButton != null)
		{
			pickButton.animator.Play(pickMeAnimName);
			Audio.play("FKPickMe");
			yield return new WaitForSeconds(1.0f);
			if (isButtonAvailableToSelect(pickButton))
			{
				pickButton.animator.Play(pickMeStillName);
			}
		}
	}

	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		revealWait.reset ();
		inputEnabled = false;
		winAll = false;
		WheelPick wheelPick = outcome.getNextEntry();

		removeButtonFromSelectableList(button);
		int pickIndex = getButtonIndex(button);
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);

		bool skipAllWin = true;
		currentWinAllID = -1;

		if (currentStage == 1 || currentStage == 2 || currentStage == 3)
		{
			skipAllWin = false;
			currentWinAllID = findWinAllID(wheelPick);
		}

		if ((wheelPick.canContinue && wheelPick.credits != 0) || (currentStage == 4 && wheelPick.credits != 0))
		{
			if (!skipAllWin && currentWinAllID == wheelPick.winID && currentStage != 4)
			{
				winAll = true;
				pickButtonData.animator.Play("RevealWinAll");
				Audio.play("FKRevealWinAllWolf");
			}
			else
			{
				Audio.play("FKPickItemRevealCredit");
				pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.credits);
				pickButtonData.animator.Play("RevealBonusCoin");
			}

			yield return new WaitForSeconds(1.0f);
		}
		else if (wheelPick.multiplier != 0)
		{
			pickButtonData.multiplierLabel.text = Localize.text("{0}X", (wheelPick.multiplier+1).ToString());
			pickButtonData.animator.Play("RevealChildrenIcon");
			Audio.play("FKRevealKids");
			Audio.play("KidsRescued");
			
			yield return new TIWaitForSeconds(1.0f);

			GameObject instancedSparkleTrail = CommonGameObject.instantiate(bonusSparkleTrail) as GameObject;
			instancedSparkleTrail.transform.parent = pickButtonData.revealNumberLabel.gameObject.transform;
			instancedSparkleTrail.transform.position = pickButtonData.revealNumberLabel.gameObject.transform.position;
			instancedSparkleTrail.transform.localScale = Vector3.one * 0.1f;

			iTween.MoveTo(instancedSparkleTrail,currentWinAmountTextWrapperNew.gameObject.transform.position, 0.5f);
		
			Audio.play("value_move");
			yield return new TIWaitForSeconds(0.5f);
			Audio.play("FKMultiplierLands");
			Destroy(instancedSparkleTrail);
			yield return new TIWaitForSeconds(0.25f);
		}
		else
		{
			pickButtonData.animator.Play("RevealEndBonus");
			Audio.play("FKRevealBad");

			yield return new WaitForSeconds(1.0f);
		}		

		if (wheelPick.multiplier != 0)
		{
			StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout * (wheelPick.multiplier+1)));
			BonusGamePresenter.instance.currentPayout *= (wheelPick.multiplier+1);
		}
		else
		{
			StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + wheelPick.credits));
			BonusGamePresenter.instance.currentPayout += wheelPick.credits;
		}

		int winsIndex = 0;
		for (int i = 0; i < roundButtonList[currentStage].revealNumberList.Length; i++)
		{
			if (winsIndex == wheelPick.winIndex)
			{
				winsIndex++;
			}

			if (i != pickIndex)
			{
				PickGameButtonData pick = getPickGameButton(i);

				if (winsIndex >= wheelPick.wins.Count)
				{
					// Default to a missed win if we run out of win indexes
					pick.extraLabel.text = CreditsEconomy.convertCredits((175 * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers));
					if(!revealWait.isSkipping)
					{
						Audio.play("FKRevealOthers");
					}
					pick.animator.Play("Not Selected Bonus Coin");
				}
				else if ((wheelPick.wins[winsIndex].canContinue && wheelPick.wins[winsIndex].credits != 0) || (currentStage == 4 && wheelPick.wins[winsIndex].credits != 0))
				{
					if (!skipAllWin && currentWinAllID == wheelPick.wins[winsIndex].winIndex && currentStage != 4)
					{
						if(!revealWait.isSkipping)
						{
							Audio.play("FKRevealOthers");
						}
						pick.animator.Play("Not Selected Win All");
					}
					else
					{
						if (!winAll)
						{
							if(!revealWait.isSkipping)
							{
								Audio.play("FKRevealOthers");
							}
							pick.extraLabel.text = CreditsEconomy.convertCredits(wheelPick.wins[winsIndex].credits);
							pick.animator.Play("Not Selected Bonus Coin");
						}
						else
						{
							if(!revealWait.isSkipping)
							{
								Audio.play("RevealSparklyCmajPhantom");
							}
							pick.revealNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.wins[winsIndex].credits);
							pick.animator.Play("RevealBonusCoin");
						}
					}
				}
				else if (wheelPick.wins[winsIndex].multiplier != 0)
				{
					if(!revealWait.isSkipping)
					{
						Audio.play("FKRevealOthers");
					}
					pick.multiplierOutlineLabel.text = Localize.text("{0}X", (wheelPick.wins[winsIndex].multiplier+1).ToString());
					pick.animator.Play("Not Selected children icon");
				}
				else
				{
					if(!revealWait.isSkipping)
					{
						Audio.play("FKRevealOthers");
					}
					pick.animator.Play("Not Selected End Bonus");
				}
				winsIndex++;
				yield return StartCoroutine(revealWait.wait(revealWaitTime));
			}
		}

		if (currentStage == 4)
		{
			Audio.play("FKIntroToLevel5");
		}
		Audio.play("FKIncreaseLevelWhoosh");
		yield return new TIWaitForSeconds(0.5f);

		if (wheelPick.canContinue)
		{
			if (currentStage == 4)
			{
				Audio.play("FKBonusBgLevel5");
			}
			else
			{
				Audio.play("BonusBgPhantom");
			}
			textMessage.Play("Text message_Up");
			logoTransition.Play("logo_Transition_on");
			yield return new TIWaitForSeconds(0.1f);
			continueToNextStage();
			if (currentStage == 4)
			{
				Audio.play("PHCaveLairBrotherhoodCantHideAnyLonger");
			}
			else
			{
				Audio.play("BonusIntroVOPhantom");
			}
			map.Play("Map_X_"+ (currentStage+1));
			yield return new TIWaitForSeconds(1.4f);
			roundTextWrapper.text = Localize.text("round_{0}", currentStage+1);
			roundTextOutlineWrapper.text = Localize.text("round_{0}", currentStage+1);
			if (currentStage == 4)
			{
				helpMessage1.SetActive(false);
				helpMessage2.SetActive(true);
			}
			logoTransition.Play("logo_Transition_off");
			textMessage.Play("Text message_Down");
			inputEnabled = true;
		}
		else
		{
			BonusGamePresenter.instance.gameEnded();
			Audio.play("SummaryVOPhantom");
		}
	}

	private int findWinAllID(WheelPick wheelPick)
	{
		long highest = 0;
		int winID = -1;
		for (int i = 0; i < wheelPick.wins.Count; i++)
		{
			if (wheelPick.wins[i].credits > highest)
			{
				highest = wheelPick.wins[i].credits;
				if (i == wheelPick.winIndex)
				{
					winID = wheelPick.winID;
				}
				else
				{
					winID = wheelPick.wins[i].winIndex;
				}
			}
		}

		return winID;
	}
	
}



