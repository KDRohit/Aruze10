using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the Lls 07 Ice Princesses base game
*/
public class Lls07 : SlotBaseGame
{
	[SerializeField] private GameObject twSnowballSymbolPrefab = null;				// Snowball animations used before transforming to TW symbols
	[SerializeField] private GameObject baseGameBkg = null;							// Base game background that is swapped for the freespins background during the transition
	[SerializeField] private GameObject baseGameBkgEffects = null;					// Effects for the background that need to be turned off during the transition
	[SerializeField] private GameObject freeSpinsBkg = null;						// Free spin background shown after the shatter transition before the actual free spins loads
	[SerializeField] private Animator iceShatterTransitionAnim = null;				// Transition animation of full screen ice shatter

	// Transition Consts
	private const string ICE_SHATTER_TRANSITION_INTRO_ANIM_NAME = "LLS07_transitionIntro";	// Animation name for the ice shatter transition
	private const float ICE_SHATTER_INTRO_TIME = 1.0f;										// Animation time of the animation for the ice to cover the screen
	private const float AFTER_ICE_SHATTER_WAIT_TIME = 0.2f;									// Very slight delay so the player sees the free spin background before game swap
	private const float SHATTER_SOUND_DELAY = 0.6f;											// delay before playing hte shatter sound

	// TW Snowball Consts
	private const string SNOWBALL_ANIMATION_NAME = "LLS07_IcePrincesses_Reel_TWSnowballs_01_Animation"; // Animation name for the snowball toss animation
	private const string ANIM_NOT_ANIMATING = "not_animating";
	private const float SNOWBALL_STAGGER_TIME = 0.25f;								// Time value to stagger the snowballs by so they hit at slightly different times
	private const float SNOWBALL_MOVE_TIME = 0.666f;									// Time it takes for the snowball to move, syncs with the animation of the snowball flying up and down

	// TW Snowball statics
	private static List<GameObject> s_freeSnowballs = new List<GameObject>();		// List of unused snowball animations which can be reused
	private static List<GameObject> s_displayedSnowballs = new List<GameObject>();	// List of snowballs that are currently being played, used to track when it is safe to proceed to showing payouts

	// Sound Consts
	private const string SNOWBALL_LAUNCH_SOUND = "LaunchSnowball";
	private const string SNOWBALL_LANDS_SOUND = "SnowballTurnsWild";
	private const string BEAR_BREAKS_ICE_SOUND = "BearBreaksIce";
	private const string BEAR_ROAR_SOUND = "BearRoar";								// Bear roar to play after the initial ice break sound
	private const string ICE_TRANSITION_START_SOUND = "IceTransitionStart";
	private const string ICE_TRANSITION_END_SOUND = "IceTransitionFinish";

	protected override void OnDestroy()
	{
		base.OnDestroy();

		// make sure we cleanup the static bomb animations shared 
		// by the base and freespins when the base game is exited
		Lls07.s_freeSnowballs.Clear();
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
		if (_outcome.isBonus)
		{
			// handle playing this early, so that it happens before the transition starts
			yield return StartCoroutine(doPlayBonusAcquiredEffects());

			// Do the transition before going to the free spins game.
			yield return StartCoroutine(doFreeSpinsTransition());
		}

		// convert frozen bears to regular bears
		Lls07.doSpecialWildMutations(this, _outcomeDisplayController, _outcome);

		if (mutationManager.mutations.Count != 0)
		{
			this.StartCoroutine(Lls07.doSnowballWilds(this, base.reelsStoppedCallback, twSnowballSymbolPrefab));
		}
		else 
		{
			// no mutations, so don't need to handle any TW stuff
			base.reelsStoppedCallback();
		}
	}
	
