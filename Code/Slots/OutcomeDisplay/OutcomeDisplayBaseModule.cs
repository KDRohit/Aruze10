using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// Base class for displaying particular outcome result types.
public abstract class OutcomeDisplayBaseModule : TICoroutineMonoBehaviour
{
	public const float MIN_DISPLAY_TIME = 1.0f;

	protected OutcomeDisplayController _controller;
	
	protected OutcomeDisplayedDelegate _outcomeDisplayedDelegate;
	
	protected SlotOutcome _outcome;
	protected bool _outcomeDisplayDone;
	protected float _outcomeTimer;
	
	protected bool _loop = false;

	protected float _currentAnimSoundEnd;
	protected string _playedAnimSounds;
	protected int animPlayingCounter;
	
	public virtual void init(OutcomeDisplayController controller)
	{
		_controller = controller;
	}
	
	public virtual void playOutcome(SlotOutcome outcome, bool isPlayingSound)
	{
		if (outcome == null)
		{
			_outcome = null;
			return;
		}
		
		_outcome = outcome;
		_outcomeDisplayDone = false;
	}

	// Returns a list of all of the winning symbols for a specific reel for the passed outcome
	// useful if you want to get them as each reel stops (since getSetOfWinningSymbols() will only work correctly if all reels are fully stopped)
	public abstract HashSet<SlotSymbol> getSetOfWinningSymbolsForReel(SlotOutcome outcome, int reelIndex, int row, int layer);

	// Returns a list of all symbols that are part of wins
	public abstract HashSet<SlotSymbol> getSetOfWinningSymbols(SlotOutcome outcome);
	
	/// Called by the user to completely clear out all outcome stored data.
	public virtual void clearOutcome()
	{
		// First thing symbol animations do upon stop is to call their callbacks.  Clear out _outcome first to prevent 
		_outcome = null;
	}
	
	// setOutcomeDisplayedDelegate - assigns a callback that returns that the current outcome has finished being displayed.
	public void setOutcomeDisplayedDelegate(OutcomeDisplayedDelegate callback)
	{
		_outcomeDisplayedDelegate = callback;
	}
	
	// Coroutine that checks after the minimum payline display time whether we still need to wait for symbol animations to finish.
	protected virtual IEnumerator waitToFinish()
	{
		// Wait the minimum amount of time before finishing up, even if there are no animations.
		yield return new WaitForSeconds(MIN_DISPLAY_TIME);

		// Keep waiting until there are no animations playing before finishing up.
		while (animPlayingCounter > 0)
		{
			yield return null;
		}
		// Ready to finish up.
		StartCoroutine(displayFinish());
	}

	// Callback that triggers every time SlotSymbol animation completes.
	protected virtual void onAnimDone(SlotSymbol sender)
	{
		// When all animations are done, this should reach 0,
		// allowing paylineDisplayTimeout() to continue and finish.
		animPlayingCounter--;
	}

	// Override this to fade the paylines/boxes before calling handleOutcomeComplete().
	protected abstract IEnumerator displayFinish();
	
	/// Called by the module when reporting the current display is finished.
	protected virtual void handleOutcomeComplete()
	{
		if (_outcome != null && !_outcomeDisplayDone)
		{
			_outcomeDisplayDone = true;
			
			// Make sure to call the callback as the very last thing, in case the callback triggers another outcome display.
			if (_outcomeDisplayedDelegate != null)
			{
				_outcomeDisplayedDelegate(this, _outcome);
			}
		}
	}

	// Under some circumstance, we need to prevent linewins from playing their sounds
	// Specifically we normally want to stop VO from playing in a linewin after a bigwin
	// has occurred. OutcomeDisplayModules check against this when playOutcome is called.
	protected virtual bool shouldPlaySound(string soundKey)
	{
		if (_controller.isBigWin && Audio.doesAudioClipHaveChannelTag(Audio.soundMap(soundKey), Audio.VO_CHANNEL_KEY))
		{
			return false;
		}

		return true;
	}

	public virtual bool displayPaylineCascade(GenericDelegate doneCallback, GenericDelegate failedCallback)
	{
		return false;
	}

	// Makes all the lines inactive
	public abstract void hideLines();
	// Makes all the lines active
	public abstract void showLines();

	// Log specific text for help debugging the module
	public abstract string getLogText();
}

public delegate void OutcomeDisplayedDelegate(OutcomeDisplayBaseModule displayModule, SlotOutcome outcome);
