using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Freespin game where a major symbol is picked before the freespins start
 */ 
public class PickMajorFreeSpinsWithAnimations : PickMajorFreeSpins 
{
	// Inspector variables		
	[SerializeField] private string OBJECT_PICKME_ANIM_NAME = "pickme";
	[SerializeField] private string OBJECT_REVEAL_MAJOR_ANIM_NAME = "reveal_";
	[SerializeField] private string OBJECT_UNPICKED_GRAY_ANIM_NAME = "reveal_unpicked_";
	[SerializeField] private string BANNER_ANIM_NAME = "";									// Prefix for major symbol anbimations in banner.

	[SerializeField] private float OBJECT_REVEAL_DELAY;
	[SerializeField] private float OBJECT_REVEAL_FANFARE_DELAY;
	[SerializeField] private float OBJECT_REVEAL_VO_DELAY;
	[SerializeField] private float FREESPIN_MINIPICK_INTRO_VO_DELAY;

	[SerializeField] private bool useFreeSpinWingsForMiniPick = false;
	[SerializeField] private bool hideWingsInMiniPick = false;
	[SerializeField] private bool shouldActivateTransitionObject = false;
	[SerializeField] private bool shouldDeactivateTransitionObject = false;
	[SerializeField] private bool shouldPlayPrespinIdleLoopOnGameEnd = true;
	[SerializeField] private bool playFreespinsMusic = true;

	[SerializeField] private Animator transitionAnimator;
	[SerializeField] private Animator bannerAnimator;										// Animator for the FS banner. Should play animations that reveal selected major symbol
	[SerializeField] private string transitionAnimationName;
	[SerializeField] private float SECONDS_TO_WAIT_AFTER_TRANSITION_BEGINS;
	[SerializeField] private AnimationListController.AnimationInformationList transitionAnimations;

	// Sound constants
	private const string FREESPIN_MINIPICK_BG_KEY = "freespins_minipick_bg";				// free spin picking bg music
	private const string FREESPIN_MINIPICK_INTRO_VO_KEY = "freespins_minipick_intro_vo";	// free spin picking intro vo
	private const string FREESPIN_MINIPICK_AMBIENCE_KEY = "freespin_minipick_ambience";		// free spin picking ambience sound
	private const string OBJECT_PICKED_KEY = "freespin_minipick_picked";					// sound played when object picked
	private const string OBJECT_REVEAL_KEY = "freespin_minipick_reveal";					// sound played when object revealed
	private const string OBJECT_REVEAL_VO_KEY = "freespin_minipick_reveal_vo";					// sound collection vo played when object revealed
	private const string OBJECT_REVEAL_FANFARE_KEY = "freespin_minipick_reveal_fanfare";	// fanfare sound played when object revealed
	private const string OBJECT_REVEAL_VO_KEY_PREFIX = "freespin_minipick_reveal_m";				// vo sound played when object revealed
	private const string OBJECT_REVEAL_VO_KEY_POSTFIX = "_vo";				// vo sound played when object revealed
	[SerializeField] private string REVEAL_OTHERS_KEY = "bonus_portal_reveal_others";					// sound played while revealing unpicked objects
	private const string PICK_ME_SOUND_KEY = "freespin_minipick_pickme";					// sound played during pickme animation
	private const string INTRO_FREESPIN_KEY = "freespinintro";								// intro to free spins
	private const string FREESPIN_INTRO_VO_KEY = "freespin_intro_vo";						// intro VO to free spins
	private const string FREESPIN_BG_KEY = "freespin";                                      // free spin bg music

	[SerializeField] private bool playFreespinsIntroOnInit = true;	// conditionals for freespins intro audio (gen60 requires only 1 event)
	[SerializeField] private bool playFreespinsIntroOnStage2 = true;

	private bool playRevealSound = true; // If we are skipping the reveal, don't play extra sounds otherwise they overlap.

