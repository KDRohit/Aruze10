using UnityEngine;
using System.Collections;

/**
The wheel game for osa06 which also has a PickingGame compoment after the spin
*/
public class Osa06Wheel : WheelGameIntoPickingGame
{	
	[SerializeField] private Animator spinButtonAnimator = null;						// The animator for the button to switch to the win view
	[SerializeField] private GameObject[] progessiveContainers = new GameObject[5];		// Containers for the progressive, needed so if the character gets a character we can animate it to where it lives in the pick game
	[SerializeField] private GameObject jackpotLocationInPickem = null;					// Needed to know where to move the progressive container to
	[SerializeField] private GameObject slideInPickBackground = null;					// The pick background which slides in
	[SerializeField] private GameObject wheelObjToSlideIn = null;						// wheel object that slides in with the monkey attached
	[SerializeField] private GameObject monkeyObjToSlideOut = null;						// Monkey object that will continue off the top of the screen once the wheel is slid in
	[SerializeField] private GameObject wheelWinFrame = null;							// Game object containing the win frame, need this so I can shut it off since it doesn't fade correctly
	[SerializeField] private GameObject wheelWinText = null;							// Game object containing the win text, need this so I can shut it off since it doesn't fade correctly

	private bool isButtonAnimPlayed = false;		// Ensure the button animation is played regardless of if we click or spin

	private const float FINAL_DEGREE_ADJUST = 18.0f;		// adjustment needed to put the wedge in the middle of the pointer
	private const float TIME_TO_MOVE_PROGRESSIVE = 1.0f;	// amount of time it takes to move the progressive to where it will sit on the picking game

	private const float PAUSE_BEFORE_CHARACTER_SLIDE_IN = 0.25f;	// Add a sight pause so that all the characters can be seen sliding in
	private const float TIME_TO_SLIDE_IN_PROGRESSIVE = 0.25f;		// Stagger time between each progressive container coming on
	private const float TIME_TO_MOVE_WHEEL_ON = 1.5f;				// How fast to slide the wheel on
	private const float TIME_TO_MOVE_MONKEY_OFF = 0.45f;			// How fast the monkey flys away after the wheel is dropped
	private const float TIME_TO_FADE_OUT_WHEEL_STUFF = 1.0f;		// Time to fade out the wheel and the un-won progressives
	private const float TIME_BEFORE_SETTING_UP_PICK = 1.5f;			// The wait time before the pick game init stuff starts happening

	private const string WIN_CREDITS_SOUND = "WheelSpinRevealCredit";						// Sound for winning credits
	private const string WIN_CHARACTER_SOUND = "WheelSpinRevealCharacter";					// Sound for winning a character
	private const string CHARACTER_SLIDE_IN_SOUND = "WheelCharacterSwooshHauntedForest";	// Sound for character progressive sliding in
	private const string MONKEY_FLAP_SOUND = "MonkeyFlap1HauntedForest";					// Sound for the monkey flying on with the wheel and off again
	private const string WHEEL_CLICK_TO_SPIN_SOUND_KEY = "wheel_click_to_spin";				// Sound key for the click to spin sound

	private const string SPIN_BUTTON_CHANGE_TO_OUTCOME_ANIM = "OSA06_WheelBonus_SpinBT_animation"; // Animation name for the animation that changes the spin button to outcome view


	private readonly string[] WIN_CHARACTER_VOS = new string[5] { "wwlaugh03", "M1HauntedForest", "M3HauntedForest", "M4HauntedForest", "M2HauntedForest" };

	public override void init() 
	{
		StartCoroutine(doIntroAnimation());
		
		base.init();
	}

	/// Move the wheel onto the screen, then move the monkey off
	private IEnumerator doIntroAnimation()
	{
		// Move the wheel on
		Audio.play(MONKEY_FLAP_SOUND);
		yield return new TITweenYieldInstruction(iTween.MoveTo(wheelObjToSlideIn, iTween.Hash("y", 651, "time", TIME_TO_MOVE_WHEEL_ON, "islocal", true, "easetype", iTween.EaseType.linear)));

		// initialize the wheel again, now that it is on screen
		initSwipeableWheel();
		
		// Start the monkey moving off, but don't wait and play the progressive slide in at the same time
		Audio.play(MONKEY_FLAP_SOUND);
		iTween.MoveTo(monkeyObjToSlideOut, iTween.Hash("y", 900, "time", TIME_TO_MOVE_MONKEY_OFF, "islocal", true, "oncompletetarget", gameObject, "oncomplete", "hideMonkey", "easetype", iTween.EaseType.linear));

		// slide the progressives on
		StartCoroutine(moveProgressiveContainersOn());

		// Start the music now that the game is fully setup
		Audio.switchMusicKeyImmediate(Audio.soundMap(WHEEL_CLICK_TO_SPIN_SOUND_KEY));
	}

	/// Hide the monkey once it is off screen
	private void hideMonkey()
	{
		monkeyObjToSlideOut.SetActive(false);
	}

	/// Slides the progressive containers onto the screen
	private IEnumerator moveProgressiveContainersOn()
	{
		yield return new TIWaitForSeconds(PAUSE_BEFORE_CHARACTER_SLIDE_IN);

		for (int i = progessiveContainers.Length - 1; i >= 0; i--)
		{
			Audio.play(CHARACTER_SLIDE_IN_SOUND);
			yield return new TITweenYieldInstruction(iTween.MoveTo(progessiveContainers[i], iTween.Hash("x", 597, "time", TIME_TO_SLIDE_IN_PROGRESSIVE, "islocal", true, "easetype", iTween.EaseType.linear)));
		}
	}

