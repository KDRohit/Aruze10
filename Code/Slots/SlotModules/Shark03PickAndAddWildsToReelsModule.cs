using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module that implments the moon01 feature where a pick UI determines a number of wilds that will be apllied to the reels
*/
public class Shark03PickAndAddWildsToReelsModule : PickAndAddWildsToReelsModule 
{
	[SerializeField] private Animator pickingObjectHolderAnimator;
	[SerializeField] private Animator countBoxAnimator;

	private bool isFirstPickPhase = true;

	private const string PICK_OBJ_PICKME_ANIM_NAME = "pickme";
	private const float BOX_PICKME_ANIM_LENGTH = 1.0f;
	private const string PICK_OBJ_STILL_ANIM_NAME = "still";
	private const string PICK_OBJ_REVEAL_ANIM_NAME = "reveal";
	private const string PICK_OBJ_REVEAL_GRAY_ANIM_NAME = "revealGray";

	private const string PICK_UI_INTRO_ANIM_NAME = "intro";
	private const string PICK_UI_OUTRO_ANIM_NAME = "outro";
	
	private const string COUNT_BOX_SLIDE_IN_ANIM_NAME = "intro";
	private const string COUNT_BOX_SLIDE_OUT_ANIM_NAME = "outro";

	// Sounds
	private const string FREE_SPIN_INTRO_MUSIC = "IntroFreespinShark3";
	private const string PICK_STAGE_MUSIC = "FreespinPickShark3";
	private const string SPIN_STAGE_MUSIC = "FreespinSpinShark3";

	private const string SHOW_DIAMOND_PICK_UI_SOUND = "SharkPickOverlayRises";
	private const string HIDE_DIAMOND_PICK_UI_SOUND = "SharkPickOverlayDropsShark3";
	
	private const string DIAMOND_WILDS_COUNTER_SOUND = "SharkRainWindAmbience"; // needs to be looped then canceled when the counter is done

	private const string OBJECT_PICKME_ANIM_SOUND = "SharknadoPickMeShark3";
	private const string OBJECT_REVEAL_SOUND = "PickASharknadoShark3";
	private const string OBJECT_REVEAL_LAST_SPIN_SOUND = "SharknadoRevealLastSpinShark3";
	private const float OBJECT_REVEAL_LAST_SPIN_DELAY = 0.75f;
	private const string OBJECT_REVEAL_OTHERS_SOUND_KEY = "reveal_not_chosen";

	private const string POST_PICK_VO = "RainingShark3VO";

	private const string PICK_STAGE_START_VO = "FreespinIntroVOShark3";		// VO that plays every time you come back to the pick stage, assuming this means don't play it for the first time

	private const string DROP_EFFECT_LAND_SOUND = "ScatterWildLandsShark3";

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

		
		PlayingAudio diamondWildsCounterSound = Audio.play(DIAMOND_WILDS_COUNTER_SOUND);
		
		yield return StartCoroutine(base.executePreReelsStopSpinning());

		if (diamondWildsCounterSound != null)
		{
			diamondWildsCounterSound.stop();
		}
		countBoxAnimator.Play(COUNT_BOX_SLIDE_OUT_ANIM_NAME);

		yield return new TIWaitForSeconds(1.6f); // how long until the last diamond drop finishes

	}

	/// Cleanup assets from the picking stage
	protected override IEnumerator cleanupPickStage()
	{		
		// let the user see the picks before hiding the object picking screen
		yield return new TIWaitForSeconds(0.5f);

		Audio.play(HIDE_DIAMOND_PICK_UI_SOUND);
		pickingObjectHolderAnimator.Play(PICK_UI_OUTRO_ANIM_NAME);

		// get the picking screen off before reseting the buttons
		yield return new TIWaitForSeconds(0.5f);

		pickGameShroud.SetActive(false);

		// now hide the buttons
		foreach(GameObject button in pickingObjects)
		{			
			button.GetComponentInChildren<PickGameButton>().animator.Play(PICK_OBJ_STILL_ANIM_NAME);
		}

		// minor wait 
		yield return new TIWaitForSeconds(0.25f);

		yield return StartCoroutine(base.cleanupPickStage());

		Audio.play(POST_PICK_VO);
		Audio.switchMusicKeyImmediate(SPIN_STAGE_MUSIC);
	}

	/// Handling playing an aniamtion here by overriding
	public override void playPickingObjectReveal(Animator animator, bool isLastSpin, bool isUserPick)
	{
		StartCoroutine(playPickingObjectRevealCoroutine(animator, isLastSpin, isUserPick));
	}

	private IEnumerator playPickingObjectRevealCoroutine(Animator animator, bool isLastSpin, bool isUserPick)
	{

		if (isUserPick)
		{
			animator.Play(PICK_OBJ_REVEAL_ANIM_NAME);
			Audio.play(OBJECT_REVEAL_SOUND);

			if (isLastSpin)
			{
				Audio.play(OBJECT_REVEAL_LAST_SPIN_SOUND, 1.0f, 0.0f, OBJECT_REVEAL_LAST_SPIN_DELAY);
			}
		}
		else
		{
			animator.Play(PICK_OBJ_REVEAL_GRAY_ANIM_NAME);
			Audio.play(Audio.soundMap(OBJECT_REVEAL_OTHERS_SOUND_KEY));
			yield return new TIWaitForSeconds(0.25f);
		}
	}

	/// Overriding to add in a sound
	protected override IEnumerator placeAndSlideSymbolOff(Vector3 effectLocalPos, int reelIndex)
	{
		Audio.play(DROP_EFFECT_LAND_SOUND);
		yield return StartCoroutine(base.placeAndSlideSymbolOff(effectLocalPos, reelIndex));
	}
}
