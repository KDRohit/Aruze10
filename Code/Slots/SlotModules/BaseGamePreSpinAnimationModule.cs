using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BaseGamePreSpinAnimationModule : SlotModule 
{
	[Tooltip("AnimationList support for this script which can be used to replace a lot of what this script was doing by just handling it in a list")]
	[SerializeField] private AnimationListController.AnimationInformationList preGameEffectAnimationList; // Newer version to handle pre game effect lists without having to keep adding stuff in here
	[SerializeField] private Animator reelAnimator;
	[SerializeField] private string reelsAnimName;
	[SerializeField] private GameObject reels;
	[SerializeField] private GameObject ambientGameEffects;
	[SerializeField] private List<GameObject> objectsToActivateBeforeAnimation = new List<GameObject>();
	[SerializeField] private List<GameObject> objectsToActivateAfterAnimation = new List<GameObject>();
	[SerializeField] private List<GameObject> objectsToDeactivateAfterAnimation = new List<GameObject>();
	[SerializeField] private List<GameObject> objectsToDestroyAfterAnimation = new List<GameObject>();
	[SerializeField] private GameObject popupMessage;
	[SerializeField] private string popupMessageHideAnimation;

	[SerializeField] private float secondsToWaitBeforeReelAnimation = 1.0f;
	[SerializeField] private float secondsToWaitAfterReelAnimation = 1.0f;
	[SerializeField] private float secondsToWaitBeforeDestroyingObjects = 1.0f;

	[SerializeField] private bool isWaitingForReelAnimatorAnim = true;

	[Header("UI display")]
	[Tooltip("Hide top nav and spin panel with when game starts.")]
	[SerializeField] private bool isFadingOverlayPanels;
	[Tooltip("Time to fade spin and overlay panels")]
	[SerializeField] private float fadeInTime;
	[Tooltip("Delay the fade in until intro animations are complete")]
	[SerializeField] private float fadeInDelay;
	[Tooltip("This will allow the player to tap to skip the fade in delay, and use the skippedFadeInTime")]
	[SerializeField] private bool isFadeInSkippable;
	[Tooltip("Time to fade spin if player skips animations, set to 0 for immediate fade in.")]
	[SerializeField] private float skippedFadeInTime;

	public override bool needsToExecuteAfterLoadingScreenHidden()
	{
		return true;
	}
	
	public override IEnumerator executeAfterLoadingScreenHidden()
	{
		// any animations that can play in parallel can be added here
		// so we can wait for them to complete at the end
		List<TICoroutine> allCoroutines = new List<TICoroutine>();

		if (isFadingOverlayPanels)
		{
			fadeOutOverlayPanelsImmediately();
			allCoroutines.Add(StartCoroutine(fadeInOverlayPanels()));
		}

		foreach (GameObject objectToActivate in objectsToActivateBeforeAnimation)
		{
			objectToActivate.SetActive (true);
		}
		
		if (preGameEffectAnimationList != null && preGameEffectAnimationList.animInfoList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(preGameEffectAnimationList));
		}

		if (secondsToWaitBeforeReelAnimation > 0.0f)
		{
			yield return new TIWaitForSeconds(secondsToWaitBeforeReelAnimation);
		}

		if (reelAnimator != null && !string.IsNullOrEmpty(reelsAnimName))
		{
			if (isWaitingForReelAnimatorAnim)
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(reelAnimator, reelsAnimName));
			}
			else
			{
				reelAnimator.Play(reelsAnimName);
			}

			if (secondsToWaitAfterReelAnimation > 0.0f)
			{
				yield return new TIWaitForSeconds(secondsToWaitAfterReelAnimation);
			}
		}

		foreach (GameObject objectToActivate in objectsToActivateAfterAnimation)
		{
			objectToActivate.SetActive (true);
		}

		if (reels != null)
		{
			reels.SetActive(true);
		}	

		foreach (GameObject objectToDeactivate in objectsToDeactivateAfterAnimation)
		{
			objectToDeactivate.SetActive (false);
		}

		if (secondsToWaitAfterReelAnimation > 0.0f)
		{
			yield return new TIWaitForSeconds(secondsToWaitAfterReelAnimation);
		}

		if (ambientGameEffects != null)
		{
			ambientGameEffects.SetActive(true);
		}

		if (objectsToDestroyAfterAnimation.Count > 0)
		{
			if (secondsToWaitBeforeDestroyingObjects > 0.0f)
			{
				yield return new TIWaitForSeconds(secondsToWaitBeforeDestroyingObjects);
			}

			foreach (GameObject objectToDestroy in objectsToDestroyAfterAnimation)
			{
				Destroy(objectToDestroy);
			}

		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
	}

	public override bool needsToExecuteOnPreSpin()
	{
		if (popupMessage != null)
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnPreSpin()
	{
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(popupMessage.GetComponent<Animator>(), popupMessageHideAnimation));
		Destroy(popupMessage);
	}

	// instantly fades out the spin panel and overlay panel
	private void fadeOutOverlayPanelsImmediately()
	{
		SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
		Overlay.instance.top.show(true);
		SpinPanel.instance.fadeOutNow();
		Overlay.instance.fadeOutNow();
		SpinPanel.instance.hidePanels();
		Overlay.instance.top.show(false);
	}

	// fade in the spin panel and overlay panel
	private IEnumerator fadeInOverlayPanels()
	{
		// We use the fadeInDelay to wait until it's time to fade in. If the player taps to skip
		// the animations skip this delay by setting it to 0.
		while (fadeInDelay > 0)
		{
			fadeInDelay -= Time.deltaTime;

			// Checks if the player taps the screen to skip animations
			if (isFadeInSkippable && TouchInput.didTap)
			{
				fadeInDelay = 0;
				fadeInTime = skippedFadeInTime;
			}

			yield return null;
		}

		SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
		Overlay.instance.top.show(true);

		List<TICoroutine> fadeInCoroutines = new List<TICoroutine>();
		fadeInCoroutines.Add(StartCoroutine(Overlay.instance.fadeIn(fadeInTime)));
		fadeInCoroutines.Add(StartCoroutine(SpinPanel.instance.fadeIn(fadeInTime)));
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(fadeInCoroutines));
	}
}