	protected override IEnumerator onBonusGameEndedCorroutine()
	{
		yield return StartCoroutine(base.onBonusGameEndedCorroutine());

		// Now put the base game back in a good state to return to
		baseGameBkg.SetActive(true);
		baseGameBkgEffects.SetActive(true);
		freeSpinsBkg.SetActive(false);
		
		// show the reels again
		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			getReelRootsAt(i).SetActive(true);
		}
	}

	// mutate all WDs (ice bears) to their unfrozen counterparts
	public static void doSpecialWildMutations (ReelGame reelGame, OutcomeDisplayController outcomeDisplayController, SlotOutcome outcome)
	{
		HashSet<SlotSymbol> winningSymbols = outcomeDisplayController.getSetOfWinningSymbols(outcome);

		int numSoundsPlayed = 0;

		foreach (SlotSymbol symbol in winningSymbols)
		{
			if (symbol.name.Contains("WD") && !symbol.isWildShowing)
			{
				symbol.showWild();
				Audio.play(BEAR_BREAKS_ICE_SOUND);

				if(numSoundsPlayed > 0)
				{
					Audio.play(BEAR_ROAR_SOUND, 0.5f);
				}

				++numSoundsPlayed;
			}
		}
	}

	/*
	Do the transition of the base game fading into the free spin game
	*/
	private IEnumerator doFreeSpinsTransition()
	{
		// Freeze the screen then explode showing the free spin background
		// Changing the layer so we don't see the state that resets the positions
		CommonGameObject.setLayerRecursively(iceShatterTransitionAnim.gameObject, Layers.ID_HIDDEN);
		iceShatterTransitionAnim.gameObject.SetActive(true);
		iceShatterTransitionAnim.Play(ICE_SHATTER_TRANSITION_INTRO_ANIM_NAME);

		// ensure we've left the idle state
		while (iceShatterTransitionAnim.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
		{
			yield return null;
		}

		Audio.play(ICE_TRANSITION_START_SOUND);

		// Now that we're out of the idle reset state we can display the transition objects
		CommonGameObject.setLayerRecursively(iceShatterTransitionAnim.gameObject, Layers.ID_SLOT_OVERLAY);

		yield return new TIWaitForSeconds(ICE_SHATTER_INTRO_TIME);

		// hide the reels since the background is getting swapped
		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			getReelRootsAt(i).SetActive(false);
		}

		// Swap the backgrounds now that the screen is covered
		baseGameBkg.SetActive(false);
		baseGameBkgEffects.SetActive(false);
		freeSpinsBkg.SetActive(true);

		yield return new TIWaitForSeconds(SHATTER_SOUND_DELAY);
		Audio.play(ICE_TRANSITION_END_SOUND);

		// wait for the shatter animation to finish
		while (!iceShatterTransitionAnim.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
		{
			yield return null;
		}

		iceShatterTransitionAnim.gameObject.SetActive(false);

		yield return new TIWaitForSeconds(AFTER_ICE_SHATTER_WAIT_TIME);
	}
	
	// This function handles the snowballs mutating symbols into wilds.
	// It is used by both the base game and the free spins game, so it is a static
	// function that passes in the game (base or free spins).
	public static IEnumerator doSnowballWilds(ReelGame reelGame, GenericDelegate gameReelStoppedCallback, GameObject snowballPrefab)
	{
		//Audio.play("TriggerWildTaDa");

		SlotEngine engine = reelGame.engine;
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;

		// First mutate any TW symbols to TWWD (the duck call wild).
		SlotSymbol twSymbol = null;

		// TW symbol appears on the middle reel
		SlotReel[] reelArray = engine.getReelArray();

		SlotReel reel = reelArray[2];

		List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;

		for (int j = 0; j < symbols.Count; j++)
		{
			if (symbols[j].animator != null && symbols[j].animator.symbolInfoName== "TW")
			{
				twSymbol = symbols[j];
				break;
			}
		}


		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					reelGame.StartCoroutine(Lls07.playSnowballAnimation(reelGame.getReelRootsAt(2).transform, twSymbol, reelArray[i].visibleSymbolsBottomUp[j], snowballPrefab));
					// stagger the snowballs slightly
					yield return new TIWaitForSeconds(SNOWBALL_STAGGER_TIME);
				}
			}
		}

		// wait for the bombs to finish going off
		while (Lls07.s_displayedSnowballs.Count > 0)
		{
			yield return null;
		}

		twSymbol.showWild();
		
		// wait a bit to let the user see the new layout of the reels
		yield return new WaitForSeconds(0.25f);
		
		// start showing the outcomes
		gameReelStoppedCallback();
	}

	/**
	Play a snowball animation for a -TW mutation symbol
	*/
	private static IEnumerator playSnowballAnimation(Transform parentTransform, SlotSymbol twSymbol, SlotSymbol targetSymbol, GameObject snowballPrefab)
	{
		GameObject snowball = null;
		if (Lls07.s_freeSnowballs.Count > 0)
		{
			// can grab a snowball that is already created
			snowball = Lls07.s_freeSnowballs[Lls07.s_freeSnowballs.Count - 1];
			Lls07.s_freeSnowballs.RemoveAt(Lls07.s_freeSnowballs.Count - 1);
		}
		else
		{
			// need to create a new snowball, don't have enough
			snowball = CommonGameObject.instantiate(snowballPrefab) as GameObject;
		}

		// double check that everything is alright with the snowball
		if (snowball != null)
		{
			Animator snowballAnimator = snowball.GetComponent<Animator>();

			snowball.transform.parent = parentTransform;
			snowball.transform.position = twSymbol.animator.gameObject.transform.position;
			CommonGameObject.setLayerRecursively(snowball, Layers.ID_SLOT_OVERLAY);

			snowball.SetActive(true);

			// play the snowball animation for each symbol that is going to change
			Lls07.s_displayedSnowballs.Add(snowball);
			snowballAnimator.Play(SNOWBALL_ANIMATION_NAME);

			Audio.play(SNOWBALL_LAUNCH_SOUND);

			yield return new TITweenYieldInstruction(iTween.MoveTo(snowball, iTween.Hash("position", targetSymbol.animator.gameObject.transform.position, "time", SNOWBALL_MOVE_TIME, "islocal", false, "easetype", iTween.EaseType.linear)));

			Audio.play(SNOWBALL_LANDS_SOUND);

			// swap the symbol during the splat
			targetSymbol.mutateTo("TW");
			targetSymbol.showWild();

			while (!snowballAnimator.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
			{
				yield return null;
			}

			// save the snowball so it can be reused again later
			snowball.SetActive(false);
			Lls07.s_freeSnowballs.Add(snowball);
			Lls07.s_displayedSnowballs.Remove(snowball);
		}
		else
		{
			// even if something happens and the bomb doesn't load still swap the symbol
			targetSymbol.mutateTo("TW");
			targetSymbol.showWild();
		}
	}

	/// Used by the free spin game so it can unparent the snowballs so they aren't destroyed
	public static void unparentSnowballs()
	{
		foreach (GameObject snowball in s_freeSnowballs)
		{
			if (SlotBaseGame.instance != null)
			{
				// try to parent back to the base game from the free spin game
				snowball.transform.parent = SlotBaseGame.instance.gameObject.transform;
			}
			// else this is gifted bonus, so leave them attached to the free spin game so they will be cleaned up
		}

		if (SlotBaseGame.instance == null)
		{
			// gifted free spins, need to clear the static effects because they will be left around and cause issues if they aren't
			Lls07.s_freeSnowballs.Clear();
		}
	}
}
