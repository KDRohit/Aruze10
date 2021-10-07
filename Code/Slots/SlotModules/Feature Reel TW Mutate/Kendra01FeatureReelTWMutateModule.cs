using UnityEngine;
using System.Collections;

/**
Module for kendra01 Kendra On Top where getting a TR on the feature reel triggers a TW replacement feature
Needed because we need special handling to reset an animation on a big win happening
*/
public class Kendra01FeatureReelTWMutateModule : FeatureReelTWMutateModule 
{
	// executeOnBigWinEnd() section
	// Functions here are executed after the big win has been removed from the screen.
	public override bool needsToExecuteOnBigWinEnd()
	{
		if (multiplierPayBoxModule != null 
			&& multiplierPayBoxModule.getCurrentFeatureEnum() != MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.None 
			&& multiplierPayBoxModule.getCurrentFeatureEnum() != MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.BN)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public override void executeOnBigWinEnd()
	{
		multiplierPayBoxModule.resetBoxDisplayAnimationToEnd();
	}
}
