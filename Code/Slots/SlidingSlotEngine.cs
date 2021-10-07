using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**
SlidingSlotEngine
Class to manage the spinning of a collection of reels, and report back to the owner what the current reel motion state is.  This class
is designed to be usable by either a "base" slot game or a free spin bonus game.  As such it does not handle win results or user input.
It gets referenced by OutcomeDisplayController so that the reel symbols can be animated when wins are being processed.

Contains an array of SlotReel instances.  Each of these manages the movement of a vertical group of SlotSymbols.
SlotGameData is needed from global data because it contains specifiers on symbol size, movement speed, etc.
ReelSetData includes info on how many reels there are and what reel strips to use.  Note that due to the "tiers" payout management system,
the ReelSetData can change during play and should not result in an instantaneous reel instance rebuild.  Rather, the new symbol strips get
fed in during the next spin.

The state progression goes:
Stopped: Nothing is moving.  When entering this state, _reelsStoppedDelegate gets triggered.
BeginSpin: If all reels start spinning simultaneously, this state is skipped.  Otherwise this state processes the delay timer to start spinning each reel.
Spinning: All reels have been told to start spinning.
EndSpin: The reels are told to stop, Remains in this state until every reel has stopped moving. 
         Note that while in this state, a SlamStop will eliminate the delays between when reels are told to stop.  Reel Rollbacks still need to
         complete before leaving this state.
*/
public class SlidingSlotEngine : LayeredSlotEngine
{

	private bool slideLock = false;								// Locks the engine while the sliding coroutine should be happening.	

	public SlidingSlotEngine(ReelGame reelGame, bool isLinkingLayersEverySpin, string freeSpinsPaytableKey = "") : base(reelGame, isLinkingLayersEverySpin, freeSpinsPaytableKey)
	{

	}

	// Lets the engine know that sliding has ended and that it's time to stop the reels.
	public IEnumerator endSliding()
	{
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnReelsSlidingEnded())
			{
				runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(module.executeOnReelsSlidingEnded()));
			}
		}
		// Wait for all the coroutines to end.
		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));

		// Reset the timer so we can stop the reels.
		timer = 0f;
		slideLock = false;
	}


	// stopReels - calls the sliding delegate if there is one set.
	public override void stopReels()
	{
		// if() prevents double calls
		if (!reelsStopWaitStarted)
		{
			slideLock = true;
			// Lock the engine, and call the sliding delegate
			RoutineRunner.instance.StartCoroutine(reelsSlidingCoroutine());
		}

		base.stopReels();
	}

	private IEnumerator reelsSlidingCoroutine()
	{
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnReelsSlidingCallback())
			{
				runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(module.executeOnReelsSlidingCallback()));
			}
		}
		// Wait for all the coroutines to end.
		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));

		yield return RoutineRunner.instance.StartCoroutine(endSliding());
	}

	protected override void updateStateEndSpin()
	{
		if (!slideLock)
		{
			base.updateStateEndSpin();
		}
	}

	// @todo (9/7/2016 Scott Lepthien) : temp function to handle stuff that needs to happen after the reel 
	// set data changes, this should be stripped out when we change setReelSetData to actually be used and 
	// anything here should be placed at the end of that function
	public override void tempPostReelSetDataChangedHandler()
	{
		// After we swap out and change the reels, update the list of reels linked through the reel data
		updateDataLinkedReelList();
	}
}
