using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Files that exists to set the stop order for this game, since it needs a fairly custom one due to having
 * different types of reel layers.
 *
 * Original Author: Carl Gloria
 * Creation Date: 5/29/2019
 */
public class Zynga06 : IndependentReelBaseGame 
{
	protected override void Awake()
	{
		// prevents adding BONUS_SYMBOL_FANFARE_KEY_PREFIX added to anticipation reel in base class
		// so 3rd BN symbol does not have 2 fanfare sounds when it lands
		isDoingSoundOverrides = false;

		base.Awake();
	}
}