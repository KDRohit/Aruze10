using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the Duck Dynasty 3 Stars & Stripes 4th of July base game
*/
public class DuckDyn03 : SlotBaseGame
{
	private const float EXPLOSION_SYMBOL_SWAP_TIMING = 0.5f;		// Timing value to wait before swapping a symbol after showing the explosion
	private const float PLUNGER_DROP_ANIMATION_SHOW_TIME = 1.25f;	// TIming value to show the plunger drop animation before starting the symbol swap
	private const float TIME_BETWEEN_EXPLOSIONS = 0.125f;			// Slight wait time to stagger the explosions
	private const float DELAY_BEFORE_SHOW_PAYLINES = 0.25f;			// Timing value to wait after all explosions happen and gives user a chance to process result before showing outcomes
	private const float DELAY_BEFORE_SHOWING_EXPLOSIONS = 0.15f;	// Delay before showing the explosions
	private const float DELAY_BEFORE_PLUNDGE_ANIMATION = 0.5f;		// How long to wait before starting the plundge animation.

	private const float TRANSITION_FADE_TO_BLACK_TIME = 1.15f;		// How long the fade to black takes
	private const float SI_ROCKET_MOVE_IN_TIME = 2.0f;				// Time it takes Si's rocket to move into the middle of the screen
	private const float SI_ROCKET_MOVE_OUT_TIME = 1.0f;				// Time it takes Si's rocket to move into the middle of the screen
	private const float SI_ROCKET_START_X = -3.0f;					// Position to start the rocket
	private const float SI_ROCKET_CENTER_X = 0.0f;				// Position value to center the rocket over the reels
	private const float SI_ROCKET_END_X = 3.0f;					// Position to end the rocket

	private const string ROCKET_TRANSITION_MOVE_ANIM_NAME = "DD03_Transition_Rocket Si all"; 	// Name of the movement animation for the rocket transition
	private const string ROCKET_TRANSITION_COMBINED_ANIMS_NAME = "Take 001";					// Name of the combined animations for Si on the rocket
	private const string ANIM_NOT_ANIMATING = "not_animating";

	//Sound names
	private const string BEFORE_PLUNDGE_ANIMATION_SOUND = "WRFireInTheHole";	// The name of the sound that gets played before the plundger is pushed down.
	private const string PLUNDGE_ANIMATION_SOUND = "TWDuck74Plunger";			// Name of the sound played when the plunger starts moving
	private const string EXPLOSION_SOUND = "TWDuck74Explosion";					// Name of the sound played when the explosions are happening.
	private const string POST_EXPLOSION_VO = "SiBaWhoom74";						// Name of the sound played when the explosions have ended.
	private const string ROCKET_START_SOUND = "BonusTransitionDuck74Pt1";
	private const string ROCKET_IDLE_SOUND = "BonusTransitionDuck74Loop";
	private const string ROCKET_VO = "SiRocketVO";
	private const string ROCKET_EXIT_SOUND = "BonusTransitionDuck74Pt2";

	// Sound timings
	private const float ROCKET_VO_WAIT_TIME = 1.08f;							// Time to wait before playing the rocket VO sound
	private const float ROCKET_EXIT_WAIT_TIME = 1.25f;							// Time to wait before playing the rocket exit sound VO


	[SerializeField] private GameObject explosionPrefab = null;					// Prefab object of the explosion that transforms a symbol to a wild
	[SerializeField] private GameObject blackTransitionFade = null;				// Transition fade out
	[SerializeField] private Animation rocketTransitionAnimation = null;		// Si Rocket animations, used for transition into free spins
	[SerializeField] private Animator rocketTransitionMovementAnimator = null;	// The animator that controls the movement of the rocket
	[SerializeField] private GameObject fireworkTransitionAnimation = null;		// Firework animation that plays during the transition to free spins

	private List<GameObject> freeExplosionAnimations = new List<GameObject>();	// List of explosion prefabs which are free to use

	private bool isFullyFadedToBlack = false;									// Flag to track if the fade to black is finished

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject gets disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	/// Handles custom transition stuff for this game as well as standard
	/// reel stop override stuff
	private IEnumerator reelsStoppedCoroutine()
	{
		if (_outcome.isBonus)
		{
			// handle playing this early, so that it happens before the transition starts
			yield return StartCoroutine(doPlayBonusAcquiredEffects());

			// Do the transition before going to the free spins game.
			yield return StartCoroutine(doFreeSpinsTransition());
		}

		if (mutationManager.mutations.Count != 0)
		{
			StartCoroutine(doDynamiteWilds());
		}
		else 
		{
			// no mutations, so don't need to handle any bomber stuff
			base.reelsStoppedCallback();
		}
	}

	protected override IEnumerator onBonusGameEndedCorroutine()
	{
		yield return base.StartCoroutine(base.onBonusGameEndedCorroutine());

		// hide the black fade
		CommonGameObject.alphaGameObject(blackTransitionFade, 0.0f);
	}

