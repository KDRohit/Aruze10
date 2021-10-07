using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickingRoundCreditsThenMultiplierThenTotal : PickingRoundGO
{
	public override void initRound()
	{
		base.initRound();
		
		genericPickemGame.currentMultiplier = 1;
		
		if (genericPickemGame.currentStage < genericPickemGame.currentMultiplierLabels.Length)
		{
			genericPickemGame.currentMultiplierLabels[genericPickemGame.currentStage].text =
				Localize.text("{0}X", CommonText.formatNumber(genericPickemGame.currentMultiplier));
		}
		
	}
	
	public override IEnumerator pickemButtonPressedCoroutine(GameObject pickButton)
	{
		genericPickemGame.inputEnabled = false;
		
		PickGameButtonData pick = genericPickemGame.getPickGameButtonAndRemoveIt(pickButton);
		genericPickemGame.showPickemGlows(false);
		
		CorePickData pickData = genericPickemGame.getNextEntry();
		long credits = pickData.credits;
		long multiplier = pickData.multiplier;
		
		if (multiplier > 0)
		{
			yield return StartCoroutine(genericPickemGame.revealPickedCredits(pick, credits));
			
			credits *= genericPickemGame.currentMultiplier;
			
			yield return StartCoroutine(
				genericPickemGame.revealPickedMultiplier(
					pick, "{0}X", genericPickemGame.currentMultiplier, credits));
			
			yield return StartCoroutine(genericPickemGame.doSparkleTrail(pickButton));
			yield return StartCoroutine(genericPickemGame.waitAfterSparkleEffects());
			yield return StartCoroutine(genericPickemGame.addCredits(credits));
			genericPickemGame.stopRevealEffects();
			
			genericPickemGame.currentMultiplier += multiplier;

			if (genericPickemGame.currentStage < genericPickemGame.currentMultiplierLabels.Length)
			{
				genericPickemGame.currentMultiplierLabels[genericPickemGame.currentStage].text =
					Localize.text("{0}X", CommonText.formatNumber(genericPickemGame.currentMultiplier));
				
				genericPickemGame.playSceneSounds(SoundDefinition.PlayType.OnIncrementMultiplier);
				
				yield return StartCoroutine(
					genericPickemGame.playSceneAnimations(
						AnimationDefinition.PlayType.OnIncrementMultiplier));
			}
			
			genericPickemGame.showPickemGlows(true);
		}
		else
		{
			yield return StartCoroutine(genericPickemGame.revealPickedBadEnd(pick, credits));
			
			if (credits > 0)
			{
				yield return StartCoroutine(genericPickemGame.addCredits(credits));
			}
			
			genericPickemGame.hasGameEnded = true;
			yield return StartCoroutine(genericPickemGame.revealRemainingPicks());
		}
		
		genericPickemGame.inputEnabled = true;
	}
	
	public override void revealRemainingPick(PickGameButtonData pickData)
	{
		genericPickemGame.showPickemGlowShadow(pickData, false);
		
		CorePickData revealData = genericPickemGame.getNextReveal();
		long credits = revealData.credits;
		
		if (revealData.pick == "BAD")
		{
			genericPickemGame.revealUnpickedBadEnd(pickData, credits);
		}
		else if (revealData.credits > 0)
		{
			genericPickemGame.revealUnpickedCredits(pickData, credits);
		}
		else
		{
			Debug.LogErrorFormat("Revealed pick {0} has {1} credits! Shouldn't it be BAD??", revealData.pick, revealData.credits );
		}
	}
}