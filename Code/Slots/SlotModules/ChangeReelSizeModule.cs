using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This module uses the payline set key to change the size of the reel using animations and updates the payline size
public class ChangeReelSizeModule : SlotModule 
{
	[SerializeField] private Animator reelsAnimator;
	[SerializeField] private List<ReelChangeData> reelChangeData;
	[SerializeField] private List<ReelChangeData> reelResetData;
	[SerializeField] private string basePaylineSet;
	[SerializeField] private float PRE_REEL_CHANGE_FANFARE_AUDIO_DELAY = 0.0f;

	private string previousPaylineSet;

	private const string PRE_REEL_CHANGE_FANFARE_AUDIO_KEY = "pre_scale_fanfare";

	public override bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		if (stoppingReel.reelID > reelGame.outcome.getNumberOfVisibleReels())
		{
			return true;
		}
		return false;
	}

	public override void executeOnSpecificReelStopping(SlotReel stoppingReel)
	{
		stoppingReel.shouldPlayReelStopSound = false;
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		if (string.IsNullOrEmpty(previousPaylineSet))
		{
			previousPaylineSet = basePaylineSet;
		}

		string currentPaylineSet = reelGame.outcome.getPayLineSet();

		if (currentPaylineSet != previousPaylineSet)
		{
			if (previousPaylineSet != basePaylineSet)
			{
				foreach (ReelChangeData reset in reelResetData)
				{
					if (reset.paylineSet == previousPaylineSet)
					{
						yield return StartCoroutine(animateReel(reset));
						foreach (GameObject reelParent in reset.reelParents)
						{
							reelParent.transform.localScale = Vector3.zero;
						}
					}
				}
			}

			if (currentPaylineSet != basePaylineSet)
			{
				foreach (ReelChangeData change in reelChangeData)
				{
					if (change.paylineSet == currentPaylineSet)
					{
						foreach (GameObject reelParent in change.reelParents)
						{
							reelParent.transform.localScale = change.reelParentsScale;
						}
						yield return StartCoroutine(animateReel(change));
					}
				}
			}
		}

		previousPaylineSet = currentPaylineSet;
	}

	private IEnumerator animateReel(ReelChangeData data)
	{
		updatePaylineScale(data.verticalSpacingMultiplier, data.payboxSizeMultiplier);

		if (data.preReelChangeEffectAnimator != null && !string.IsNullOrEmpty(data.preReelChangeEffectAnimationName))
		{
			Audio.play(Audio.soundMap(PRE_REEL_CHANGE_FANFARE_AUDIO_KEY), 1, 0, PRE_REEL_CHANGE_FANFARE_AUDIO_DELAY);
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(data.preReelChangeEffectAnimator, data.preReelChangeEffectAnimationName));
		}

		StartCoroutine(AudioListController.playListOfAudioInformation(data.sounds));
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(reelsAnimator, data.reelChangeAnimation));
	}

	private void updatePaylineScale(float verticalSpacingMultiplier, Vector2 payboxSizeMultiplier)
	{
		reelGame.symbolVerticalSpacingWorld = reelGame.startingSymbolVerticalSpacingWorld * verticalSpacingMultiplier * reelGame.reelGameBackground.getVerticalSpacingModifier();
		reelGame.payBoxSize = new Vector2(reelGame.startingPayBoxSize.x * payboxSizeMultiplier.x, reelGame.startingPayBoxSize.y * payboxSizeMultiplier.y) * reelGame.reelGameBackground.getVerticalSpacingModifier();
	}
}

[System.Serializable]
public class ReelChangeData
{
	public string paylineSet;
	public Animator preReelChangeEffectAnimator;
	public string preReelChangeEffectAnimationName;
	public string reelChangeAnimation;
	public GameObject[] reelParents;
	public Vector3 reelParentsScale = Vector3.one;
	public Vector2 payboxSizeMultiplier = Vector2.one;
	public float verticalSpacingMultiplier = 1.0f;
	public AudioListController.AudioInformationList sounds = new AudioListController.AudioInformationList();
}