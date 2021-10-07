using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Typical setup has 3 possible Wild Banners
//Each spin the wild banners will either fill up their designated reel or "miss" and nothing happens
//Any number of wild banners can hit per spin

public class WildBannerHitOrMissReplacementModule : SlotModule
{
	[SerializeField] private Animator[] wdLaunchers; //Objects shooting from the bottom of the reels
	[SerializeField] private Animator[] wdTargets; //Objects at the top of the reels being shot at

	[SerializeField] private GameObject[] wdBannerPrefabs; //WD banners that are reveals when hit
	[SerializeField] private Animator introAnimator; //WD banners that are reveals when hit
	[SerializeField] private Animator outroAnimator; //WD banners that are reveals when hit

	[SerializeField] private string launcherSetupAnimName = ""; //Animation that plays at the start of each spin
	[SerializeField] private string launcherHitAnimName = "";
	[SerializeField] private string launcherMissAnimName = "";

	[SerializeField] private string targetSetupAnimName = ""; //Animation that plays at the start of each spin
	[SerializeField] private string targetHitAnimName = "";
	[SerializeField] private string targetMissAnimName = "";

	[SerializeField] private string introAnimName = "";
	[SerializeField] private string outroAnimName = "";

	[SerializeField] private float TIME_BETWEEN_REVEALS = 0.0f;
	[SerializeField] private float TIME_BEFORE_TARGET_HIT_WAIT = 0.0f;
	[SerializeField] private float TIME_BEFORE_TARGET_MISS_WAIT = 0.0f;
	[SerializeField] private float POST_WD_REVEALS_WAIT = 0.0f;

	[SerializeField] private string mutatedSymbolName = "";

	private bool needsToSplitBanners = false;

	private const string freespinFeaturePopulateSound = "freespin_feature_populate";
	private const string freespinFeatureActivateSound = "freespin_feature_activate";
	private const string freespinFeaturePooperSound = "freespin_feature_pooper";
	private const string freespinFeatureLandSound = "freespin_feature_land1";

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		if (introAnimator != null)
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(introAnimator, introAnimName));
		}
		bool[] shouldFire = new bool[wdLaunchers.Length];
		for (int i = 0; i < shouldFire.Length; i++)
		{
			shouldFire[i] = false;
		}
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;

		//Reels that are being hit come from the server
		foreach (int reelIndex in currentMutation.reels)
		{
			shouldFire[reelIndex-1] = true;
		}

		for (int i = 0; i < shouldFire.Length; i++)
		{
			Audio.playSoundMapOrSoundKey(freespinFeatureActivateSound);
			if (shouldFire[i])
			{
				wdLaunchers[i].Play(launcherHitAnimName);
				yield return new TIWaitForSeconds(TIME_BEFORE_TARGET_HIT_WAIT);
				Audio.play(Audio.soundMap(freespinFeatureLandSound));
				wdTargets[i].Play(targetHitAnimName);
				needsToSplitBanners = true;
				wdBannerPrefabs[i].SetActive(true); //Activate our banners once the target is hit
			}
			else
			{
				wdLaunchers[i].Play(launcherMissAnimName);
				yield return new TIWaitForSeconds(TIME_BEFORE_TARGET_MISS_WAIT);
				Audio.playSoundMapOrSoundKey(freespinFeaturePooperSound);
				wdTargets[i].Play(targetMissAnimName);
			}
			yield return new TIWaitForSeconds(TIME_BETWEEN_REVEALS);
		}

		if (outroAnimator != null)
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(outroAnimator, outroAnimName));
		}

		yield return new TIWaitForSeconds(POST_WD_REVEALS_WAIT);
	}

	public override IEnumerator executeOnPreSpin ()
	{
		//Play all our setup animations when the spin starts.
		reelGame.clearOutcomeDisplay();
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		Audio.playSoundMapOrSoundKey(freespinFeaturePopulateSound);
		for (int i = 0; i < wdLaunchers.Length; i++)
		{
			runningCoroutines.Add(StartCoroutine(CommonAnimation.playAnimAndWait(wdTargets[i], targetSetupAnimName)));
			runningCoroutines.Add(StartCoroutine(CommonAnimation.playAnimAndWait(wdLaunchers[i], launcherSetupAnimName)));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
	}

	public override bool needsToExecuteOnPreSpin ()
	{
		return true;
	}

	public override bool needsToExecuteOnReelsStoppedCallback ()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback ()
	{
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;
		foreach(int reelIndex in currentMutation.reels)
		{
			reelGame.engine.getVisibleSymbolsAt(reelIndex)[0].mutateTo("WD1-4A");
			wdBannerPrefabs[reelIndex-1].SetActive(false);
		}

		yield break;
	}
}
