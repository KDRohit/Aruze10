using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickPortal : ChallengeGame 
{
	[SerializeField] protected List<PickGameButton> pickButtons = new List<PickGameButton>(); 	// Animators for the books, used to
	[SerializeField] protected float minPickMeTime = 1.5f;			// Minimum time an animation might take to play next
	[SerializeField] protected float maxPickMeTime = 2.5f;			// Maximum time an animation might take to play next
	[SerializeField] protected UILabel[] bottomTextLabel;
	[SerializeField] protected bool hasCreditBonus = true;			// Tells if this portal includes a credit bonus
	[SerializeField] protected bool hasPickMajorFreespins = false;
	[SerializeField] protected int numberOfMajorSymbols = 4;		// Used when hasPickMajorFreespins == true
	[SerializeField] protected BonusGameDataTypeEnum bonusOutcomeType = BonusGameDataTypeEnum.WheelOutcome; // Tells what type of data the bonus game outcome should be read as
	[SerializeField] protected GameObject portalPicksBackdrop;		//Game Object surrounding the picks
	[SerializeField] protected Animator portalPicksBackdropTextAnimator;		//Game Object surrounding the picks
	[SerializeField] protected GameObject[] objectsToHideOnPick;
	[SerializeField] protected GameObject[] objectsToShowOnPick;
	[SerializeField] protected Animator preBackgroundAnimator; // If you want to play animations before the backgroundAnimator anims.
	[SerializeField] protected Animator backgroundAnimator;
	[SerializeField] protected AnimationListController.AnimationInformationList portalIntroAnimList; // list of portal intro animations/sounds
	[SerializeField] protected GameObject portalTransitionParent;
	[SerializeField] protected bool shouldActivateTransitionParent = false;
	[SerializeField] protected Animator portalTransitionAnimator; // Use this if they gave you one animator for free spins and picking game transitions.
	[SerializeField] protected Animator freeSpinsTransitionAnimator;   // Use this if they gave you a separate animator for free spins.
	[SerializeField] protected Animator pickingGameTransitionAnimator; // Use this if they gave you a separate animator for the picking game.
	[SerializeField] protected bool shouldContinueTransitionAfterPortalIsDestroyed = false;
	[SerializeField] protected GameObject[] objectsToHideForTransition;	// List of objects to hide during the transition animation, probably at least the buttons
	[SerializeField] protected bool fadeBasegameMusicImmediately = false;
	[SerializeField] protected bool playOutroSoundOnce = false;
	[SerializeField] protected bool hideObjectsBeforeOutroTransition = false;
	[SerializeField] protected bool transitionIsUsingIntroMusic = false;

	protected bool isInputEnabled = false;
	protected SlotOutcome bonusOutcome = null;
	protected PortalTypeEnum portalType = PortalTypeEnum.NONE;
	protected List<PortalTypeEnum> randomPortalTypeList = new List<PortalTypeEnum>();
	protected List<PortalTypeEnum> revealedPortalTypeList = new List<PortalTypeEnum>(); // We need to track the order things were revealed in so we know what type each reveal was
	protected CoroutineRepeater pickMeController;										// Class to call the pickme animation on a loop

	protected enum PortalTypeEnum
	{
		NONE 		= -1,
		PICKING 	= 0,
		FREESPINS 	= 1,
		CREDITS 	= 2
	}

	// When adding game types to this enum always add to the end of the list!
	protected enum BonusGameDataTypeEnum
	{
		WheelOutcome 					= 0,
		PickemOutcome 					= 1,
		NewBaseBonusOutcome 			= 2,
		WheelOutcomeWithChildOutcomes 	= 3,
		CrosswordOutcome 				= 4	// This is a special crossword outcome used for zynga04 and any related games.
	}

	[SerializeField] protected string BACKGROUND_ANIM_INTRO_NAME = "";
	[SerializeField] protected string PORTAL_PICKS_INTRO_ANIM_NAME = "";
	[SerializeField] protected string PORTAL_INTRO_TRANSITION_ANIM = "";
	[SerializeField] protected string PORTAL_OUTRO_TRANSITION_ANIM = "";
	[SerializeField] protected bool gameSpecificPortalOutro = false; // Is the outro transition different between freespins / challenge?
	[SerializeField] protected string PORTAL_OUTRO_FREESPINS_TRANSITION_ANIM = "";
	[SerializeField] protected string PORTAL_OUTRO_FREESPINS_TRANSITION_SOUND = "";
	[SerializeField] protected string PORTAL_OUTRO_CHALLENGE_TRANSITION_ANIM = "";
	[SerializeField] protected string PORTAL_OUTRO_CHALLENGE_TRANSITION_SOUND = "";
	[SerializeField] protected bool	switchUpcomingWingsDuringOutro = false; // if true, swap the freespins or challenge wings during the outro
	[SerializeField] protected float waitBeforeSwitchingToFreespinsWings = 0.0f; // In the case you need the wings to swap half way through the outro anim this will allow you to time it
	[SerializeField] protected float waitBeforeSwitchingToChallengeWings = 0.0f; 
	[SerializeField] protected bool	hideUpcomingWingsDuringOutroFreespins = false; // if true, hide the upcoming wings during the outro for FS
	[SerializeField] protected bool	hideUpcomingWingsDuringOutroChallenge = false; // if true, hide the upcoming wings during the outro for challenge
	[SerializeField] protected string PORTAL_OUTRO_CREDIT_TRANSITION_ANIM = "";
	[SerializeField] protected string PORTAL_OUTRO_CREDIT_TRANSITION_SOUND = "";
	[SerializeField] protected string REVEAL_PICKING_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_FREESPINS_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_PICKING_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FREESPINS_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_CREDITS_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_GRAY_PICKING_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_GRAY_FREESPINS_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_GRAY_CREDITS_ANIM_NAME = "";
	[SerializeField] protected string PICKME_ANIM_NAME = "";
	[SerializeField] protected bool PLAY_RANDOM_PICKME_ANIM = false;	// if more than one pickme anim exists, choose at random
	[SerializeField] protected string[] RANDOM_PICKME_ANIM_NAMES;
	[SerializeField] protected string PRE_REVEAL_PICK_ANIM_NAME = "";
	[SerializeField] protected string CHALLENGE_OUTCOME_NAME = "";
	[SerializeField] protected string FREESPIN_OUTCOME_NAME = "";
	[SerializeField] protected string CREDIT_OUTCOME_NAME = "";
	[SerializeField] protected string FREESPIN_PORTAL_OVERRIDE_NAME = "";
	[SerializeField] protected string CHALLENGE_LOCALIZATION_KEY = "";
	[SerializeField] protected string FREESPIN_LOCALIZATION_KEY = "";
	[SerializeField] protected string PRE_REVEAL_PICKING_BACKGROUND_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_PICKING_BACKGROUND_ANIM_NAME = "";
	[SerializeField] protected string PRE_REVEAL_FREESPINS_BACKGROUND_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_FREESPINS_BACKGROUND_ANIM_NAME = "";
	[SerializeField] protected string PORTAL_PICKS_BACKDROP_EXIT_ANIM_NAME = "";
	[SerializeField] protected string PORTAL_PICKS_BACKDROP_TEXT_EXIT_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_PICKED_PICKING_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_UNPICKED_PICKING_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_PICKED_FREESPINS_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_UNPICKED_FREESPINS_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_PICKED_CREDITS_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_UNPICKED_CREDITS_ANIM_NAME = "";

	[SerializeField] protected float BACKGROUND_ANIM_INTRO_WAIT = 0.0f;
	[SerializeField] protected float PRE_INTRO_TRANSITION_WAIT = 0.0f;
	[SerializeField] protected float POST_INTRO_TRANSITION_WAIT = 0.0f;
	[SerializeField] protected float POST_INTRO_VO_WAIT = 0.0f; // how long to wait before setting _didInit to true after everything happens (for long VOs)
	[SerializeField] protected float PRE_OUTRO_TRANSITION_WAIT = 0.0f;
	[SerializeField] protected float PRE_PICKME_ANIM_WAIT = 0.0f;
	[SerializeField] protected float WAIT_BEFORE_ENDING_PORTAL = 0.0f;
	[SerializeField] protected float WAIT_BEFORE_ENDING_PORTAL_FREESPIN = 0.0f;
	[SerializeField] protected float WAIT_BEFORE_ENDING_PORTAL_CHALLENGE = 0.0f;
	[SerializeField] protected float WAIT_BEFORE_ENDING_PORTAL_CREDIT = 0.0f;

	[SerializeField] protected float REVEAL_PICKING_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float REVEAL_FREESPINS_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float REVEAL_CREDITS_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float REVEAL_NOT_SELECTED_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float PICKME_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float REVEAL_BONUS_VO_SOUND_KEY_DELAY = 0.0f;
	[SerializeField] protected float REVEAL_OTHERS_SOUND_KEY_DELAY = 0.0f;
	[SerializeField] protected float PORTAL_VO_SOUND_KEY_DELAY = 0.0f;
	[SerializeField] protected float POST_REVEAL_PICKING_SOUND_DELAY = 0.0f;
	[SerializeField] protected float POST_REVEAL_FREESPINS_SOUND_DELAY = 0.0f;
	[SerializeField] protected float POST_TRANSITION_ANIMATION_WAIT = 0.0f;
	[SerializeField] protected float REVEAL_FREESPINS_SOUND_KEY_DELAY = 0.0f;
	[SerializeField] protected float REVEAL_PICKING_SOUND_KEY_DELAY = 0.0f;
	[SerializeField] protected float BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY_DELAY = 0.0f;
	[SerializeField] protected float SECONDS_TO_WAIT_FOR_SPLIT_TRANSITION = 0.0f; // Use this if it's a shared transition animation.
	[SerializeField] protected float WAIT_FOR_SPLIT_FREE_SPINS_TRANSITION = -1.0f; // Use this if free spins has its own transition anim.
	[SerializeField] protected float WAIT_FOR_SPLIT_PICKING_GAME_TRANSITION = -1.0f; // Use this if the picking game has its own transition anim.
	[SerializeField] protected float WAIT_BEFORE_DESTROYING_TRANSITION_OBJECT = 0.0f;
	[SerializeField] protected float WAIT_BEFORE_DESTROYING_FREESPIN_TRANSITION_OBJECT = 0.0f;
	[SerializeField] protected float WAIT_BEFORE_DESTROYING_CHALLENGE_TRANSITION_OBJECT = 0.0f;
	[SerializeField] protected float WAIT_BEFORE_DESTROYING_CREDIT_TRANSITION_OBJECT = 0.0f;
	[SerializeField] protected float PLAY_FADE_ANIMATIONS_DELAY = 0.0f;							// Delay before fading out the objects so that the player can see them


	[SerializeField] protected bool shouldPlayFreeSpinIntroSound = false;
	protected const string FREESPIN_INTRO_SOUND_KEY = "freespinintro";

	[SerializeField] protected bool shouldQueuePortalBgMusic = false;
	[SerializeField] protected bool shouldPlayPortalTransitionSound = false;
	//This is for games like LIS01 where we have a long sound that plays during the background animation and we want to hold off the portal bg music 
	[SerializeField] protected bool playBGMusicAfterBackgroundAnimation = false;
	//This is used for the transition animation from the basegame but not the portals background animation 
	[SerializeField] protected float PORTAL_BG_MUSIC_DELAY_AFTER_TRANSITION = 0.0f;
	protected const string TRANSITION_TO_PORTAL_SOUND_KEY = "bonus_portal_transition_to_portal";
	[SerializeField] protected float TRANSITION_TO_PORTAL_SOUND_DELAY = 0.0f;
	[SerializeField] bool isPlayingTransitionToPortalSoundAsMusic = true;

	protected const string PORTAL_VO_SOUND_KEY = "bonus_portal_vo";
	protected const string BONUS_PORTAL_INTRO_ANIM_SOUND_KEY = "bonus_portal_intro_anim";
	protected const string PORTAL_PICKME_SOUND_KEY = "bonus_portal_pickme";
	[SerializeField] protected string REVEAL_BONUS_VO_SOUND_KEY = "bonus_portal_reveal_bonus_vo";
	protected const string REVEAL_PICK_SOUND_KEY = "bonus_portal_reveal_bonus";
	protected const string BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY = "bonus_portal_pick_animation";
	protected const string REVEAL_PICK_FREESPINS_SOUND_KEY = "bonus_portal_reveal_freespins";
	protected const string REVEAL_PICK_PICKING_SOUND_KEY = "bonus_portal_reveal_picking";
	protected const string REVEAL_PICK_CREDITS_SOUND_KEY = "bonus_portal_reveal_credits";
	protected const string REVEAL_OTHERS_SOUND_KEY = "bonus_portal_reveal_others";
	protected const string REVEAL_OTHERS_FREESPINS_SOUND_KEY = "bonus_portal_reveal_others_freespins";
	protected const string REVEAL_OTHERS_PICKING_SOUND_KEY = "bonus_portal_reveal_others_picking";
	protected const string REVEAL_OTHERS_CREDITS_SOUND_KEY = "bonus_portal_reveal_others_credits";
	protected const string BONUS_PORTAL_BG_MUSIC_KEY = "bonus_portal_bg";
	protected const string POST_REVEAL_PICKING_SOUND_KEY = "bonus_portal_pickme_post_reveal";
	protected const string POST_REVEAL_FREESPINS_SOUND_KEY = "bonus_portal_freespins_post_reveal";
	[SerializeField] protected string PORTAL_TRANSITION_PICKING_SOUND_KEY = "bonus_portal_transition_picking"; // Sometimes web has this as transition_welcome
	[SerializeField] protected float PORTAL_TRANSITION_PICKING_SOUND_DELAY = 0.0f;	
	[SerializeField] protected string PORTAL_TRANSITION_FREESPINS_SOUND_KEY = "bonus_portal_transition_freespins";
	[SerializeField] protected float PORTAL_TRANSITION_FREESPINS_SOUND_DELAY = 0.0f;	
	[SerializeField] protected string PORTAL_BACKDROP_SOUND_KEY = "bonus_portal_backdrop";
	[SerializeField] protected string PORTAL_END_FREESPINS_SOUND_KEY = "bonus_portal_end_outcome_freespins";
	[SerializeField] protected float PORTAL_END_FREESPINS_SOUND_DELAY = 0.0f;
	[SerializeField] protected string PORTAL_END_PICKING_SOUND_KEY = "bonus_portal_end_outcome_picking";
	[SerializeField] protected float PORTAL_END_PICKING_SOUND_DELAY = 0.0f;
	[SerializeField] protected string BONUS_PORTAL_END_PICKING_BG_MUSIC_KEY = "bonus_portal_end_picking_bg";
	[SerializeField] protected float BONUS_PORTAL_END_PICKING_BG_MUSIC_KEY_DELAY = 0.0f;
	[SerializeField] protected string BONUS_PORTAL_END_FREESPINS_BG_MUSIC_KEY = "bonus_portal_end_freespins_bg";
	[SerializeField] protected float BONUS_PORTAL_END_FREESPINS_BG_MUSIC_KEY_DELAY = 0.0f;
	
	protected const string CHALLENGE_REVEAL_VO_KEY = "portal_reveal_pick_bonus_vo";
	protected const string FREESPIN_REVEAL_VO_KEY = "portal_reveal_freespin_vo";

	[SerializeField] protected string portalOutroSound;
	[SerializeField] protected string freeSpinsOutroSound;
	[SerializeField] protected string pickingGameOutroSound;
	[SerializeField] protected float PORTAL_OUTRO_SOUND_DELAY = 0.0f;
	[SerializeField] protected string PORTAL_OUTRO_VO;
	[SerializeField] protected float PORTAL_OUTRO_VO_DELAY = 0.0f;


	[SerializeField] protected AudioListController.AudioInformationList pickIntroSounds;

	/// Init game specific stuff
	public override void init()
	{
		setPseudoConstants();

		if (shouldPlayPortalTransitionSound)
		{
			if (isPlayingTransitionToPortalSoundAsMusic)
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(TRANSITION_TO_PORTAL_SOUND_KEY));
			}
			else
			{
				Audio.playWithDelay(Audio.soundMap(TRANSITION_TO_PORTAL_SOUND_KEY), TRANSITION_TO_PORTAL_SOUND_DELAY);
			}
		}
		else
		{
			if (shouldQueuePortalBgMusic)
			{
				Audio.switchMusicKey(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));
			}
			else
			if (fadeBasegameMusicImmediately)
			{
				Audio.stopMusic(0.0f);
				Audio.switchMusicKey(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));
			}
			else
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));
			}

			if (Audio.canSoundBeMapped(PORTAL_VO_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(PORTAL_VO_SOUND_KEY), 1, 0, PORTAL_VO_SOUND_KEY_DELAY);
			}	
		}

		// Randomize a list of portal type objects that will reveal
		randomPortalTypeList.Add(PortalTypeEnum.PICKING);
		randomPortalTypeList.Add(PortalTypeEnum.FREESPINS);

		if (hasCreditBonus)
		{
			randomPortalTypeList.Add(PortalTypeEnum.CREDITS);
		}

		// Add in any extra "filler" reveals based on the length of the pickButtons list and the entrys already in the randomPortalTypeList
		// This is in case there are more than 2 buttons (or 3 with Credits) that need to play reveals 
		// Example of this is Zynga04 - Words with friends
		List<PortalTypeEnum> gamePortalTypes = new List<PortalTypeEnum>(randomPortalTypeList);
		int remainingPicks = System.Math.Abs(pickButtons.Count - randomPortalTypeList.Count);
		for (int i = 0; i < remainingPicks; ++i)
		{
			PortalTypeEnum randomPortalType = gamePortalTypes[Random.Range(0, gamePortalTypes.Count)];
			randomPortalTypeList.Add(randomPortalType);
		}

		CommonDataStructures.shuffleList<PortalTypeEnum>(randomPortalTypeList);

		if (SlotBaseGame.instance.bonusGameOutcomeOverride != null)
		{
			bonusOutcome = SlotBaseGame.instance.bonusGameOutcomeOverride;
			SlotBaseGame.instance.bonusGameOutcomeOverride = null;
		}
		else
		{
			bonusOutcome = SlotBaseGame.instance.getCurrentOutcome();
		}
		// cancel being a portal now that we're in it
		bonusOutcome.isPortal = false;

		string formattedChallengeOutcomeName = CHALLENGE_OUTCOME_NAME;
		if (GameState.game != null)
		{
			formattedChallengeOutcomeName = string.Format(CHALLENGE_OUTCOME_NAME, GameState.game.keyName);
		}

		SlotOutcome challenge_outcome = bonusOutcome.getBonusGameOutcome(formattedChallengeOutcomeName);
		if (challenge_outcome != null)
		{
			bonusOutcome.isChallenge = true;
			BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = createChallengeBonusData(challenge_outcome);
			portalType = PortalTypeEnum.PICKING;
		}

		if (CREDIT_OUTCOME_NAME != "" && hasCreditBonus)
		{
			string formattedCreditsOutcomeName = CREDIT_OUTCOME_NAME;
			if (GameState.game != null)
			{
				formattedCreditsOutcomeName = string.Format(CREDIT_OUTCOME_NAME, GameState.game.keyName);
			}

			SlotOutcome credit_outcome = bonusOutcome.getBonusGameOutcome(formattedCreditsOutcomeName);
			if (credit_outcome != null)
			{
				bonusOutcome.isCredit = true;
				WheelOutcome creditOutcome = new WheelOutcome(credit_outcome);
				bonusOutcome.winAmount = creditOutcome.getNextEntry().credits;
				BonusGamePresenter.instance.currentPayout = SlotBaseGame.instance.getCreditBonusValue();
				portalType = PortalTypeEnum.CREDITS;
			}
		}

		SlotOutcome freespinOutcome = getFreeSpinOutcome();
		if (freespinOutcome != null)
		{
			bonusOutcome.isGifting = true;
			BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(freespinOutcome);
			portalType = PortalTypeEnum.FREESPINS;

			if (FREESPIN_PORTAL_OVERRIDE_NAME != "")
			{
				BonusGame thisBonusGame = BonusGame.find(FREESPIN_PORTAL_OVERRIDE_NAME);
				BonusGameManager.instance.summaryScreenGameName = FREESPIN_PORTAL_OVERRIDE_NAME;
				BonusGameManager.instance.isGiftable = thisBonusGame.gift;
			}
		}

		if (!wingsIncludedInBackground)
		{
			BonusGameManager.instance.wings.forceShowPortalWings(true);
		}

		if (portalType == PortalTypeEnum.NONE)
		{
			// Encountered a major error, just going to exit this portal
			Debug.LogError("Portal Data wasn't in expected format, terminating the portal!");
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			randomPortalTypeList.Remove(portalType);
			StartCoroutine(waitThenSetDidInit());
		}
		
		if (bonusOutcome.isGifting)
		{
			if (string.IsNullOrEmpty(portalOutroSound) && !string.IsNullOrEmpty(freeSpinsOutroSound))
			{
				portalOutroSound = freeSpinsOutroSound;
			}
			
			if (freeSpinsTransitionAnimator != null)
			{
				portalTransitionAnimator = freeSpinsTransitionAnimator;
			}
			
			if (WAIT_FOR_SPLIT_FREE_SPINS_TRANSITION != -1.0f)
			{
				SECONDS_TO_WAIT_FOR_SPLIT_TRANSITION = WAIT_FOR_SPLIT_FREE_SPINS_TRANSITION;
			}
		}
		else if (bonusOutcome.isChallenge)
		{
			if (string.IsNullOrEmpty(portalOutroSound) && !string.IsNullOrEmpty(pickingGameOutroSound))
			{
				portalOutroSound = pickingGameOutroSound;
			}
			
			if (pickingGameTransitionAnimator != null)
			{
				portalTransitionAnimator = pickingGameTransitionAnimator;
			}
			
			if (WAIT_FOR_SPLIT_PICKING_GAME_TRANSITION != -1.0f)
			{
				SECONDS_TO_WAIT_FOR_SPLIT_TRANSITION = WAIT_FOR_SPLIT_PICKING_GAME_TRANSITION;
			}	
		}
	}

	private IEnumerator waitThenSetDidInit()
	{
		if (shouldPlayPortalTransitionSound)
		{
			//We may not want to start the bg music here but instead later after the background animation has finished
			if (!playBGMusicAfterBackgroundAnimation)
			{
				//delaying bg music if there is a transition in the portal prefab that is being played
				yield return new TIWaitForSeconds(PORTAL_BG_MUSIC_DELAY_AFTER_TRANSITION);
				if (fadeBasegameMusicImmediately)
				{
					Audio.stopMusic(0.0f);
					Audio.switchMusicKey(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));
				}
				else
				{
					Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));
				}
			}

			if (Audio.canSoundBeMapped(PORTAL_VO_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(PORTAL_VO_SOUND_KEY), 1, 0, PORTAL_VO_SOUND_KEY_DELAY);
			}
		}

		if(portalPicksBackdrop != null)
		{
			yield return new TIWaitForSeconds(2.0f);
			portalPicksBackdrop.SetActive(true);
		}

		if (Audio.canSoundBeMapped(PORTAL_BACKDROP_SOUND_KEY))
		{
			Audio.play(Audio.soundMap(PORTAL_BACKDROP_SOUND_KEY));
		}

		if (portalTransitionAnimator != null && !string.IsNullOrEmpty(PORTAL_INTRO_TRANSITION_ANIM))
		{
			yield return new TIWaitForSeconds(PRE_INTRO_TRANSITION_WAIT);
			portalTransitionAnimator.Play(PORTAL_INTRO_TRANSITION_ANIM);
			// Pick the larger of the two wait times
			yield return new TIWaitForSeconds(POST_INTRO_VO_WAIT > POST_INTRO_TRANSITION_WAIT ? POST_INTRO_VO_WAIT : POST_INTRO_TRANSITION_WAIT);
		}
		else if (!Audio.muteSound)
		{
			if (POST_INTRO_VO_WAIT > 0.0f)
			{
				yield return new TIWaitForSeconds(POST_INTRO_VO_WAIT);
			}
		}

		bool isIntroAnimPlayed = false;
		if (backgroundAnimator != null && !string.IsNullOrEmpty(BACKGROUND_ANIM_INTRO_NAME))
		{
			if (Audio.canSoundBeMapped(BONUS_PORTAL_INTRO_ANIM_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(BONUS_PORTAL_INTRO_ANIM_SOUND_KEY));
			}
			backgroundAnimator.Play(BACKGROUND_ANIM_INTRO_NAME);
			yield return new TIWaitForSeconds(BACKGROUND_ANIM_INTRO_WAIT);
			isIntroAnimPlayed = true;
		}

		if (portalIntroAnimList != null && portalIntroAnimList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(portalIntroAnimList));
			isIntroAnimPlayed = true;
		}

		if (isIntroAnimPlayed)
		{
			//Once the background animation is finished turn on the bg music (first used in LIS01)
			if (playBGMusicAfterBackgroundAnimation)
			{
				if (fadeBasegameMusicImmediately)
				{
					Audio.stopMusic(0.0f);
					Audio.switchMusicKey(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));
				}
				else
				{
					Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));
				}
			}
		}

		if (!string.IsNullOrEmpty(PORTAL_PICKS_INTRO_ANIM_NAME))
		{
			foreach(PickGameButton button in pickButtons)
			{
				button.GetComponent<Animator>().Play(PORTAL_PICKS_INTRO_ANIM_NAME);
			}
		}

		if (pickIntroSounds != null)
		{
			StartCoroutine(AudioListController.playListOfAudioInformation(pickIntroSounds));
		}

		if (PRE_PICKME_ANIM_WAIT > 0.0f)
		{
			yield return new TIWaitForSeconds(PRE_PICKME_ANIM_WAIT);
		}

		foreach(PickGameButton button in pickButtons)
		{
			button.animator.enabled = true;
		}
		pickMeController = new CoroutineRepeater(minPickMeTime, maxPickMeTime, pickMeAnimCallback);

		_didInit = true;
		isInputEnabled = true;
	}

	protected virtual void setPseudoConstants()
	{

	}

	protected override void Update()
	{
		base.Update();

		if (isInputEnabled && _didInit)
		{
			pickMeController.update();
		}
	}

	/// Pick me animation player
	protected IEnumerator pickMeAnimCallback()
	{
		int randomButtonIndex = Random.Range(0, pickButtons.Count);
		Animator scrollAnimator = pickButtons[randomButtonIndex].animator;

		Audio.play(Audio.soundMap(PORTAL_PICKME_SOUND_KEY));

		// if set to play random animations, choose one, check, and assign.
		if (PLAY_RANDOM_PICKME_ANIM)
		{
			string randomAnim = RANDOM_PICKME_ANIM_NAMES[Random.Range(0, RANDOM_PICKME_ANIM_NAMES.Length)];
			if (!string.IsNullOrEmpty(randomAnim))
			{
				PICKME_ANIM_NAME = randomAnim;
			}
		}

		scrollAnimator.Play(PICKME_ANIM_NAME);
		yield return new TIWaitForSeconds(PICKME_ANIM_LENGTH);
	}

	protected void showObjects()
	{
		foreach(var o in objectsToShowOnPick)
		{
			o.SetActive(true);
		}
	}

	protected void hideObjects()
	{
		foreach(var o in objectsToHideOnPick)
		{
			o.SetActive(false);
		}
	}

	/// Triggered by UI Button Message when a book is clicked
	public void choiceSelected(GameObject obj)
	{
		if (isInputEnabled)
		{
			isInputEnabled = false;
			showObjects();
			hideObjects();
			StartCoroutine(choiceSelectedCoroutine(obj));
		}
	}

	protected virtual IEnumerator gameSpecificSelectedCoroutine(PickGameButton button)
	{
		yield break;
	}

	protected virtual void setCreditText(PickGameButton button, bool isPick)
	{

	}

	/// Coroutine to handle timing stuff once a book is picked
	public IEnumerator choiceSelectedCoroutine(GameObject obj)
	{
		PickGameButton button = obj.GetComponent<PickGameButton>();
		yield return StartCoroutine(gameSpecificSelectedCoroutine(button));
		Animator pickedAnimator = button.animator;

		if (Audio.canSoundBeMapped(REVEAL_PICK_SOUND_KEY))
		{
			Audio.play(Audio.soundMap(REVEAL_PICK_SOUND_KEY));
		}
		if (Audio.canSoundBeMapped(REVEAL_BONUS_VO_SOUND_KEY))
		{
			Audio.play(Audio.soundMap(REVEAL_BONUS_VO_SOUND_KEY), 1, 0, REVEAL_BONUS_VO_SOUND_KEY_DELAY);
		}
		if (PRE_REVEAL_PICK_ANIM_NAME != "")
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, PRE_REVEAL_PICK_ANIM_NAME));
		}
		if (bonusOutcome.isChallenge)
		{
			pickedAnimator.Play(REVEAL_PICKING_ANIM_NAME);
			if (Audio.canSoundBeMapped(BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY))
			{
				Audio.playSoundMapOrSoundKeyWithDelay(BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY, BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY_DELAY);
			}
			foreach (UILabel label in bottomTextLabel)
			{
				label.text = Localize.textUpper(CHALLENGE_LOCALIZATION_KEY);
			}

			if (Audio.canSoundBeMapped(REVEAL_PICK_PICKING_SOUND_KEY))
			{
				Audio.playWithDelay(Audio.soundMap(REVEAL_PICK_PICKING_SOUND_KEY), REVEAL_PICKING_SOUND_KEY_DELAY);
			}

			if (Audio.canSoundBeMapped(CHALLENGE_REVEAL_VO_KEY))
			{
				Audio.playSoundMapOrSoundKeyWithDelay(CHALLENGE_REVEAL_VO_KEY, REVEAL_BONUS_VO_SOUND_KEY_DELAY);
			}

			// wait on animation
			yield return new TIWaitForSeconds(REVEAL_PICKING_ANIM_LENGTH);
		}
		else if (bonusOutcome.isGifting)
		{
			pickedAnimator.Play(REVEAL_FREESPINS_ANIM_NAME);
			if (Audio.canSoundBeMapped(BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY))
			{
				Audio.playSoundMapOrSoundKeyWithDelay(BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY, BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY_DELAY);
			}
			foreach (UILabel label in bottomTextLabel)
			{
				label.text = Localize.textUpper(FREESPIN_LOCALIZATION_KEY);
			}

			if (Audio.canSoundBeMapped(REVEAL_PICK_FREESPINS_SOUND_KEY))
			{
				Audio.playWithDelay(Audio.soundMap(REVEAL_PICK_FREESPINS_SOUND_KEY), REVEAL_FREESPINS_SOUND_KEY_DELAY);
			}

			if (Audio.canSoundBeMapped(FREESPIN_REVEAL_VO_KEY))
			{
				Audio.playSoundMapOrSoundKeyWithDelay(FREESPIN_REVEAL_VO_KEY, REVEAL_BONUS_VO_SOUND_KEY_DELAY);
			}

			// wait on animation
			yield return new TIWaitForSeconds(REVEAL_FREESPINS_ANIM_LENGTH);
		}
		else
		{
			// credits
			pickedAnimator.Play(REVEAL_CREDITS_ANIM_NAME);
			if (Audio.canSoundBeMapped(BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY))
			{
				Audio.playSoundMapOrSoundKeyWithDelay(BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY, BONUS_PORTAL_PICK_ANIMATION_SOUND_KEY_DELAY);
			}
			foreach (UILabel label in bottomTextLabel)
			{
				label.text = "";
			}
			setCreditText(button, true);

			if (Audio.canSoundBeMapped(REVEAL_PICK_CREDITS_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(REVEAL_PICK_CREDITS_SOUND_KEY));
			}

			// wait on animation
			yield return new TIWaitForSeconds(REVEAL_CREDITS_ANIM_LENGTH);
		}

		revealedPortalTypeList.Clear();
		foreach (PickGameButton currentButton in pickButtons)
		{
			if (currentButton != button && randomPortalTypeList.Count > 0)
			{
				// grab a portal type from the random list
				PortalTypeEnum nextPortal = randomPortalTypeList[randomPortalTypeList.Count - 1];
				randomPortalTypeList.RemoveAt(randomPortalTypeList.Count - 1);

				// track the order of the reveals
				revealedPortalTypeList.Add(nextPortal);

				Audio.play(Audio.soundMap(REVEAL_OTHERS_SOUND_KEY), 1.0f, 0.0f, REVEAL_OTHERS_SOUND_KEY_DELAY);
				if (PRE_REVEAL_PICK_ANIM_NAME != "")
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(currentButton.animator, PRE_REVEAL_PICK_ANIM_NAME));
				}

				switch (nextPortal)
				{
				case PortalTypeEnum.PICKING:
					currentButton.animator.Play(REVEAL_GRAY_PICKING_ANIM_NAME);
					if (Audio.canSoundBeMapped(REVEAL_OTHERS_PICKING_SOUND_KEY))
					{
						Audio.play(Audio.soundMap(REVEAL_OTHERS_PICKING_SOUND_KEY));
					}
					break;
				case PortalTypeEnum.FREESPINS:
					currentButton.animator.Play(REVEAL_GRAY_FREESPINS_ANIM_NAME);
					if (Audio.canSoundBeMapped(REVEAL_OTHERS_FREESPINS_SOUND_KEY))
					{
						Audio.play(Audio.soundMap(REVEAL_OTHERS_FREESPINS_SOUND_KEY));
					}
					break;
				case PortalTypeEnum.CREDITS:
					setCreditText(currentButton, false);
					currentButton.animator.Play(REVEAL_GRAY_CREDITS_ANIM_NAME);
					if (Audio.canSoundBeMapped(REVEAL_OTHERS_CREDITS_SOUND_KEY))
					{
						Audio.play(Audio.soundMap(REVEAL_OTHERS_CREDITS_SOUND_KEY));
					}
					break;
				}

				// wait on animation
				yield return new TIWaitForSeconds(REVEAL_NOT_SELECTED_ANIM_LENGTH);
			}
		}

		yield return StartCoroutine(playFadeAnimations(obj));
		//Play exit portal animations
		if (bonusOutcome.isChallenge)
		{
			if (Audio.canSoundBeMapped(BONUS_PORTAL_END_PICKING_BG_MUSIC_KEY))
			{
				if (BONUS_PORTAL_END_PICKING_BG_MUSIC_KEY_DELAY > 0)
				{
					Audio.switchMusicKey(BONUS_PORTAL_END_PICKING_BG_MUSIC_KEY);
					RoutineRunner.instance.StartCoroutine(this.stopMusic(BONUS_PORTAL_END_PICKING_BG_MUSIC_KEY_DELAY));
				}
				else
				{
					Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_END_PICKING_BG_MUSIC_KEY));
				}
			}
			if (Audio.canSoundBeMapped(POST_REVEAL_PICKING_SOUND_KEY))
			{
				Audio.playSoundMapOrSoundKeyWithDelay(POST_REVEAL_PICKING_SOUND_KEY, POST_REVEAL_PICKING_SOUND_DELAY);
			}
			
			if (POST_REVEAL_PICKING_ANIM_NAME != "")
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, POST_REVEAL_PICKING_ANIM_NAME));
			}

			if (preBackgroundAnimator != null && PRE_REVEAL_PICKING_BACKGROUND_ANIM_NAME != "")
			{
				yield return StartCoroutine(
					CommonAnimation.playAnimAndWait(preBackgroundAnimator, PRE_REVEAL_PICKING_BACKGROUND_ANIM_NAME)); 
			}
			
			if (backgroundAnimator != null && REVEAL_PICKING_BACKGROUND_ANIM_NAME != "")
			{
				yield return StartCoroutine(
					playBackgroundTransition(
						REVEAL_PICKING_BACKGROUND_ANIM_NAME, PORTAL_TRANSITION_PICKING_SOUND_KEY, PORTAL_TRANSITION_PICKING_SOUND_DELAY));
			}
			
			Audio.play(Audio.soundMap(PORTAL_END_PICKING_SOUND_KEY), 1, 0, PORTAL_END_PICKING_SOUND_DELAY);
		}
		else if (bonusOutcome.isGifting)
		{
			if (Audio.canSoundBeMapped(BONUS_PORTAL_END_FREESPINS_BG_MUSIC_KEY))
			{
				if (BONUS_PORTAL_END_FREESPINS_BG_MUSIC_KEY_DELAY > 0)
				{
					Audio.switchMusicKey(BONUS_PORTAL_END_FREESPINS_BG_MUSIC_KEY);
					RoutineRunner.instance.StartCoroutine(this.stopMusic(BONUS_PORTAL_END_FREESPINS_BG_MUSIC_KEY_DELAY));
				}
				else
				{
					Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_END_FREESPINS_BG_MUSIC_KEY));
				}
			}

			if (Audio.canSoundBeMapped(POST_REVEAL_FREESPINS_SOUND_KEY))
			{
				Audio.playSoundMapOrSoundKeyWithDelay(POST_REVEAL_FREESPINS_SOUND_KEY, POST_REVEAL_FREESPINS_SOUND_DELAY);
			}

			if (POST_REVEAL_FREESPINS_ANIM_NAME != "")
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, POST_REVEAL_FREESPINS_ANIM_NAME));
				Audio.stopSound(Audio.findPlayingAudio(Audio.soundMap(POST_REVEAL_FREESPINS_SOUND_KEY)));
			}

			if (shouldPlayFreeSpinIntroSound)
			{
				Audio.play(Audio.soundMap(FREESPIN_INTRO_SOUND_KEY));
			}

			if (preBackgroundAnimator != null && PRE_REVEAL_FREESPINS_BACKGROUND_ANIM_NAME != "")
			{
				yield return StartCoroutine(
					CommonAnimation.playAnimAndWait(preBackgroundAnimator, PRE_REVEAL_FREESPINS_BACKGROUND_ANIM_NAME)); 
			}
			
			if (backgroundAnimator != null && REVEAL_FREESPINS_BACKGROUND_ANIM_NAME != "")
			{
				yield return StartCoroutine(
					playBackgroundTransition(
						REVEAL_FREESPINS_BACKGROUND_ANIM_NAME, PORTAL_TRANSITION_FREESPINS_SOUND_KEY, PORTAL_TRANSITION_FREESPINS_SOUND_DELAY));
			}
			Audio.play(Audio.soundMap(PORTAL_END_FREESPINS_SOUND_KEY), 1, 0, PORTAL_END_FREESPINS_SOUND_DELAY);
		}
		else if (bonusOutcome.isCredit)
		{
			foreach (GameObject objectToHide in objectsToHideForTransition)
			{
				objectToHide.SetActive(false);
			}
		}

		if(portalTransitionAnimator != null)
		{
			if (PRE_OUTRO_TRANSITION_WAIT > 0.0f)
			{
				yield return new TIWaitForSeconds(PRE_OUTRO_TRANSITION_WAIT);
			}

			if (shouldActivateTransitionParent)
			{
				portalTransitionParent.SetActive(true);
			}
			if (!string.IsNullOrEmpty(portalOutroSound))
			{
				if (playOutroSoundOnce)
				{
					Audio.play(Audio.soundMap(portalOutroSound), 1, 0, PORTAL_OUTRO_SOUND_DELAY);
				}
				else
				{
					Audio.switchMusicKeyImmediate(Audio.soundMap(portalOutroSound));
				}

				if (!string.IsNullOrEmpty(PORTAL_OUTRO_VO))
				{
					Audio.playSoundMapOrSoundKeyWithDelay(PORTAL_OUTRO_VO, PORTAL_OUTRO_VO_DELAY);
				}
			}
			if (shouldContinueTransitionAfterPortalIsDestroyed)
			{
				portalTransitionParent.transform.parent = null;
				RoutineRunner.instance.StartCoroutine(playPortalOutroTransition()); // play the outro, but don't wait
				RoutineRunner.instance.StartCoroutine(destroyTransitionObject());
				yield return new TIWaitForSeconds(SECONDS_TO_WAIT_FOR_SPLIT_TRANSITION);
			}
			else
			{
				// wait for the portal outro to finish before destroying
				yield return StartCoroutine(playPortalOutroTransition());
			}
		}

		if (gameSpecificPortalOutro)
		{
			// play a specific outro transition depending on the game selected
			if (bonusOutcome.isGifting)
			{
				yield return new TIWaitForSeconds(WAIT_BEFORE_ENDING_PORTAL_FREESPIN);

			}
			else if (bonusOutcome.isChallenge)
			{
				yield return new TIWaitForSeconds(WAIT_BEFORE_ENDING_PORTAL_CHALLENGE);

			}
			else if (bonusOutcome.isCredit)
			{
				yield return new TIWaitForSeconds(WAIT_BEFORE_ENDING_PORTAL_CREDIT);

			}
		}
		else
		{
			// let player see their choices for just a bit before starting the bonus
			yield return new TIWaitForSeconds(WAIT_BEFORE_ENDING_PORTAL);
		}

		beginBonus();
			
	}

	private IEnumerator turnOnUpcomingWings(float wait)
	{
		yield return new TIWaitForSeconds(wait);

		if (bonusOutcome.isGifting)
		{
			BonusGameManager.instance.wings.forceShowFreeSpinWings(true);
		}
		else if (bonusOutcome.isChallenge)
		{
			BonusGameManager.instance.wings.forceShowChallengeWings(true);
		}
	}

	private IEnumerator stopMusic(float delay)
	{
		if(delay > 0)
		{
			yield return new TIWaitForSeconds(delay);
		}
		Audio.stopMusic();
	}

	// Play the portal's defined outro transition
	private IEnumerator playPortalOutroTransition()
	{
		if (hideObjectsBeforeOutroTransition)
		{
			foreach (GameObject objectToHide in objectsToHideForTransition)
			{
				objectToHide.SetActive(false);
			}
		}
		if (gameSpecificPortalOutro)
		{
			// play a specific outro transition depending on the game selected
			if (bonusOutcome.isGifting)
			{
				if (!string.IsNullOrEmpty(PORTAL_OUTRO_FREESPINS_TRANSITION_SOUND))
				{
					Audio.playSoundMapOrSoundKey(PORTAL_OUTRO_FREESPINS_TRANSITION_SOUND);
				}

				if (!string.IsNullOrEmpty(PORTAL_OUTRO_FREESPINS_TRANSITION_ANIM))
				{
					if (switchUpcomingWingsDuringOutro)
					{
						//Used for turning on the wing part way through the transition (Cesar01 Portal -> Freespins transition)
						StartCoroutine(turnOnUpcomingWings(waitBeforeSwitchingToFreespinsWings));
					}

					// if no wings are desired for the bonus game, hide them during the transition
					if (hideUpcomingWingsDuringOutroFreespins)
					{
						BonusGameManager.instance.wings.hide();
					}

					yield return StartCoroutine(CommonAnimation.playAnimAndWait(portalTransitionAnimator, PORTAL_OUTRO_FREESPINS_TRANSITION_ANIM));
				}
			}
			else if (bonusOutcome.isChallenge)
			{
				if (!string.IsNullOrEmpty(PORTAL_OUTRO_CHALLENGE_TRANSITION_SOUND))
				{
					Audio.playSoundMapOrSoundKey(PORTAL_OUTRO_CHALLENGE_TRANSITION_SOUND);
				}

				if (!string.IsNullOrEmpty(PORTAL_OUTRO_CHALLENGE_TRANSITION_ANIM))
				{
					if (switchUpcomingWingsDuringOutro)
					{
						//Used for turning on the wing part way through the transition
						StartCoroutine(turnOnUpcomingWings(waitBeforeSwitchingToChallengeWings));
					}

					// if no wings are desired for the bonus game, hide them during the transition
					if (hideUpcomingWingsDuringOutroChallenge)
					{
						BonusGameManager.instance.wings.hide();
					}

					yield return StartCoroutine(CommonAnimation.playAnimAndWait(portalTransitionAnimator, PORTAL_OUTRO_CHALLENGE_TRANSITION_ANIM));
				}
			}
			else if (bonusOutcome.isCredit)
			{
				if (!string.IsNullOrEmpty(PORTAL_OUTRO_CREDIT_TRANSITION_SOUND))
				{
					Audio.playSoundMapOrSoundKey(PORTAL_OUTRO_CREDIT_TRANSITION_SOUND);
				}

				if (!string.IsNullOrEmpty(PORTAL_OUTRO_CREDIT_TRANSITION_ANIM))
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(portalTransitionAnimator, PORTAL_OUTRO_CREDIT_TRANSITION_ANIM));
				}
			}
		}
		else
		{
			// play the generic portal outro transition
			if (!string.IsNullOrEmpty(PORTAL_OUTRO_TRANSITION_ANIM))
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(portalTransitionAnimator, PORTAL_OUTRO_TRANSITION_ANIM));
			}
		}
	}

	/// Play a transition animation
	private IEnumerator playBackgroundTransition(string transitionAnimName, string transitionAnimSoundKey, float transitionAnimSoundDelay = 0.0f)
	{
		if (backgroundAnimator != null && transitionAnimName != "")
		{
			foreach (GameObject objectToHide in objectsToHideForTransition)
			{
				objectToHide.SetActive(false);
			}

			if (transitionIsUsingIntroMusic)
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(transitionAnimSoundKey));
			}
			else
			{
				Audio.playWithDelay(Audio.soundMap(transitionAnimSoundKey), transitionAnimSoundDelay);
			}
			
			backgroundAnimator.gameObject.SetActive(true);
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(backgroundAnimator, transitionAnimName));
			yield return new TIWaitForSeconds(POST_TRANSITION_ANIMATION_WAIT);
		}
	}

	private IEnumerator destroyTransitionObject()
	{
		if (gameSpecificPortalOutro)
		{
			// play a specific outro transition depending on the game selected
			if (bonusOutcome.isGifting)
			{
				yield return new TIWaitForSeconds(WAIT_BEFORE_DESTROYING_FREESPIN_TRANSITION_OBJECT); //CommonAnimation.waitForAnimDur(portalTransitionAnimator));
				Destroy(portalTransitionParent);
			}
			else if (bonusOutcome.isChallenge)
			{
				yield return new TIWaitForSeconds(WAIT_BEFORE_DESTROYING_CHALLENGE_TRANSITION_OBJECT);
				Destroy(portalTransitionParent);
			}
			else if (bonusOutcome.isCredit)
			{
				yield return new TIWaitForSeconds(WAIT_BEFORE_DESTROYING_CREDIT_TRANSITION_OBJECT); 
				Destroy(portalTransitionParent);
			}
		}
		else
		{
			yield return new TIWaitForSeconds(WAIT_BEFORE_DESTROYING_TRANSITION_OBJECT); 
			Destroy(portalTransitionParent);
		}
	}

	private IEnumerator playFadeAnimations(GameObject obj)
	{
		if (PLAY_FADE_ANIMATIONS_DELAY != 0.0f)
		{
			yield return new TIWaitForSeconds(PLAY_FADE_ANIMATIONS_DELAY);
		}

		PickGameButton button = obj.GetComponent<PickGameButton>();
		Animator pickedAnimator = button.animator;
		Animator portalPicksBackdropAnimator = null;
		if(portalPicksBackdrop != null)
		{
			portalPicksBackdropAnimator = portalPicksBackdrop.GetComponent<Animator>();
		}


		if (bonusOutcome.isChallenge && POST_REVEAL_FADE_PICKED_PICKING_ANIM_NAME != "")
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, POST_REVEAL_FADE_PICKED_PICKING_ANIM_NAME));
		}
		else if (bonusOutcome.isGifting && POST_REVEAL_FADE_PICKED_FREESPINS_ANIM_NAME != "")
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, POST_REVEAL_FADE_PICKED_FREESPINS_ANIM_NAME));
		}
		else if (bonusOutcome.isCredit && POST_REVEAL_FADE_PICKED_CREDITS_ANIM_NAME != "")
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, POST_REVEAL_FADE_PICKED_CREDITS_ANIM_NAME));
		}

		if (portalPicksBackdropAnimator != null && PORTAL_PICKS_BACKDROP_EXIT_ANIM_NAME != "")
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(portalPicksBackdropAnimator, PORTAL_PICKS_BACKDROP_EXIT_ANIM_NAME));
		}

		if (portalPicksBackdropTextAnimator != null && PORTAL_PICKS_BACKDROP_TEXT_EXIT_ANIM_NAME != "")
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(portalPicksBackdropTextAnimator, PORTAL_PICKS_BACKDROP_TEXT_EXIT_ANIM_NAME));
		}

		// Track the last animation we played for a fade out of the other options and then wait on that one
		TICoroutine fadeUnpickedCoroutine = null;

		int revealIndex = 0;

		foreach (PickGameButton currentButton in pickButtons)
		{
			if (currentButton != button)
			{
				PortalTypeEnum nextPortal = PortalTypeEnum.NONE;

				if (revealedPortalTypeList.Count > 0 && revealIndex < revealedPortalTypeList.Count)
				{
					nextPortal = revealedPortalTypeList[revealIndex];
					revealIndex++;
				}

				if (nextPortal == PortalTypeEnum.FREESPINS && POST_REVEAL_FADE_UNPICKED_FREESPINS_ANIM_NAME != "")
				{
					fadeUnpickedCoroutine = StartCoroutine(CommonAnimation.playAnimAndWait(currentButton.animator, POST_REVEAL_FADE_UNPICKED_FREESPINS_ANIM_NAME));
				}
				else if (nextPortal == PortalTypeEnum.PICKING && POST_REVEAL_FADE_UNPICKED_PICKING_ANIM_NAME != "")
				{
					fadeUnpickedCoroutine = StartCoroutine(CommonAnimation.playAnimAndWait(currentButton.animator, POST_REVEAL_FADE_UNPICKED_PICKING_ANIM_NAME));
				}
				else if (nextPortal == PortalTypeEnum.CREDITS && POST_REVEAL_FADE_UNPICKED_CREDITS_ANIM_NAME != "")
				{
					fadeUnpickedCoroutine = StartCoroutine(CommonAnimation.playAnimAndWait(currentButton.animator, POST_REVEAL_FADE_UNPICKED_CREDITS_ANIM_NAME));
				}
			}
		}

		if (fadeUnpickedCoroutine != null)
		{
			yield return fadeUnpickedCoroutine;
		}
	}
	/// Start the actual bonus revealed in this portal
	protected void beginBonus()
	{
		if (bonusOutcome.isCredit)
		{
			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.goIntoBonus();
			}
			else
			{
				Debug.LogError("There is no SlotBaseGame instance, can't start bonus game...");
			}
		}
		else
		{
			if (bonusOutcome.isChallenge)
			{
				BonusGameManager.instance.create(BonusGameType.CHALLENGE);
			}
			else
			{
				BonusGameManager.instance.create(BonusGameType.GIFTING);
			}

			BonusGameManager.instance.show();
		}
	}

	protected virtual SlotOutcome getFreeSpinOutcome()
	{
		string formattedFreespinOutcomeName = FREESPIN_OUTCOME_NAME;
		if (GameState.game != null)
		{
			formattedFreespinOutcomeName = string.Format(FREESPIN_OUTCOME_NAME, GameState.game.keyName);
		}

		if (hasPickMajorFreespins)
		{
			return SlotOutcome.getPickMajorFreeSpinOutcome(bonusOutcome, formattedFreespinOutcomeName, numberOfMajorSymbols);
		}
		else
		{
			return bonusOutcome.getBonusGameOutcome(formattedFreespinOutcomeName);
		}
	}

	/// Overridable function for generating the challenge game data, may use something other than WheelOutcome, if so override and return a new instance of one of those
	protected BaseBonusGameOutcome createChallengeBonusData(SlotOutcome challenge_outcome)
	{
		switch (bonusOutcomeType)
		{
		case BonusGameDataTypeEnum.WheelOutcome:
			return new WheelOutcome(challenge_outcome);

		case BonusGameDataTypeEnum.WheelOutcomeWithChildOutcomes:
			return new WheelOutcome(challenge_outcome, false, 0, true);

		case BonusGameDataTypeEnum.PickemOutcome:
			return new PickemOutcome(challenge_outcome);

		case BonusGameDataTypeEnum.NewBaseBonusOutcome:
			return new NewBaseBonusGameOutcome(challenge_outcome);

		case BonusGameDataTypeEnum.CrosswordOutcome:
			return new CrosswordOutcome(challenge_outcome);
		}

		// if we reach here then we have an unhandled outcome type
		Debug.LogError("PickPortal::createChallengeBonusData() - Don't know how to handle bonusOutcomeType = " + bonusOutcomeType);
		return null;
	} 
}
