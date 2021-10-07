using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

//A super simple class to play one of the new animation infor on slot game start
public class PlayAnimationListOnSlotGameStartModule : SlotModule
{
	[SerializeField] private AnimationListController.AnimationInformationList animationInfo;
	[SerializeField] private bool delayUIPanelsReveal;
	[SerializeField] private float fadeInUIPanelsDelay;
	[SerializeField] private float fadeInUIPanelsDuration;
	public UnityEvent onAnimationCompleteEvent;
	
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return reelGame.isFreeSpinGame();
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationInfo));
	}

	public override bool needsToExecuteAfterLoadingScreenHidden()
	{
		return !reelGame.isFreeSpinGame();
	}

	public override IEnumerator executeAfterLoadingScreenHidden()
	{
		List<TICoroutine> revealUICoroutines = new List<TICoroutine>();
		if (delayUIPanelsReveal && fadeInUIPanelsDelay > 0.0f)
		{
			revealUICoroutines.Add(StartCoroutine(delaySpinPanelReveal()));
		}
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationInfo, revealUICoroutines));
		onAnimationCompleteEvent.Invoke();
	}

	private IEnumerator delaySpinPanelReveal()
	{
		Overlay.instance.fadeOutNow();
		SpinPanel.instance.fadeOutNow(); // call fade out so we can restore alpha later
		Overlay.instance.top.show(false); // deactivate panels so update doesn't change alpha values
		SpinPanel.instance.hidePanels();
		float timeElapsed = 0.0f;
		// manually roll up timer so we can handle tap to skip
		while (timeElapsed < fadeInUIPanelsDelay)
		{
			timeElapsed += Time.deltaTime;
			if (TouchInput.didTap && animationInfo.isAllowingTapToSkip)
			{
				// show UI panel immediately
				yield return StartCoroutine(revealSpinPanel());
				yield break;
			}
			yield return null;
		}
		yield return StartCoroutine(revealSpinPanel());
	}

	private IEnumerator revealSpinPanel()
	{
		Overlay.instance.top.show(true);
		SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
		StartCoroutine(Overlay.instance.fadeIn(fadeInUIPanelsDuration));
		StartCoroutine(SpinPanel.instance.fadeIn(fadeInUIPanelsDuration));
		yield return new WaitForSeconds(fadeInUIPanelsDuration);
	}
}