using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gen11Portal : PickPortal 
{

	[SerializeField] protected bool pickemCreditsAreVerticle = true;

	protected override void setCreditText(PickGameButton button, bool isPick)
	{
		if (isPick)
		{
			if (button.extraLabel != null)
			{
				button.revealNumberLabel.text = SlotBaseGame.instance.getCreditBonusValueText(pickemCreditsAreVerticle);
			}
		}
		else
		{
			if(button.extraLabel != null)
			{
				button.extraLabel.text = SlotBaseGame.instance.getCreditBonusValueText(pickemCreditsAreVerticle);
			}
		}
	}

	/// Function to check all the free spin variations and see if the user got one
	protected override SlotOutcome getFreeSpinOutcome()
	{
		return SlotOutcome.getPickMajorFreeSpinOutcome(bonusOutcome, FREESPIN_OUTCOME_NAME);
	}
}
