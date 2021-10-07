using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module for ignoring and skipping hte bonus acquired effects for a specific bonus type
for instance in aruze02 the picking bonus isn't triggered by BN symbols so needs to ignore playing them

Original Author: Scott Lepthien

Creation Date: 4/13/2017
*/
public class SkipPlayBonusAcquiredEffectsForBonusTypeModule : SlotModule 
{
	[SerializeField] bool isSkippingEffectsForChallenge = false;
	[SerializeField] bool isSkippingEffectsForFreespins = false;
	[SerializeField] bool isSkippingEffectsForPortal = false;
	[SerializeField] bool isSkippingEffectsForCredits = false;

	public override bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		return (isSkippingEffectsForChallenge && reelGame.outcome.isChallenge) 
			|| (isSkippingEffectsForFreespins && reelGame.outcome.isGifting)
			|| (isSkippingEffectsForPortal && reelGame.outcome.isPortal)
			|| (isSkippingEffectsForCredits && reelGame.outcome.isCredit);
	}
	
	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		// not doing anything since we are just ignoring and skipping the acquired effects
		yield break;
	}
}
