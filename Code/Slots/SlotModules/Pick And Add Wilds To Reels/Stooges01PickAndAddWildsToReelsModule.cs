using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module that implments the stooges01 feature where a pick UI determines a number of wilds that will be apllied to the reels
*/
public class Stooges01PickAndAddWildsToReelsModule : PickAndAddWildsToReelsModule 
{
	[SerializeField] private Animator pickingObjectHolderAnimator;
	[SerializeField] private Animator countBoxAnimator;

	private bool isFirstPickPhase = true;

	// Game Timing
	private const float DROPPING_FINISHED_DELAY = 1.6f; // how long until the last drop finishes
	private const float PICK_SCREEN_SHOW_ALL_DELAY = 0.5f;	// delay to show all the possible picks before it slides off
	private const float PICK_SCREEN_SLIDE_OFF_DELAY = 0.5f; // allows the pick screen time to get off before reseting it
	private const float PICK_SCREEN_MINOR_RESET_WAIT = 0.25f;
	private const float PICK_REVEAL_OTHER_SOUND_DELAY = 0.25f;

	// Animations
	private const string PICK_OBJ_PICKME_ANIM_NAME = "stooges01_FreeSpins_PickingObject_PickMe";
	private const string PICK_OBJ_STILL_ANIM_NAME = "stooges01_FreeSpins_PickingObject_Still";
	private const string PICK_OBJ_REVEAL_ANIM_NAME = "stooges01_FreeSpins_PickingObject_Reveal";
	private const string PICK_UI_INTRO_ANIM_NAME = "stooges01_FreeSpins_PickingObject_BKGReference_Intro";
	private const string PICK_UI_OUTRO_ANIM_NAME = "stooges01_FreeSpins_PickingObject_BKGReference_outro";
	private const string COUNT_BOX_SLIDE_IN_ANIM_NAME = "stooges01_FreeSpins_diamond count box_Intro";
	private const string COUNT_BOX_SLIDE_OUT_ANIM_NAME = "stooges01_FreeSpins_diamond count box_Outro";

	private const float BOX_PICKME_ANIM_LENGTH = 1.0f;

	// Sounds
	private const string FREE_SPIN_INTRO_MUSIC = "IntroFreespinStooges";
	private const string PICK_STAGE_MUSIC = "FreespinStooges";
	private const string SPIN_STAGE_MUSIC = "BoxingGloveReelSpin";
	private const string CURLY_FACE_WIPE_SOUND = "CurlyFaceWipe";
	private const string SHOW_DIAMOND_PICK_UI_SOUND = "TransitionFreespinAtlanta";
	private const string HIDE_DIAMOND_PICK_UI_SOUND = "RingBoxOverlayDrops";
	private const string OBJECT_PICKME_ANIM_SOUND = "ViolinPickMe";
	private const string OBJECT_REVEAL_SOUND = "PickAViolin";
	private const string OBJECT_REVEAL_NUM_WILDS_SOUND = "ViolinRevealNumWilds";
	private const string OBJECT_REVEAL_LAST_SPIN_SOUND = "ViolinRevealLastSpin";
	private const string OBJECT_REVEAL_OTHERS_SOUND_KEY = "reveal_not_chosen";
	private const string FIGHT_BELL_SOUND = "FightBellStooges";
	private const string BOXING_AMBIENCE_NOISE = "CurlyBoxingAmbience";
	private const string PICK_STAGE_START_VO = "FreespinPickVOStooges";		// VO that plays every time you come back to the pick stage, assuming this means don't play it for the first time
	private const string DROP_EFFECT_LAND_SOUND = "BoxingGloveImpactStooges";

	private const float OBJECT_REVEAL_LAST_SPIN_DELAY = 0.75f;
	private const float OBJECT_REVEAL_NUM_WILDS_DELAY = 0.25f;
	private const float CURLY_FACE_WIPE_SOUND_DELAY = 0.5f;

// we will execute on every spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		if(!isFirstPickPhase)
		{
			// we are returning to the pick phase so need to play some audio
			Audio.play(PICK_STAGE_START_VO);
			Audio.switchMusicKeyImmediate(PICK_STAGE_MUSIC);
		}
		else
		{
			// this is the first pick stage so the Free Spin Intro VO and music are already setup to play
			isFirstPickPhase = false;
		}

