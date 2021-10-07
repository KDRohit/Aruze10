using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class osa02FreeSpins : PickMajorFreeSpins
{
	public GameObject symbolCamera;
	[SerializeField] private GameObject backgrounds;
	[SerializeField] private ReelGameBackground backgroundScript;			// The script that holds onto the wings.
	private bool isSlideComplete = false;
	private int revealPosition = 0;
	// Constants
	private const float TIME_FADE_IN_SYMBOLS = 0.5f;
	private const float TRANSITION_SLIDE_TIME = 2.5f;
	private const float BACKGROUNDS_START_POS = -8.2f;
	private const float BACKGROUNDS_END_POS = 0.0f;
	private const float SPRITE_GROW_TIME = 1.0f;
	// Sprite names
	private readonly string[] REVEAL_SPRITE_NAMES = { "4star", "2star", "3star", "1star" };
	private readonly string[] SHOW_SPRITE_SOUNDS = { "RBHowDoYouDo",  "MHWasItYou", "JGHowDoYouDo", "ToToArfArfDontBeSillyToto" };
	// Sound names
	private const string PICK_ME_SOUND = "CrowCawPickMe";
	private const string SHOW_PICK_SOUND = "PickACrowCawFlap";
	private const string BG_INTRO = "IntroFreespinOSA02";
	private const string BG_MUSIC = "FreespinOSA02";
	private const string M1_REVEAL_SOUND = "FreespinSymbolInitOSA022";
	private const string DEFAULT_REVEAL_SOUND = "PickACrowRevealCharacter";
	private const string CROW_SOUND = "RevealSparkly";
	private const string FREESPIN_MAP_SOUND = "freespin";
	private const string FREESPIN_INTRO_MAP_SOUND = "freespinintro";
	private const string SUMMARY_SOUND = "SummaryVOOSA02";
	// Animation names
	private const string PICK_ME_ANIMATION = "osa02_FreeSpin_CrowPick_PickingObject_PickMe";
	private const string SHOW_PICK_ANIMATION = "osa02_FreeSpin_CrowPick_PickingObject_Reveal";
	private const string END_PICK_ANIMATION = "osa02_FreeSpin_CrowPick_PickingObject_End";

	public override void initFreespins()
	{
		cameFromTransition = true;
		base.initFreespins();
		// Switch the symbols to fill up the reel based off what was picked.
		string majorSymbolName = "M" + (int)(stageType + 1);
		// Need to do this weird flip here because of how the data gets sent down.
		if (majorSymbolName == "M2")
		{
			majorSymbolName = "M3";
		}
		else if (majorSymbolName == "M3")
		{
			majorSymbolName = "M2";
		}
		foreach (SlotSymbol symbol in engine.getVisibleSymbolsAt(0))
		{
			symbol.mutateTo(majorSymbolName, null, false, true);
		}
		foreach (SlotSymbol symbol in engine.getVisibleSymbolsAt(4))
		{
			symbol.mutateTo(majorSymbolName, null, false, true);
		}
		engine.getVisibleSymbolsAt(1)[0].mutateTo(majorSymbolName + "-4A-3A", null, false, true);

		// Fade out the symbols
		SlotReel[] reelArray = engine.getReelArray();

		foreach (SlotReel reel in reelArray)
		{
			foreach (SlotSymbol slotSymbol in reel.symbolList)
			{
				if (slotSymbol.animator != null)
				{
					RoutineRunner.instance.StartCoroutine(slotSymbol.animator.fadeSymbolOutOverTime(0));
				}
			}
		}
		symbolCamera.SetActive(false);
		if (backgroundScript != null)
		{
			BonusGameManager.instance.wings.hide();
			if (backgroundScript.wings != null)
			{
				backgroundScript.wings.transform.parent = transform;
				backgroundScript.setWingsTo(ReelGameBackground.WingTypeOverrideEnum.Fullscreen);
			}
		}
	}

	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available knocker game objects
		int randomIndex = Random.Range(0, buttonSelections.Count);
		GameObject button = buttonSelections[randomIndex];

		// Start the animation
		Animator crowAnimator = button.GetComponent<Animator>();
		if (crowAnimator != null)
		{
			Audio.play(PICK_ME_SOUND);
			crowAnimator.Play(PICK_ME_ANIMATION);
			// Wait for the animation to start
			while (!crowAnimator.GetCurrentAnimatorStateInfo(0).IsName(PICK_ME_ANIMATION))
			{
				yield return null;
			}
			// Wait for the animation to stop.
			while (crowAnimator.GetCurrentAnimatorStateInfo(0).IsName(PICK_ME_ANIMATION))
			{
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning("There was no animator attached to crow." + randomIndex);
		}
	}

	protected override IEnumerator showPick(GameObject button)
	{
		int index = buttonSelections.IndexOf(button);
		GameObject pickedSpriteObject = revealSprites[index];
		if (pickedSpriteObject != null)
		{
			UISprite pickedSprite = pickedSpriteObject.GetComponent<UISprite>();
			if (pickedSprite != null)
			{
				pickedSprite.spriteName = REVEAL_SPRITE_NAMES[(int)stageType];
			}
		}
		revealSprites.RemoveAt(index);
		Animator crowAnimator = button.GetComponent<Animator>();
		if (crowAnimator != null)
		{
			Audio.play(SHOW_PICK_SOUND);
			crowAnimator.Play(SHOW_PICK_ANIMATION);
			// Wait for the animation to start
			while (!crowAnimator.GetCurrentAnimatorStateInfo(0).IsName(SHOW_PICK_ANIMATION))
			{
				yield return null;
			}
			// Wait for the animation to stop.
			while (crowAnimator.GetCurrentAnimatorStateInfo(0).IsName(SHOW_PICK_ANIMATION))
			{
				yield return null;
			}
			if (stageType == Stage1Type.M1)
			{
				Audio.play(M1_REVEAL_SOUND);
			}
			else
			{
				Audio.play(DEFAULT_REVEAL_SOUND);
			}
			Audio.play(SHOW_SPRITE_SOUNDS[(int)stageType]);
		}
		else
		{
			Debug.LogWarning("There was no animator attached to the selected pick.");
		}
	}

	protected override IEnumerator showReveal(GameObject button)
	{
		if (revealPosition == (int)stageType)
		{
			revealPosition++;
		}

		Animator crowAnimator = button.GetComponent<Animator>();
		if (crowAnimator != null)
		{
			Audio.play(CROW_SOUND);
			crowAnimator.Play(END_PICK_ANIMATION);
			// Wait for the animation to start
			while (!crowAnimator.GetCurrentAnimatorStateInfo(0).IsName(END_PICK_ANIMATION))
			{
				yield return null;
			}
		}

		int index = buttonSelections.IndexOf(button);
		GameObject revealedSpriteObject = revealSprites[index];
		if (revealedSpriteObject != null)
		{
				UISprite revealedSprite = revealedSpriteObject.GetComponent<UISprite>();
				if (revealedSprite != null)
				{
					revealedSprite.spriteName = REVEAL_SPRITE_NAMES[revealPosition];
					revealedSprite.color = Color.gray;
					revealPosition++;
				}
		}
		yield return null;
	}

	protected override IEnumerator transitionIntoStage2()
	{
		Audio.switchMusicKey(Audio.soundMap(FREESPIN_MAP_SOUND));
		Audio.playMusic(Audio.soundMap(FREESPIN_INTRO_MAP_SOUND));
		// Slide the game down
		if (backgrounds != null)
		{
			// Background
			isSlideComplete = false;
			iTween.ValueTo(gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "time", TRANSITION_SLIDE_TIME, "onupdate", "slideBackgrounds", "oncomplete", "onBackgroundSlideComplete"));


			if (SpinPanel.instance != null)
			{
				float spinPanelSlideOutTime = TRANSITION_SLIDE_TIME;
				if (SpinPanel.instance.backgroundWingsWidth != null)
				{
					float spinPanelBackgroundHeight = SpinPanel.instance.backgroundWingsWidth.localScale.y;
					spinPanelSlideOutTime *= spinPanelBackgroundHeight / NGUIExt.effectiveScreenHeight;
					spinPanelSlideOutTime = TRANSITION_SLIDE_TIME - spinPanelSlideOutTime;
				}
				SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
				StartCoroutine(SpinPanel.instance.slideSpinPanelInFrom(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Down, spinPanelSlideOutTime, false));
				if (backgroundScript != null)
				{
					StartCoroutine(backgroundScript.tweenWingsTo(ReelGameBackground.WingTypeOverrideEnum.Freespins, spinPanelSlideOutTime, iTween.EaseType.linear));
				}
			}
		}

		while (!isSlideComplete)
		{
			// Wait for the slide to finish.
			yield return null;
		}

		symbolCamera.SetActive(true);
		SlotReel[] reelArray = engine.getReelArray();


		// Fade in all of the symbols.
		foreach (SlotReel reel in reelArray)
		{
			foreach (SlotSymbol slotSymbol in reel.symbolList)
			{
				if (slotSymbol.animator != null)
				{
					RoutineRunner.instance.StartCoroutine(slotSymbol.fadeInSymbolCoroutine(TIME_FADE_IN_SYMBOLS));
				}
			}
		}
		yield return new TIWaitForSeconds(TIME_FADE_IN_SYMBOLS);
		yield return StartCoroutine(base.transitionIntoStage2());
	}

	/// Function called by iTween to slide the backgrounds, slideAmount is 0.0f-1.0f
	public void slideBackgrounds(float slideAmount)
	{
		if (backgrounds != null)
		{
			// Move the reel background
			float targetReellessBkgYPos = ((BACKGROUNDS_END_POS - BACKGROUNDS_START_POS) * slideAmount) + BACKGROUNDS_START_POS;
			Vector3 currentBkgPos = backgrounds.transform.localPosition;
			backgrounds.transform.localPosition = new Vector3(currentBkgPos.x, targetReellessBkgYPos, currentBkgPos.z);
			// Move the Pick background
			float targetStageBackgroundYPos = NGUILoader.instance.nguiRoot.maximumHeight * slideAmount;
			Vector3 currentPickBkgPos = pickemStageObjects.transform.localPosition;
			pickemStageObjects.transform.localPosition = new Vector3(currentPickBkgPos.x, targetStageBackgroundYPos, currentPickBkgPos.z);
		}
	}

	// Makes sure that we get the background set to where we want it.
	public void onBackgroundSlideComplete()
	{
		isSlideComplete = true;
		slideBackgrounds(1);
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		Audio.play(SUMMARY_SOUND);
		base.gameEnded();
	}
}
