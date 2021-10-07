using UnityEngine;
using System.Collections;

/**
The wheel game for osa06 which also has a PickingGame compoment after the spin
*/
public class gen09WheelTikiGame : WheelGameIntoPickingGame
{
	// Tunables
	
	[SerializeField] private float WAIT_TO_SHOW_MOON = 1.0f;        // Adjust this to match the animation with the sound.
	[SerializeField] private float WAIT_FOR_MOON = 5.0f;            // Wait for the moon to finish bouncing around the screen.
	[SerializeField] private float WAIT_FOR_INTRO_SPACESHIP = 1.0f; // Wait for the intro spaceship to finish its loop-to-loop.
	
	[SerializeField] private float DIST_TO_MOVE_WHEEL_ON = 500.0f;          // Slide the wheel from below the screen to onscreen.
	[SerializeField] private float TIME_TO_MOVE_WHEEL_ON = 1.5f;            // How fast to slide the wheel on.
	[SerializeField] private float WAIT_FOR_WHEEL = 2.0f;                   // Total wait time for the wheel spaceship.
	[SerializeField] private float WAIT_TO_LAUNCH_AMBIENT_SPACESHIP = 2.0f; // Wait to launch the ambient spaceship.
	
	[SerializeField] private float WAIT_TO_FLASH_DUR = 2.0f;   // Wait this long between flashes.
	[SerializeField] private float FADE_IN_FLASH_DUR = 1.0f;   // How long it takes to fade in the flash.
	[SerializeField] private float MAX_FLASH_ALPHA = 0.8f;     // How opaque the flash gets.
	[SerializeField] private float SHOW_FLASH_DUR = 0.1f;      // How long to stay at max opacity.
	[SerializeField] private float FADE_OUT_FLASH_DUR = 1.0f;  // How long it takes to fade out the flash.
	
	[SerializeField] private float PAUSE_BEFORE_CHARACTER_SLIDE_IN = 0.25f; // Add a sight pause so that all the characters can be seen sliding in
	[SerializeField] private float TIME_TO_SLIDE_IN_PROGRESSIVE = 0.25f;    // Stagger time between each progressive container coming on
	[SerializeField] private float PROGRESSIVE_END_X = 400.0f;

	[SerializeField] private float FINAL_DEGREE_ADJUST = 18.0f; // Adjustment needed to put the wedge in the middle of the pointer

	[SerializeField] private float TIME_BEFORE_SETTING_UP_PICK = 1.5f;  // The wait time before the pick game init stuff starts happening
	[SerializeField] private float TIME_TO_MOVE_PROGRESSIVE = 1.0f;     // amount of time it takes to move the progressive to where it will sit on the picking game
	[SerializeField] private float TIME_TO_FADE_OUT_WHEEL_STUFF = 1.0f; // Time to fade out the wheel and the un-won progressives

	// Game Objects
	
	[SerializeField] private Animator moon = null;             // The moon that bounces around the screen.
	[SerializeField] private Animator introSpaceship = null;   // Spaceship that does the loop-to-loop.
	[SerializeField] private Animator wheelSpaceship = null;   // Spaceship that pushes the wheel into the screen.
	[SerializeField] private Animator ambientSpaceship = null; // Spaceship that flies in the background.
	
	[SerializeField] private GameObject wheelObjToSlideIn = null;                   // Wheel object that slides in with the spaceship.
	[SerializeField] private GameObject[] progessiveContainers = new GameObject[5]; // Containers for the progressive, needed so if the player gets a character we can animate it to where it lives in the pick game.
	
	[SerializeField] private Animator spinButtonAnimator = null;    // The animator for the button to switch to the win view
	[SerializeField] private UISprite spinButtonFlashSprite = null; // Manually flash the spin button highlight.
	
	[SerializeField] private GameObject jackpotLocationInPickem = null;          // Needed to know where to move the progressive container to.
	[SerializeField] private GameObject slideInPickBackground = null;            // The pick background which slides in
	[SerializeField] private GameObject wheelWinFrame = null;                    // Game object containing the win frame, need this so I can shut it off since it doesn't fade correctly.
	[SerializeField] private GameObject wheelWinText = null;                     // Game object containing the win text, need this so I can shut it off since it doesn't fade correctly.
	[SerializeField] private Animator[] winCharacterAnimators = new Animator[5]; // Animate the winning head on the wheel.

	// Variables
	
	private bool isButtonAnimPlayed = false; // Ensure the button animation is played regardless of if we click or spin
	private long baseGameMultiplierAtWheelStart = 0; // Added debug info to try and track down an issue where jackpot values aren't matching what was displayed during the wheel
	
	// Constants