		Audio.play(SHOW_DIAMOND_PICK_UI_SOUND);
		pickingObjectHolderAnimator.Play(PICK_UI_INTRO_ANIM_NAME);

		yield return StartCoroutine(base.executeOnPreSpin());
	}

	/// Play a pick me animation for one of the choice buttons
	protected override IEnumerator playPickMeAnimation(Animator animator)
	{
		Audio.play(OBJECT_PICKME_ANIM_SOUND);
		animator.Play(PICK_OBJ_PICKME_ANIM_NAME);

		// Wait for the animation duration
		float elapsedTime = 0;
		while (elapsedTime < BOX_PICKME_ANIM_LENGTH && isInputEnabled)
		{
			elapsedTime += Time.deltaTime;
			yield return null;
		}
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		countBoxAnimator.Play(COUNT_BOX_SLIDE_IN_ANIM_NAME);

		reelShroud.SetActive(true);

		Audio.play(CURLY_FACE_WIPE_SOUND, 1.0f, 0.0f, CURLY_FACE_WIPE_SOUND_DELAY);
		
		yield return StartCoroutine(base.executePreReelsStopSpinning());
		
		countBoxAnimator.Play(COUNT_BOX_SLIDE_OUT_ANIM_NAME);

		yield return new TIWaitForSeconds(DROPPING_FINISHED_DELAY);
	}

	/// Cleanup assets from the picking stage
	protected override IEnumerator cleanupPickStage()
	{		
		// let the user see the picks before hiding the object picking screen
		yield return new TIWaitForSeconds(PICK_SCREEN_SHOW_ALL_DELAY);

		Audio.play(HIDE_DIAMOND_PICK_UI_SOUND);
		pickingObjectHolderAnimator.Play(PICK_UI_OUTRO_ANIM_NAME);

		// get the picking screen off before reseting the buttons
		yield return new TIWaitForSeconds(PICK_SCREEN_SLIDE_OFF_DELAY);

		pickGameShroud.SetActive(false);

		// now hide the buttons
		foreach(GameObject button in pickingObjects)
		{			
			button.GetComponentInChildren<PickGameButton>().animator.Play(PICK_OBJ_STILL_ANIM_NAME);
		}

		// minor wait 
		yield return new TIWaitForSeconds(PICK_SCREEN_MINOR_RESET_WAIT);

		yield return StartCoroutine(base.cleanupPickStage());

		Audio.play(FIGHT_BELL_SOUND);
		Audio.play(BOXING_AMBIENCE_NOISE);
		Audio.switchMusicKeyImmediate(SPIN_STAGE_MUSIC);
	}

	/// Handling playing an aniamtion here by overriding
	public override void playPickingObjectReveal(Animator animator, bool isLastSpin, bool isUserPick)
	{
		StartCoroutine(playPickingObjectRevealCoroutine(animator, isLastSpin, isUserPick));
	}

	private IEnumerator playPickingObjectRevealCoroutine(Animator animator, bool isLastSpin, bool isUserPick)
	{
		animator.Play(PICK_OBJ_REVEAL_ANIM_NAME);

		if (isUserPick)
		{
			Audio.play(OBJECT_REVEAL_SOUND);
			Audio.play(OBJECT_REVEAL_NUM_WILDS_SOUND, 1.0f, 0.0f, OBJECT_REVEAL_NUM_WILDS_DELAY);

			if (isLastSpin)
			{
				Audio.play(OBJECT_REVEAL_LAST_SPIN_SOUND, 1.0f, 0.0f, OBJECT_REVEAL_LAST_SPIN_DELAY);
			}
		}
		else
		{
			yield return new TIWaitForSeconds(PICK_REVEAL_OTHER_SOUND_DELAY);
			Audio.play(Audio.soundMap(OBJECT_REVEAL_OTHERS_SOUND_KEY));
		}
	}

	/// Overriding to add in a sound
	protected override IEnumerator placeAndSlideSymbolOff(Vector3 effectLocalPos, int reelIndex)
	{
		Audio.play(DROP_EFFECT_LAND_SOUND);
		yield return StartCoroutine(base.placeAndSlideSymbolOff(effectLocalPos, reelIndex));
	}
}
