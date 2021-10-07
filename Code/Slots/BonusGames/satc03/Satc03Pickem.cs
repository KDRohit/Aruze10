using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
class Satc03PickRound
{
	public string discriminator = null;
	public GameObject topLevelObject;
	public Animator card;
	
	public List<GameObject> thingsToHideOnRoundInit = new List<GameObject>();
}

public class Satc03Pickem : PickemGameStagesUsingWheelPicks {

	[SerializeField] private Satc03PickRound[] rounds;
	
	[SerializeField] private string pickemRevealCreditsAudioKey = "pickem_reveal_win";
	[SerializeField] private string pickemRevealExtraPicksAudioKey = "pickem_reveal_other";
	[SerializeField] private float pickemRevealExtraPicksAudioKeyDelay = 0.0f;
	[SerializeField] private string sparkleTrailLeaveAudioKey = "pickem_multiplier_travel";
	[SerializeField] private string sparkleTrailArriveAudioKey = "pickem_multiplier_arrive";
	[SerializeField] private string bgMusicAudioKey = "bonus_bg";
	[SerializeField] private string bonusSummaryVOAudioKey = "bonus_summary_vo";
	[SerializeField] private string revealNotChosenAudioKey = "reveal_not_chosen";
	[SerializeField] private float revealNotChosenAudioKeyDelay = 0.0f;
	[SerializeField] private string bonusIntroVOAudioKey = "bonus_intro_vo";
	[SerializeField] private string selectionAudioKey = "pickem_pick_selected";
	[SerializeField] private string pickemRoundSelectedAudioKey = "pickem_round_selected";
	[SerializeField] private string pickemPopulateAudioKey = "pickem_round_selected";
	
	[SerializeField] private string offAnimState = "default";
	[SerializeField] private string populateAnimName = "populate";
	[SerializeField] private string stillGrayAnimName = "stillGray";
	[SerializeField] private string revealCreditAnimName = "revealCredit";
	[SerializeField] private string revealCreditGrayAnimName = "revealCreditGray";
	[SerializeField] private string revealBonusPlus1AnimName = "revealMrBig+1";
	[SerializeField] private string revealBonusPlus1GrayAnimName = "revealMrBig+1Gray";
	
	[SerializeField] private string cardDefaultAnimName = "card_default";
	[SerializeField] private string cardUpAnimName = "card_up";
	[SerializeField] private string cardActiveAnimName = "card_active";
	[SerializeField] private string cardDeactiveAnimName = "card_deactive";
	
	[SerializeField] private float delayBeforeRevealingRemainingPicks = 0.5f;
	[SerializeField] private float delayBeforeInitiatingNextRound = 1.5f;
	[SerializeField] private float timeBetweenRandomCard = 0.125f;
	[SerializeField] private float cardActivationDelay = 0.5f;
	[SerializeField] private float sparkleTrailDelay = 1.0f;
	[SerializeField] private float sparkleExplosionDelay = 1.0f;
	[SerializeField] private float populateItemDelay = 0.0625f;
	[SerializeField] private float rollupAfterCreditDelay = 1.0f;
	[SerializeField] private float pickemRoundSelectedSoundDelay = 0.5f;
	[SerializeField] private float beforeBonusSparkleTrailDelay = 0.75f;
	
	[SerializeField] private int numberOfCardShuffles = 15;
	
	// Since we have multiple sparkle explosions animating at the same time, we need to maintain some instances we can move around and play
	private List<GameObject> sparkleExplosionInstances = new List<GameObject>();
	
	private void generateSparkleExplosionInstances()
	{
		int maxInstances = 0;
		
		for(var i = 0; i < rounds.Length; i++)
		{
			if(roundButtonList[i].animatorList.Length > maxInstances)
			{
				maxInstances = roundButtonList[i].animatorList.Length;
			}
		}
		
		for (int i = 0; i < maxInstances; i++)
		{
			GameObject obj = CommonGameObject.instantiate(bonusSparkleExplosion) as GameObject;
			obj.transform.parent = bonusSparkleExplosion.transform.parent;
			obj.transform.position = bonusSparkleExplosion.transform.position;
			obj.transform.localScale = bonusSparkleExplosion.transform.localScale;
			sparkleExplosionInstances.Add(obj);
		}
	}
	
