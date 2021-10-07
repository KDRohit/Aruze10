using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MillionDollarWheel : WheelGame 
{
	[SerializeField] private UILabel multiplierLabel = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent multiplierLabelWrapperComponent = null;

	public LabelWrapper multiplierLabelWrapper
	{
		get
		{
			if (_multiplierLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierLabelWrapper = multiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierLabelWrapper = new LabelWrapper(multiplierLabel);
				}
			}
			return _multiplierLabelWrapper;
		}
	}
	private LabelWrapper _multiplierLabelWrapper = null;
	
	[SerializeField] private UILabel multiplierShadowLabel = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent multiplierShadowLabelWrapperComponent = null;

	public LabelWrapper multiplierShadowLabelWrapper
	{
		get
		{
			if (_multiplierShadowLabelWrapper == null)
			{
				if (multiplierShadowLabelWrapperComponent != null)
				{
					_multiplierShadowLabelWrapper = multiplierShadowLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierShadowLabelWrapper = new LabelWrapper(multiplierShadowLabel);
				}
			}
			return _multiplierShadowLabelWrapper;
		}
	}
	private LabelWrapper _multiplierShadowLabelWrapper = null;
	
	[SerializeField] private UILabel millionText = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent millionTextWrapperComponent = null;

	public LabelWrapper millionTextWrapper
	{
		get
		{
			if (_millionTextWrapper == null)
			{
				if (millionTextWrapperComponent != null)
				{
					_millionTextWrapper = millionTextWrapperComponent.labelWrapper;
				}
				else
				{
					_millionTextWrapper = new LabelWrapper(millionText);
				}
			}
			return _millionTextWrapper;
		}
	}
	private LabelWrapper _millionTextWrapper = null;
	
	[SerializeField] private CoinScript zyngaCoin = null;
	[SerializeField] private GameObject multiplierEffect = null;
	[SerializeField] private Animator smallSliceWinAnimation = null;
	[SerializeField] private Animator largeSliceWinAnimation = null;
	[SerializeField] private Animator[] bigSliceWinAnimations = null;

	[SerializeField] private string SPIN_BUTTON_ANIMATION_NAME = "";
	[SerializeField] private string POINTER_WIN_ANIMATION_NAME = "";
	[SerializeField] private string BIG_SLICE_ANIMATION_NAME = "";

	//Scat Sound Keys (Setup rollup sounds in scat keys wheel_rollup and wheel_rollup_term)
	protected const string WHEEL_IDLE = "wheel_click_to_spin";
	protected const string WHEEL_SPIN = "wheel_slows_music";
	protected const string WHEEL_STOP = "wheel_stops";
	protected const string WHEEL_STOP_BIG = "wheel_stops_medium";
	protected const string WHEEL_STOP_BIGGEST = "wheel_stops_special";
	protected const string MULTIPLIER_EFFECT = "wheel_multiplier_effect";
	protected const string MULTIPLIER_EFFECT_TERM = "wheel_multiplier_effect_term";
	protected const string BONUS_INTRO_VO = "bonus_intro_vo";

	// Sound names.
	[SerializeField] private string WHEEL_STOP_BIG_VO = "";
	[SerializeField] private string WHEEL_PRESPIN_VO = "";
	
	[SerializeField] private float TIME_MOVE_MULTIPLIER_EFFECT = 1.0f;

	private const float TIME_MOVE_COIN = 1.0f;
	private const float TIME_ROLLUP_VALUE = 1.0f;

	private long multiplier = 1;


	private const int NUMBER_BIG_SLICES = 4;

	public override void init()
	{
		base.init();
		// This can be gifted, if that's the case the multipler is 1.
		if (SlotBaseGame.instance != null)
		{
			multiplier = BonusGameManager.instance.currentMultiplier;
		}

		multiplierLabelWrapper.text = Localize.text("{0}X", CommonText.formatNumber(multiplier));

		Audio.play(Audio.soundMap(WHEEL_IDLE));
		if(Audio.canSoundBeMapped(BONUS_INTRO_VO))
		{
			Audio.play (Audio.soundMap (BONUS_INTRO_VO));
		}
		// Million dollar cheat, there should be a cheat to get into this too.
		//wheelPick.winIndex = 0;
		//wheelPick.credits = 1000000;
	}



	/// Get the final degrees of the wheel, virtual in case you need to apply an adjustment for your game
	protected override float getFinalSpinDegress()
	{
		float finalAngle = wheelPick.winIndex * degreesPerSlice + (wheelPick.winIndex / NUMBER_BIG_SLICES) * degreesPerSlice;
		if (wheelPick.winIndex % NUMBER_BIG_SLICES != 0)
		{
			finalAngle += degreesPerSlice / 2;
		}
		return finalAngle;
	}

	protected override void onWheelSpinComplete()
	{
		PlayingAudio wheelSpinAudio = Audio.findPlayingAudio(Audio.soundMap(WHEEL_SPIN));
		if(wheelSpinAudio != null)
		{
			wheelSpinAudio.stop();
		}
		// Play the right wheel ending sound.
		if (wheelPick.credits == 1000000)
		{
			Audio.play(Audio.soundMap(WHEEL_STOP_BIGGEST));
		}
		else if (wheelPick.winIndex % NUMBER_BIG_SLICES == 0)
		{
			// We are on a big slice
			Audio.play(Audio.soundMap(WHEEL_STOP_BIG));
		}
		else
		{
			Audio.play(Audio.soundMap(WHEEL_STOP));
		}
		base.onWheelSpinComplete();
	}

	protected override IEnumerator animateScore()
	{
		if (topPointerAnimation != null)
		{
			Animator topPointerAnimatior = topPointerAnimation.GetComponent<Animator>();
			if (topPointerAnimatior != null && POINTER_WIN_ANIMATION_NAME != "")
			{
				topPointerAnimatior.Play(POINTER_WIN_ANIMATION_NAME);
			}
		}

		if (wheelPick.winIndex % NUMBER_BIG_SLICES == 0)
		{
			if (largeSliceWinAnimation != null)
			{
				largeSliceWinAnimation.gameObject.SetActive(true);
			}
			int bigSliceIndex = wheelPick.winIndex / NUMBER_BIG_SLICES;
			if (bigSliceWinAnimations != null && bigSliceWinAnimations.Length > bigSliceIndex && bigSliceWinAnimations[bigSliceIndex] != null)
			{
				bigSliceWinAnimations[bigSliceIndex].Play(BIG_SLICE_ANIMATION_NAME);
			}
		}
		else
		{
			if (smallSliceWinAnimation != null)
			{
				smallSliceWinAnimation.gameObject.SetActive(true);
			}
		}

		long payout = wheelPick.credits;

		if (payout > 0)
		{
			// Although we visually show the multiplier, in the final payout we don't want to include it. So that is can be
			//  seen on the summary screen.
			if (wheelWinBoxAnimation != null && multiplier <= 1)
			{
				wheelWinBoxAnimation.SetActive(true);
			}

			if (winLabel != null)
			{
				winLabel.gameObject.SetActive(true);
			}

			BonusGamePresenter.instance.currentPayout = finalPayout + payout;
			if (winText != null)
			{
				winText.gameObject.SetActive(true);
				winText.enabled = true;
				winText.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			}

			finalPayout += payout;
		}

		if (payout == 1000000)
		{
			Audio.play(WHEEL_STOP_BIG_VO);
		}

		// Sink in time
		yield return new TIWaitForSeconds(1.0f);
		
		if (multiplier > 1)
		{
			if (zyngaCoin != null)
			{
				zyngaCoin.gameObject.SetActive(true);
				zyngaCoin.spin();
				zyngaCoin.transform.position = multiplierLabelWrapper.transform.position;
				iTween.MoveTo(zyngaCoin.gameObject, winText.transform.position, TIME_MOVE_COIN);
				yield return new TIWaitForSeconds(TIME_MOVE_COIN);
				zyngaCoin.gameObject.SetActive(false);
			}
			if (multiplierEffect != null)
			{
				SafeSet.gameObjectActive(multiplierLabelWrapper.gameObject, false);
				SafeSet.gameObjectActive(multiplierShadowLabelWrapper.gameObject, false);

				multiplierEffect.SetActive(true);
				multiplierEffect.transform.position = multiplierLabelWrapper.transform.position;
				Audio.play(Audio.soundMap(MULTIPLIER_EFFECT));
				iTween.MoveTo(multiplierEffect, winText.transform.position, TIME_MOVE_MULTIPLIER_EFFECT);
				yield return new TIWaitForSeconds(TIME_MOVE_MULTIPLIER_EFFECT);
				Audio.play(Audio.soundMap(MULTIPLIER_EFFECT_TERM));
				multiplierEffect.SetActive(false);
			}
			if (winText != null)
			{
				// We want make sure the win label is on here, because we wait to turn it on if we get the MILLION outcome.
				winText.gameObject.SetActive(true);
			}
			SafeSet.gameObjectActive(millionTextWrapper.gameObject, false);
			SafeSet.gameObjectActive(wheelWinBoxAnimation, true);

			yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout * multiplier, winText, true, TIME_ROLLUP_VALUE));
		}
		// Now we want to move the zynga coin from the multiplier onto the win label.
	}


	protected override IEnumerator processSpin()
	{
		Audio.play(Audio.soundMap(WHEEL_SPIN));
		Audio.play(WHEEL_PRESPIN_VO);
		if (spinButton != null && SPIN_BUTTON_ANIMATION_NAME != "")
		{
			Animator spinButtonAnimator = spinButton.GetComponent<Animator>();
			if (spinButtonAnimator != null)
			{
				spinButtonAnimator.Play(SPIN_BUTTON_ANIMATION_NAME);
			}
		}

		yield return StartCoroutine(base.processSpin());
	}

}
