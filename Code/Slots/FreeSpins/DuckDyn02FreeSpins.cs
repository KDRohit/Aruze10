using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
DuckDyn03 has 3 wild banners that can all appear in the center 3 reels
*/
public class DuckDyn02FreeSpins : FreeSpinGame
{
	// Intro Variables
	[SerializeField] private GameObject introGame = null;						// Game object for the entire intro game
	[SerializeField] private GameObject bkgFillerObj = null;					// The filler object that hides the exploded part
	[SerializeField] private GameObject dynamiteObject = null;					// Dynamite game object that hides when the explosition happens
	[SerializeField] private GameObject fireParticles = null;					// Fire particles that start after intro
	[SerializeField] private UISpriteAnimator explosionAnimator = null;			// Contains the controls for the explosion animation
	[SerializeField] private GameObject[] majorSymbolReveals = null;			// The reveals for the major symbols
	[SerializeField] private GameObject instructionText = null;					// Instruction text which I'll hide when a detonator is choosen
	[SerializeField] private DuckDyn02FreeSpinDetonator[] detonatorList = null;	// List of detonators, used to randomly select one to play the pick me anim on
	[SerializeField] private Material[] revealMaterials = null;					// Material list for major symbols to apply to reveal objects

	private bool isIntroFinished = false;										// Tells if the intro game is finished yet and free spins can start
	private bool introInputEnabled = true;										// Tells if the game buttons are currently accepting input
	private CoroutineRepeater pickMeController;									// Class to call the pickme animation on a loop
	private SkippableWait revealWait = new SkippableWait();						// Class for handling reveals that can be skipped

	private GameTypeEnum gameType = GameTypeEnum.NONE;							// Tracks what Major symbol is the featured one for this free spin instance

	private const float MIN_TIME_PICKME = 1.5f;						// Minimum time an animation might take to play next
	private const float MAX_TIME_PICKME = 2.5f;						// Maximum time an animation might take to play next
	private const float REVEAL_FEATURED_MAJOR_SYMBOL_TIME = 1.0f;	// Time the revealed major is shown before transitioning to the spinning phase
	private const float REVEAL_WAIT_AFTER_EXPLOSION_TIME = 0.3f;	// Time to wait after starting the explosion before showing the major symbol
	private const float TIME_BEFORE_REVEALS_START = 0.5f;			// Introduce a slight delay before the reveals start, since they feel like they happen too fast after the explosion
	private const float TIME_BETWEEN_REVEALS = 0.4f;				// The amount of time to wait between reveals.

	private const string PICK_DETONATOR_BG_MUSIC = "PickADetonatorBg";			// Looped music for the detonator portion of the game
	private const string DETONATOR_PICK_INTRO_VO = "JRTheSuspenseIsKillinMe";	// Intro voice over for the detonator pick
	private const string DETONATOR_PICK_ME_SOUND = "PickMeDetonator";			// Sound for hte pick me animation of the detonators
	private const string EXPLOSION_VO = "SiBaWhoom02";							// Voice over played after the explosion
	private const string FREESPIN_START_VO = "FreespinIntroVODuck02";			// Voice over to play at the start of the free spin portion

	// An easy way to keep track of what we should be revealing.
	public enum GameTypeEnum
	{
		NONE = -1,
		M1 = 0,
		M2 = 1,
		M3 = 2,
		M4 = 3,
	}

	public override void initFreespins()
	{
		base.initFreespins();

		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, introPickMeAnimCallback);

		BonusGameManager.instance.wings.forceShowNormalWings(true);
		SpinPanel.instance.showSideInfo(false);