	public override void init()
	{
		base.init();
		Audio.play(Audio.soundMap(bonusIntroVOAudioKey));
		Audio.switchMusicKeyImmediate(Audio.soundMap(bgMusicAudioKey));
		generateSparkleExplosionInstances();
	}

	// override this function to stop the music before ending the game, not sure
	// why we have to do this manually in this game, it's a bit hacky
	protected override void endGame()
	{
		Audio.switchMusicKeyImmediate(""); // force the music off so the summary music starts right away
		Audio.play(Audio.soundMap(bonusSummaryVOAudioKey));
		BonusGamePresenter.instance.gameEnded();
	}
	
	private void deactivateCurrentStage()
	{
		for(int i = 0; i < rounds.Length; i++)
		{
			// Play sound here
			rounds[i].card.Play(cardDefaultAnimName);
		}
		
		rounds[currentStage].topLevelObject.SetActive(false);
	}
	
	private IEnumerator populatePickemObjects()
	{
		for (int i = 0; i < roundButtonList[currentStage].animatorList.Length; i++)
		{
			Audio.play(Audio.soundMap(pickemPopulateAudioKey));
			roundButtonList[currentStage].buttonList[i].SetActive(true);
			roundButtonList[currentStage].animatorList[i].Play(populateAnimName);
			
			sparkleExplosionInstances[i].transform.position = roundButtonList[currentStage].buttonList[i].transform.position;
			sparkleExplosionInstances[i].GetComponent<Animator>().Play("anim");
			
			yield return new TIWaitForSeconds(populateItemDelay);
		}
	}
	
	private IEnumerator activateCurrentStage()
	{
		for(int i = 0; i < rounds.Length; i++)
		{		
			Audio.play(Audio.soundMap(pickemRoundSelectedAudioKey), 1.0f, 0.0f, pickemRoundSelectedSoundDelay);
			rounds[i].card.Play(cardDeactiveAnimName);
		}
		
		for(var i = 0; i < rounds.Length; i++)
		{
			if(wheelPick.paytableName.Contains(rounds[i].discriminator))
			{
				currentStage = i;
				rounds[i].card.Play(cardDefaultAnimName);
				yield return new TIWaitForSeconds(timeBetweenRandomCard);
				rounds[i].card.Play(cardActiveAnimName);
				yield return new TIWaitForSeconds(cardActivationDelay);
				rounds[i].topLevelObject.SetActive(true);
				
				foreach(var obj in rounds[i].thingsToHideOnRoundInit)
				{
					obj.SetActive(false);
				}
				
				yield return StartCoroutine(populatePickemObjects());
				break;
			}
		}
	}

	private IEnumerator shuffleCards()
	{
		for(int i = 0; i < rounds.Length; i++)
		{
			rounds[i].card.Play(cardDeactiveAnimName);
		}
		
		int prevCard = -1;
		
		for(int x = 0; x < numberOfCardShuffles; x++)
		{
			int i = Random.Range(0, rounds.Length);
			
			// Keep rolling random numbers till you get one that isn't the last one
			while(i == prevCard)
			{
				i = Random.Range(0, rounds.Length);
			}
			
			if(prevCard != -1)
			{
				rounds[prevCard].card.Play(cardDeactiveAnimName);
			}
			
			Audio.play(Audio.soundMap(selectionAudioKey));
			rounds[i].card.Play(cardUpAnimName);
			prevCard = i;
			
			yield return new TIWaitForSeconds(timeBetweenRandomCard);
		}
	}
	
	protected override IEnumerator beginPrePickAnimationSequence()
	{	
		deactivateCurrentStage();
		
		yield return StartCoroutine(shuffleCards());
		yield return StartCoroutine(activateCurrentStage());
		
		// We need to do this explicitly at this point
		inputEnabled = true;
			
		yield return null;
	}
	