	/// Get the final degrees of the wheel, virtual in case you need to apply an adjustment for your game
	protected override float getFinalSpinDegress()
	{
		return wheelPick.winIndex * degreesPerSlice - FINAL_DEGREE_ADJUST;
	}

	/// Coroutine for clicking the NGUI spin button, allows for elements that need timingß
	protected override IEnumerator onSpinClickedCoroutine()
	{
		spinButtonAnimator.Play(SPIN_BUTTON_CHANGE_TO_OUTCOME_ANIM);
		isButtonAnimPlayed = true;

		yield return StartCoroutine(startSpinFromClickCoroutine());
	}

	/// Handles changing out what is visible after the spin button has been pressed
	protected override IEnumerator processSpin()
	{
		if (!isButtonAnimPlayed)
		{
			spinButtonAnimator.Play(SPIN_BUTTON_CHANGE_TO_OUTCOME_ANIM);
			isButtonAnimPlayed = true;
		}

		yield return StartCoroutine(base.processSpin());
	}

	/// Coroutine version of onWheelSpinComplete callback so we can handle timing stuff
	protected override IEnumerator onWheelSpinCompleteCoroutine()
	{
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

			Osa06WheelPickingGame.Osa06WheelCharacterEnum selectedCharacter = Osa06WheelPickingGame.Osa06WheelCharacterEnum.None;
			
			//Audio.play("BonusTaDaSATC");
			switch (wheelPick.wins[wheelPick.winIndex].bonusGame)
			{
				case "osa06_common_pickem_5":
					selectedCharacter = Osa06WheelPickingGame.Osa06WheelCharacterEnum.WickedWitch;
					break;
				case "osa06_common_pickem_4":
					selectedCharacter = Osa06WheelPickingGame.Osa06WheelCharacterEnum.Dorothy;
					break;
				case "osa06_common_pickem_2":
					selectedCharacter = Osa06WheelPickingGame.Osa06WheelCharacterEnum.TinMan;
					break;
				case "osa06_common_pickem_1":
					selectedCharacter = Osa06WheelPickingGame.Osa06WheelCharacterEnum.Lion;
					break;
				case "osa06_common_pickem_3":
					selectedCharacter = Osa06WheelPickingGame.Osa06WheelCharacterEnum.Scarecrow;
					break;
			}

			Audio.play(WIN_CHARACTER_SOUND);
			yield return StartCoroutine(showWinSliceAnimation());

			Audio.play(WIN_CHARACTER_VOS[(int)selectedCharacter]);
			

			if (progressivePoolEffects.Length > 0)
			{
				CommonGameObject.parentsFirstSetActive(progressivePoolEffects[(int)selectedCharacter].gameObject, true);
			}
			
			yield return new TIWaitForSeconds(TIME_BEFORE_SETTING_UP_PICK);

			Osa06WheelPickingGame osa06PickingGame = pickingGame as Osa06WheelPickingGame;
			// setup the game to use the selected character
			osa06PickingGame.setSelectedCharacter(selectedCharacter);
			// Set the jackpot amount
			osa06PickingGame.setJackpotText(progressivePoolValues[(int)selectedCharacter]);

			// Fade out the wheel and un-won progressives
			yield return StartCoroutine(fadeOutWheelAndNonWonProgressives(selectedCharacter));

			// move the progressive to where it will sit during the picking game
			GameObject progressiveToMove = progessiveContainers[(int)selectedCharacter];
			Vector3 currentPos = progressiveToMove.transform.localPosition;
			// make sure that this progressive goes over everything as it moves into position
			progressiveToMove.transform.localPosition = new Vector3(currentPos.x, currentPos.y, -7);

			Vector3 moveToPos = jackpotLocationInPickem.transform.position;
			moveToPos.z = progressiveToMove.transform.position.z;

			yield return new TITweenYieldInstruction(iTween.MoveTo(progressiveToMove, iTween.Hash("position", moveToPos, "time", TIME_TO_MOVE_PROGRESSIVE, "islocal", false, "easetype", iTween.EaseType.linear)));

			// now slide in the fake picking background
			slideInPickBackground.SetActive(true);
			yield return new TITweenYieldInstruction(iTween.MoveTo(slideInPickBackground, iTween.Hash("y", 0, "time", TIME_TO_MOVE_PROGRESSIVE, "islocal", true, "easetype", iTween.EaseType.linear)));
			slideInPickBackground.SetActive(false);

			// transition into the PickingGame
			transitionToPickingGame();
		}
	}

	/// Fade out the wheel and non-selected progressives
	private IEnumerator fadeOutWheelAndNonWonProgressives(Osa06WheelPickingGame.Osa06WheelCharacterEnum selectedCharacter)
	{
		float elapsedTime = 0;

		// hide the couple objects that have issues fading
		wheelWinFrame.SetActive(false);
		wheelWinText.SetActive(false);

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

		// Force the wheel to be fully alpha'd out
		CommonGameObject.alphaGameObject(wheelObjToSlideIn, 0.0f);
		NGUIExt.fadeGameObject(wheelObjToSlideIn, 0.0f);

		wheelObjToSlideIn.SetActive(false);

		// Force the progressives to be fully alpha'd out
		for (int i = 0; i < progessiveContainers.Length; i++)
		{
			if (i != (int)selectedCharacter)
			{
				CommonGameObject.alphaGameObject(progessiveContainers[i], 0.0f);
				NGUIExt.fadeGameObject(progessiveContainers[i], 0.0f);
			}
		}
	}
}
