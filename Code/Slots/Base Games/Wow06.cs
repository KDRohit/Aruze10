using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Wow06 : SlotBaseGame, IResetGame
{
	public GameObject firePrefab; // prefab that is flown in during mutations
	public Animator leftDragonAnimator;
	public Animator rightDragonAnimator;

	private static bool isBreathingInFire = false; // are we in the middle of a mutation

	// Sound constants
	private const string FIRE_SOUND = "TWDragonFire";
	private const string TW_SOUND = "TWDragonInit";
	private const string TRANSFORM_SOUND = "TWDragonTransformFireburst"; // symbol actually mutates sound

	// Animation constants
	private const string LEFT_DRAGON_ANIMATION = "Dragon_Lf_ani";
	private const string LEFT_DRAGON_STILL = "Dragon_Lf_still";
	private const string RIGHT_DRAGON_ANIMATION = "Dragon_Rt_ani";
	private const string RIGHT_DRAGON_STILL = "Dragon_Rt_still";

	// Timing constants
	private const float TW_OUTCOME_ANIMATION_SHOW_TIME = 1.65f;	// Timing value to show the TW symbol acquired animation before starting the symbol swap
	private const float SCATTER_ANIMATION_WAIT = 1.5f;
	private const float TIME_BETWEEN_MUTATION = .25f;
	private const float TIME_BEFORE_START_ANIMATION = 1.25f;
	private const float BURN_WAIT_TIME = 0.65f;
	private const float PRE_MUTATE_WAIT_TIME = 0.5f;
	private const float FIRE_SOUND_DELAY = 0.5f;
	
	protected override void reelsStoppedCallback()
	{
		StartCoroutine(Wow06.doAnimationWilds(this, base.reelsStoppedCallback, firePrefab, leftDragonAnimator, rightDragonAnimator));
	}

	// If we have a scatter bonus game, loop through and find the scatter symbols
	// then play their outcome animation before moving on to the bonus game
	public static IEnumerator doScatterSymbolAnimations(ReelGame reelGame)
	{
		if (reelGame.outcome.isCredit)
		{
			SlotEngine engine = reelGame.engine;

			// play outcome animations on all scatter symbols
			SlotReel[] reelArray = engine.getReelArray();

			for (int i = 0; i < reelArray.Length; i++)
			{
				// There is at least one symbol to change in this reel.
				SlotReel reel = reelArray[i];
				
				List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;
				
				for (int j = 0; j < symbols.Count; j++)
				{
					if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "SC")
					{
						symbols[j].animateOutcome();
					}
				}
			}

			yield return new TIWaitForSeconds(SCATTER_ANIMATION_WAIT); // wait for the animations to do their thang
		}

		yield break;
	}

	// This function handles the firebreating dragon sequence.
	// It is used by both the base game and the free spins game, so it is a static
	// function that passes in the game (base or free spins).
	public static IEnumerator doAnimationWilds(ReelGame reelGame, GenericDelegate callback, GameObject revealPrefab, Animator leftDragonAnimator, Animator rightDragonAnimator)
	{
		yield return reelGame.StartCoroutine(doScatterSymbolAnimations(reelGame));

		if (reelGame.mutationManager.mutations.Count == 0)
		{
				// No mutations, so do nothing special.
				callback();
				yield break;
		}

		SlotEngine engine = reelGame.engine;
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;

		if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
		{
			Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
			yield break;
		}

		Wow06.isBreathingInFire = true;
		SlotSymbol initialTWSymbol = null;

		// First mutate any TW symbol to the landing, and animate it.
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			// There is at least one symbol to change in this reel.
			SlotReel reel = reelArray[i];

			List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;

			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].animator != null && symbols[j].serverName == "TW")
				{
					initialTWSymbol = symbols[j];
					symbols[j].mutateTo("TWLanding", null, true, true); // Don't play the animation since we call it manually.
					reelGame.StartCoroutine(playTWAnimationTillAnimationDone(symbols[j], reelGame, revealPrefab));
				}
			}
		}

		// Begin the dragon sequence finally
		leftDragonAnimator.Play(LEFT_DRAGON_ANIMATION);
		rightDragonAnimator.Play(RIGHT_DRAGON_ANIMATION);
		Audio.play(FIRE_SOUND, 1, 0, FIRE_SOUND_DELAY);

		yield return new WaitForSeconds(TIME_BEFORE_START_ANIMATION);

		// Go from row to row begin the single fire sequence
		for (int z = 0; z < 4; z++)
		{
			for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
			{
				for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
				{
					if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "" && j == z)
					{
						SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
						reelGame.StartCoroutine(burnSymbol(symbol));
					}
				}
			}
			yield return new TIWaitForSeconds(TIME_BETWEEN_MUTATION);
		}
		
		Wow06.isBreathingInFire = false;
		yield return new WaitForSeconds(PRE_MUTATE_WAIT_TIME);

		// Mutate the initial symbol into the fire TW finally
		initialTWSymbol.mutateTo("TWWD");

		// Make the dragon animation go back to still, as it doesn't loop back to still once its done.
		leftDragonAnimator.Play(LEFT_DRAGON_STILL);
		rightDragonAnimator.Play(RIGHT_DRAGON_STILL);

		callback();
	}

	// Turn it into the fireball symbol, animate, then turn it into the final TW symbol
	public static IEnumerator burnSymbol(SlotSymbol symbol)
	{
		SlotSymbol mutateSymbol = new SlotSymbol(ReelGame.activeGame);
		mutateSymbol.setupSymbol("TWMutate", symbol.index, symbol.reel);
		CommonGameObject.setLayerRecursively(mutateSymbol.gameObject, Layers.ID_SLOT_FOREGROUND);
		mutateSymbol.gameObject.name = "TWMutateTemp";
		mutateSymbol.gameObject.transform.localPosition = mutateSymbol.gameObject.transform.localPosition + new Vector3(0, 0, -1);
		mutateSymbol.animateOutcome();
		
		// Want to wait less than the original animation time as we can just pop the symbol in instead of offshift an incorrectly shifted symbol into place.
		yield return new TIWaitForSeconds(BURN_WAIT_TIME);

		Destroy(mutateSymbol.gameObject);
		symbol.mutateTo("TWWD");
		Audio.play(TRANSFORM_SOUND);
	}

	/// Custom handling for specific reel features
	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		if (!Wow06.isBreathingInFire)
		{
			List<SlotSymbol> symbols = stoppedReel.visibleSymbolsBottomUp;

			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "TW")
				{
					Audio.play(TW_SOUND);
					symbols[j].animateOutcome();
				}
			}
		}

		yield break;
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		isBreathingInFire = false;
	}

	/// Used to continually animate the TW symbol until the mutations are done
	private static IEnumerator playTWAnimationTillAnimationDone(SlotSymbol twSymbol, ReelGame reelGame, GameObject revealPrefab)
	{
		while (Wow06.isBreathingInFire)
		{
			twSymbol.animateOutcome();

			// something about the animation is causing the symbol to revert to the SLOT_REELS layer, so after playing the
			// outcome animation we need to set the object to the correct layer every time
			//CommonGameObject.setLayerRecursively(twSymbol.animator.gameObject, Layers.ID_SLOT_FRAME);
			yield return new TIWaitForSeconds(TW_OUTCOME_ANIMATION_SHOW_TIME);
		}
	}
}
