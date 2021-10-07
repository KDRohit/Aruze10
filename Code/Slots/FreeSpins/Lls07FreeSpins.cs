using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the Lls 07 Ice Princesses base game
*/
public class Lls07FreeSpins : FreeSpinGame
{
	// TW Snowballs
	[SerializeField] private GameObject twSnowballSymbolPrefab = null;				// Snowball animations used before transforming to TW symbols

	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		// unparent the snowballs from this object so they can still be used by the base game
		Lls07.unparentSnowballs();
	}

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject gets disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	/**
	Handles custom transition stuff for this game as well as standard
	reel stop override stuff
	*/
	private IEnumerator reelsStoppedCoroutine()
	{
		// convert frozen bears to regular bears
		Lls07.doSpecialWildMutations(this, _outcomeDisplayController, _outcome);

		if (mutationManager.mutations.Count != 0)
		{
			yield return this.StartCoroutine(Lls07.doSnowballWilds(this, base.reelsStoppedCallback, twSnowballSymbolPrefab));
		}
		else 
		{
			// no mutations, so don't need to handle any bomber stuff
			base.reelsStoppedCallback();
		}
	}
}
