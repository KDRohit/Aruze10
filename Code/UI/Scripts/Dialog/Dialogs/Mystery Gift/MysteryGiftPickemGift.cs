using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Property holder for Mystery Gift pickem boxes.
*/

public class MysteryGiftPickemGift : MysteryGiftBasePickem
{

	public override void setup (int index)
	{
		// Nothing required for HIR Mystery Gift.
	}
	
	public override IEnumerator pick(WheelPick data)
	{
		// Make sure everything is inactive by default.
		hideAll();
		
		// Now activate just the thing we need.
		
		if (data.bonusGame == "mystery_gift_scratch_card")
		{
			// Scratch card.
			scratcher.SetActive(true);
		}
		else if (data.bonusGame == "mystery_gift_wheel")
		{
			// Wheel.
			wheel.SetActive(true);
		}
		else if (data.multiplier > 0)
		{
			// Double bet.
			doubleBet.SetActive(true);
		}
		else if (data.credits > 0)
		{
			// Plain multiplier.
			multiplierSprite.gameObject.SetActive(true);
			multiplierSprite.spriteName = string.Format("{0}x", data.baseCredits);
			multiplierSprite.MakePixelPerfect();
		}
		else
		{
			// This should never happen.
			Debug.LogError("Unexpected data to pick mystery gift.");
			gameObject.SetActive(false);
			yield break;
		}
		
		animator.Play("gift_box_reveal");
		
		yield return null;	// Wait a frame for the animation to start playing before we can get the current animator state info.
		yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
	}
	
	public override void reveal(WheelPick data)
	{
		// Make sure everything is inactive by default.
		hideAll();
				
		// Now activate just the thing we need.
		
		if (data.bonusGame == "mystery_gift_scratch_card")
		{
			// Scratch card.
			scratcherRevealed.SetActive(true);
		}
		else if (data.bonusGame == "mystery_gift_wheel")
		{
			// Wheel.
			wheelRevealed.SetActive(true);
		}
		else if (data.multiplier > 0)
		{
			// Double bet.
			doubleBetRevealed.SetActive(true);
		}
		else if (data.credits > 0)
		{
			// Plain multiplier.
			multiplierSprite.gameObject.SetActive(true);
			multiplierSprite.spriteName = string.Format("{0}x_desat", data.baseCredits);
			multiplierSprite.MakePixelPerfect();
		}
		else
		{
			// This should never happen.
			Debug.LogError("Unexpected data to reveal mystery gift.");
			gameObject.SetActive(false);
			return;
		}

		animator.Play("gift_box_revealed");
	}
	
	protected override void hideAll()
	{
		multiplierSprite.gameObject.SetActive(false);
		scratcher.SetActive(false);
		scratcherRevealed.SetActive(false);
		wheel.SetActive(false);
		wheelRevealed.SetActive(false);
		doubleBet.SetActive(false);
		doubleBetRevealed.SetActive(false);	
	}
}
