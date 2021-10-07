using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Free Spin class for Gen25
 * Clone of Gen06
 */ 
public class Gen25FreeSpins : PickMajorFreeSpins 
{
	// Inspector variables	
	[SerializeField] private float REVEAL_CHARACTER_VO_DELAY;
	[SerializeField] private float SPIN_STAGE_INTRO_VO_DELAY;
	[SerializeField] private float SUMMARY_VO_DELAY;
	[SerializeField] private float MAJOR_SYMBOL_VO_SOUNDS_DELAY;
	[SerializeField] private float WATERFALL_TOP_DELAY;
	[SerializeField] private float WATERFALL_BOTTOM_DELAY;
	
	private const string OBJECT_PICKME_ANIM_NAME = "pickme";
	private const string OBJECT_REVEAL_MAJOR_ANIM_NAME = "reveal_";
	private const string OBJECT_UNPICKED_MAJOR_ANIM_NAME = "reveal_";
	private const string OBJECT_UNPICKED_GRAY_ANIM_NAME = "reveal_unpicked_";
	private const string BANNER_ANIM_NAME = "zynga02_FreeSpins_BannerAnimation_";
	
	private const float OBJECT_PICKME_ANIM_LENGTH = 0.8f;
	private const float OBJECT_REVEAL_MAJOR_ANIM_LENGTH = 0.3f;

	// Sound constants
	private const string PORTAL_BG_KEY = "bonus_portal_bg";									// background sound of canoe pick screen
	private const string OBJECT_PICKED_KEY = "bonus_portal_reveal_bonus";					// sound played when canoe picked
	private const string REVEAL_STINGER = "PickACanoeRevealSymbol";							// Name of the sound played when the big symbol is revealed.
	private const string REVEAL_OTHERS_KEY = "bonus_portal_reveal_others";					// Colection name played when the other symbols are revealed.
	private const string PICK_ME_SOUND_KEY = "bonus_portal_pickme";							// Sound played when the canoe starts to shake for the pick me.
	private const string PORTAL_INTRO_VO_KEY = "bonus_intro_vo";							// Sound name played when the games starts.
	private const string FREESPINS_SUMMARY_VO_KEY = "freespin_summary_vo";					// Sound name played once the summary screen comes up for this game.
	private const string TRANSITION_SOUND_KEY = "bonus_portal_transition_freespins";		// sound played when we go to free spins
	private const string WATERFALL_TOP_SOUND = "WaterfallAmbienceTopJCats";					// looping waterfall sound played during stage 1 of free spins
	private const string WATERFALL_BOTTOM_SOUND = "WaterfallAmbienceBottomFreespinJCats";	// looping waterfall sound played during stage 2 of free spins
	private const string INTRO_FREESPIN_KEY = "freespinintro";								// intro to free spins
	private const string FREESPIN_INTRO_VO_KEY = "freespin_intro_vo";						// intro VO to free spins
	private const string FREESPIN_BG_KEY = "freespin";										// free spin bg music

	public override void initFreespins()
	{
		base.initFreespins();
		Audio.switchMusicKeyImmediate(Audio.soundMap(PORTAL_BG_KEY));
		Audio.play(Audio.soundMap(PORTAL_INTRO_VO_KEY));
		
		// Cache a bunch of major symbols to the pool in a coroutine, that way it doesn't take a performance hit when spinning
		cacheSymbolsToPool(stageTypeAsString(), 26, true);
		BonusGameManager.instance.wings.forceShowPortalWings (true);
	}
	
	// Play a pickme animation
	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available weapon game objects
		int randomObjectIndex = 0;
		
		randomObjectIndex = Random.Range(0, buttonSelections.Count);
		GameObject randomObject = buttonSelections[randomObjectIndex];
		
		PickGameButton gameButton = randomObject.GetComponentInChildren<PickGameButton>();
		
		Audio.play(Audio.soundMap(PICK_ME_SOUND_KEY));
		gameButton.animator.Play(OBJECT_PICKME_ANIM_NAME);
		yield return new TIWaitForSeconds(OBJECT_PICKME_ANIM_LENGTH);
	}
	
	// Handle an object being picked
	protected override IEnumerator showPick(GameObject button)
	{
		PickGameButton gameButton = button.GetComponentInChildren<PickGameButton>();
		Audio.play(REVEAL_STINGER);
		Audio.play(Audio.soundMap(OBJECT_PICKED_KEY), 1, 0, MAJOR_SYMBOL_VO_SOUNDS_DELAY);
		gameButton.animator.Play(OBJECT_REVEAL_MAJOR_ANIM_NAME + stageTypeAsString());
		yield return new TIWaitForSeconds(OBJECT_REVEAL_MAJOR_ANIM_LENGTH);
	}
	
	// Handle showing the unpicked reveals
	protected override IEnumerator showReveal(GameObject button)
	{
		PickGameButton gameButton = button.GetComponentInChildren<PickGameButton>();
		Audio.play(Audio.soundMap(REVEAL_OTHERS_KEY));
		gameButton.animator.Play(OBJECT_UNPICKED_GRAY_ANIM_NAME + PickMajorFreeSpins.convertStage1TypeToString(getNextRandomizedStageType()));
		yield break;
	}

	// Overriding to setup 1st Stage
	protected override void setupStage()
	{
		base.setupStage();

		Audio.play(WATERFALL_TOP_SOUND, 1.0f, 0.0f, WATERFALL_TOP_DELAY, 0.0f);
	}

	// Overriding to handle showing the right banner
	protected override IEnumerator transitionIntoStage2()
	{
		Audio.play(Audio.soundMap(TRANSITION_SOUND_KEY));

		yield return StartCoroutine(base.transitionIntoStage2());
		
		Audio.play(Audio.soundMap(INTRO_FREESPIN_KEY));
		Audio.play(Audio.soundMap(FREESPIN_INTRO_VO_KEY));
		Audio.play(WATERFALL_BOTTOM_SOUND, 1.0f, 0.0f, WATERFALL_BOTTOM_DELAY, 0.0f);
	}

	protected override void beginFreeSpinMusic()
	{
		// play free spin audio and start the spin
		if (!cameFromTransition)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_BG_KEY));
		}
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		Audio.play(Audio.soundMap("prespin_idle_loop"));
		Audio.play(Audio.soundMap(FREESPINS_SUMMARY_VO_KEY), 1.0f, 0.0f, SUMMARY_VO_DELAY);
		base.gameEnded();
	}

	override protected IEnumerator knockerClickedCoroutine(GameObject button) {
		// Reveal the pick that was selected.
		yield return StartCoroutine(showPick(button));
		// Remove the button from the list because we're not using it anymore.
		buttonSelections.Remove(button);
		// Show reveals after 1 second
		yield return new WaitForSeconds(1);
		yield return StartCoroutine(showReveals());
		yield return new WaitForSeconds(TIME_AFTER_REVEALS);
		// Transition into the freespins game
		yield return StartCoroutine(transitionIntoStage2());
	}

}