	private IEnumerator rollUpScore()
	{
		if(winBox != null)
		{
			winBox.GetComponent<Animator>().Play(winboxAnimationName);
		}
		
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + amountWonThisRound, winAmountLabelWrapper));
		
		BonusGamePresenter.instance.currentPayout += amountWonThisRound;
	}

	protected override IEnumerator beginPostPickSequence()
	{	
		revealSinglePickemObject();
	
		if(wheelPick.extraRound > 0)
		{
			Audio.play(Audio.soundMap(pickemRevealExtraPicksAudioKey), 1.0f, 0.0f, pickemRevealExtraPicksAudioKeyDelay);

			yield return new WaitForSeconds(beforeBonusSparkleTrailDelay);

			picksRemaining = picksRemaining + wheelPick.extraRound;
			
			PickGameButtonData pick = getPickGameButton(pickButtonIndex);
			bonusSparkleTrail.transform.parent = pick.button.transform;
			Vector3 start = pick.button.gameObject.transform.position;
			Vector3 end = pickCountLabelWrapper.gameObject.transform.position;
			bonusSparkleTrail.transform.position = start;
			bonusSparkleTrail.transform.localScale = Vector3.one * 0.1f;
			bonusSparkleTrail.GetComponent<Animator>().Play("intro");
			
			iTween.MoveTo(bonusSparkleTrail, end, sparkleTrailDelay);
			
			Audio.play(Audio.soundMap(sparkleTrailLeaveAudioKey));
			
			yield return new WaitForSeconds(sparkleTrailDelay);
			
			Audio.play(Audio.soundMap(sparkleTrailArriveAudioKey));
			bonusSparkleTrail.GetComponent<Animator>().Play("default");
			
			bonusSparkleExplosion.transform.position = end;
			bonusSparkleExplosion.GetComponent<Animator>().Play("anim");
			
			pickCountLabelWrapper.text = picksRemaining.ToString();
			
			yield return new WaitForSeconds(sparkleExplosionDelay);
		}
		else
		{
			// If we get in here, we are rolling up credits, give some time for the reveal animation before rolling up credits
			yield return new WaitForSeconds(rollupAfterCreditDelay);
		}
		
		yield return StartCoroutine(rollUpScore());
		
		yield return new WaitForSeconds(delayBeforeRevealingRemainingPicks);
	
		yield return StartCoroutine(revealRemainingObjects());
		
		beginPrePickSequence();
	}

	protected new void revealSinglePickemObject()
	{	
		PickGameButtonData pick = getPickGameButton(pickButtonIndex);
		
		Audio.play(Audio.soundMap(pickemRevealCreditsAudioKey));
		
		if (wheelPick.extraRound > 0)
		{
			pick.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(wheelPick.credits);
			pick.animator.Play(revealBonusPlus1AnimName);
		}
		else
		{
			pick.revealNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.credits);
			pick.animator.Play(revealCreditAnimName);
		}
		
		setRemainingObjectsToStillGrayState();
	}

	private void setRemainingObjectsToStillGrayState()
	{
		for (int i = 0; i < roundButtonList[currentStage].buttonList.Length; i++)
		{	
			if (i != pickButtonIndex)
			{
				PickGameButtonData pick = getPickGameButton(i);
				pick.animator.Play(stillGrayAnimName);
			}
		}
	}
	
	private void setRemainingObjectsOffState()
	{
		for (int i = 0; i < roundButtonList[currentStage].buttonList.Length; i++)
		{
			PickGameButtonData pick = getPickGameButton(i);
			pick.animator.Play(offAnimState);
		}
	}
	
	private IEnumerator revealRemainingObjects()
	{	
		revealWait.reset ();
		int winIndex = 0;
		
		for (int i = 0; i < roundButtonList[currentStage].buttonList.Length; i++)
		{	
			if (i != pickButtonIndex)
			{
				// Skip the current win
				if(winIndex == wheelPick.winIndex)
				{
					winIndex++;
				}
				
				PickGameButtonData pick = getPickGameButton(i);
				
				if (wheelPick.wins[winIndex].extraRound > 0)
				{
					pick.revealGrayNumberOutlineLabel.text = CreditsEconomy.convertCredits(wheelPick.wins[winIndex].credits);
					pick.animator.Play(revealBonusPlus1GrayAnimName);
				}
				else
				{
					pick.revealGrayNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.wins[winIndex].credits);
					pick.animator.Play(revealCreditGrayAnimName);
				}

				Audio.play(Audio.soundMap(revealNotChosenAudioKey), 1.0f, 0.0f, revealNotChosenAudioKeyDelay);

				// stagger the reveals
				yield return StartCoroutine(revealWait.wait(revealWaitTime));
				
				winIndex++;
			}
		}
		
		yield return new TIWaitForSeconds(delayBeforeInitiatingNextRound);
	}
}
