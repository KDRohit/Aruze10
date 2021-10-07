using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FindJackpotWithCreditsAndBadEnd : PickingRoundGO
{
	[SerializeField] protected int numToFind;      // Find this many items to win the jackpot.
	[SerializeField] protected string findGroupId; // This is the code in SCAT that means you found the item.
	[SerializeField] protected string endGroupId;  // This is the code in SCAT that means bad end.
	
	[SerializeField] protected Animator[] foundItemAnimators;
	[SerializeField] protected string[] foundItemAnimSequence; // Set this if each item has its own special animation.
	[SerializeField] protected string[] sparkleTrailOnAnims; // Each item has its own special sparkle trail on animation.
	[SerializeField] protected Animator foundAllAnimator;
	
	[SerializeField] protected string foundAllSoundName;
	[SerializeField] protected int foundAllSoundDelay;
	[SerializeField] protected string jackpotWinSoundName;

	protected int numFound;
	protected long jackpotAmount;

	public override void initRound ()
	{
		if (genericPickemGame.outcomeType == BonusOutcomeTypeEnum.PickemOutcomeType)
		{	
			foreach (JSON paytableGroup in genericPickemGame.pickemOutcome.paytableGroups)
			{
				if (paytableGroup.getString("group_code", "") == findGroupId)
				{
					jackpotAmount = paytableGroup.getLong("credits", 0L);
					jackpotAmount *= GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;

					if (pickingRound.jackpotLabelWrapper != null)
					{
						pickingRound.jackpotLabelWrapper.text = CreditsEconomy.convertCredits(jackpotAmount);
					}
				}
			}
		}
	
		numFound = 0;
	}
	public override IEnumerator pickemButtonPressedCoroutine(GameObject pickButton)
	{
		genericPickemGame.inputEnabled = false;
		
		PickGameButtonData pick = genericPickemGame.getPickGameButtonAndRemoveIt(pickButton);
		genericPickemGame.showPickemGlows(false);

		CorePickData pickData = genericPickemGame.getNextEntry();
		
		string groupId = "";
		long credits = pickData.credits;

		if (genericPickemGame.outcomeType == BonusOutcomeTypeEnum.PickemOutcomeType)
		{
			PickemPick pickemPick = pickData as PickemPick;
			groupId = pickemPick.groupId;
		}

		if (findGroupId != "" && groupId == findGroupId)
		{
			numFound++;
			
			if (numFound < foundItemAnimSequence.Length)
			{
				pickingRound.animationNames.REVEAL_PICKED_SPECIAL1_ANIM_NAME =
					foundItemAnimSequence[numFound];
			}
			
			if (numFound < sparkleTrailOnAnims.Length)
			{
				pickingRound.sparkleTrailDefinition.INSTANCED_SPARKLE_TRAIL_ANIMATION_NAME_BY_ROUND =
					sparkleTrailOnAnims[numFound];
			}
			
			yield return StartCoroutine(genericPickemGame.revealPickedSpecial1(pick));
			
			Animator foundItemAnimator = null;
			if (numFound < foundItemAnimators.Length)
			{
				foundItemAnimator = foundItemAnimators[numFound];
			}
			if (foundItemAnimator != null)
			{	
				yield return StartCoroutine(genericPickemGame.doSparkleTrail(pickButton, foundItemAnimator.gameObject));
				yield return StartCoroutine(genericPickemGame.waitAfterSparkleEffects());
				genericPickemGame.stopRevealEffects();
			   
				foundItemAnimator.gameObject.SetActive(true);
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(foundItemAnimator, "on"));
			}
			
			// Just in case you notice that the find has credits,
			// The server doesn't count them, DO NOT ADD THEM!
			
			if (numFound == numToFind)
			{
				if (foundAllAnimator != null)
				{
					foundAllAnimator.Play("found all");
					Audio.play(foundAllSoundName, 1, 0, foundAllSoundDelay);
					
					yield return StartCoroutine(CommonAnimation.waitForAnimDur(foundAllAnimator));
					
					if (pickingRound.jackpotWinEffect != null)
					{
						pickingRound.jackpotWinEffect.SetActive(true);
						Audio.play(jackpotWinSoundName);
					}
					
					yield return StartCoroutine(genericPickemGame.addCredits(jackpotAmount));

					if (pickingRound.jackpotWinEffect != null)
					{
						pickingRound.jackpotWinEffect.SetActive(false);
					}
				}
			}
		}
		else if (credits > 0)
		{
			yield return StartCoroutine(genericPickemGame.revealPickedCredits(pick, credits));
			yield return StartCoroutine(genericPickemGame.addCredits(credits));
		}
		
		genericPickemGame.inputEnabled = true;
	}
	
	public override void revealRemainingPick(PickGameButtonData pick)
	{
		CorePickData revealData = genericPickemGame.getNextReveal();
		
		string groupId = "";
		long credits = revealData.credits;
		
		if (genericPickemGame.outcomeType == BonusOutcomeTypeEnum.PickemOutcomeType)
		{
			PickemPick pickemPick = revealData as PickemPick;
			groupId = pickemPick.groupId;
		}
		
		if (findGroupId != "" && groupId == findGroupId)
		{
			genericPickemGame.revealUnpickedSpecial1(pick, credits);
		}
		else if (credits > 0)
		{
			genericPickemGame.revealUnpickedCredits(pick, credits);
		}
	}
}
