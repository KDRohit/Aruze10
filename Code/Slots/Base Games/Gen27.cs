using UnityEngine;
using System.Collections;
using TMPro;
using TMProExtensions;

public class Gen27 : Hi03 
{
	public TextMeshPro[] sideInfoNumber;
	public TextMeshPro[] sideInfoText;
	public TextMeshPro[] upperSideInfoNumber;
	public TextMeshPro[] upperSideInfoText;

	public TextMeshPro jackpotAmount_TMPro_Txt;

	public string jackPotDollarAnimationPrefix;
	public string jackPotDollarAnimationPostfix;

	public Animator[] dollarIndicatorAnimators;
	public string indicatorOffAnimation;

	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
	}	

	// setup "lines" boxes on sides
	// the background for the ways lines boxes is baked in, so we have to do our own instead of spin panel doing it.
	protected override void setSpinPanelWaysToWin(string reelSetName)
	{
		initialWaysLinesNumber = slotGameData.getWinLines(reelSetName);

		upperSideInfoText[1].text = upperSideInfoText[0].text = Localize.textUpper("line");
		sideInfoText[0].text = sideInfoText[1].text = Localize.textUpper("lines");
		sideInfoNumber[0].text = sideInfoNumber[1].text = CommonText.formatNumber(initialWaysLinesNumber);
		upperSideInfoNumber[1].text = upperSideInfoNumber[0].text = CommonText.formatNumber(1);
	}

	protected override void resetSlotMessage()
	{
		if (jackpotAmount_TMPro_Txt != null)
		{
			jackpotAmount_TMPro_Txt.text = CreditsEconomy.convertCredits(multiplier * JACKPOT_AMOUNT * GameState.baseWagerMultiplier);
		}
		base.resetSlotMessage();
	}

	protected override void slotStartedEventCallback(JSON data)
	{
		base.slotStartedEventCallback(data);

		resetSlotMessage();
	}
	
	// Handle lighting dollar sign lights
	protected override IEnumerator lightDollarSignIndicator(int dollarLightIndex)
	{
		if (dollarLightIndex >= 0 && dollarLightIndex < activeDollars.Length)
		{
			if (dollarLightIndex < dollarIndicatorAnimators.Length && dollarIndicatorAnimators[dollarLightIndex] != null)
			{
				// oz07 has seperate animators for each indicator so we don't need to build an animation name
				dollarIndicatorAnimators[dollarLightIndex].Play(jackPotDollarAnimationPrefix);
			} 
			else if (meterAnimator != null)
			{
				// gen27 uses a single animator for each indicator
				meterAnimator.Play(jackPotDollarAnimationPrefix + (dollarLightIndex+1) + jackPotDollarAnimationPostfix);
			}
		}

		yield return null;
	}


	/// Reset the dollar sign indicator lights
	protected override void resetDollarLightIndicators()
	{
		if (dollarIndicatorAnimators.Length > 0)
		{
			foreach (Animator dollarAnimator in dollarIndicatorAnimators)
			{
				if (dollarAnimator != null)
				{
					dollarAnimator.Play(indicatorOffAnimation);
				}

			}
		}
		
		if (meterAnimator != null)
		{
			meterAnimator.Play(jackPotDeactivateAnimation);
		}
	}	

	/// Hide all of the anticipation effects
	protected override void hideAllAnticipationVFX()
	{
		// do nothing the hi03 base game calls this at the wrong moment turning off the anticiption before it even gets a chance to play
		// if the TW happens to appear on the top reel on its own without floating up from the bottom reel.
		// we turn it off correctly in all cases with the handleSpecificReelStop override
	}	

	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		yield return StartCoroutine(base.handleSpecificReelStop(stoppedReel));

		if (stoppedReel.layer == 1 && stoppedReel.reelID == 3 && isAnticipationVFXShowing())
		{
			// make sure the top anticipation is off
			base.hideAllAnticipationVFX();
		}

		yield return null;
	}
	
}