	private const string SPIN_BUTTON_CHANGE_TO_OUTCOME_ANIM = "SpinButton_Ani"; // Animation name for the animation that changes the spin button to outcome view
	
	private const string WHEEL_BG_MUSIC = "TransitionToWheelTiki";
	private const string CHARACTER_SLIDE_IN_SOUND = "WheelCharacterSwooshTiki"; // Sound for character progressive sliding in
	private const string WHEEL_CLICK_TO_SPIN_SOUND_KEY = "wheel_click_to_spin"; // Sound key for the click to spin sound
	private const string WHEEL_SLOWS_MUSIC_SOUND_KEY = "wheel_slows_music";     // Sound key when the wheel slows down.
	private const string WHEEL_STOPS_SOUND = "wheel_stops";                     // Sound key when the wheel stops.
	private const string WIN_CREDITS_SOUND = "WheelSpinRevealCreditTiki";       // Sound for winning credits
	private const string WIN_CHARACTER_SOUND = "WheelSpinRevealCharacterTiki";  // Sound for winning a character
	
	private readonly string[] WIN_CHARACTER_ANIM_NAMES = new string[5]
	{
		"Chief_Ani",
		"Larry_ani",
		"Biff_Ani",
		"todd_Ani",
		"heathcliff_Ani"
	};
	private readonly string[] WIN_CHARACTER_VOS = new string[5]
	{
		"M1Tiki",
		"M1Tiki",
		"M3Tiki",
		"M4Tiki",
		"M2Tiki"
	};
	
	private const string WHEEL_TO_PICKEM_SOUND = "WheelTransition2PickTiki";
	private const string PICK_SCROLL_SOUND = "PickScrollOnTiki";

/*==========================================================================================================*\
	Init
\*==========================================================================================================*/

	public override void init() 
	{
		StartCoroutine(doIntroAnimation());
		base.init();
		baseGameMultiplierAtWheelStart = SlotBaseGame.instance.multiplier;
	}

	private IEnumerator doIntroAnimation()
	{
		string VOLCANO_LOOP_SOUND = "VolcanoLoop";
		Audio.play(VOLCANO_LOOP_SOUND);
		
		// Wait for the moon.
		
		yield return new WaitForSeconds(WAIT_TO_SHOW_MOON);
		moon.gameObject.SetActive(true);
		yield return new WaitForSeconds(WAIT_FOR_MOON);
		
		// Launch the intro spaceship.

		introSpaceship.gameObject.SetActive(true);
		yield return new WaitForSeconds(WAIT_FOR_INTRO_SPACESHIP);
		
		// Slide the progressives on.
		
		StartCoroutine(moveProgressiveContainersOn());
		
		// The wheel spaceship pulls the wheel into the screen.

		wheelSpaceship.gameObject.SetActive(true);
		
		iTween.MoveBy(
			wheelSpaceship.gameObject,
			iTween.Hash(
				"y", DIST_TO_MOVE_WHEEL_ON,
				"time", TIME_TO_MOVE_WHEEL_ON,
				"islocal", true,
				"easetype", iTween.EaseType.linear));
		
		iTween.MoveBy(
			wheelObjToSlideIn,
			iTween.Hash(
				"y", DIST_TO_MOVE_WHEEL_ON,
				"time", TIME_TO_MOVE_WHEEL_ON,
				"islocal", true,
				"easetype", iTween.EaseType.linear));
		
		yield return new WaitForSeconds(WAIT_FOR_WHEEL);

		// initialize the wheel again, now that it is on screen
		initSwipeableWheel();

		StartCoroutine("animateSpinButtonFlash");

		// Launch the ambient spaceship in the background.
		
		yield return new WaitForSeconds(WAIT_TO_LAUNCH_AMBIENT_SPACESHIP);
		ambientSpaceship.gameObject.SetActive(true);

		Audio.switchMusicKeyImmediate(Audio.soundMap(WHEEL_CLICK_TO_SPIN_SOUND_KEY));
	}

	private IEnumerator moveProgressiveContainersOn()
	{
		yield return new WaitForSeconds(PAUSE_BEFORE_CHARACTER_SLIDE_IN);
		
		for (int i = progessiveContainers.Length - 1; i >= 0; i--)
		{
			Audio.play(CHARACTER_SLIDE_IN_SOUND);
			
			yield return new TITweenYieldInstruction(
				iTween.MoveTo(
				progessiveContainers[i],
				iTween.Hash(
					"x", PROGRESSIVE_END_X,
					"time", TIME_TO_SLIDE_IN_PROGRESSIVE,
					"islocal", true,
					"easetype", iTween.EaseType.linear)));
		}
	}
	
