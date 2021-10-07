using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // For array.Contains

public class Kendra01MultiReelReplacementModule : MultiReelReplacementModule 
{
	[SerializeField] private Animator[] wdBanners = null;
	[SerializeField] private float TIME_BETWEEN_BANNERS = 0.25f;
	[SerializeField] private float WAIT_FOR_BANNER_DUR = 0.0f;
	[SerializeField] private bool shouldBottleMoveDown = true;
	[SerializeField] private float BOTTLE_MOVE_DOWN_TIME = 0.75f;

	[SerializeField] private bool shouldPlayTeasers = false;
	[SerializeField] private float TEASER_DUR = 0.25f;

	[SerializeField] private string WILD_BANNER_START_SOUND = "WildSymbolChampagneInitKendra";
	[SerializeField] private float WILD_BANNER_START_DELAY = 0.0f;
	[SerializeField] private string WILD_BANNER_START_VO = "";
	[SerializeField] private float WILD_BANNER_START_VO_DELAY = 0.0f;
		[SerializeField] private string WILD_BANNER_END_SOUND = "WildSymbolLandKendra";
	[SerializeField] private string WILD_BANNER_END_VO = "";

	private bool[] wdBannersPlaying = null;

	// Constants
	private const float BOTTLE_SLIDE_IN_Y_TARGET = 1.75f;
	private const float BOTTLE_START_Y_POS = 2.51f;
	private const string WILD_REVEAL_NAME = "wild_reveal";
	private const string WILD_FINAL_NAME = "filled_loop";
	private const string WILD_DEFAULT_NAME = "idle";
	private const string WILD_TEASER_NAME = "teaser";

	// Sound names
	private const string ROUS_MOVE_SOUND = "FreespinROUSSkitter";

	public override void Awake()
	{
		base.Awake();

		// force the present type for this game to be while the game is still spinning
		presentType = PresentOrderEnum.DuringReelSpin;

		if (wdBanners != null)
		{
			wdBannersPlaying = new bool[wdBanners.Length];
		}
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		for (int i = 0; i < featureMutation.reels.Length; i++)
		{
			if (i != 0 && TIME_BETWEEN_BANNERS != 0)
			{
				// Don't wait for the first one, or if there isn't any time.
				yield return new WaitForSeconds(TIME_BETWEEN_BANNERS);
			}
			int reelID = featureMutation.reels[i];
			StartCoroutine(playWildBannerAt(reelID));
		}

		if (shouldPlayTeasers)
		{
			StartCoroutine(playTeasers());
		}


		// make sure the cork popping animations are done
		while (wdBannersPlaying.Contains(true))
		{
			yield return null;
		}
	}

	private IEnumerator playTeasers()
	{
		List<int> teaserReels = new List<int>();

		// Teaser reels are reels that are not feature mutations.
		for (int reelID=0; reelID<wdBanners.Length; reelID++)
		{
			if (!featureMutation.reels.Contains(reelID))
			{
				teaserReels.Add(reelID);
			}
		}

		// Always play at least one teaser, but don't always play all of them.
		// Do it by randomly removing some teasers from the list.
		int numToRemove = Random.Range(0, teaserReels.Count-1);
		for (int iRemove=0; iRemove<numToRemove; iRemove++)
		{
			int indexToRemove = Random.Range(0, teaserReels.Count);
			teaserReels.RemoveAt(indexToRemove);
		}
        
		// Play the teasers.
		for (int iTeaser=0; iTeaser<teaserReels.Count; iTeaser++)
		{
			yield return new WaitForSeconds(TEASER_DUR);

			int reelID = teaserReels[iTeaser];
			Animator bannerAnimator = wdBanners[reelID];
			bannerAnimator.Play("teaser");
		}
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		StartCoroutine(base.executeOnReelsStoppedCallback());

		if (featureMutation == null)
		{
			Debug.LogError("Trying to execute module on invalid data.");
			yield break;
		}

		foreach (int reelID in featureMutation.reels)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
			{
				// skip animaitons so we don't see the wild animations going on under the banner
				symbol.skipAnimationsThisOutcome();
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

				if (shouldBottleMoveDown)
				{
					// move the bottle back up
					GameObject parentAnchor = bannerAnimator.gameObject.transform.parent.gameObject;
					Vector3 parentLocalPos = parentAnchor.transform.localPosition;
					parentAnchor.transform.localPosition = new Vector3(parentLocalPos.x, BOTTLE_START_Y_POS, parentLocalPos.y);
				}
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
			if (shouldBottleMoveDown)
			{
				// move the bottle into position
				GameObject parentAnchor = bannerAnimator.gameObject.transform.parent.gameObject;
				yield return new TITweenYieldInstruction(iTween.MoveTo(parentAnchor, iTween.Hash(
																"y", BOTTLE_SLIDE_IN_Y_TARGET,
																"time", BOTTLE_MOVE_DOWN_TIME,
																"islocal", true,
																"easetype", iTween.EaseType.linear)));
			}

			Audio.play(WILD_BANNER_START_SOUND, 1.0f, 0.0f, WILD_BANNER_START_DELAY);

			if (!string.IsNullOrEmpty(WILD_BANNER_START_VO))
			{
				Audio.play(WILD_BANNER_START_VO, 1.0f, 0.0f, WILD_BANNER_START_VO_DELAY);
			}

			bannerAnimator.Play(WILD_REVEAL_NAME);
			while (!bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(WILD_REVEAL_NAME))
			{
				yield return null;
			}

			// Wait for the animation to finish,
			// unless they set a banner duration in the prefab, then wait for that duration, instead.

			if (WAIT_FOR_BANNER_DUR != 0.0f)
			{
				// wait for banner dur

				GameTimer waitForBannerTimer = new GameTimer(WAIT_FOR_BANNER_DUR);
				while (!waitForBannerTimer.isExpired)
				{
					yield return null;
				}
			}
			else
			{
				// now wait for the animation to finish and go back to the idle state
				while (bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(WILD_REVEAL_NAME))
				{
					yield return null;
				}
			}

			if (WILD_BANNER_END_SOUND != "")
			{
				Audio.play(WILD_BANNER_END_SOUND);
			}
			if (WILD_BANNER_END_VO != "" && !Audio.isPlaying(WILD_BANNER_END_VO))
			{
				Audio.play(WILD_BANNER_END_VO);
			}
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
