using UnityEngine;
using System.Collections;

/// Compass spinning portal for entering into the Big Buck Hunter bonus games
public class Bbh01PortalScript : PortalScript 
{
	[SerializeField] private GameObject portalGameObject = null;				// GameObject for the portal, needs to show when the portal starts

	[SerializeField] private Animator spinButtonAnimator = null;				// Button animator for the spin button
	[SerializeField] private GameObject spinButtonText = null;					// Text on the spin button, needs to hide and show with it
	[SerializeField] private Animator needleAnimator = null;					// Needle animator for controlling where the needle stops
	[SerializeField] private GameObject[] segmentDarkouts = new GameObject[3];	// Darkouts that turn on for the non winning segments
	[SerializeField] private BonusGamePresenter myPresenter = null;				// Need a direct reference to this so we can clean it up as the next game launches

	private CompassPortalEnum bonusGame = CompassPortalEnum.PappysJugs;

	// Constant variables
	private const float COMPASS_SPIN_ANIM_LENGTH = 2.5f;					// Amount of time the compass spin animations take
	private const float WAIT_BEFORE_BONUS_GAME_START = 1.0f;				// Delay so the user can see the result before entering the bonus
	private const float WAIT_BEFORE_DARKING_OUT_OTHERS = 0.75f;				// Delay for sound to announce winning bonus before darking out others
	private const float TIME_BETWEEN_DARK_OUTS = 0.25f;						// Slight delay between each darkout so you can hear the sound

	private const float COMPASS_SPIN_START_SOUND_DELAY = 0.5f;				// Delay after starting the compass spin animation before playing the noise
	private const float COMPASS_STOP_SOUND_DELAY = 0.416f;					// Delay before the compass stops spinning to play the stop sound

	private const string BUTTON_UP_ANIM_NAME = "up";						// Name for the button up animation state
	private const string BUTTON_DOWN_ANIM_NAME = "down";					// Name for the button down animation state
	private const string NEEDLE_START_POS_ANIM_NAME = "needle_start_pos";	// Name for the needle start pos animation

	private const string COMPASS_SPIN_SOUND = "PortalCompassSpinBBH";				// Sound compass makes when spinning
	private const string COMPASS_STOP_SOUND = "PortalCompassStopBBH";				// Sound the compass makes when stopping
	private const string REVEAL_BONUS_SOUND_KEY = "bonus_portal_reveal_bonus";		// Sound made when the bonus is revealed
	private const string REVEAL_OTHERS_SOUND_KEY = "bonus_portal_reveal_others";	// Sound made when revealing the other bonuses not won

	private enum CompassPortalEnum
	{
		PappysJugs = 0,
		HunterBonus,
		FreeSpins
	};

	private readonly string[] COMPASS_STOP_POS_NAMES = new string[3] { "pappys_jugs", "hunting_bonus", "free_spins" };
	
	// Our start to getting a portal to display
	public override void beginPortal(GameObject[] bannerRoots, SlotBaseGame.BannerInfo[] banners, GameObject bannerOverlay, SlotOutcome outcome, long multiplier)
	{
		_outcome = outcome;

		SlotOutcome pappyWheelBonus = outcome.getBonusGameOutcome("bbh01_pappy_jug_wheel");
		if (pappyWheelBonus != null)
		{
			_outcome.isChallenge = true;
			BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new WheelOutcome(pappyWheelBonus);
			bonusGame = CompassPortalEnum.PappysJugs;
		}

		SlotOutcome bigBuckPickem = outcome.getBonusGameOutcome("bbh01_big_buck_bonus");
		if (bigBuckPickem != null)
		{
			_outcome.isCredit = true;
			BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new WheelOutcome(bigBuckPickem, false, 0, true);
			bonusGame = CompassPortalEnum.HunterBonus;
		}

		SlotOutcome bigBuckFreeSpins = outcome.getBonusGameOutcome("bbh01_freespin");
		if (bigBuckFreeSpins != null)
		{
			_outcome.isGifting = true;
			BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(bigBuckFreeSpins);
			bonusGame = CompassPortalEnum.FreeSpins;
		}

		_multiplier = multiplier;

		// This is setup to cover the screen like a challenge game so we hide the overlay and show the larger wings.
		BonusGameManager.instance.wings.forceShowNormalWings(true);
		Overlay.instance.top.show(false);
		SpinPanel.instance.hidePanels();

		portalGameObject.SetActive(true);

		resetPortalElements();

		Audio.switchMusicKeyImmediate(Audio.soundMap("bonus_portal_bg"));
	}

