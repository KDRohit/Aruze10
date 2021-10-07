using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Free Spin bonus for Zynga02 CastleVille Legends
 * Uses the PickMajorFreeSpins base class
 */ 
public class Zynga02FreeSpins : PickMajorFreeSpins 
{
	// inspector variables
	[SerializeField] private Animator bannerAnimator = null;

	private const string OBJECT_PICKME_ANIM_NAME = "zynga02_FS_IntroPick_PickingObject_pickMe";
	private const string OBJECT_REVEAL_MAJOR_ANIM_NAME = "reveal_";
	private const string OBJECT_UNPICKED_MAJOR_ANIM_NAME = "unpicked_";
	private const string BANNER_ANIM_NAME = "zynga02_FreeSpins_BannerAnimation_";

	private const string CARD_PICKME_SOUND = "PickMeCard";
	private const string CARD_PICK_REVEAL_SOUND = "PickACardSymbolPick";
	private const string CARD_REVEAL_OTHERS_SOUND_KEY = "reveal_other_choices";
	private const string PICK_A_CARD_BG_MUSIC = "PickACardBg";
	private const string PICK_STAGE_INTRO_VO_SOUND = "PickACardVOCastleville";
	private const string SPIN_STAGE_INTRO_VO_SOUND = "FreespinIntroVOCastleville";
	private const string FREESPIN_BG_MUSIC = "FreespinCastleville";
	private const string REVEAL_CHARACTER_VO_POSTFIX = "Castleville";
	private const string SUMMARY_VO = "FreespinSummaryVOCastleville";

	private const float OBJECT_PICKME_ANIM_LENGTH = 0.9f;
	private const float OBJECT_REVEAL_MAJOR_ANIM_LENGTH = 1.2f;
	private const float REVEAL_CHARACTER_VO_DELAY = 0.8f;
	private const float SPIN_STAGE_INTRO_VO_DELAY = 1.0f;
	private const float SUMMARY_VO_DELAY = 0.6f;

	public override void initFreespins()
	{
		base.initFreespins();
		
		Audio.switchMusicKeyImmediate(PICK_A_CARD_BG_MUSIC);
		Audio.play(PICK_STAGE_INTRO_VO_SOUND);

		// Cache a bunch of major symbols to the pool in a coroutine, that way it doesn't take a performance hit when spinning
		cacheSymbolsToPool(stageTypeAsString(), 26, true);
	}

	/// Play a pickme animation
	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available weapon game objects
		int randomObjectIndex = 0;
		
		randomObjectIndex = Random.Range(0, buttonSelections.Count);
		GameObject randomObject = buttonSelections[randomObjectIndex];

		Zynga02FreeSpinButton gameButton = randomObject.GetComponent<Zynga02FreeSpinButton>();

		Audio.play(CARD_PICKME_SOUND);
		gameButton.animator.Play(OBJECT_PICKME_ANIM_NAME);
		yield return new TIWaitForSeconds(OBJECT_PICKME_ANIM_LENGTH);
	}

	/// Handle an object being picked
	protected override IEnumerator showPick(GameObject button)
	{
		Zynga02FreeSpinButton gameButton = button.GetComponent<Zynga02FreeSpinButton>();
		Audio.play(CARD_PICK_REVEAL_SOUND);
		Audio.play(stageTypeAsString() + REVEAL_CHARACTER_VO_POSTFIX, 1, 0, REVEAL_CHARACTER_VO_DELAY);
		gameButton.animator.Play(OBJECT_REVEAL_MAJOR_ANIM_NAME + stageTypeAsString());
		yield return new TIWaitForSeconds(OBJECT_REVEAL_MAJOR_ANIM_LENGTH);
	}

	/// Handle showing the unpicked reveals
	protected override IEnumerator showReveal(GameObject button)
	{
		Zynga02FreeSpinButton gameButton = button.GetComponent<Zynga02FreeSpinButton>();
		gameButton.greyOutMajorSymbols();
		Audio.play(Audio.soundMap(CARD_REVEAL_OTHERS_SOUND_KEY));
		gameButton.animator.Play(OBJECT_UNPICKED_MAJOR_ANIM_NAME + PickMajorFreeSpins.convertStage1TypeToString(getNextRandomizedStageType()));
		yield break;
	}

	/// Overriding to handle showing the right banner
	protected override IEnumerator transitionIntoStage2()
	{
		yield return StartCoroutine(base.transitionIntoStage2());

		bannerAnimator.Play(BANNER_ANIM_NAME + stageTypeAsString());

		Audio.play(SPIN_STAGE_INTRO_VO_SOUND, 1, 0, SPIN_STAGE_INTRO_VO_DELAY);
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		Audio.play(Audio.soundMap("prespin_idle_loop"));
		// Play the summary VO .6 seconds after the game has ended.
		Audio.play(SUMMARY_VO, 1.0f, 0.0f, SUMMARY_VO_DELAY);
		base.gameEnded();
	}
}
