using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Elvis01Portal : PickPortal
{
	protected override void setCreditText(PickGameButton button, bool isPick)
	{
		button.revealNumberWrapper.text = SlotBaseGame.instance.getCreditBonusValueText(false);
	}

	/// Function to check all the free spin variations and see if the user got one
	protected override SlotOutcome getFreeSpinOutcome()
	{
		return SlotOutcome.getPickMajorFreeSpinOutcome(bonusOutcome, FREESPIN_OUTCOME_NAME);
	}
}
