using UnityEngine;
using System.Collections;

// The class that handles the specific ablilites of the Elvira02FreeSpins.
// A Sliding Slot game that keeps spinning as long as there are paylines.
// Once a payline is missed the reels slide back over to the LHS and the multipler goes down to 1.

public class Gen12FreeSpins : SlidingSlotFreeSpinGame 
{
	[SerializeField] private bool ignoreTransition = false;

	public override void initFreespins()
	{
		// If true, it prevents the game from suppressing the vo
		if (!ignoreTransition)
		{
			cameFromTransition = true;
		}

		base.initFreespins();
	}
}
