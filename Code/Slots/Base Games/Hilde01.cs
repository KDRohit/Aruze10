
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// The Spin it Rich base class. A pretty simple game, but takes care of adding a card / card with seam,
/// behind every symbol.

public class Hilde01 : SlotBaseGame
{

	[SerializeField] private Animator transitionEffect;
	[SerializeField] private GameObject shroud;

	private PlayingAudio backgroundHum = null;
	private bool playedWDAudio = false;

	// Contant Variables
	private const string TRANSITION_ANIMATION = "hilde01 Transition";

	// Sounds Names
	private const string SPIN_START_SOUND = "mechreel03startCommon";
	private const string SPINNING_SOUND = "mechreel03loopCommon";
	private const string W2_ANIMATING_SOUND = "SymbolW2HiLimit7";
	private const string TRANSITION_SOUND1 = "TransitionToWheelHilde01Pt1";
	private const string TRANSITION_VO = "TransitionToBonusVOHilde01";
	private const string TRANSITION_SOUND2 = "TransitionToWheelHilde01Pt2";

	protected override void SymbolAnimatingCallback(SymbolAnimator animator)
	{
		// Check and see if the symbol is a W2.
		if (animator != null)
		{
			if (!playedWDAudio)
			{
				playedWDAudio = true;
				if (animator.symbolInfoName == "W2" || animator.symbolInfoName == "WD") // Check for WD because they're the same symbol and backend switches them sometimes.
				{
					Audio.play(W2_ANIMATING_SOUND);
				}
			}
		}
	}

	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());

		playedWDAudio = false;
		//Audio.play(SPIN_START_SOUND);
		backgroundHum = Audio.play(SPINNING_SOUND, 1, 0, 0, float.PositiveInfinity);
	}

	protected override void reelsStoppedCallback()
	{
		if (backgroundHum != null)
		{
			backgroundHum.stop(0);
		}
		StartCoroutine(reelsStoppedCoroutine());
	}

	private IEnumerator reelsStoppedCoroutine()
	{
		if (_outcome.isBonus)
		{
			yield return StartCoroutine(doPlayBonusAcquiredEffects());
			shroud.SetActive(true);
			transitionEffect.Play(TRANSITION_ANIMATION);
			Audio.play(TRANSITION_SOUND1);
			Audio.play(TRANSITION_VO, 1.0f, 0.0f, 1.0f);
			Audio.play(TRANSITION_SOUND2, 1.0f, 0.0f, 3.0f);
			yield return new TIWaitForSeconds(4.3f);
		}
		base.reelsStoppedCallback();
		shroud.SetActive(false);
	}
}