	private IEnumerator animateSpinButtonFlash()
	{
		spinButtonFlashSprite.gameObject.SetActive(true);
		spinButtonFlashSprite.alpha = 0.0f;
		
		while ( true )
		{
			yield return new WaitForSeconds(WAIT_TO_FLASH_DUR);
			
			iTween.ValueTo(
				gameObject,
				iTween.Hash(
				"from", 0.0f,
				"to", MAX_FLASH_ALPHA,
				"time", FADE_IN_FLASH_DUR,
				"onupdate", "updateSpinButtonFlashAlpha"));
			yield return new WaitForSeconds(FADE_IN_FLASH_DUR);
			
			yield return new WaitForSeconds(SHOW_FLASH_DUR);
			
			iTween.ValueTo(
				gameObject,
				iTween.Hash(
				"from", MAX_FLASH_ALPHA,
				"to", 0.0f,
				"time", FADE_OUT_FLASH_DUR,
				"onupdate", "updateSpinButtonFlashAlpha"));
			yield return new WaitForSeconds(FADE_OUT_FLASH_DUR);
		}
	}
	
	private void updateSpinButtonFlashAlpha(float alpha)
	{
		spinButtonFlashSprite.alpha = alpha;
	}

/*==========================================================================================================*\
	On Spin Clicked Coroutine
\*==========================================================================================================*/

	protected override IEnumerator onSpinClickedCoroutine()
	{
		Audio.switchMusicKeyImmediate(Audio.soundMap(WHEEL_SLOWS_MUSIC_SOUND_KEY));

		stopFlashingSpinButton();

		spinButtonAnimator.Play(SPIN_BUTTON_CHANGE_TO_OUTCOME_ANIM);
		isButtonAnimPlayed = true;
		
		yield return StartCoroutine(startSpinFromClickCoroutine());
	}

/*==========================================================================================================*\
	Spin
\*==========================================================================================================*/
	
	protected override IEnumerator processSpin()
	{
		if (!isButtonAnimPlayed)
		{
			spinButtonAnimator.Play(SPIN_BUTTON_CHANGE_TO_OUTCOME_ANIM);
			isButtonAnimPlayed = true;
		}
		
		yield return StartCoroutine(base.processSpin());
	}

	protected override float getFinalSpinDegress()
	{
		return wheelPick.winIndex * degreesPerSlice - FINAL_DEGREE_ADJUST;
	}
	
/*==========================================================================================================*\
	On Wheel Spin Complete Coroutine
\*==========================================================================================================*/
	
