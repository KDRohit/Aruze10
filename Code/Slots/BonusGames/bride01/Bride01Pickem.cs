using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implements the picking game for Bride's Maids
*/
public class Bride01Pickem : PickingGame<WheelOutcome> 
{
	[SerializeField] private GameObject[] stageGameObjects = null;		// Collection of the highest parent object of each stage of the game
	[SerializeField] private UISprite dressCharacterSprite = null;		// Character portrait that appears in the dress portion of the pickem game, matches what the user reveals in stall stage
	[SerializeField] private UILabelStyle grayedOutRevealStyle;			// Our swapped out reveal text
	[SerializeField] private UILabel jackpotAmountText = null;			// Textfield for the jackpot amount, which can change -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent jackpotAmountTextWrapperComponent = null;			// Textfield for the jackpot amount, which can change

	public LabelWrapper jackpotAmountTextWrapper
	{
		get
		{
			if (_jackpotAmountTextWrapper == null)
			{
				if (jackpotAmountTextWrapperComponent != null)
				{
					_jackpotAmountTextWrapper = jackpotAmountTextWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotAmountTextWrapper = new LabelWrapper(jackpotAmountText);
				}
			}
			return _jackpotAmountTextWrapper;
		}
	}
	private LabelWrapper _jackpotAmountTextWrapper = null;
	
	[SerializeField] private GameObject jackpotAmountScaler = null;		// Scaler object for the jackpot amount, used to pulse the value and alert the player it changed
	[SerializeField] private GameObject jackpotWinFrameAnimObj = null;	// Object to turn on the jackpot win frame animation
	[SerializeField] private GameObject[] round2Characters = null;	

	private GameCharacterEnum gameCharacter = GameCharacterEnum.NONE;	// Tells which character, and jackpot, will be used
	private GameStageEnum gameStage = GameStageEnum.NONE;				// Tells what stage of the game the user is in
	private List<int> stallCharacterReveals = new List<int>();			// Stores a randomized list of character indexs which are used to reveal the stalls
	private PickemOutcome pickemOutcome = null;							// This game uses a WheelOutcome as a parent and then a nested PickemOutcome
	private PickemPick savedPick = null;								// Storing out the current pick because the SAVE stage of the game needs the info stored in it
	private List<long> characterJackpots = new List<long>();			// A list of the character jackpot values for this pickem game, taken from the paytables
	private long currentJackpotAmount = 0L;								// Tracks what the current jackpot amount is
	private int numDressesPicked = 0;									// Tracks the number of dresses that have been picked, used to play a VO clip every other dress pick

	private const float TIME_BEFORE_NEXT_STAGE = 1.0f;				// Wait time before moving to the next stage / summary dialog
	private const float TRAY_STAGE_SHOW_DELAY = 0.25f;				// Add a slight delay after showing a tray in the dress stage so they have a chance to see it

	private const float STALL_REVEAL_WAIT = 0.5f;					// Time between stall reveals

	// Enum telling what stage of the pick game we are currently in, for Bride's Maids the game can go back and forth between DRESS_PICK and TRAY_PICK
	public enum GameStageEnum
	{
		NONE 		= -1,
		STALL_PICK 	= 0,
		DRESS_PICK 	= 1,
		TRAY_PICK 	= 2
	}

	// An easy way to keep track of what character shows up in the stall and the second part of the game
	public enum GameCharacterEnum
	{
		NONE 	= -1,
		ELLIE 	= 0,
		HELEN 	= 1,
		MEGAN 	= 2,
		LILLIAN = 3,
		ANNIE 	= 4
	}

	// Number of stars to reveal during the jackpot stall selection
	private readonly int[] CHARACTER_STAR_NUMBERS = { 1,
													  3,
													  2,
													  4,
													  5 };

	[SerializeField] private string[] characterPaytableNames;

	// Reveal animation names for each character
	private readonly string[] STALL_REVEAL_ANIM_NAMES = { 	"bride01_curtainReveal_Ellie", 
															"bride01_curtainReveal_Helen", 
															"bride01_curtainReveal_Megan", 
															"bride01_curtainReveal_Lillian",
															"bride01_curtainReveal_Annie" };

	// Jackpot icon reveal animation names
	private readonly string[] DRESS_JACKPOT_REVEAL_ANIM_NAMES = {	"bride01_dressReveal_Ellie",
																	"bride01_dressReveal_Helen",
																	"bride01_dressReveal_Megan",
																	"bride01_dressReveal_Lillian",
																	"bride01_dressReveal_Annie"	};

