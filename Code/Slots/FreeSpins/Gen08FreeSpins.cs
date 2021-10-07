	using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the Free Spin Bonus Game of Gen08 aka Sugar Palace
*/
public class Gen08FreeSpins : FreeSpinGame
{
	[SerializeField] private Animator[] m1LargeSymbols = null;					// large M1 side bar symbols for the M1 feautre
	[SerializeField] private Animator[] m2LargeSymbols = null;					// large side bar symbols for the M2 feature
	[SerializeField] private Animator[] m3LargeSymbols = null;				// large side bar symbols for the M3 feature
	[SerializeField] private GameObject m3FeatureAnticipation = null;			// played when the mega symbol is spinning during the m3 feature
	[SerializeField] private GameObject normalFrame = null;						// Frame used when normal symbols are spinning
	[SerializeField] private GameObject cakeFrame = null;						// Frame used when the large cake is in the middle
	[SerializeField] private Animator[] featureTexts = new Animator[3];			// The array of texts that show when a feature is triggered
	[SerializeField] private GameObject chocolateRainEffectsParent = null;		// The parent object that will hold all the chocolate rain effects
	[SerializeField] private Animator[] ambientRainAnimators = null;			// List of ambient rain animators
	[SerializeField] private GameObject chocolateWildRevealPrefab = null;		// Effect prefab for the chocolate splash that reveals a wild 
	[SerializeField] private Animator sugarFrostingRespinStartAnim = null;		// Starting animation for the M3 Suage Frosting Respin
	[SerializeField] private GameObject lollipopEffectPrefab = null;			// Prefab object to make lollipop effect clones from

	private const string BACKGROUND_MUSIC_SOUND_KEY = "freespin";			// Basic background music, used to restore the background music

	protected override void OnDestroy()
	{
		base.OnDestroy();

		// unparent the bombs from this object so they can still be used by the base game
		Gen08.unparentAnimationsObjects();
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		// put the cake frame away if we were using it
		setCakeFrameActive(false);

		// make sure that the WD1 symbols aren't on the overlay layer
		Gen08.resetWD1SymbolsLayer(this);

		// hide large symbols that might be left over from a feature occuring on the previous spin
		Gen08.turnOffLargeSymbols(m1LargeSymbols, m2LargeSymbols, m3LargeSymbols);
		Gen08.splitAnyLargeSideSymbols(this);
	}

