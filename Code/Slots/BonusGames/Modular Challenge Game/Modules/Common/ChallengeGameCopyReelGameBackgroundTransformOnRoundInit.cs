using UnityEngine;

/**
 * Module that can be used to copy the auto scaling transform information from the base game onto elements in a challenge game.
 * NOTE : The elements need to be setup in a way where they have the default scale that the base game background has when not auto
 * scaled.  That when when the auto scaling modification is done it is performed correctly.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 5/14/2020
 */
public class ChallengeGameCopyReelGameBackgroundTransformOnRoundInit : ChallengeGameModule
{
	[Tooltip("List of transforms which will have SlotBaseGame instance's ReelGameBackground scale and offset copied to them.")]
	[SerializeField] private Transform[] transformsToCopyTo;

	// executeOnRoundInit() section
	// executes right when a round starts or finishes initing.
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Overrides HAVE TO call base.executeOnRoundInit!
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		
		// Read the base game background script size and copy it into the transforms
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.reelGameBackground != null)
		{
			Transform baseGameBackgroundTransform = SlotBaseGame.instance.reelGameBackground.gameObject.transform;
			for (int i = 0; i < transformsToCopyTo.Length; i++)
			{
				Transform currentTransform = transformsToCopyTo[i];
				currentTransform.localScale = baseGameBackgroundTransform.localScale;
				currentTransform.localPosition = baseGameBackgroundTransform.localPosition;
			}
		}
	}
}