		if (BonusGameManager.instance.bonusGameName.Contains("_m1"))
		{
			gameType = GameTypeEnum.M1;
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_m2"))
		{
			gameType = GameTypeEnum.M2;	
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_m3"))
		{
			gameType = GameTypeEnum.M3;
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_m4"))
		{
			gameType = GameTypeEnum.M4;
		}
		else
		{
			gameType = GameTypeEnum.NONE;
			Debug.LogError("There was an unexpected format for the name of the DuckDyn02FreeSpins game, don't know what symbol to reveal for " +
							BonusGameManager.instance.bonusGameName);
		}

		playLoopedMusic(PICK_DETONATOR_BG_MUSIC);
		Audio.play(DETONATOR_PICK_INTRO_VO);
	}

	/// Overriding the update method so we can handle the pick me animations
	protected override void Update()
	{
		// Play the pickme animation.
		if (introInputEnabled)
		{
			if (pickMeController != null)
			{
				pickMeController.update();
			}
		}

		base.Update();
	}

	/// Going to launch the intro pick from here
	protected override void startGame()
	{
		// Going to play an intro pick game before the spins start
		_didStart = true;		
		StartCoroutine(playIntroPick());
	}

	/// Handle the intro pick for the free spin game. Spins don't start until this finishes
	private IEnumerator playIntroPick()
	{
		// Wait for the loading screen to disappear before starting spins.
		while (Loading.isLoading)
		{
			yield return null;
		}

		// Wait for the intro to finish before starting free spins
		while (!isIntroFinished)
		{
			yield return null;
		}

		// hide the intro and show the free spin reel game
		introGame.SetActive(false);

		// Turn on the UI elements and hide the wings used for this intro stage
		SpinPanel.instance.showSideInfo(true);
		fireParticles.SetActive(true);
		BonusGameManager.instance.wings.hide();
		SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
		yield return null;
		hasFreespinGameStarted = true;
		Audio.play(FREESPIN_START_VO);

		// Start the free spins now that the picks are over
		StartCoroutine(startAnimationAndSpin());
	}

	/// Callback for one of the detonators being picked in the intro part of the game
	public void pickemButtonPressed(GameObject buttonObj)
	{
		if (introInputEnabled) 
		{
			StartCoroutine(pickemButtonPressedCoroutine(buttonObj));
		}
	}

	/// Coroutine called when a button is pressed, used to handle timing stuff that may need to happen
	private IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		introInputEnabled = false;

		instructionText.SetActive(false);

		DuckDyn02FreeSpinDetonator detonator = buttonObj.GetComponent<DuckDyn02FreeSpinDetonator>();

		// gray out the unpicked detonators now
		for (int k = 0; k < detonatorList.Length; ++k)
		{
			if (detonatorList[k] != detonator)
			{
				detonatorList[k].grayOut();
			}
		}

		// play the fuse
		yield return StartCoroutine(detonator.playDetonatorAnimations());

		dynamiteObject.SetActive(false);

		// play the explosion
		StartCoroutine(explosionAnimator.play());

		yield return new TIWaitForSeconds(REVEAL_WAIT_AFTER_EXPLOSION_TIME);

		// hide the filler to show the explosion hole
		bkgFillerObj.SetActive(false);

		// show the major symbol that is featured
		if (gameType != GameTypeEnum.NONE)
		{
			majorSymbolReveals[(int)gameType].SetActive(true);
		}

		Audio.play(EXPLOSION_VO);

		yield return new TIWaitForSeconds(TIME_BEFORE_REVEALS_START);

		yield return StartCoroutine(revealOthers());

		yield return new TIWaitForSeconds(REVEAL_FEATURED_MAJOR_SYMBOL_TIME);

		isIntroFinished = true;
	}

	/// Pick me animation player
	private IEnumerator introPickMeAnimCallback()
	{
		// Get one of the available detonators
		int randomDetonatorIndex = Random.Range(0, detonatorList.Length);
		DuckDyn02FreeSpinDetonator detonator = detonatorList[randomDetonatorIndex];

		// Play the animation
		Audio.play(DETONATOR_PICK_ME_SOUND);
		yield return StartCoroutine(detonator.playPickAnimation());
	}

	/// Play looped music
	private static void playLoopedMusic(string musicKey)
	{
		Audio.switchMusicKeyImmediate(musicKey);
	}

	// reveal the rest of the choices
	private IEnumerator revealOthers()
	{
		for (int i = 0; i < revealMaterials.Length; i++)
		{
			if (i != (int)gameType)
			{
				// find an open slot
				for (int k = 0; k < detonatorList.Length; ++k)
				{
					DuckDyn02FreeSpinDetonator detonator = detonatorList[k];

					if (!detonator.isRevealed)
					{
						yield return StartCoroutine(detonator.playRevealAnimation(revealMaterials[i]));
						yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
						break;
					}
				}
			}
		}
	}
}