	/// Custom handling for specific reel features
	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		// want to ignore and not re-play the effects on reevaluation spins
		if (currentReevaluationSpin == null)
		{
			yield return StartCoroutine(Gen08.checkAndPlayReelFeature(this, stoppedReel, m1LargeSymbols, m2LargeSymbols, m3LargeSymbols, true));
		}
	}

	/// Overriding to handle what to do before the reevaluation spin starts
	protected override IEnumerator startNextReevaluationSpin()
  	{ 
  		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		Gen08.ReelFeatureEnum triggeredFeature = Gen08.getTriggeredFeature(this);

		if (triggeredFeature == Gen08.ReelFeatureEnum.SugarFrostingM3)
		{
			// turn side symbols back on so matrix validation doesn't have an issue
			m3LargeSymbols[(int)Gen08.LargeSymbolLocEnum.Left].gameObject.SetActive(true);
			m3LargeSymbols[(int)Gen08.LargeSymbolLocEnum.Left].Play("m3_" + Gen08.LARGE_SYMBOL_IDLE_ANIMATION_NAME);
  			m3LargeSymbols[(int)Gen08.LargeSymbolLocEnum.Right].gameObject.SetActive(true);
  			m3LargeSymbols[(int)Gen08.LargeSymbolLocEnum.Right].Play("m3_" + Gen08.LARGE_SYMBOL_IDLE_ANIMATION_NAME);
  			Gen08.splitAnyLargeSideSymbols(this);

  			// clear pay boxes
			clearOutcomeDisplay();

			// swap the frame to the cake frame
			setCakeFrameActive(true);

			// do the frosting spin
			yield return StartCoroutine(Gen08.doFrostingLinkedReels(this, base.startNextReevaluationSpin, m3FeatureAnticipation, sugarFrostingRespinStartAnim, m3LargeSymbols, featureTexts, true));
		}
		else if (triggeredFeature == Gen08.ReelFeatureEnum.LollipopWildRespinsM1)
		{
			yield return StartCoroutine(Gen08.startLollipopRespin(m1LargeSymbols, base.startNextReevaluationSpin));
		}
		else
		{
			yield return StartCoroutine(base.startNextReevaluationSpin());
		}
  	}

	/// overridable function for handling a symbol becoming stuck on the reels, may become stuck as different symbol, passed in by stuckSymbolName
	protected override IEnumerator changeSymbolToSticky(SlotSymbol symbol, string stickSymbolName, int row)
	{
		Gen08.changeSymbolToLollipop(this, symbol, stickSymbolName, row, lollipopEffectPrefab);
		yield return new TIWaitForSeconds(Gen08.TIME_BETWEEN_STICKY_M1_WILDS);
	}

	/// Allows any sort of cleanup that may need to happen on the symbol animator
    protected override void preReleaseStickySymbolAnimator(SymbolAnimator animator)
    {
    	CommonGameObject.setLayerRecursively(animator.gameObject, Layers.ID_SLOT_REELS);
    }

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject might get disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	/// Control which frame is used, cake one only used during the m3 mega cake spin
	private void setCakeFrameActive(bool isActive)
	{
		cakeFrame.SetActive(isActive);
		normalFrame.SetActive(!isActive);
	}

	/// reevaluationReelStoppedCallback - called when all reels stop, only on reevaluated spins
	protected override IEnumerator handleReevaluationReelStop()
	{
		// using this to track if this is the last spin after the base class goes, since it might decrement how many spins remain
		bool isFinalReevaluationSpin = false;

		if (!hasReevaluationSpinsRemaining)
		{
			isFinalReevaluationSpin = true;

			Gen08.turnOffLargeSymbols(m1LargeSymbols, m2LargeSymbols, m3LargeSymbols);

			// make sure the mega symbol anticipation is hidden
			if (m3FeatureAnticipation.activeSelf)
			{
				m3FeatureAnticipation.SetActive(false);
			}
		}

		Gen08.ReelFeatureEnum triggeredFeature = Gen08.getTriggeredFeature(this);
		if (isFinalReevaluationSpin && triggeredFeature == Gen08.ReelFeatureEnum.LollipopWildRespinsM1)
		{
			Gen08.hideAllLollipopOverlays();
			Gen08.placeWD1SymbolsOnOverlayLayer(this);
		}

		yield return StartCoroutine(base.handleReevaluationReelStop());

		// turn off the music from the M1 or M3 feature and go back to the regular music
		if (isFinalReevaluationSpin && (triggeredFeature == Gen08.ReelFeatureEnum.SugarFrostingM3 || triggeredFeature == Gen08.ReelFeatureEnum.LollipopWildRespinsM1))
		{
			switchBackToNormalBkgMusic();
		}
	}

	/// Transition the game music back to the standard background
	private void switchBackToNormalBkgMusic()
	{
		Audio.switchMusicKeyImmediate(Audio.soundMap(BACKGROUND_MUSIC_SOUND_KEY));
	}

	/// Handles custom transition stuff for this game as well as standard
	/// reel stop override stuff
	private IEnumerator reelsStoppedCoroutine()
	{
		// need to wait for the reveal animations to finish before moving on
		while (Gen08.areLargeSymbolOverlaysAnimating(m1LargeSymbols, m2LargeSymbols, m3LargeSymbols))
		{
			yield return null;
		}

		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		Gen08.ReelFeatureEnum triggeredFeature = Gen08.getTriggeredFeature(this);

		switch (triggeredFeature)
		{
			case Gen08.ReelFeatureEnum.RegularSpin:
				// just a normal spin
				Gen08.swapOverlaysForSymbolInstance(this, m1LargeSymbols, m2LargeSymbols, m3LargeSymbols, true);
				base.reelsStoppedCallback();
				break;
			case Gen08.ReelFeatureEnum.LollipopWildRespinsM1:
				Audio.play(Gen08.M1_FEATURE_START_VO_SOUND_NAME);
				yield return StartCoroutine(Gen08.playFeatureTextAnimation(featureTexts[(int)Gen08.ReelFeatureEnum.LollipopWildRespinsM1]));
				StartCoroutine(Gen08.doLollipopRespins(this, base.reelsStoppedCallback));
				break;
			case Gen08.ReelFeatureEnum.ChocolateRainWildsM2:
				Audio.play(Gen08.CHOCOLATE_RAIN_VO);
				yield return StartCoroutine(Gen08.playFeatureTextAnimation(featureTexts[(int)Gen08.ReelFeatureEnum.ChocolateRainWildsM2]));
				StartCoroutine(Gen08.doChocolateRainWilds(this, base.reelsStoppedCallback, chocolateRainEffectsParent, chocolateWildRevealPrefab, ambientRainAnimators, m2LargeSymbols, true));
				break;
			case Gen08.ReelFeatureEnum.SugarFrostingM3:
				// handled in startNextReevaluationSpin() override
				Gen08.swapOverlaysForSymbolInstance(this, m1LargeSymbols, m2LargeSymbols, m3LargeSymbols, true);
				base.reelsStoppedCallback();
				break;
		}

		yield break;
	}

	/// Allows derived classes to define when to use a feature specific feature anticipation
	public override string getFeatureAnticipationName()
	{
		return Gen08.getFeatureAnticipationName(this);
	}
}
