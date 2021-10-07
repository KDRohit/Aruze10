using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickingRoundJackpotLadder : PickingRoundGO
{
	[SerializeField] protected LabelWrapper[] ladderJackpotLabels;
	protected List<long> ladderJackpotCredits = new List<long>();
		
	[SerializeField] protected float WAIT_BETWEEN_MULTIPLY_CREDITS = 0.25f;
	
	protected int jackpotIndex;
	protected int numJackpots;
	
	[SerializeField] protected RevealDefinition multiplierRevealEffect;
	
	public override void initRound()
	{
		foreach (CorePickData pickData in genericPickemGame.pickemOutcome.entries)
		{
			long credits = pickData.credits;
			
			if (credits > 0 && !ladderJackpotCredits.Contains(credits))
			{
				ladderJackpotCredits.Add(credits);
			}
		}
		
		foreach (CorePickData pickData in genericPickemGame.pickemOutcome.reveals)
		{
			long credits = pickData.credits;
			
			if (credits > 0 && !ladderJackpotCredits.Contains(credits))
			{
				ladderJackpotCredits.Add(credits);
			}
		}
		
		if (ladderJackpotLabels.Length != ladderJackpotCredits.Count)
		{
			Debug.LogError("The number of jackpot labels does NOT match the number of jackpots!");
		}
		
		ladderJackpotCredits.Sort();
		numJackpots = Mathf.Min(ladderJackpotLabels.Length, ladderJackpotCredits.Count);
		
		for (jackpotIndex = 0; jackpotIndex < numJackpots; jackpotIndex++)
		{
			long credits = ladderJackpotCredits[jackpotIndex];
			ladderJackpotLabels[jackpotIndex].text = CreditsEconomy.convertCredits(credits);
		}
		
		jackpotIndex = 0;
	}
	
	public override IEnumerator pickemButtonPressedCoroutine(GameObject pickButton)
	{
		genericPickemGame.playPickemPickedSound();
		
		genericPickemGame.inputEnabled = false;
		PickGameButtonData pick = genericPickemGame.getPickGameButtonAndRemoveIt(pickButton);
		
		CorePickData pickData = genericPickemGame.getNextEntry();
		
		if (pickData.multiplier > 0)
		{
			yield return StartCoroutine(genericPickemGame.revealPickedMultiplier(pick));
			
			for (int iJackpot = jackpotIndex; iJackpot < numJackpots; iJackpot++)
			{
				yield return new WaitForSeconds(WAIT_BETWEEN_MULTIPLY_CREDITS);

				yield return StartCoroutine(
					genericPickemGame.doSparkleTrail(
						pick.extraGo,
						ladderJackpotLabels[iJackpot].gameObject,
						false,
						-1,
						multiplierRevealEffect));
					
				yield return StartCoroutine(genericPickemGame.waitAfterSparkleEffects());
				
				long credits = 2 * ladderJackpotCredits[iJackpot];
				ladderJackpotCredits[iJackpot] = credits;
				ladderJackpotLabels[iJackpot].text = CreditsEconomy.convertCredits(credits);
			}
			
			genericPickemGame.inputEnabled = true;
		}
		else if (pickData.credits > 0)
		{
			long credits = ladderJackpotCredits[jackpotIndex];
			yield return StartCoroutine(genericPickemGame.revealPickedCredits(pick, credits));
			
			yield return StartCoroutine(
				genericPickemGame.playSceneAnimations(
					AnimationDefinition.PlayType.PreCreditsReveal, jackpotIndex));
					
			yield return StartCoroutine(
				genericPickemGame.playSceneAnimations(AnimationDefinition.PlayType.Jackpot, jackpotIndex));

			yield return StartCoroutine(
				genericPickemGame.doSparkleTrail(
					ladderJackpotLabels[jackpotIndex].gameObject,
					genericPickemGame.currentWinAmountTextWrappers[genericPickemGame.currentStage].gameObject));
					
			yield return StartCoroutine(genericPickemGame.waitAfterSparkleEffects());
			yield return StartCoroutine(genericPickemGame.addCredits(credits));
			
			yield return StartCoroutine(
				genericPickemGame.playSceneAnimations(
					AnimationDefinition.PlayType.PostCreditsReveal, jackpotIndex));

			yield return StartCoroutine(
				genericPickemGame.playSceneAnimations(
					AnimationDefinition.PlayType.PostJackpotReveal, jackpotIndex));

			jackpotIndex++;
			genericPickemGame.inputEnabled = true;
		}
		else
		{
			yield return StartCoroutine(genericPickemGame.revealPickedBadEnd(pick));
			
			yield return StartCoroutine(
				genericPickemGame.playSceneAnimations(
					AnimationDefinition.PlayType.PostBadReveal, jackpotIndex));
		}
	}
	
	public override void revealRemainingPick(PickGameButtonData pick)
	{
		CorePickData revealData = genericPickemGame.getNextReveal();

		if (revealData.multiplier > 0)
		{
			genericPickemGame.revealUnpickedMultiplier(pick);
		}
		else
		if (revealData.credits > 0)
		{
			genericPickemGame.revealUnpickedCredits(pick, 0);
		}
		else
		{
			genericPickemGame.revealUnpickedBadEnd(pick);
		}
	}
}