	/// Do the transition of the base game of Si rocketing across and the
	/// game fading to black
	private IEnumerator doFreeSpinsTransition()
	{
		// turn the fireworks on
		fireworkTransitionAnimation.SetActive(true);

		// show the rocketeer
		rocketTransitionMovementAnimator.gameObject.SetActive(true);
		Audio.play(ROCKET_START_SOUND);

		// start the rocket movement
		rocketTransitionMovementAnimator.Play(ROCKET_TRANSITION_MOVE_ANIM_NAME);

		// start the Si animations
		rocketTransitionAnimation.Play(ROCKET_TRANSITION_COMBINED_ANIMS_NAME);

		// Not sure about this, I guess play it here
		Audio.play(ROCKET_IDLE_SOUND);

		// Wait for the animation to reach the raise glass animation and then play the VO
		yield return new TIWaitForSeconds(ROCKET_VO_WAIT_TIME);
		Audio.play(ROCKET_VO);

		// Wait for the animation to get to the exit part and then play the sound
		yield return new TIWaitForSeconds(ROCKET_EXIT_WAIT_TIME);
		Audio.play(ROCKET_EXIT_SOUND);

		// Wait just a little more before starting the fade
		yield return new TIWaitForSeconds(0.75f);

		// turn the fireworks off as he zooms off
		fireworkTransitionAnimation.SetActive(false);

		// fade to black
		CommonGameObject.alphaGameObject(blackTransitionFade, 0.0f);
		isFullyFadedToBlack = false;
		StartCoroutine(fadeGameToBlack(TRANSITION_FADE_TO_BLACK_TIME));

		// ensure he has finished his animaiton
		while (!rocketTransitionMovementAnimator.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
		{
			yield return null;
		}

		// hide him now that his animation is done
		rocketTransitionMovementAnimator.gameObject.SetActive(false);

		// wait on the fade which should go slightly beyond the animation
		while (!isFullyFadedToBlack)
		{
			yield return null;
		}
	}
	
	/// Handles the Dynamite Wild mutations caused by a TW symbol landing on reel 3
	public IEnumerator doDynamiteWilds()
	{
		Audio.play("TriggerWildTaDa");

		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;

		// First play the plunger pressing animation and change the TW symbol to TW-WD (a dynamite wild).
		SlotReel[] reelArray =  engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			SlotReel reel = reelArray[i];
	
			List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;
	
			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "TW")
				{
					SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];

					Audio.play(BEFORE_PLUNDGE_ANIMATION_SOUND);
					yield return new TIWaitForSeconds(DELAY_BEFORE_PLUNDGE_ANIMATION);
					Audio.play(PLUNDGE_ANIMATION_SOUND);
					// using a stretching animation to imply the plunger is being pressed
					symbol.animateOutcome();

					// wait a bit for the animation to play, waiting till the actual 
					// end of the animation was causing too much of a pause with nothing
					// happening
					yield return new TIWaitForSeconds(PLUNGER_DROP_ANIMATION_SHOW_TIME);

					// convert this to the TW-WD symbol
					symbol.mutateTo("TW-WD");

					// turn on the TW overlay//
					symbol.showWild();
				}
			}
		}
				
		//Wait before starting all the explosions
		yield return new WaitForSeconds(DELAY_BEFORE_SHOWING_EXPLOSIONS);

		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					StartCoroutine(playExplosion(getReelRootsAt(i).transform, Vector3.up * getSymbolVerticalSpacingAt(i) * j, reelArray[i].visibleSymbolsBottomUp[j], "WD3"));

					// introduce a bit of a delay to stagger the explosions
					yield return new TIWaitForSeconds(TIME_BETWEEN_EXPLOSIONS);
				}
			}
		}
		// Wait for all of the explosions to finish
		yield return new TIWaitForSeconds(EXPLOSION_SYMBOL_SWAP_TIMING - TIME_BETWEEN_EXPLOSIONS);
		Audio.play(POST_EXPLOSION_VO);
		yield return new TIWaitForSeconds(DELAY_BEFORE_SHOW_PAYLINES);
		
		base.reelsStoppedCallback();
	}

	/// Play an explosion for a -TW mutation symbol
	private IEnumerator playExplosion(Transform parentTransform, Vector3 pos, SlotSymbol symbol, string newSymbolName)
	{
		GameObject explosion = null;
		if (freeExplosionAnimations.Count > 0)
		{
			// can grab an explosion that is already created
			explosion = freeExplosionAnimations[freeExplosionAnimations.Count - 1];
			freeExplosionAnimations.RemoveAt(freeExplosionAnimations.Count - 1);
		}
		else
		{
			// need to create a new explosion, don't have enough
			explosion = CommonGameObject.instantiate(explosionPrefab) as GameObject;
		}

		// double check that everything is alright with the animation
		if (explosion != null)
		{
			explosion.transform.parent = parentTransform;
			explosion.transform.localPosition = pos;
			CommonGameObject.setLayerRecursively(explosion, Layers.ID_SLOT_FRAME);
			explosion.SetActive(true);

			// Play the sound
			Audio.play(EXPLOSION_SOUND);
			// let the explosion get a ways into the animation
			yield return new TIWaitForSeconds(EXPLOSION_SYMBOL_SWAP_TIMING);

			// swap in the new symbol
			symbol.mutateTo(newSymbolName);

			// let the explosion continue and finish
			yield return new TIWaitForSeconds(EXPLOSION_SYMBOL_SWAP_TIMING);

			// save the explosion so it can be reused again later
			explosion.SetActive(false);
			freeExplosionAnimations.Add(explosion);
		}
		else
		{
			// at least make sure the symbol changes, even if the explosion fails
			symbol.mutateTo(newSymbolName);
		}
	}

	/**
	Fades in the black overlay to cover the game
	*/
	private IEnumerator fadeGameToBlack(float duration)
	{
		float timeElapsed = 0;

		while (timeElapsed < duration)
		{
			timeElapsed += Time.deltaTime;

			CommonGameObject.alphaGameObject(blackTransitionFade, timeElapsed/duration);

			yield return null;
		}

		CommonGameObject.alphaGameObject(blackTransitionFade, 1.0f);
		isFullyFadedToBlack = true;
	}
}
