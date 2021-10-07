using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PortalWithLabelWrapperCredits : PickPortal 
{
	[SerializeField] protected bool pickemCreditsAreVertical = true;

	protected override void setCreditText(PickGameButton button, bool isPick)
	{
		if (isPick)
		{
			if (button.revealNumberWrapper != null)
			{
				button.revealNumberWrapper.text = SlotBaseGame.instance.getCreditBonusValueText(pickemCreditsAreVertical);
			}
		}
		else
		{
			if (button.revealGrayNumberWrapper != null)
			{
				button.revealGrayNumberWrapper.text = SlotBaseGame.instance.getCreditBonusValueText(pickemCreditsAreVertical);
			}
		}
	}
}
