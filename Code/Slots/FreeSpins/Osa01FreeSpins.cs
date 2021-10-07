using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the Free Spin Bonus Game of Oz game Munchkinland / Witches of Oz / Dueling Witches
*/
public class Osa01FreeSpins : FreeSpinGame
{
	[SerializeField] private GameObject rubySlipperAnticipation = null;
	[SerializeField] private Animator[] glindaLargeSymbols = null;
	[SerializeField] private Animator[] wickedWitchLargeSymbols = null;
	[SerializeField] private Animator[] dorothyLargeSymbols = null;
	[SerializeField] private GameObject monkey = null;						// flying monkey used for the wicked witch featere
	[SerializeField] private GameObject monkeyHeldSymbol = null;			// the WD2 symbol that the monkey is holding

	[SerializeField] private GameObject glindaFeatureTextObj = null;			// The text that shows when you get the glinda feature
	[SerializeField] private GameObject wickedWitchFeatureTextObj = null;		// The text that shows when you get the wicked witch feature
	[SerializeField] private GameObject dorothyFeatureTextObj = null;			// The text that shows when you get the dorothy feature
	[SerializeField] private Animation featureTextAnimation = null;				// Animation for the feature text that calls attention to the text

	private const float WAIT_AND_SHOW_RUBY_SLIPPER_TIME = 1.0f;				// Small wait so the user can see the large 3x3 ruby slipper that is now on the reels
	private const float SHOW_FIRE_WILD_STAGGER_TIME = 0.4f;					// Time value to stagger the witch fire wilds by so they hit at slightly different times

	private const string BACKGROUND_MUSIC_SOUND_KEY = "freespin";			// Basic background music, used to restore the background music

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		// hide large symbols that might be left over from a feature occuring on the previous spin
		Osa01.turnOffLargeSymbols(glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols);
		Osa01.splitAnyLargeSideSymbols(this);
	}

	/// Custom handling for specific reel features
	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		// want to ignore and not re-play the effects on reevaluation spins
		if (currentReevaluationSpin == null)
		{
			yield return StartCoroutine(Osa01.checkAndPlayReelFeature(this, stoppedReel, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, true));
		}
	}

	/// Overriding to handle what to do before the ruby slipper spin starts
	protected override IEnumerator startNextReevaluationSpin()
  	{ 
  		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		Osa01.ReelFeatureEnum triggeredFeature = Osa01.getTriggeredFeature(this);

		if (triggeredFeature == Osa01.ReelFeatureEnum.RubySlippersLinkedReelsM3)
		{
			// turn side symbols back on so matrix validation doesn't have an issue
			dorothyLargeSymbols[(int)Osa01.LargeSymbolLocEnum.Left].gameObject.SetActive(true);
  			dorothyLargeSymbols[(int)Osa01.LargeSymbolLocEnum.Right].gameObject.SetActive(true);
  			Osa01.splitAnyLargeSideSymbols(this);

  			// clear pay boxes
  			clearOutcomeDisplay();

  			// do the ruby slipper spin
			yield return StartCoroutine(Osa01.doRubySlippersLinkedReels(this, base.startNextReevaluationSpin, rubySlipperAnticipation, true));
		}
		else
		{
			yield return StartCoroutine(base.startNextReevaluationSpin());
		}
  	}

	/// overridable function for handling a symbol becoming stuck on the reels, may become stuck as different symbol, passed in by stuckSymbolName
	protected override IEnumerator changeSymbolToSticky(SlotSymbol symbol, string stickSymbolName, int row)
	{
		Osa01.changeSymbolToBubble(this, symbol, stickSymbolName, row);
		yield return new TIWaitForSeconds(Osa01.TIME_BETWEEN_GLINDA_BUBBLES);
	}

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject might get disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	/// reevaluationReelStoppedCallback - called when all reels stop, only on reevaluated spins
	protected override IEnumerator handleReevaluationReelStop()
	{
		// using this to track if this is the last spin after the base class goes, since it might decrement how many spins remain
		bool isFinalReevaluationSpin = false;

		if (!hasReevaluationSpinsRemaining)
		{
			isFinalReevaluationSpin = true;
			
			Osa01.swapOverlaysForSymbolInstance(this, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, true);

			// make sure the ruby slipper anticipation is hidden
			if (rubySlipperAnticipation.activeSelf)
			{
				rubySlipperAnticipation.SetActive(false);
			}
		}

		yield return StartCoroutine(base.handleReevaluationReelStop());

		// turn off the music from the ruby slipper or glinda spin and go back to the regular music
		Osa01.ReelFeatureEnum triggeredFeature = Osa01.getTriggeredFeature(this);
		if (isFinalReevaluationSpin && (triggeredFeature == Osa01.ReelFeatureEnum.RubySlippersLinkedReelsM3 || triggeredFeature == Osa01.ReelFeatureEnum.GlindaBubbleRespinsM1))
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
		while (Osa01.areLargeSymbolOverlaysAnimating(glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols))
		{
			yield return null;
		}

		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		Osa01.ReelFeatureEnum triggeredFeature = Osa01.getTriggeredFeature(this);

		switch (triggeredFeature)
		{
			case Osa01.ReelFeatureEnum.RegularSpin:
				// just a normal spin
				Osa01.swapOverlaysForSymbolInstance(this, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, true);
				base.reelsStoppedCallback();
				break;
			case Osa01.ReelFeatureEnum.GlindaBubbleRespinsM1:
				yield return StartCoroutine(Osa01.playFeatureTextAnimation(glindaFeatureTextObj, featureTextAnimation));
				StartCoroutine(Osa01.doGlindaBubbleRespins(this, base.reelsStoppedCallback));
				break;
			case Osa01.ReelFeatureEnum.WickedWitchFireBallWildsM2:
				yield return StartCoroutine(Osa01.playFeatureTextAnimation(wickedWitchFeatureTextObj, featureTextAnimation));
				StartCoroutine(Osa01.doWickedWitchFireballWilds(this, base.reelsStoppedCallback, monkey, monkeyHeldSymbol, wickedWitchLargeSymbols, true));
				break;
			case Osa01.ReelFeatureEnum.RubySlippersLinkedReelsM3:
				// handled in startNextReevaluationSpin() override
				yield return StartCoroutine(Osa01.playFeatureTextAnimation(dorothyFeatureTextObj, featureTextAnimation));
				Osa01.swapOverlaysForSymbolInstance(this, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, true);
				base.reelsStoppedCallback();
				break;
		}

		yield break;
	}

	/// Allows derived classes to define when to use a feature specific feature anticipation
	public override string getFeatureAnticipationName()
	{
		return Osa01.getFeatureAnticipationName(this);
	}
}
