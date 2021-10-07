using System.Collections;
using UnityEngine;

public class SpinBoxAnimationModule : GrantFreespinsModule
{
	[SerializeField] AnimationListController.AnimationInformationList spinBoxAnimationList = new AnimationListController.AnimationInformationList();
	[SerializeField] bool useSpinCountLabelPosition;
	[SerializeField] string amountTextLabelPrefix;
	[SerializeField] private LabelWrapperComponent amountTextLabel;
	[SerializeField] private float INCREMENT_FREESPIN_COUNT_DELAY = 0.0f;

	// Overriding this so that we can control when the RETRIGGER_BANNER_SOUND is played
	protected override void incrementFreespinCount()
	{
		reelGame.numberOfFreespinsRemaining += numberOfFreeSpins;
	}

	// create a delay before the count is incremented, this will allow the count to increment during the animation list
	// if you want the count to increment at the end of all the animations, then this delay should be the total of all
	// of the animation times that will be played
	private IEnumerator incrementFreespinCountAfterDelay(float delay)
	{
		yield return new TIWaitForSeconds(delay);
		incrementFreespinCount();
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Now start the increment of the freespin count with a delay, that way it can trigger during the animation list
		TICoroutine incrementFreespinCountCoroutine = StartCoroutine(incrementFreespinCountAfterDelay(INCREMENT_FREESPIN_COUNT_DELAY));

		// Set the amount labels if they are set before we start the animations
		amountTextLabel.text = amountTextLabelPrefix + CommonText.formatNumber(numberOfFreeSpins);

		if (useSpinCountLabelPosition)
		{
			foreach (AnimationListController.AnimationInformation animInfo in spinBoxAnimationList.animInfoList)
			{
				animInfo.targetAnimator.transform.position = BonusSpinPanel.instance.spinCountLabel.transform.position;
			}
		}

		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(spinBoxAnimationList));

		// make sure that the increment coroutine has finished before we stop blocking
		if (incrementFreespinCountCoroutine != null)
		{
			while (!incrementFreespinCountCoroutine.isFinished)
			{
				yield return null;
			}
		}
	}
}
