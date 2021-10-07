using UnityEngine;
using System.Collections;
//This handles linking this pick to the round variant we want to go to.
public class PickingGameLinkedRoundVariantPickItem : PickingGameBasePickItemAccessor
{	
	[SerializeField] private ModularChallengeGameVariant _linkedRound;

	public ModularChallengeGameVariant linkedRound
	{
		get { return _linkedRound; }
	}
}
