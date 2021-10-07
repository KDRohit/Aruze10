using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module which keeps track of and populates jackpot ladder style games based on paytable data
 */

public class JackpotLabel : TICoroutineMonoBehaviour
{
	[SerializeField] public LabelWrapperComponent label;
	[SerializeField] public AnimationListController.AnimationInformationList multiplierRevealAnimation;
	[SerializeField] public AnimationListController.AnimationInformationList creditWinRevealAnimation;
	[SerializeField] public AnimationListController.AnimationInformationList badEndRevealAnimation;

	// When a rank is attained, flagging this as not available let's other modules know that this rank has already been attained
	[System.NonSerialized] public bool isAvailable = true;
	[System.NonSerialized] public long initialCredits = 0;
}