	protected override IEnumerator onWheelSpinCompleteCoroutine()
	{
		Audio.play(Audio.soundMap(WHEEL_STOPS_SOUND));
		
		long _payout = wheelPick.wins[wheelPick.winIndex].credits;
		
		if (_payout > 0)
		{
			Audio.play(WIN_CREDITS_SOUND);			
			StartCoroutine(rollupAndEnd());
		}
		else
		{
			SlotOutcome pickemGame = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame);
			pickemOutcome = new PickemOutcome(pickemGame);
			
			gen09PickemTikiGame.CharacterEnum selectedCharacter = gen09PickemTikiGame.CharacterEnum.None;
			
			switch (wheelPick.wins[wheelPick.winIndex].bonusGame)
			{
				case "gen09_common_pickem_5":
				case "gen39_common_pickem_5":
					selectedCharacter = gen09PickemTikiGame.CharacterEnum.WickedWitch;
					break;
					
				case "gen09_common_pickem_4":
				case "gen39_common_pickem_4":
					selectedCharacter = gen09PickemTikiGame.CharacterEnum.Dorothy;
					break;
					
				case "gen09_common_pickem_2":
				case "gen39_common_pickem_2":
					selectedCharacter = gen09PickemTikiGame.CharacterEnum.TinMan;
					break;
					
				case "gen09_common_pickem_1":
				case "gen39_common_pickem_1":
					selectedCharacter = gen09PickemTikiGame.CharacterEnum.Lion;
					break;
					
				case "gen09_common_pickem_3":
				case "gen39_common_pickem_3":
					selectedCharacter = gen09PickemTikiGame.CharacterEnum.Scarecrow;
					break;
			}
			
			Audio.play(WIN_CHARACTER_SOUND);
			winCharacterAnimators[(int)selectedCharacter].Play(WIN_CHARACTER_ANIM_NAMES[(int)selectedCharacter]);
			yield return StartCoroutine(showWinSliceAnimation());
			
			Audio.play(WIN_CHARACTER_VOS[(int)selectedCharacter]);
			
			if (progressivePoolEffects.Length > 0)
			{
				CommonGameObject.parentsFirstSetActive(progressivePoolEffects[(int)selectedCharacter].gameObject, true);
			}
			
			yield return new TIWaitForSeconds(TIME_BEFORE_SETTING_UP_PICK);

			Audio.play(WHEEL_TO_PICKEM_SOUND);
						
			gen09PickemTikiGame tikiPickingGame = pickingGame as gen09PickemTikiGame;
			tikiPickingGame.setSelectedCharacter(selectedCharacter);			
			tikiPickingGame.setJackpotText(progressivePoolValues[(int)selectedCharacter], baseGameMultiplierAtWheelStart);
			
			// Fade out the wheel and un-won progressives.
			
			yield return StartCoroutine(fadeOutWheelAndNonWonProgressives(selectedCharacter));
			
			// Move the progressive to where it will sit during the picking game.
			
			GameObject progressiveToMove = progessiveContainers[(int)selectedCharacter];
			Vector3 currentPos = progressiveToMove.transform.localPosition;
			
			// Make sure that this progressive goes over everything as it moves into position.
			
			progressiveToMove.transform.localPosition = new Vector3(currentPos.x, currentPos.y, -7);
			
			Vector3 moveToPos = jackpotLocationInPickem.transform.position;
			moveToPos.z = progressiveToMove.transform.position.z;
			
			yield return new TITweenYieldInstruction(
				iTween.MoveTo(
					progressiveToMove,
					iTween.Hash(
						"position", moveToPos,
						"time", TIME_TO_MOVE_PROGRESSIVE,
						"islocal", false,
						"easetype", iTween.EaseType.linear)));
			
			// Now slide in the fake picking background.
			
			slideInPickBackground.SetActive(true);
			Audio.play(PICK_SCROLL_SOUND);
			           
			yield return new TITweenYieldInstruction(
				iTween.MoveTo(
					slideInPickBackground,
					iTween.Hash("y", 0,
					"time", TIME_TO_MOVE_PROGRESSIVE,
					"islocal", true,
					"easetype", iTween.EaseType.linear)));
					
			slideInPickBackground.SetActive(false);
			
			// Transition into the PickingGame.
			
			transitionToPickingGame();
		}
	}

	private IEnumerator fadeOutWheelAndNonWonProgressives(gen09PickemTikiGame.CharacterEnum selectedCharacter)
	{
		float elapsedTime = 0;
		
		// Hide the couple objects that have issues fading.
		
		wheelWinFrame.SetActive(false);
		wheelWinText.SetActive(false);

		// the spin button is transparent right now. Let's deactivate it so that it doesn't appear
		// when we start fading the Wheel object.
		spinButton.gameObject.SetActive(false);
		
		while (elapsedTime < TIME_TO_FADE_OUT_WHEEL_STUFF)
		{
			elapsedTime += Time.deltaTime;
			CommonGameObject.alphaGameObject(wheelObjToSlideIn, 1 - (elapsedTime / TIME_TO_FADE_OUT_WHEEL_STUFF));
			NGUIExt.fadeGameObject(wheelObjToSlideIn, 1 - (elapsedTime / TIME_TO_FADE_OUT_WHEEL_STUFF));
			
			for (int i = 0; i < progessiveContainers.Length; i++)
			{
				if (i != (int)selectedCharacter)
				{
					CommonGameObject.alphaGameObject(progessiveContainers[i], 1 - (elapsedTime / TIME_TO_FADE_OUT_WHEEL_STUFF));
					NGUIExt.fadeGameObject(progessiveContainers[i], 1 - (elapsedTime / TIME_TO_FADE_OUT_WHEEL_STUFF));
				}
			}
			
			yield return null;
		}
		
		// Force the wheel to be fully alpha'd out.
		
		CommonGameObject.alphaGameObject(wheelObjToSlideIn, 0.0f);
		NGUIExt.fadeGameObject(wheelObjToSlideIn, 0.0f);
		
		wheelObjToSlideIn.SetActive(false);
		
		// Force the progressives to be fully alpha'd out.
		
		for (int i = 0; i < progessiveContainers.Length; i++)
		{
			if (i != (int)selectedCharacter)
			{
				CommonGameObject.alphaGameObject(progessiveContainers[i], 0.0f);
				NGUIExt.fadeGameObject(progessiveContainers[i], 0.0f);
			}
		}
	}

/*==========================================================================================================*/

	protected void stopFlashingSpinButton()
	{
		StopCoroutine("animateSpinButtonFlash");
		spinButtonFlashSprite.alpha = 0.0f;
		spinButtonFlashSprite.gameObject.SetActive(false);
	}

	protected override void onSwipeStart()
	{
		stopFlashingSpinButton();
		base.onSwipeStart();
	}
}