	/// Resets the game, needs to happen as the portal shows each time so things are reset from the last time it came up
	private void resetPortalElements()
	{
		// hide all the darkouts
		for (int i = 0; i < segmentDarkouts.Length; ++i)
		{
			segmentDarkouts[i].SetActive(false);
		}

		// reset the spin button
		spinButtonText.SetActive(true);
		spinButtonAnimator.gameObject.SetActive(true);
		spinButtonAnimator.Play(BUTTON_UP_ANIM_NAME);

		// set the needle in its starting position
		needleAnimator.Play(NEEDLE_START_POS_ANIM_NAME);
	}

	/// UIButtonMessage function for when the spin button is clicked
	public void onCompassButtonClicked(GameObject buttonObj)
	{
		// hide the spin button
		spinButtonAnimator.gameObject.SetActive(false);
		spinButtonText.SetActive(false);

		StartCoroutine(onSpinButtonClickedCoroutine());
	}

	/// Coroutine to handle the sequence of spinning and showing the result of the compass spin
	private IEnumerator onSpinButtonClickedCoroutine()
	{
		needleAnimator.Play(COMPASS_STOP_POS_NAMES[(int)bonusGame]);

		yield return new TIWaitForSeconds(COMPASS_SPIN_START_SOUND_DELAY);
		Audio.play(COMPASS_SPIN_SOUND);

		// wait for animation to finish, less the time we waited for the startup sound and the time it will take to play the stop sound
		yield return new TIWaitForSeconds(COMPASS_SPIN_ANIM_LENGTH - COMPASS_SPIN_START_SOUND_DELAY - COMPASS_STOP_SOUND_DELAY);
		Audio.play(COMPASS_STOP_SOUND);
		yield return new TIWaitForSeconds(COMPASS_STOP_SOUND_DELAY);

		Audio.play(Audio.soundMap(REVEAL_BONUS_SOUND_KEY));
		yield return new TIWaitForSeconds(WAIT_BEFORE_DARKING_OUT_OTHERS);

		// turn on the darkouts for the segements that aren't hit
		for (int i = 0; i < segmentDarkouts.Length; ++i)
		{
			if (i != (int)bonusGame)
			{
				segmentDarkouts[i].SetActive(true);
				Audio.play(Audio.soundMap(REVEAL_OTHERS_SOUND_KEY));
				yield return new TIWaitForSeconds(TIME_BETWEEN_DARK_OUTS);
			}
		}

		yield return new TIWaitForSeconds(WAIT_BEFORE_BONUS_GAME_START);

		beginBonus();
	}

	// We destroy our banners and get into the game already.
	protected override void beginBonus()
	{
		//portalGameObject.SetActive(false);

		BonusGameManager.instance.currentMultiplier = _multiplier;
		BonusGameManager.currentBaseGame = SlotBaseGame.instance;

		if (_outcome.isChallenge)
		{
			BonusGameManager.instance.create(BonusGameType.CHALLENGE);
		}
		else if (_outcome.isCredit)
		{
			BonusGameManager.instance.create(BonusGameType.CREDIT);
		}
		else
		{
			// no credits in bbh, so if it isn't a challenge then it is the free spin game
			BonusGameManager.instance.create(BonusGameType.GIFTING);
		}

		BonusGameManager.instance.show();

		spinsAdded = false;
		bonusAdded = false;

		myPresenter.endBonusGameImmediately();
	}
}