	[SerializeField] private string STALL_PICKME_ANIM_NAME = "bride01_curtainPickme";	// Pick me animation name

	private const string DRESS_CREDIT_REVEAL_ANIM_NAME = "bride01_dressReveal_Credits";	// Animation name for revealing credits
	private const string DRESS_TRAY_REVEAL_ANIM_NAME = "bride01_dressReveal_Tray";		// Animation name for revealing a tray
	[SerializeField] private string DRESS_PICKME_ANIM_NAME = "bride01_dressPickme";				// Pickme animaiton for dresses

	private readonly string[] CHARACTER_SPRITE_NAMES = {	"ellie",
															"helen",
															"megan",
															"lillian",
															"annie" };

	[SerializeField] private bool playMajorMinorVosStall = false;
	[SerializeField] private bool useAnimationsForGreyReveals = false;
	[SerializeField] private string trayRSVPRevealAnimation = null;
	[SerializeField] private string trayFlowerRevealAnimation = null;
	[SerializeField] private string trayRSVPRevealAnimationGrey = null;
	[SerializeField] private string trayFlowerRevealAnimationGrey = null;

	private const string TRAY_RSVP_REVEAL_ANIM = "bride01_trayReveal_RSVP";			// Animation to reveal an RSVP card
	private const string TRAY_FLOWER_REVEAL_ANIM = "bride01_trayReveal_Flowers";	// Animation to reveal flowers
	private const string TRAY_IDLE_ANIM = "bride01_trayIdle";						// Idle animation for a tray, needed to reset the game
	private const float TRAY_REVEAL_ANIM_TIME = 1.0f;								// How long to wait for a tray reveal animaiton to play
	[SerializeField] private string TRAY_PICKME_ANIM = "bride01_trayPickme";
	[SerializeField] private float WAIT_BEFORE_TRAY_TO_DRESS_TRANSITION = 0.0f;

	private const float PICKME_ANIM_PLAY_TIME = 0.5f;				// TIme the pick me animations take to play

	private const string BONUS_INTRO_VO_SOUND_KEY = "bonus_intro_vo";					// Intro VO for the game
	[SerializeField] private string CURTAIN_PICK_BG_MUSIC = "BonusP1Bride01";						// Pick a stall background music
	[SerializeField] private string STALL_PICKME_SOUND = "rollover_sparkly";						// General sound for the pickem animations
	[SerializeField] private string DRESS_PICKME_SOUND = "rollover_sparkly";						// General sound for the pickem animations
	[SerializeField] private string TRAY_PICKME_SOUND = "rollover_sparkly";						// General sound for the pickem animations

	[SerializeField] private string STALL_MAJOR_VO_SOUND = "";
	[SerializeField] private string STALL_MINOR_VO_SOUND = "";
	[SerializeField] private string STALL_PICKED_SOUND = "";						// General sound for the pickem animations
	[SerializeField] private string DRESS_PICKED_SOUND = "";						// General sound for the pickem animations
	[SerializeField] private string TRAY_PICKED_SOUND = "";						// General sound for the pickem animations
	[SerializeField] private string TRAY_INTRO_VO = "";                         // Intro VO for the Tray round
	[SerializeField] private string TRAY_REVEAL_BAD_VO = "";                    // VO for game-ending reveal

	[SerializeField] private string CURTAIN_REVEAL_SOUND = "DressShopRevealCharacterCurtain";		// Sound played when a curtain is picked

	// Voiceover for each character when a stall is picked
	[SerializeField] private string[] CURTAIN_CHARACTER_VO_SOUND_LIST = { 	"EKWowNeverBeenThisPartTownBefore",
																	"RBOhMyGod",
																	"MMReferringMyselfMeganMeMegan",
																	"MRYeahWasntItMyTurnToBeCrazy",
																	"KWLookAtThisOneThisIsPretty" };

	private const string REVEAL_OTHER_SOUND_KEY = "reveal_not_chosen"; 		// Sound played when unpicked choices are revealed

	[SerializeField] private string DRESS_PICK_BG_MUSIC = "BonusP2Bride01";			// Dress pick background music
	[SerializeField] private string TRAY_PICK_BG_MUSIC = "BonusP3Bride01";				// Tray pick background music

	[SerializeField] private string REVEAL_JACKPOT_SOUND = "DressShopRevealJackpot";