	public override void initFreespins()
	{
		base.initFreespins();
		SpinPanel.instance.hidePanels();
		Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_MINIPICK_BG_KEY));

		if (playFreespinsIntroOnInit)
		{
			Audio.play(Audio.soundMap(INTRO_FREESPIN_KEY));
		}

		Audio.play(Audio.soundMap(FREESPIN_MINIPICK_INTRO_VO_KEY), 1, 0, FREESPIN_MINIPICK_INTRO_VO_DELAY); // Play minipick intro vo is there is one
		Audio.play(Audio.soundMap(FREESPIN_MINIPICK_AMBIENCE_KEY)); // Play ambience sound if there is one

		// Cache a bunch of major symbols to the pool in a coroutine, that way it doesn't take a performance hit when spinning
		cacheSymbolsToPool(stageTypeAsString(), 26, true);

		if (isShowingWingsForPickStage)
		{
			if (useFreeSpinWingsForMiniPick)
			{
				BonusGameManager.instance.wings.forceShowFreeSpinWings(true);
			}
			else if (hideWingsInMiniPick)
			{
				BonusGameManager.instance.wings.hide();
			}
			else
			{
				BonusGameManager.instance.wings.forceShowPortalWings(true);
			}
		}
	}
	
	// Play a pickme animation
	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available weapon game objects
		int randomObjectIndex = 0;
		
		randomObjectIndex = Random.Range(0, buttonSelections.Count);
		GameObject randomObject = buttonSelections[randomObjectIndex];

		Audio.play(Audio.soundMap(PICK_ME_SOUND_KEY));
		Animator objectAnimator = randomObject.GetComponent<Animator>();
		if (objectAnimator != null)
		{			
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(objectAnimator, OBJECT_PICKME_ANIM_NAME));
		}
	}
	
	// Handle an object being picked
	protected override IEnumerator showPick(GameObject button)
	{
		Audio.play(Audio.soundMap(OBJECT_PICKED_KEY));
		yield return new TIWaitForSeconds(OBJECT_REVEAL_DELAY);
		Audio.play(Audio.soundMap(OBJECT_REVEAL_KEY));
		Audio.play(Audio.soundMap(OBJECT_REVEAL_FANFARE_KEY), 1, 0, OBJECT_REVEAL_FANFARE_DELAY);
		Audio.play(Audio.soundMap(OBJECT_REVEAL_VO_KEY));
		Audio.play(Audio.soundMap(OBJECT_REVEAL_VO_KEY_PREFIX + (stageTypeAsInt() + 1) + OBJECT_REVEAL_VO_KEY_POSTFIX), 1, 0, OBJECT_REVEAL_VO_DELAY);
		Animator buttonAnimator = button.GetComponent<Animator>();
		if (buttonAnimator != null)
		{			
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(buttonAnimator, OBJECT_REVEAL_MAJOR_ANIM_NAME + stageTypeAsString()));
		}
	}
	
	// Handle showing the unpicked reveals
	protected override IEnumerator showReveal(GameObject button)
	{
		if (playRevealSound)
		{
			Audio.play(Audio.soundMap(REVEAL_OTHERS_KEY));
			if (revealWait.isSkipping)
			{
				playRevealSound = false;
			}
		}
		Animator buttonAnimator = button.GetComponent<Animator>();
		if (buttonAnimator != null)
		{			
			buttonAnimator.Play(OBJECT_UNPICKED_GRAY_ANIM_NAME + PickMajorFreeSpins.convertStage1TypeToString(getNextRandomizedStageType()));
		}
		yield break;
	}

	// Overriding to handle showing the right banner
	protected override IEnumerator transitionIntoStage2()
	{
		if (transitionAnimations != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(transitionAnimations));
		}

		if (shouldActivateTransitionObject)
		{
			transitionAnimator.gameObject.SetActive(true);
		}

		if (transitionAnimator != null && !string.IsNullOrEmpty(transitionAnimationName))
		{
			transitionAnimator.Play(transitionAnimationName);
			yield return new TIWaitForSeconds(SECONDS_TO_WAIT_AFTER_TRANSITION_BEGINS);
		}

		if (shouldDeactivateTransitionObject)
		{
			transitionAnimator.gameObject.SetActive(false);
		}

		yield return StartCoroutine(base.transitionIntoStage2());

		// Have the banner Animator play the correct animation to reveal the major symbol the player picked, if the game does it this way
		if (bannerAnimator != null)
		{
			// When a Major Symbol animation finishes, it should automatically transition to FreeSpintText
			bannerAnimator.Play(BANNER_ANIM_NAME + stageTypeAsString());
		}

		if (playFreespinsIntroOnStage2)
		{
			Audio.play(Audio.soundMap(INTRO_FREESPIN_KEY));
		}

		Audio.play(Audio.soundMap(FREESPIN_INTRO_VO_KEY));
	}

	protected override void beginFreeSpinMusic()
	{
		// play free spin audio and start the spin
		if (!cameFromTransition && playFreespinsMusic)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_BG_KEY));
		}
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		if (shouldPlayPrespinIdleLoopOnGameEnd)
		{
			Audio.play(Audio.soundMap("prespin_idle_loop"));
		}
		base.gameEnded();
	}
}