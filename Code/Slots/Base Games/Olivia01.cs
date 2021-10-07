using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the Olivia 01 Wild Bomshells base game
*/
public class Olivia01 : SlotBaseGame
{
	// Ambient Plane Consts
	private const string AMBIENT_AIRPLANE_ANIM_STATE_1 = "Airplane_play1_Animation";
	private const string ANIM_NOT_ANIMATING = "not_animating";

	private const float MIN_AMBIENT_AIRPLANE_ANIM_TIME = 5.0f;
	private const float MAX_AMBIENT_AIRPLANE_ANIM_TIME = 8.0f;

	// TW Plane Bombing Consts
	private const string TW_BOMBER_FLY_OVER_ANIM_NAME = "Oli01_planeFlyOver_Animation";
	private const string TW_BOMB_FALLING_ANIM_NAME = "Oli01_FallingBomb_Animation";

	private const float TIME_BEFORE_TW_BOMBS_GO_OFF = 0.75f;
	private const float TW_BOMB_OUTCOME_ANIMATION_SHOW_TIME = 1.65f;	// Timing value to show the TW symbol acquired animation before starting the symbol swap
	private const float BOMB_DROP_STAGGER_TIME = 0.25f;					// Time value to stagger the bomb drops by so they hit at slightly different times

	// FreeSpin Transition Consts
	private const string FREESPIN_TRANSITION_PLANE_FLYOVER = "free_spin_plane_flyover";

	private const float FREESPIN_TRANSITION_FADE_OUT_BASE_TIME = 1.0f;
	private const float FREESPIN_TRANSITION_FADE_IN_FS_TIME = 1.0f;

	// Sound consts
	private const string BOMB_WHISTLE_SOUND = "BombWhistle";						// Whistle sound played at start of TW mutation phase
	private const string PLANE_FLY_BY_SOUND = "TWOliviaFlyby";						// Sound played when a plane flys by for TW or transition
	private const string PRE_BOMB_DROP_VO_SOUND = "TWOliviaPreBombVO";				// Voice over played before the bombing run starts
	private const string BOMB_DROP_VO_SOUND = "TWOliviaBombDropVO";					// Voice over played at start of TW mutation phase
	private const string POST_BOMB_DROP_VO_SOUND = "TWOliviaPostBombVO";			// Voice over that plays after the bombs have been dropped
	private const string BOMB_SOUND = "Bomb";										// Bomb noise played when the bomb hits a symbol
	private const string FREE_SPIN_TRANSITION_FLY_BY_SOUND = "FreespinOliviaFlyby";	// Fly by sound for the planes that transition into free spins

	// Ambient Plane Vars
	[SerializeField] private Animator ambientAirplaneAnimator = null;		// Ambient airplane object that contains animations that play periodically
	private float ambientPlaneAnimTimer;
	private bool isPlayingAmbientPlaneAnim = false;							// Tracks if the ambient plane anim should be playing
	private bool isAmbientPlaneAnimStarted = false;							// Tracks if the ambient plane anim has started yet, used because animator isn't starting right away for some dumb reason

	// TW Plane Bombinb Vars
	[SerializeField] private Animator twBomberFlyOverAnimator = null;		// Plane the flys over the reels when the TW symbol shows up on the reels
	[SerializeField] private GameObject bombDropPrefab = null;				// Prefab for the bomb drop animation
	private static List<GameObject> s_freeBombAnimations = new List<GameObject>();		// List of unused bomb animations which can be reused
	private static List<GameObject> s_playingBombAnimations = new List<GameObject>();	// List of bombs that are currently being played, used to track when it is safe to proceed to showing payouts
	private static bool isDroppingBombs = false;										// Flag to track if bombs are still being dropped

	// FreeSpin Transition Vars
	[SerializeField] private MeshRenderer backgroundMesh = null;			// Mesh renderer for the background, used to fade for the freespin transition
	[SerializeField] private MeshRenderer freeSpinBackgroundMesh = null;	// Mesh which is a copy of the free spin background so we can show it here before we swap over to the actual game
	[SerializeField] private GameObject gameFrame = null;					// GameObject for the Frame, hides during the transition to freespin game
	[SerializeField] private Animation freeSpinPlaneTransition = null;		// Animation for the planes that fly over the reels right before the transition to the freespins

