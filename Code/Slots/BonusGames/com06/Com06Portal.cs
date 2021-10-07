using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Com06Portal : PickPortal 
{
	[SerializeField] protected Animator knifeAnimator;
	[SerializeField] protected string[] knifeAnimationNames;
	[SerializeField] protected float KNIFE_ANIM_LENGTH;
	[SerializeField] protected string KNIFE_THROW = "";

	protected override void setCreditText(PickGameButton button, bool isPick)
	{
		long madeUpCreditValue = SlotBaseGame.instance.getCreditMadeupValue();
		button.revealNumberLabel.text = CreditsEconomy.convertCredits(madeUpCreditValue);
		button.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(madeUpCreditValue);
	}

	protected override IEnumerator gameSpecificSelectedCoroutine(PickGameButton button)
	{
		Animator scrollAnimator = button.animator;
		scrollAnimator.Play(PICKME_ANIM_NAME);
		
		int buttonIndex = pickButtons.IndexOf(button);
		Audio.play(KNIFE_THROW);
		knifeAnimator.Play (knifeAnimationNames[buttonIndex]);

		yield return new TIWaitForSeconds(KNIFE_ANIM_LENGTH);
	}

}