	// Voice over per character played when a character icon is found
	[SerializeField] private string[] REVEAL_JACKPOT_CHARACTER_VO_SOUND_LIST = { 	"EKICantWaitToBeMarriedAsLongAsYou",
																		 	"RBUniqueSpecialCoutureMadeInFrance",
																			"MMNotConfident",
																			"MRjustSentMeasurementsFranceYall",
																			"KWItsReallyNice" };

	// Voice over sounds to play when the RSVP card is picked in the tray screen
	[SerializeField] private string[] CHARACTER_FOUND_RSVP_CARD_VO_SOUNDS = { 	"DressShopRsvpVOBecca",
																		"DressShopRsvpVOHelen",
																		"DressShopRsvpVOMegan",
																		"DressShopRsvpVOLillian",
																		"DressShopRsvpVOAnnie" };

	// Voice over clips for each character to be played every other dress that is picked
	private const string EVERY_OTHER_DRESS_PICK_VO = "Bride01EveryOtherDressVO";

	[SerializeField] private string REVEAL_INVITE_SOUND = "DressShopRevealInvitation";		// Sound for finding the SAVE invite during the tray pick stage
	[SerializeField] private string REVEAL_BOUQUET_SOUND = "DressShopRevealBouquet";		// Sound for finding the game over bouquet during the tray pick stage

	[SerializeField] private string REVEAL_DRESS_CREDITS_SOUND = "DressShopRevealCredit";	// Dress pick credits reveal sound
	[SerializeField] private string REVEAL_DRESS_RSVP_SOUND = "DressShopRevealCredit";	// Dress pick RSVP reveal sound

	private const string PICKME_SOUND = "rollover_sparkly";						// General sound for the pickem animations
	[SerializeField] private string JACKPOT_VALUE_INCREASED_SOUND = "DressShopAdvanceJackpotSparklyFs";	// Sound played when the jackpot value increases when coming back from the tray phase

	private const string BONUS_SUMMARY_VO = "BonusSummaryVOBride01";			// Voice over played as the bonus summary appears

	private const float JACKPOT_PULSATE_ONE_WAY_TIME = 0.25f;	// Controls how long pulsating up/down of the jackpot amount
	private const float WAIT_BEFORE_UNLOCKING_INPUT = 0.15f;	// Need a slight delay before unlocking input going into tray section because objects can still be clicked that should be behind new screen
	private const float JACKPOT_VO_DELAY_TIME = 0.5f;			// Delay for jackpot VO so it doesn't collide with the reveal sound
	
	private Vector3 dressStagePos = new Vector3();
	
	// This is a filthy hack to support elvira03
	[SerializeField] private bool hideDressStageWhenTrayIsVisible = false;

	/// Init stuff for the game, derived classes SHOULD call base.init(); so the outcome is set and the pickme animation controller is setup
	public override void init()
	{
		base.init();

		WheelPick characterPick = outcome.getNextEntry();

		if (characterPick.bonusGame.Contains("becca"))
		{
			gameCharacter = GameCharacterEnum.ELLIE;
		}
		else if (characterPick.bonusGame.Contains("helen"))
		{
			gameCharacter = GameCharacterEnum.HELEN;	
		}
		else if (characterPick.bonusGame.Contains("megan"))
		{
			gameCharacter = GameCharacterEnum.MEGAN;
		}
		else if (characterPick.bonusGame.Contains("lilian"))
		{
			gameCharacter = GameCharacterEnum.LILLIAN;
		}
		else if (characterPick.bonusGame.Contains("annie"))
		{
			gameCharacter = GameCharacterEnum.ANNIE;
		}
		else
		{
			gameCharacter = GameCharacterEnum.NONE;
			Debug.LogError("There was an unexpected format for the name of the Bride01Pickem game, don't know what character to reveal for " +
							characterPick.bonusGame);
		}

		// extract the PickemOutcome
		pickemOutcome = new PickemOutcome(SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, characterPick.bonusGame));

		changeStage(GameStageEnum.STALL_PICK);

		// readout starting jackpot values for each character
		setCharacterJackpotStartingValues();

		// generate a random reveal order for the characters in the stalls
		generateRandomStallRevealOrder();

		Audio.play(Audio.soundMap(BONUS_INTRO_VO_SOUND_KEY));
		Audio.switchMusicKeyImmediate(CURTAIN_PICK_BG_MUSIC);
		