	public void OnAwake()
	{
		ambientPlaneAnimTimer = Random.Range(MIN_AMBIENT_AIRPLANE_ANIM_TIME, MAX_AMBIENT_AIRPLANE_ANIM_TIME);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		// make sure we cleanup the static bomb animations shared 
		// by the base and freespins when the base game is exited
		Olivia01.s_freeBombAnimations.Clear();
	}

	protected override void Update()
	{
		if (!isPlayingAmbientPlaneAnim)
		{
			// check if we should start playing the next plane animation
			ambientPlaneAnimTimer -= Time.deltaTime;

			if (ambientPlaneAnimTimer <= 0)
			{
				// time to start playing the animation
				ambientAirplaneAnimator.Play(AMBIENT_AIRPLANE_ANIM_STATE_1);

				isPlayingAmbientPlaneAnim = true;
				// tracking this because animator isn't changing state fast enough
				isAmbientPlaneAnimStarted = false;
			}
		}
		else
		{
			if (!isAmbientPlaneAnimStarted)
			{
				// wait for the animation to change states and actually start animating
				if (!ambientAirplaneAnimator.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
				{
					isAmbientPlaneAnimStarted = true;
				}
			}
			else
			{
				// now it is safe to check if the animation state is in the ended state

				// currently playing the plane animation, check if we're done
				if (ambientAirplaneAnimator.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
				{
					// reset the timer
					ambientPlaneAnimTimer = Random.Range(MIN_AMBIENT_AIRPLANE_ANIM_TIME, MAX_AMBIENT_AIRPLANE_ANIM_TIME);

					// mark that we aren't playing the animation anymore
					isPlayingAmbientPlaneAnim = false;
				}
			}
		}

		base.Update();
	}

	/// Make sure the ambient planes get put in a good state when the game comes back
	private void resetAmbientPlanes()
	{
		ambientAirplaneAnimator.Play(ANIM_NOT_ANIMATING);
		ambientAirplaneAnimator.gameObject.transform.localPosition = new Vector3(17.5f, 4.0f, 6.5f);
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

		if (mutationManager.mutations.Count != 0)
		{
			this.StartCoroutine(Olivia01.doBomberStrikeWilds(this, base.reelsStoppedCallback, twBomberFlyOverAnimator, bombDropPrefab));
		}
		else 
		{
			// no mutations, so don't need to handle any bomber stuff
			base.reelsStoppedCallback();
		}
	}
	
	protected override IEnumerator onBonusGameEndedCorroutine()
	{
		yield return StartCoroutine(base.onBonusGameEndedCorroutine());

		// Put the base game back in a good state to return to
		
		// make sure the ambient animation is in a good state
		resetAmbientPlanes();
		
		// hide the free spin background
		setFadeAmount(freeSpinBackgroundMesh, 1.0f);
		
		// reset to the normal background with the frame
		setFadeAmount(backgroundMesh, 0.0f);
		gameFrame.SetActive(true);
		
		// turn the reels back on
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			getReelRootsAt(i).SetActive(true);
		}
	}

	/**
	Set how much the background is faded into the freespin background
	0.0f - Base game
	1.0f - Freespin game
	*/
	private static void setFadeAmount(MeshRenderer meshRenderer, float amount)
	{
		meshRenderer.material.SetFloat("_Fade", amount);
	}

	/*
	Do the transition of the base game fading into the free spin game
	*/
	private IEnumerator doFreeSpinsTransition()
	{
		// Instantiate a bunch of transition objects and make them fly toward the camera (or appear to, at least).
		
		// @todo : Perform the transition animation

		// hide the reels since when the frame is hidden they aren't going to be masked 
		// @todo : might consider making them fade and not just hide, we'll see
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			getReelRootsAt(i).SetActive(false);
		}

		// hide the frame
		gameFrame.SetActive(false);

		// play audio for fly over
		Audio.play(FREE_SPIN_TRANSITION_FLY_BY_SOUND);

		// play the plane flying over the reels
		freeSpinPlaneTransition.gameObject.SetActive(true);
		freeSpinPlaneTransition[FREESPIN_TRANSITION_PLANE_FLYOVER].speed = 0.5f;
		freeSpinPlaneTransition.Play(FREESPIN_TRANSITION_PLANE_FLYOVER);

		// fade the background to the free spin background
		float elapsedTime = 0;
		while (elapsedTime < FREESPIN_TRANSITION_FADE_OUT_BASE_TIME)
		{
			elapsedTime += Time.deltaTime;
			yield return null;
			setFadeAmount(backgroundMesh, elapsedTime / FREESPIN_TRANSITION_FADE_OUT_BASE_TIME);
		}

		// make sure the blue sky is fully faded in now
		setFadeAmount(backgroundMesh, 1.0f);

		// wait a bit with the blue background before we fade in the freespin background
		yield return new TIWaitForSeconds(0.25f);

		elapsedTime = 0;
		while (elapsedTime < FREESPIN_TRANSITION_FADE_IN_FS_TIME)
		{
			elapsedTime += Time.deltaTime;
			yield return null;
			setFadeAmount(freeSpinBackgroundMesh, 1 - elapsedTime / FREESPIN_TRANSITION_FADE_IN_FS_TIME);
		}

		// make sure that the free spin background is fully faded in
		setFadeAmount(freeSpinBackgroundMesh, 0.0f);

		// make sure the planes are fully offscreen
		while (freeSpinPlaneTransition.isPlaying)
		{
			yield return null;
		}
		freeSpinPlaneTransition.gameObject.SetActive(false);
	}
	
