using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class that causes basegame symbol wins to be paid out before freespins in base happens.
This is different from the standard flow where they are just rolled into the win meter and
then carried over into the freespins.  This was first used for gen97 Cash Tower where a
multiplier that could trigger in the freespins and applied to the win meter was not supposed
to apply to the base game winnings so they had to be already awarded before freespins.

Creation Date: 5/5/2020
Original Author: Scott Lepthien
*/
public class PayBasegameWinsBeforeFreespinsInBaseModule : SlotModule
{
	public override bool isPayingBasegameWinsBeforeFreespinsInBase()
	{
		return true;
	}
}
