using UnityEngine;
using System.Collections;

/*
 * If credits in this pick item are multiplied with an animation series, define here
 * (Used with PickingGameMultiplyCreditsModule.cs)
 */
public class PickingGameMultipliedCreditsPickItem : PickingGameBasePickItemAccessor 
{
	[SerializeField] private LabelWrapperComponent multiplierLabel;
	[SerializeField] private AnimationListController.AnimationInformationList creditMultiplyAnimations;

	// Set the appropriate label value & play the animation list
	public IEnumerator playMultipliedEffects(int multiplierValue)
	{
		if (multiplierLabel != null)
		{
			multiplierLabel.text = Localize.text("{0}X", multiplierValue);
		}

		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(creditMultiplyAnimations));
	}
}