using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the kendra01 Kendra On Top base game
*/
public class Kendra01 : SlotBaseGame
{
	[SerializeField] private MultiplierPayBoxDisplayModule multiplierPayBoxModule = null;	// Reference to the module which controls the 5th reel
	[SerializeField] private GameObjectPayBoxScript featureSymbolPayBox = null; // The box that goes around the feature symbol
	[SerializeField] private GameObject featureSymbolPayboxSizer = null;	// Special object to control the size of the special symbol pay box

	[HideInInspector] public string featureName = "";						// Tracks what the current feature symbol is, used by PaylineOutcomeDisplayModule to play feature sounds in place of normal symbol sounds
	private MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum currentFeature = MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.None;

	private const string BONUS_INIT_SOUND_KEY = "bonus_symbol_fanfare1";
	private const string BONUS_SYMBOL_ANIMATE_SOUND_KEY = "bonus_symbol_animate";

	[SerializeField] private float BONUS_FANFARE_DELAY = 5.0f;

	protected override void Awake()
	{
		base.Awake();

		// generate a pay box for the additional reel
		featureSymbolPayBox.init(featureSymbolPayboxSizer);
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		// reset the flag to play pre win stuff
		isPlayingPreWin = true;
	}

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject might get disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	/// Show the feature symbol and store info about it, do this early so we can unmask it as the scroll comes down
	private void showFeatureSymbol()
	{
		// only one feature symbol for kendra01
		currentFeature = MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.None;
		List<string> featureSymbolsList = _outcome.getReevaluationFeatureSymbols();
		if (featureSymbolsList.Count > 0)
		{
			currentFeature = MultiplierPayBoxDisplayModule.getFeatureForSymbol(featureSymbolsList[0]);

			if (currentFeature != MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.None)
			{
				featureName = featureSymbolsList[0];

				bool isPlayingCameraSound = currentFeature != MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.None && currentFeature != MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.BN;
				StartCoroutine(multiplierPayBoxModule.playBoxDisplayAnimation(isPlayingCameraSound));

				if (currentFeature == MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.BN)
				{
					// handle playing this here so it syncs with the animation
					StartCoroutine(doPlayBonusAcquiredEffects());
				}
			}
		}
	}

	/// Handles stuff for the extra reel in this game which just appears
	/// reel stop override stuff
	private IEnumerator reelsStoppedCoroutine()
	{
		if (outcome.isBonus)
		{
			Audio.play(Audio.soundMap(BONUS_INIT_SOUND_KEY));

			if(GameState.game.keyName.Contains("tapatio01") || GameState.game.keyName.Contains("gen23"))
			{
				// HACK ALERT! Because of the way 'bonus_symbol_animate' and fanfare sounds are setup for this game,
				//	the sounds aren't playing in the correct order. Instead, we play the sounds here.
				//	THIS SHOULD BE FIXED AT SOME POINT! (see HIR-18931)
				Audio.play(Audio.soundMap(BONUS_SYMBOL_ANIMATE_SOUND_KEY), 1f, 0f, 1f);

				// Added a delay here to ensure that the fanfare would finish playing before the bonus screen is shown.
				yield return new TIWaitForSeconds(BONUS_FANFARE_DELAY);
			}
		}

		if (outcome.isBonus)
		{
			yield return new TIWaitForSeconds(multiplierPayBoxModule.getCurrentFeatureAnimationLength());
		}

		// need to determine if we should skip the pre win presentation, which happens if a 4 across happens with a multiplier in the 5th reel
		if (featureName.Contains('W') && outcome.hasSubOutcomes())
		{
			PayTable payTable = _outcomeDisplayController.getPayTableForOutcome(outcome);

			foreach (SlotOutcome subOutcome in outcome.getSubOutcomesReadOnly())
			{
				int winId = subOutcome.getWinId();
				if (winId != -1)
				{
					PayTable.LineWin lineWin = payTable.lineWins[winId];

					if (lineWin.symbolMatchCount == 4)
					{
						isPlayingPreWin = false;
						break;
					}
				}
			}
		}

		base.reelsStoppedCallback();

		if (outcome.isBonus)
		{
			// Wait for the bonus to load so we don't see a hiccup
			while (BonusGameManager.instance != null && !BonusGameManager.instance.isBonusGameLoaded())
			{
				yield return null;
			}

			multiplierPayBoxModule.playFeatureIdleAnim();
		}
	}

	/// Custom handling for specific reel features
	/// In kendra01 this function will handle showing the feature symbol
	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		// picking a reel that will hopefully have the extend finish so the reveal can occur right after the 4th reel stops
		if (stoppedReel.reelID == 1)
		{
			showFeatureSymbol();
		}

		// Required to trigger executeOnSpecificReelStop() for other slot modules that might be attached
		yield return StartCoroutine(base.handleSpecificReelStop(stoppedReel));
	}
}