	// This function handles the bombs dropping in and mutating symbols into wilds.
	// It is used by both the base game and the free spins game, so it is a static
	// function that passes in the game (base or free spins).
	public static IEnumerator doBomberStrikeWilds(ReelGame reelGame, GenericDelegate gameReelStoppedCallback, Animator twBomberFlyOverAnimator, GameObject bombDropPrefab)
	{
		Olivia01.isDroppingBombs = true;

		Audio.play("TriggerWildTaDa");

		PlayingAudio sound = null;

		SlotEngine engine = reelGame.engine;
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;

		SlotReel[] reelArray = engine.getReelArray();

		// First mutate any TW symbols to TWWD (the duck call wild).
		for (int i = 0; i < reelArray.Length; i++)
		{
			// There is at least one symbol to change in this reel.
			SlotReel reel = reelArray[i];
	
			List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;
	
			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "TW")
				{
					reelGame.StartCoroutine(playTWAnimationTillBombingDone(symbols[j]));
				}
			}
		}

		Audio.play(PLANE_FLY_BY_SOUND);

		// play intro VO
		sound = Audio.play(PRE_BOMB_DROP_VO_SOUND);
		// wait on the intro VO
		while (sound != null && sound.isPlaying)
		{
			yield return null;
		}

		// Play the plane flying and let it get over the reels before the bombs start dropping
		twBomberFlyOverAnimator.gameObject.SetActive(true);
		twBomberFlyOverAnimator.Play(TW_BOMBER_FLY_OVER_ANIM_NAME);
				
		// Let the plane get about half-way over the screen
		yield return new TIWaitForSeconds(TIME_BEFORE_TW_BOMBS_GO_OFF);

		// Wait on this voice over to complete before actually starting
		sound = Audio.play(BOMB_DROP_VO_SOUND);
		while (sound != null && sound.isPlaying)
		{
			yield return null;
		}
		
		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					reelGame.StartCoroutine(playBombAnimation(reelGame.getReelRootsAt(i).transform, Vector3.up * reelGame.getSymbolVerticalSpacingAt(i) * j, reelArray[i].visibleSymbolsBottomUp[j], bombDropPrefab));
					// stagger the drops slightly
					yield return new TIWaitForSeconds(BOMB_DROP_STAGGER_TIME);
				}
			}
		}

		// wait for the bombs to finish going off
		while (Olivia01.s_playingBombAnimations.Count > 0)
		{
			yield return null;
		}

		Olivia01.isDroppingBombs = false;

		// play and wait on the bombing complete VO
		sound = Audio.play(POST_BOMB_DROP_VO_SOUND);
		while (sound != null && sound.isPlaying)
		{
			yield return null;
		}

		// ensure that the bomber has cleared the screen
		while (!twBomberFlyOverAnimator.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
		{
			yield return null;
		}
		
		// wait a bit to let the user see the new layout of the reels
		yield return new WaitForSeconds(0.25f);

		// hide the bomber
		twBomberFlyOverAnimator.gameObject.SetActive(false);
		
		// start showing the outcomes
		gameReelStoppedCallback();
	}

	/**
	Play a bomb animation for a -TW mutation symbol
	*/
	private static IEnumerator playBombAnimation(Transform parentTransform, Vector3 pos, SlotSymbol symbol, GameObject bombDropPrefab)
	{
		GameObject bomb = null;
		if (Olivia01.s_freeBombAnimations.Count > 0)
		{
			// can grab a bomb that is already created
			bomb = Olivia01.s_freeBombAnimations[Olivia01.s_freeBombAnimations.Count - 1];
			Olivia01.s_freeBombAnimations.RemoveAt(Olivia01.s_freeBombAnimations.Count - 1);
		}
		else
		{
			// need to create a new bomb, don't have enough
			bomb = CommonGameObject.instantiate(bombDropPrefab) as GameObject;
		}

		// double check that everything is alright with the animation
		if (bomb != null)
		{
			Animator bombAnimator = bomb.GetComponent<Animator>();

			bomb.transform.parent = parentTransform;
			bomb.transform.localPosition = pos;
			CommonGameObject.setLayerRecursively(bomb, Layers.ID_SLOT_FRAME);

			bomb.SetActive(true);

			// Play bomb drop starting noise
			Audio.play(BOMB_WHISTLE_SOUND);

			// play the bomb animation for each symbol that is going to change
			Olivia01.s_playingBombAnimations.Add(bomb);
			bombAnimator.Play(TW_BOMB_FALLING_ANIM_NAME);

			yield return new TIWaitForSeconds(1.0f);
			Audio.play(BOMB_SOUND);

			// swap the symbol during the explosion
			symbol.mutateTo("TW-WD");

			while (!bombAnimator.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
			{
				yield return null;
			}

			// save the bomb so it can be reused again later
			bomb.SetActive(false);
			Olivia01.s_freeBombAnimations.Add(bomb);
			Olivia01.s_playingBombAnimations.Remove(bomb);
		}
		else
		{
			// even if something happens and the bomb doesn't load still swap the symbol
			symbol.mutateTo("TW-WD");
		}
	}

	/// Used to continually animate the TW symbol until the bombing run is done
	private static IEnumerator playTWAnimationTillBombingDone(SlotSymbol twSymbol)
	{
		while (Olivia01.isDroppingBombs)
		{
			twSymbol.animateOutcome();
			yield return new TIWaitForSeconds(TW_BOMB_OUTCOME_ANIMATION_SHOW_TIME);
		}

		twSymbol.mutateTo("TW-WD", null, true);
	}

	/// Used by the free spin game so it can unparent the bombs so they aren't destroyed
	public static void unparentBombAnimations()
	{
		foreach (GameObject bomb in s_freeBombAnimations)
		{
			if (SlotBaseGame.instance != null)
			{
				// try to parent back to the base game from the free spin game
				bomb.transform.parent = SlotBaseGame.instance.gameObject.transform;
			}
			// else this is gifted bonus, so leave them attached to the free spin game so they will be cleaned up
		}

		if (SlotBaseGame.instance == null)
		{
			// gifted free spins, need to clear the static effects because they will be left around and cause issues if they aren't
			Olivia01.s_freeBombAnimations.Clear();
		}
	}
}
