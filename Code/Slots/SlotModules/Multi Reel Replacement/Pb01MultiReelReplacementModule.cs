using UnityEngine;
using System.Collections;
using System.Linq; // For array.Contains

public class Pb01MultiReelReplacementModule : MultiReelReplacementModule 
{
	[SerializeField] private GameObject rodent = null;
	[SerializeField] private Animator[] wdBanners = null;
	
	private bool[] wdBannersPlaying = null;
	private float rodentEndXPosition;
	private float rodentStartXPosition;

	// Constants
	private const float TIME_MOVE_RODENT = 3.0f;
	private const string WILD_REVEAL_NAME = "reveal";
	private const string WILD_FINAL_NAME = "loop";
	private const string WILD_DEFAULT_NAME = "default";
	private const string WILD_BANNER_START_SOUND = "FreespinFireStoneFoom";
	private const string WILD_BANNER_END_SOUND = "FreespinFireStoneExplode";

	// Sound names
	private const string INTRO_VO = "FreespinIntroVOPbride";
	private const string ROUS_MOVE_SOUND = "FreespinROUSSkitter";

	public override void Awake()
	{
		base.Awake();

		// force the present type for this game to be while the game is still spinning
		presentType = PresentOrderEnum.DuringReelSpin;

		if (rodent != null)
		{
			rodentStartXPosition = rodent.transform.localPosition.x;
			rodentEndXPosition = -rodentStartXPosition;
		}
		if (wdBanners != null)
		{
			wdBannersPlaying = new bool[wdBanners.Length];
		}
		Audio.play(INTRO_VO);
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		if (rodent != null)
		{
			// Put the Rat back where it should be.
			Vector3 rodentPosition = rodent.transform.localPosition;
			rodentPosition.x = rodentStartXPosition;
			rodent.transform.localPosition = rodentPosition;

			// Run the Rat across the screen.
			iTween.MoveTo(rodent, iTween.Hash(
						"x", rodentEndXPosition,
						"time", TIME_MOVE_RODENT,
						"islocal", true,
						"easetype", iTween.EaseType.linear)); 
			float time = 0.0f;
			int mutationIndex = 0;
			PlayingAudio runningSound = Audio.play(ROUS_MOVE_SOUND);
			while (time < TIME_MOVE_RODENT)
			{
				yield return null;
				time += Time.deltaTime;
				// Check and see if we should be playing one of the flames.
				for (int i = mutationIndex; i < featureMutation.reels.Length; i++)
				{
					int reelID = featureMutation.reels[i];
					if (rodent.transform.localPosition.x > reelGame.getReelRootsAt(reelID).transform.localPosition.x)
					{
						mutationIndex++;
						StartCoroutine(playWildBannerAt(reelID));
					}
				}
			}
			if (runningSound != null)
			{
				runningSound.stop(0.0f);
			}
			yield return null;

			while (wdBannersPlaying.Contains(true))
			{
				yield return null;
			}
		}
	}

	private void cleanUp()
	{
		int numberOfWildsEnabled = 0;
		foreach (Animator bannerAnimator in wdBanners)
		{
			if (bannerAnimator != null)
			{
				if (bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(WILD_FINAL_NAME))
				{
					numberOfWildsEnabled++;
				}
			}
		}
		
		if (featureMutation.reels.Length != numberOfWildsEnabled)
		{
			Debug.LogError("We only have " + numberOfWildsEnabled + " wilds showing but we should have " + featureMutation.reels.Length);
		}

		foreach (Animator bannerAnimator in wdBanners)
		{
			if (bannerAnimator != null)
			{
				bannerAnimator.Play(WILD_DEFAULT_NAME);
			}
		}
	}

	private IEnumerator playWildBannerAt(int reelID)
	{
		if (wdBanners == null || reelID >= wdBanners.Length || reelID < 0)
		{
			Debug.LogError("Trying to get banner at " + reelID + " in" + wdBanners + ".");
		}
		wdBannersPlaying[reelID] = true;

		// Play the animation
		Animator bannerAnimator = wdBanners[reelID];
		if (bannerAnimator != null)
		{
			Audio.play(WILD_BANNER_START_SOUND);
			bannerAnimator.Play(WILD_REVEAL_NAME);
			while (!bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(WILD_REVEAL_NAME))
			{
				yield return null;
			}

			// now wait for the animation to finish and go back to the idle state
			while (bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(WILD_REVEAL_NAME))
			{
				yield return null;
			}
			Audio.play(WILD_BANNER_END_SOUND);
		}

		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
		{
			if (symbol.name != featureMutation.symbol)
			{
				symbol.mutateTo(featureMutation.symbol);
			}
		}
		wdBannersPlaying[reelID] = false;

	}

// executeAfterPaylinesCallback() section
// functions in this section are accessed by SlotbaseGame/FreeSpinGame.doReelsStopped()
	public override bool needsToExecuteAfterPaylines()
	{
		return true;
	}

	public override IEnumerator executeAfterPaylinesCallback(bool winsShown)
	{
		cleanUp();
		yield return null;
	}
}