		// There is something odd with the way the animations work in elvira03, so in order to simulate "hiding" the pick object,
		//	we just slide it way out of view.
		dressStagePos = stageGameObjects[(int)GameStageEnum.DRESS_PICK].transform.position;
	}

	/// Get the jackpot values for all the characters
	private void setCharacterJackpotStartingValues()
	{
		foreach (string characterPaytableName in characterPaytableNames)
		{
			JSON characterPaytable = BonusGamePaytable.findPaytable("pickem", characterPaytableName);
			long characterJackpot = characterPaytable.getLong("jackpot_credits", 0L);

			characterJackpots.Add(characterJackpot * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers);
		}
	}

	/// Generate a random order to reveal characters in for the stall part which isn't directly part of the pick outcome
	private void generateRandomStallRevealOrder()
	{
		stallCharacterReveals.Add((int)GameCharacterEnum.ELLIE);
		stallCharacterReveals.Add((int)GameCharacterEnum.HELEN);
		stallCharacterReveals.Add((int)GameCharacterEnum.MEGAN);
		stallCharacterReveals.Add((int)GameCharacterEnum.LILLIAN);
		stallCharacterReveals.Add((int)GameCharacterEnum.ANNIE);

		CommonDataStructures.shuffleList<int>(stallCharacterReveals);
	}

	/// Pick me animation player
	protected override IEnumerator pickMeAnimCallback()
	{
		List<GameObject> stageButtons = pickmeButtonList[(int)gameStage];

		int randomButtonIndex = Random.Range(0, stageButtons.Count);
		Animator buttonAnimator = stageButtons[randomButtonIndex].GetComponent<Animator>();

		switch (gameStage)
		{
			case GameStageEnum.STALL_PICK:
				buttonAnimator.Play(STALL_PICKME_ANIM_NAME);
				Audio.play(STALL_PICKME_SOUND);				
				break;

			case GameStageEnum.DRESS_PICK:
				buttonAnimator.Play(DRESS_PICKME_ANIM_NAME);
				Audio.play(DRESS_PICKME_SOUND);				
				break;

			case GameStageEnum.TRAY_PICK:
				buttonAnimator.Play(TRAY_PICKME_ANIM);
				Audio.play(TRAY_PICKME_SOUND);				
				break;
		}

		yield return new TIWaitForSeconds(PICKME_ANIM_PLAY_TIME);
	}

	/// Coroutine called when a button is pressed, used to handle timing stuff that may need to happen
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		inputEnabled = false;

		if (gameStage == GameStageEnum.STALL_PICK)
		{
			yield return StartCoroutine(revealStall(buttonObj, gameCharacter, true));

			savedPick = null;
		}
		else if (gameStage == GameStageEnum.DRESS_PICK)
		{
			savedPick = pickemOutcome.getNextEntry();
			
			Audio.play(DRESS_PICKED_SOUND);
			yield return StartCoroutine(revealSlot(buttonObj, savedPick, true));

			// only keep the pick info if we are heading to the tray phase
			if (savedPick.groupId != "SAVE")
			{
				savedPick = null;
			}
		}
		else
		{
			// this is a tray pick, which uses a savedPick from the dress phase
			Audio.play(TRAY_PICKED_SOUND);
			yield return StartCoroutine(revealSlot(buttonObj, savedPick, true));

			savedPick = null;
		}

		// ensure the current click doesn't cause the reveals to skip by waiting a frame
		yield return null;
		
		if (gameStage == GameStageEnum.STALL_PICK || (savedPick == null && pickemOutcome.entryCount == 0 && gameStage == GameStageEnum.DRESS_PICK))
		{
			// Show reveals for the stalls, and the dresses if the final pick occurs in that stage
			yield return StartCoroutine(revealRemainingSlots());
		}

		if (pickemOutcome.entryCount == 0 && savedPick == null)
		{
			// Game has ended
			yield return new TIWaitForSeconds(TIME_BEFORE_NEXT_STAGE);

			// cut the current music so the summary music plays right away
			Audio.switchMusicKeyImmediate("");
			if (GameState.game.keyName.Contains ("bride01"))
			{
				Audio.play (BONUS_SUMMARY_VO);
			}
			
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			if (gameStage == GameStageEnum.STALL_PICK)
			{
				// move from the stall pick to the dress pick
				yield return new TIWaitForSeconds(TIME_BEFORE_NEXT_STAGE);
				changeStage(GameStageEnum.DRESS_PICK);
			}
			else if (savedPick != null && savedPick.groupId == "SAVE" && gameStage == GameStageEnum.DRESS_PICK)
			{
				// chance to make a tray pick and come back to dresses
				yield return new TIWaitForSeconds(TRAY_STAGE_SHOW_DELAY);
				changeStage(GameStageEnum.TRAY_PICK);

				// wait a small amount of time to make sure that the stage is actually changed before letting the user click again
				// without this here the user can cause a tap on the previous stage object while the next stage is still being setup but assumed to have loaded
				yield return new TIWaitForSeconds(WAIT_BEFORE_UNLOCKING_INPUT);
			}

			inputEnabled = true;
		}
	}

	/// Reveal a pick slot either when it is picked, or when reveals are happening
	private IEnumerator revealSlot(GameObject slot, PickemPick pick, bool isPick)
	{
		// Prevent the user form selecting this button again
		CommonGameObject.setObjectCollidersEnabled(slot, false);

		switch (gameStage)
		{
			case GameStageEnum.STALL_PICK:
				yield return StartCoroutine(revealStall(slot, gameCharacter, isPick));
				break;

			case GameStageEnum.DRESS_PICK:
				yield return StartCoroutine(revealDress(slot, pick, isPick));
				break;

			case GameStageEnum.TRAY_PICK:
				yield return StartCoroutine(revealTray(slot, pick, isPick));
				break;
		}
	}

	/// Handles reveals for the stall stage of this pick game
	private IEnumerator revealStall(GameObject slot, GameCharacterEnum pick, bool isPick)
	{
		Bride01Stall stall = slot.GetComponent<Bride01Stall>();

		pickmeButtonList[(int)GameStageEnum.STALL_PICK].Remove(slot);

		if (isPick) 
		{
			Audio.play(STALL_PICKED_SOUND);
			Audio.play(CURTAIN_REVEAL_SOUND);
			
			if (playMajorMinorVosStall) 
			{
				if (pick == GameCharacterEnum.ELLIE) 
				{
					// play minor VO
					Audio.playSoundMapOrSoundKey(STALL_MINOR_VO_SOUND);
				} 
				else 
				{
					// play major VO
					Audio.playSoundMapOrSoundKey(STALL_MAJOR_VO_SOUND);
				}
			} 
			else 
			{
				Audio.play(CURTAIN_CHARACTER_VO_SOUND_LIST[(int)pick]);
			}
		}
		else
		{
			// play reveal audio
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND_KEY));
			}
		}

		yield return StartCoroutine(stall.reveal(STALL_REVEAL_ANIM_NAMES[(int)pick], isPick, characterJackpots[(int)pick], grayedOutRevealStyle, CHARACTER_STAR_NUMBERS[(int)pick]));

		if (isPick)
		{
			// update the starting jackpot value shown on the dress screen
			currentJackpotAmount = characterJackpots[(int)pick];
			jackpotAmountTextWrapper.text = CreditsEconomy.convertCredits(currentJackpotAmount);
		}
	}

	/// Handles reveals for the dress stage of this pick game
	private IEnumerator revealDress(GameObject slot, PickemPick pick, bool isPick)
	{
		Bride01Dress dress = slot.GetComponent<Bride01Dress>();

		pickmeButtonList[(int)GameStageEnum.DRESS_PICK].Remove(slot);

		// figure out if we should play a pick VO (not going to play one for Jackpot though, because that will probably get too crazy)
		bool isPlayingPickVO = false;
		if (isPick)
		{
			numDressesPicked++;
			isPlayingPickVO = (numDressesPicked % 2 == 0) ? true : false;
			Audio.play(DRESS_PICKED_SOUND);
		}

		if (pick.groupId == "JACKPOT")
		{
			if (isPick)
			{
				Audio.play(REVEAL_JACKPOT_SOUND);
			}
			else
			{
				// play reveal audio
				if(!revealWait.isSkipping){
					Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND_KEY));
				}
			}

			// got the jackpot
			yield return StartCoroutine(dress.reveal(DRESS_JACKPOT_REVEAL_ANIM_NAMES[(int)gameCharacter], isPick, pick.credits, grayedOutRevealStyle, true, "JACKPOT", (int)gameCharacter));

			if (isPick)
			{
				jackpotWinFrameAnimObj.SetActive(true);

				yield return new TIWaitForSeconds(JACKPOT_VO_DELAY_TIME);
				Audio.play(REVEAL_JACKPOT_CHARACTER_VO_SOUND_LIST[(int)gameCharacter]);

				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + currentJackpotAmount));
				BonusGamePresenter.instance.currentPayout += currentJackpotAmount;
			}
		}
		else if (pick.groupId == "SAVE")
		{
			if (isPick)
			{
				Audio.play(REVEAL_DRESS_RSVP_SOUND);
				if (isPlayingPickVO) {
					if (GameState.game.keyName.Contains ("bride01"))
					{
						Audio.play (EVERY_OTHER_DRESS_PICK_VO);
					}
				}
			}
			else
			{
				// play reveal audio
				if(!revealWait.isSkipping){
					Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND_KEY));
				}
			}
			
			// got a tray so going to try and recover after this
			yield return StartCoroutine(dress.reveal(DRESS_TRAY_REVEAL_ANIM_NAME, isPick, pick.credits, grayedOutRevealStyle, false, "SAVE"));
		}
		else
		{
			if (isPick)
			{
				Audio.play(REVEAL_DRESS_CREDITS_SOUND);
				if (isPlayingPickVO)
				{
					if (GameState.game.keyName.Contains ("bride01"))
					{
						Audio.play (EVERY_OTHER_DRESS_PICK_VO);
					}
				}
			}
			else
			{
				// play reveal audio
				if(!revealWait.isSkipping){
					Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND_KEY));
				}
			}

			// credits
			yield return StartCoroutine(dress.reveal(DRESS_CREDIT_REVEAL_ANIM_NAME, isPick, pick.credits, grayedOutRevealStyle, false, "CREDITS"));

			if (isPick)
			{
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + pick.credits));
				BonusGamePresenter.instance.currentPayout += pick.credits;
			}
		}
	}

	/// Handles reveals for the tray stage of this pick game
	private IEnumerator revealTray(GameObject slot, PickemPick pick, bool isPick)
	{
		Animator trayAnimator = slot.GetComponent<Animator>();

		pickmeButtonList[(int)GameStageEnum.TRAY_PICK].Remove(slot);

		if (pick == null || pick.isGameOver)
		{
			Bride01Tray tray = slot.GetComponent<Bride01Tray>();
			tray.hideObjectsForOutcome("GAMEOVER");

			if (isPick)
			{
				Audio.play(TRAY_PICKED_SOUND);
				Audio.play(REVEAL_BOUQUET_SOUND);
				Audio.playSoundMapOrSoundKeyWithDelay(TRAY_REVEAL_BAD_VO, 0.5f);
				trayAnimator.Play(trayFlowerRevealAnimation);
				yield return new TIWaitForSeconds(TRAY_REVEAL_ANIM_TIME);
				
				// Game over so show reveals now
				yield return StartCoroutine(revealRemainingSlots());

			}
			else
			{
				// play reveal audio
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND_KEY));
				}

				if (useAnimationsForGreyReveals)
				{
					if(trayFlowerRevealAnimationGrey != null)
					{
						trayAnimator.Play(trayFlowerRevealAnimationGrey);
					}
				}
				else
				{
					tray.grayOut(grayedOutRevealStyle);
					trayAnimator.Play(trayFlowerRevealAnimation);
				}
				yield return new TIWaitForSeconds(TRAY_REVEAL_ANIM_TIME);

			}
			
		}
		else
		{
			Bride01Tray tray = slot.GetComponent<Bride01Tray>();
			tray.hideObjectsForOutcome("CONTINUE");

			if (isPick)
			{
				Audio.play(TRAY_PICKED_SOUND);
				Audio.playMusic(REVEAL_INVITE_SOUND);
				Audio.switchMusicKey(DRESS_PICK_BG_MUSIC);

				trayAnimator.Play(trayRSVPRevealAnimation);
				yield return new TIWaitForSeconds(TRAY_REVEAL_ANIM_TIME);

				yield return StartCoroutine(revealRemainingSlots());

				yield return new TIWaitForSeconds(WAIT_BEFORE_TRAY_TO_DRESS_TRANSITION);
				// change back to the dress screen before rolling up the jackpot and credits
				changeStage(GameStageEnum.DRESS_PICK);

				//Hack to fix round3 animations when going into it the second time. Might have something to do with texture swapping in animations. 
				//Toggling the game object off and on again fizxes the texture swap being done in the animations
				GameObject go = stageGameObjects[(int)GameStageEnum.TRAY_PICK];
				go.transform.localPosition += new Vector3(-5000,0,0);
				go.SetActive(true);
				yield return null;
				go.SetActive(false);
				yield return null;
				go.transform.localPosition -= new Vector3(-5000,0,0);

				Audio.play(JACKPOT_VALUE_INCREASED_SOUND);
				
				// Play the character specific RSVP VO clip, delayed by 1.0s
				Audio.play(CHARACTER_FOUND_RSVP_CARD_VO_SOUNDS[(int)gameCharacter], 1.0f, 0.0f, 1.0f);
				
				currentJackpotAmount += pick.jackpotIncrease;
				jackpotAmountTextWrapper.text = CreditsEconomy.convertCredits(currentJackpotAmount);
				// pulsate the jackpot amount to draw the players eye
				StartCoroutine(pulsateJackpotAmount(3));
				
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + pick.credits));
				BonusGamePresenter.instance.currentPayout += pick.credits;
			}
			else
			{
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND_KEY));
				}

				if (useAnimationsForGreyReveals)
				{
					if(trayRSVPRevealAnimationGrey != null)
					{
						trayAnimator.Play(trayRSVPRevealAnimationGrey);
					}
				}
				else
				{
					tray.grayOut(grayedOutRevealStyle);
					trayAnimator.Play(trayRSVPRevealAnimation);
				}
				yield return new TIWaitForSeconds(TRAY_REVEAL_ANIM_TIME);

			}
		}
	}

	/// Pulsate the jackpot value to draw attention to it changing
	private IEnumerator pulsateJackpotAmount(int numPulses)
	{
		for (int i = 0; i < numPulses; ++i)
		{
			yield return new TITweenYieldInstruction(iTween.ScaleTo(jackpotAmountScaler, iTween.Hash("scale", new Vector3(1.2f, 1.2f, 1.0f), "time", JACKPOT_PULSATE_ONE_WAY_TIME, "islocal", true, "easetype", iTween.EaseType.linear)));
			yield return new TITweenYieldInstruction(iTween.ScaleTo(jackpotAmountScaler, iTween.Hash("scale", new Vector3(0.8f, 0.8f, 1.0f), "time", JACKPOT_PULSATE_ONE_WAY_TIME, "islocal", true, "easetype", iTween.EaseType.linear)));
		}
	}

	/// Reveal the remaining slots which haven't been shown yet
	private IEnumerator revealRemainingSlots()
	{
		revealWait.reset();

		switch (gameStage)
		{
			case GameStageEnum.STALL_PICK:
				yield return StartCoroutine(revealRemainingStalls());
				break;

			case GameStageEnum.DRESS_PICK:
				yield return StartCoroutine(revealRemainingDresses());
				break;

			case GameStageEnum.TRAY_PICK:
				yield return StartCoroutine(revealRemainingTrays());
				break;
		}
	}

	/// Reveal the remianing choices during the stall picking stage
	private IEnumerator revealRemainingStalls()
	{
		PickGameButtonDataList buttonList = roundButtonList[(int)GameStageEnum.STALL_PICK];

		for (int i = 0; i < stallCharacterReveals.Count; i++)
		{
			// skip the character they are getting
			if (stallCharacterReveals[i] != (int)gameCharacter)
			{
				for (int k = 0; k < buttonList.buttonList.Length; k++)
				{
					GameObject button = buttonList.buttonList[k];
					Bride01Stall stall = button.GetComponentInChildren<Bride01Stall>();

					// find stalls that haven't been revealed
					if (!stall.isRevealed)
					{
						StartCoroutine(revealStall(button, (GameCharacterEnum)stallCharacterReveals[i], false));
						break;
					}
				}

				yield return StartCoroutine(revealWait.wait(STALL_REVEAL_WAIT));
			}
		}
	}

	/// Reveal the remaining choices during the dress picking stage
	private IEnumerator revealRemainingDresses()
	{
		// copy the current list, since revealing will remove from it and we don't want it to screw up our iteration
		List<GameObject> remainingDressButtons = new List<GameObject>(pickmeButtonList[(int)GameStageEnum.DRESS_PICK]);
		
		for (int i = 0; i < remainingDressButtons.Count; i++)
		{
			// get the next reveal
			PickemPick reveal = pickemOutcome.getNextReveal();

			StartCoroutine(revealDress(remainingDressButtons[i], reveal, false));
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
		}
	}

	/// Reveal the remaining choices during the tray picking stage
	private IEnumerator revealRemainingTrays()
	{
		List<GameObject> remainingTrayButtons = new List<GameObject>(pickmeButtonList[(int)GameStageEnum.TRAY_PICK]);

		for (int i = 0; i < remainingTrayButtons.Count; i++)
		{
			PickemPick reveal = savedPick.getNextReveal();

			StartCoroutine(revealTray(remainingTrayButtons[i], reveal, false));
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
		}
	}
	
	/// Control what is shown based on what stage of the game the user is in
	private void changeStage(GameStageEnum newStage)
	{	
		if (gameStage == newStage)
		{
			return;
		}

		GameStageEnum prevStage = gameStage;
		gameStage = newStage;

		switch (gameStage)
		{
			case GameStageEnum.STALL_PICK:
				stageGameObjects[(int)GameStageEnum.STALL_PICK].SetActive(true);
				stageGameObjects[(int)GameStageEnum.DRESS_PICK].SetActive(false);
				stageGameObjects[(int)GameStageEnum.TRAY_PICK].SetActive(false);
				break;

			case GameStageEnum.DRESS_PICK:
				if (prevStage == GameStageEnum.TRAY_PICK)
				{
					// enable all the dress colliders that were disabled from the tray phase
					List<GameObject> dressButtons = pickmeButtonList[(int)GameStageEnum.DRESS_PICK];
					for (int i = 0; i < dressButtons.Count; i++)
					{
						CommonGameObject.setObjectCollidersEnabled(dressButtons[i], true);
					}

					// ensure that the tray screen is setup correctly if you get in there again, i.e. refresh and reset all the buttons
					pickmeButtonList[(int)GameStageEnum.TRAY_PICK].Clear();

					PickGameButtonDataList trayButtons = roundButtonList[(int)GameStageEnum.TRAY_PICK];
					for (int k = 0; k < trayButtons.buttonList.Length; ++k)
					{
						CommonGameObject.setObjectCollidersEnabled(trayButtons.buttonList[k], true);
						Animator buttonAnimator = trayButtons.buttonList[k].GetComponent<Animator>();
						if (GameState.game.keyName.Contains ("bride01"))
						{
							buttonAnimator.Play (TRAY_IDLE_ANIM);
						}
						pickmeButtonList[(int)GameStageEnum.TRAY_PICK].Add(trayButtons.buttonList[k]);
					}
				}
				else
				{
					// change the music right away, for coming back from the Tray stage the music will already be queued
					Audio.switchMusicKeyImmediate(DRESS_PICK_BG_MUSIC);
				}

				if(round2Characters == null || round2Characters.Length == 0)
				{
					if (gameCharacter != GameCharacterEnum.NONE && dressCharacterSprite.spriteName != CHARACTER_SPRITE_NAMES[(int)gameCharacter])
					{
						dressCharacterSprite.spriteName = CHARACTER_SPRITE_NAMES[(int)gameCharacter];
						dressCharacterSprite.MakePixelPerfect();
					}
				}
				else
				{
					round2Characters[(int)gameCharacter].SetActive(true);
				}

				stageGameObjects[(int)GameStageEnum.STALL_PICK].SetActive(false);
				stageGameObjects[(int)GameStageEnum.DRESS_PICK].SetActive(true);
				stageGameObjects[(int)GameStageEnum.TRAY_PICK].SetActive(false);
				
				// Dirty hack to support hiding/showing the dress round in elvira03
				if(hideDressStageWhenTrayIsVisible)
				{
					stageGameObjects[(int)GameStageEnum.DRESS_PICK].transform.position = dressStagePos;
				}
				break;

			case GameStageEnum.TRAY_PICK:
			{
				// disable all of the dress colliders that are still pickable so the user can't click them
				List<GameObject> dressButtons = pickmeButtonList[(int)GameStageEnum.DRESS_PICK];
				for (int i = 0; i < dressButtons.Count; i++)
				{
					CommonGameObject.setObjectCollidersEnabled(dressButtons[i], false);
				}

				// change the music
				Audio.switchMusicKeyImmediate(TRAY_PICK_BG_MUSIC);
				// intro VO
				Audio.playSoundMapOrSoundKey(TRAY_INTRO_VO);

				stageGameObjects[(int)GameStageEnum.STALL_PICK].SetActive(false);
				stageGameObjects[(int)GameStageEnum.DRESS_PICK].SetActive(true);
				stageGameObjects[(int)GameStageEnum.TRAY_PICK].SetActive(true);
				
				// Dirty hack to support hiding/showing the dress round in elvira03
				if(hideDressStageWhenTrayIsVisible)
				{
					stageGameObjects[(int)GameStageEnum.DRESS_PICK].transform.position = new Vector3(dressStagePos.x, dressStagePos.y, dressStagePos.z - 10000.0f);
				}
			}
			break;

			default:
				Debug.LogError("Trying to call changeStage() with unexpected GameStageEnum: " + newStage);
				break;
		}
	}
}

