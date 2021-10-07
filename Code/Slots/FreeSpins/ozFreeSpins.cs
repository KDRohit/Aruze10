using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Oz free spins has one reel that is considered wild, this reel moves left on each auto spin ending after it
goes through all 5 reels.
*/

public class ozFreeSpins : FreeSpinGame
{
	public GameObject expandedWildSymbol;				// The wild symbol that takes up the whole reel.
	private int moveCounter;							// The number of times the wild symobl has been moved.

	// Constant Variables
	private const float PRESPIN_PHRASE_DELAY = 1.0f;		// The amount of time to wait before playing the PRESPIN_PHRASE.
	private const float WILD_SYMBOL_MOVE_TIME = 1.0f;		// The amount of time it takes for the wild symbol to get from one symbol to the next.
	// Sound names
	private const string PRESPIN_PHRASE = "dogoodluck";						// Name of the sound played before the first spin starts.
	private const string WILD_SYMBOL_MOVING = "WildSymbolMoves1";			//Name of the sound played when the wild symbol moves left.
	private const string WILD_SYMBOL_STOPPING = "WildSymbolStopsClick";		// Name of the sound played when the wild symbol has stopped moving.
	
	public override void initFreespins()
	{
		base.initFreespins();
		
		// Set the initial position of the wild to reel 5 (0-based index).
		CommonTransform.setX(expandedWildSymbol.transform, getReelRootsAt(4).transform.position.x, Space.World);
		
		// move counter
		moveCounter = 0;
		
		// Play Doroth VO Good Luck with a bit of adelay
		Audio.play(PRESPIN_PHRASE, 1.0f, 0.0f, PRESPIN_PHRASE_DELAY);
	}
		
	protected override void startNextFreespin()
	{
		base.startNextFreespin();

		if (numberOfFreespinsRemaining >= getReelRootsLength())
		{
			Debug.LogError("The oz free spin wild doesn't know where to go!!! More numberOfFreespinsRemaining than reels exist.");
			return;
		}
		
		if (numberOfFreespinsRemaining < 0)
		{
			Debug.Log("We're below our threshold, time to bail.");
			return;
		}

		// Setting the wild index so we can skip the paylines animations later.
		engine.wildReelIndexes = new List<int>();
		int indexCounter = 4 - moveCounter < 0 ? 0 : 4 - moveCounter;
		engine.wildReelIndexes.Add(indexCounter);
		
		float x = getReelRootsAt(numberOfFreespinsRemaining).transform.position.x;
		
		iTween.MoveTo(expandedWildSymbol, iTween.Hash("x", x, "time", WILD_SYMBOL_MOVE_TIME, "easetype", iTween.EaseType.linear));
		
		// Logic so we don't have a move/click sound in the beginning/end of freespin
		if (moveCounter > 0 && moveCounter <= 4)
		{	
			// Sound Call for Move and Click on Expanded Wild Based on 1 second tween time
			Audio.play(WILD_SYMBOL_MOVING);
			Audio.play(WILD_SYMBOL_STOPPING, 1.0f, 0.0f, WILD_SYMBOL_MOVE_TIME);
		}
		
		// increment move/click counter
		moveCounter++;
	}
	
	protected override IEnumerator doReelsStopped(bool isAllowingContinueWhenReadyToEndSpin = true)
	{
		// Run the standard callback
		yield return StartCoroutine(base.doReelsStopped());
		
		// Loop through all the symbols in the currently covered reel and skip their animations so they don't pop-over our the wild banner
		foreach (SlotSymbol symbol in engine.getVisibleSymbolsAt(numberOfFreespinsRemaining))
		{
			// skip animaitons so we don't see the wild animations going on under the banner
			symbol.skipAnimationsThisOutcome();
		}
	}
}
