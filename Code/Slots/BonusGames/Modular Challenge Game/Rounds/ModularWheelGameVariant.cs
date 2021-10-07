using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
New version of ModularWheelGameVariant spun off from DeprecatedModularWheelGameVariant
in order to support games that want to spin and handle more than one wheel at a time.

Creation Date: 2/8/2018
Original Author: Scott Lepthien
*/
public class ModularWheelGameVariant : ModularChallengeGameVariant 
{
	[SerializeField] private ModularWheel[] wheels = null; // the array of wheels that are part of this round
	[SerializeField] private bool useRelativeMultiplierAsCurrentMultiplier = false;
	[SerializeField] private bool isSpinningAllWheelsOnSwipe = false;
	private bool isSpinStarted = false;
	private bool isInputEnabled = false; // input enabled flag, used to stop input before things like initial animations get to play

	public override void init(ModularChallengeGameOutcome outcome, int roundIndex, ModularChallengeGame gameParent)
	{
		base.init(outcome, roundIndex, gameParent);

		if (wheels != null)
		{
			// Check if wheels contains nulls, if so we will just error out and terminate rather than add checks everywhere
			// to try and handle that broken data setup
			bool isNullInWheels = false;
			bool isEmptyWheelsArray = wheels.Length == 0;
			for (int i = 0; i < wheels.Length; i++)
			{
				if (wheels[i] == null)
				{
					isNullInWheels = true;
				}
			}

			if (isNullInWheels)
			{
				Debug.LogError("ModularWheelGameVariant.init() - wheels contains a null entry (will not run until this is fixed), terminating this round immediately!");
				StartCoroutine(roundEnd());
			}
			else if (isEmptyWheelsArray)
			{
				Debug.LogError("ModularWheelGameVariant.init() - wheels array was empty (will not run until this is fixed), terminating this round immediately!");
				StartCoroutine(roundEnd());
			}
			else
			{
				// wheels is null free so lets do the inits
				for (int i = 0; i < wheels.Length; i++)
				{
					wheels[i].init(this);
				}
			}
		}
		else
		{
			//  Wheel data isn't setup, so just terminate the game and log an error
			Debug.LogError("ModularWheelGameVariant.init() - wheels aren't setup for this ModularWheelGameVariant, terminating this round immediately!");
			StartCoroutine(roundEnd());
		}
	}

	// Reset the spin flag on round start
	public override IEnumerator roundStart()
	{
		// activate this parent object
		gameObject.SetActive(true);
		
		isInputEnabled = false;
		isSpinStarted = false;

		// start all of these together so that the wheels all can handle stuff at the same time
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		for (int i = 0; i < wheels.Length; i++)
		{
			coroutineList.Add(StartCoroutine(wheels[i].start()));
		}

		// Wait for all the start coroutines to end.
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));

		if (useRelativeMultiplierAsCurrentMultiplier)
		{
			gameParent.currentMultiplier = (int)SlotsWagerSets.getRelativeMultiplierForGame(GameState.game.keyName, ReelGame.activeGame.currentWager);
		}
		yield return StartCoroutine(base.roundStart());

		isInputEnabled = true;
	}

	// Callback for spin clicks from a single button that spins all the reels
	// if wheels are controlled by different buttons then you should hook the
	// ModularWheel spinButtonPressed function up to that button
	public void spinAllWheelsButtonPressed()
	{
		if (isSpinStarted || !isInputEnabled)
		{
			// ignore this, don't spin again!
			return;
		}

		// make sure we can press all the spin buttons
		for (int i = 0; i < wheels.Length; i++)
		{
			if (!wheels[i].canPressSpinButton())
			{
				return;
			}
		}

		isSpinStarted = true;

		// start the actual spin by calling each wheels spinButtonPressed function
		for (int i = 0; i < wheels.Length; i++)
		{
			wheels[i].spinButtonPressed();
		}
	}

	// start a spin from a swipe with a custom start velocity
	public void spinAllWheelsFromSwipe(ModularWheel swipedWheel, float startingVelocity, bool clockwise)
	{
		// ignore double-spin actions
		if (isSpinStarted || !isInputEnabled)
		{
			return;
		}

		if (isSpinningAllWheelsOnSwipe)
		{
			// start the actual spin by calling each wheels spinSwipe function
			for (int i = 0; i < wheels.Length; i++)
			{
				ModularWheel currentWheel = wheels[i];
				if (currentWheel != swipedWheel && !currentWheel.isSpinning && !currentWheel.isSpinComplete)
				{
					wheels[i].spinSwipe(startingVelocity, clockwise);
				}
			}
		}

		isSpinStarted = true;
	}

	// Function to get how many wheels are currently spinning
	public int getNumberOfWheelsSpinning()
	{
		int numberOfReelsSpinning = 0;
		for (int i = 0; i < wheels.Length; i++)
		{
			if (wheels[i].isSpinning)
			{
				numberOfReelsSpinning++;
			}
		}

		return numberOfReelsSpinning;
	}

	//Finish round method
	public override IEnumerator roundEnd()
	{
		// start all of these together so that the wheels all can handle stuff at the same time
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		for (int i = 0; i < wheels.Length; i++)
		{
			coroutineList.Add(StartCoroutine(wheels[i].onRoundEnd(gameParent.willAdvanceRoundEndGame())));
		}

		// Wait for all the start coroutines to end.
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));

		yield return StartCoroutine(base.roundEnd());
	}

	// Coroutine when spin complete
	public void checkForAllWheelsComplete()
	{
		bool allWheelsComplete = true;
		for (int i = 0; i < wheels.Length; i++)
		{
			if (!wheels[i].isSpinComplete)
			{
				allWheelsComplete = false;
				break;
			}
		}

		if (allWheelsComplete)
		{
			// end the current round
			StartCoroutine(roundEnd());
		}
	}
	
	// Gets the last outcome that was being used to display a pick/wheel outcome,
	// if the game is still being played it will return the current outcome,
	// otherwise it will return the final one used before the game ended.
	// Those derived variant classes will define what this function does.
	public override ModularChallengeGameOutcomeEntry getMostRecentOutcomeEntry()
	{
		if (isOutcomeExpected)
		{
			// We can ignore isReturningLastEntryOnRoundEnd since this will always return the last
			// entry used so long as we are still in the same round.
			ModularChallengeGameOutcomeRound currentRound = getCurrentRoundOutcome();
			if (currentRound.entries.Count > 0)
			{
				return currentRound.entries[0];
			}
		}

		// no outcome data to get
		return null;
	}
}
