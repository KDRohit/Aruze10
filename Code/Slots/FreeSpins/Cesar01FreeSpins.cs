using UnityEngine;
using System.Collections;

//Cesar01 Freespins game needed to override the showUpdatedSpins and allow for a custom freespins awarded animation 
public class Cesar01FreeSpins : TumbleFreeSpinGame
{
	[SerializeField] private AnimationListController.AnimationInformation freespinAwardAnimation;           // Animation that plays over the main game area (reels)
	[SerializeField] private AnimationListController.AnimationInformation winBoxAnimation;                 // Animation that plays on the Spin Panel	
	[SerializeField] private float postAwardPause = 0.0f;
	[SerializeField] private LabelWrapperComponent amountTextLabel;
	[SerializeField] private float preAwardPause = 0.0f; // need this pause so that the anticipations have time to play their sounds before the retrigger banner sound goes
	
	public override IEnumerator showUpdatedSpins(int numberOfSpins)
	{
		//Set the number of freespins on the banner 
		if (amountTextLabel != null)
		{
			amountTextLabel.text = CommonText.formatNumber(numberOfSpins);
		}

		if (preAwardPause != 0.0f)
		{
			yield return new TIWaitForSeconds(preAwardPause);
		}

		//play the award animation info, should probably always have this if you are using this module
		if (freespinAwardAnimation != null)
		{
			yield return StartCoroutine(AnimationListController.playAnimationInformation(freespinAwardAnimation));
		}	

		//Update the number of freespins
		FreeSpinGame.instance.numberOfFreespinsRemaining += numberOfSpins;

		yield return new TIWaitForSeconds(postAwardPause);
	}

}
