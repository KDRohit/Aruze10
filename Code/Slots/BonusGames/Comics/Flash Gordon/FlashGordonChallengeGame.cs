using UnityEngine;
using System.Collections;

public class FlashGordonChallengeGame : ChallengeGame 
{
	[SerializeField] private FlashGordonChallengeDoor[] doors = null;		// The doors that can be picked
	[SerializeField] private UITexture background = null;					// The background, which has it's texture swapped as the player progresses in the game
	[SerializeField] private Texture2D[] levelBkgTextures = null;			// Textures used for the different stage backgrounds

	[SerializeField] private UILabel scoreLabel = null;						// Score amount label -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent scoreLabelWrapperComponent = null;						// Score amount label

	public LabelWrapper scoreLabelWrapper
	{
		get
		{
			if (_scoreLabelWrapper == null)
			{
				if (scoreLabelWrapperComponent != null)
				{
					_scoreLabelWrapper = scoreLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_scoreLabelWrapper = new LabelWrapper(scoreLabel);
				}
			}
			return _scoreLabelWrapper;
		}
	}
	private LabelWrapper _scoreLabelWrapper = null;
	
	[SerializeField] private UILabel roundLabel = null;						// Label telling which round the user is at -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent roundLabelWrapperComponent = null;						// Label telling which round the user is at

	public LabelWrapper roundLabelWrapper
	{
		get
		{
			if (_roundLabelWrapper == null)
			{
				if (roundLabelWrapperComponent != null)
				{
					_roundLabelWrapper = roundLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_roundLabelWrapper = new LabelWrapper(roundLabel);
				}
			}
			return _roundLabelWrapper;
		}
	}
	private LabelWrapper _roundLabelWrapper = null;
	
	[SerializeField] private UILabel roundBackLabel = null;					// Backer text for the round label -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent roundBackLabelWrapperComponent = null;					// Backer text for the round label

	public LabelWrapper roundBackLabelWrapper
	{
		get
		{
			if (_roundBackLabelWrapper == null)
			{
				if (roundBackLabelWrapperComponent != null)
				{
					_roundBackLabelWrapper = roundBackLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_roundBackLabelWrapper = new LabelWrapper(roundBackLabel);
				}
			}
			return _roundBackLabelWrapper;
		}
	}
	private LabelWrapper _roundBackLabelWrapper = null;
	
	[SerializeField] private UILabel daleBonusLabel = null;					// Shows what the dale bonus is worth -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent daleBonusLabelWrapperComponent = null;					// Shows what the dale bonus is worth

	public LabelWrapper daleBonusLabelWrapper
	{
		get
		{
			if (_daleBonusLabelWrapper == null)
			{
				if (daleBonusLabelWrapperComponent != null)
				{
					_daleBonusLabelWrapper = daleBonusLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_daleBonusLabelWrapper = new LabelWrapper(daleBonusLabel);
				}
			}
			return _daleBonusLabelWrapper;
		}
	}
	private LabelWrapper _daleBonusLabelWrapper = null;
	
	[SerializeField] private UILabel daleBonusBackLabel = null;				// Backer text for the dale bonus label -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent daleBonusBackLabelWrapperComponent = null;				// Backer text for the dale bonus label

	public LabelWrapper daleBonusBackLabelWrapper
	{
		get
		{
			if (_daleBonusBackLabelWrapper == null)
			{
				if (daleBonusBackLabelWrapperComponent != null)
				{
					_daleBonusBackLabelWrapper = daleBonusBackLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_daleBonusBackLabelWrapper = new LabelWrapper(daleBonusBackLabel);
				}
			}
			return _daleBonusBackLabelWrapper;
		}
	}
	private LabelWrapper _daleBonusBackLabelWrapper = null;
	

	[SerializeField] private UISpriteAnimator daleExplosionLeft = null;		// Explosion for the dale value shown when the new level loads
	[SerializeField] private UISpriteAnimator daleExplosionRight = null;	// Explosion for the dale value shown when the new level loads
	[SerializeField] private Animation daleJackpotValueAnim = null;			// Animation for the text telling the Dale jackpot value

	[SerializeField] private Animation transitionAnim = null;				// Stage transition animation

	[SerializeField] private Animation winBoxAnim = null;					// Animation for the win box that will happen when the roll up is going
	
	private WheelOutcome wheelOutcome;
	private WheelPick wheelPick;
	private SkippableWait revealWait = new SkippableWait();					// Class for handling reveals that can be skipped
	private bool inputEnabled = true;										// Tells if the game buttons are currently accepting input
	private CoroutineRepeater pickMeController;								// Class to call the pickme animation on a loop
	
	private GameStageEnum currentLevel = GameStageEnum.STAGE_1;				// The current round of the game that we're on
	private int currentIntroVoNum = -1;										// Tracks what intro voiceover is currently playing

	public enum GameStageEnum
	{
		STAGE_1 = 0,
		STAGE_2 = 1,
		STAGE_3 = 2,
		STAGE_4 = 3
	}

	// Timing Constants
	private const float MIN_TIME_PICKME = 1.5f;						// Minimum time an animation might take to play next
	private const float MAX_TIME_PICKME = 4.0f;						// Maximum time an animation might take to play next
	private const float TIME_BETWEEN_REVEALS = 0.5f;				// The amount of time to wait between reveals.
	private const float WAIT_AFTER_PICK_REVEAL = 1.0f;				// Wait time after the user's pick is reaveled and before the unpicked choices are shown
	private const float WAIT_AFTER_ALL_REVEALED = 1.5f;				// Wati time after all picks have been revealed

	// Audio Constants
	private const string INTRO_DALE_VO_SOUND = "DRIntroVODale";					// Intro voice over part 1
	private const string INTRO_FLASH_VO_SOUND = "DRIntroVOFlash";				// Intro voice over part 2
	private const string INTRO_MING_VO_SOUND = "DRIntroVOMing";					// Intro voice over part 3
	private static readonly string[] INTRO_VO_SOUNDS = {INTRO_DALE_VO_SOUND, INTRO_FLASH_VO_SOUND, INTRO_MING_VO_SOUND};
	private const string BACKGROUND_MUSIC = "DRBonusBG";						// Background music
	private const string INCREASE_LVL_WHOOSH_SOUND = "DRIncreaseLevelWhoosh";	// Played when transitioning to the next level

	// Animation Constants
	private const string DALE_JACKPOT_VALUE_ANIM_NAME = "com03_DR Challenge_JackpotBurst_Animation"; 	// Animation for the Dale jackpot value
	private const string TRANSITION_ANIM_NAME = "com03_KeyHole_Transition_Animation";					// Transition animation name for going between stages
	private const string WIN_BOX_ANIM = "com03_DR Challenge_winbox_Animation";							// Animation for the win box, just going to play it while the roll up happens

	/// setup the initial state of the game
	public override void init() 
	{
		wheelOutcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as WheelOutcome;
		
		StartCoroutine(changeLevel(GameStageEnum.STAGE_1));

		scoreLabelWrapper.text = CreditsEconomy.convertCredits(0);;

		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeAnimCallback);

		_didInit = true;

		// Make sure to reset the background music to the first stage
		PlaylistInfo playlist = PlaylistInfo.find(BACKGROUND_MUSIC);
		if (playlist != null)
		{
			playlist.reset();
		}

		// play the regular background music
		Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC, 0.0f);

		playNextIntroVoiceOver();

		//StartCoroutine(playIntroAudio());
	}

	/// Overriding the update method so we can handle the pick me animations
	protected override void Update()
	{
		// Play the pickme animation.
		if (inputEnabled && _didInit)
		{
			pickMeController.update();
		}

		base.Update();
	}
	
	/// Callback function triggered by button message
	public void doorClicked(GameObject selectedDoor)
	{
		if (inputEnabled)
		{
			inputEnabled = false;

			FlashGordonChallengeDoor pickedDoor = selectedDoor.GetComponent<FlashGordonChallengeDoor>();

			if (pickedDoor != null)
			{
				// reveal the doors
				StartCoroutine(revealDoors(pickedDoor));
			}
			else
			{
				Debug.LogError("selectedDoor was missing the FlashGordonChallengeDoor component!");
				inputEnabled = true;
			}
		}
	}
	
	/// handles revealing the doors
	private IEnumerator revealDoors(FlashGordonChallengeDoor pickedDoor)
	{
		if (wheelPick.extraData == "DALE")
		{
			// dale bonus value
			yield return StartCoroutine(pickedDoor.playDalePickRevealAnimation(wheelPick.credits, currentLevel));
		}
		else if (!wheelPick.canContinue)
		{
			// game ended
			yield return StartCoroutine(pickedDoor.playGameEndRevealAnimation());
		}
		else
		{
			// normal value
			yield return StartCoroutine(pickedDoor.playValueRevealAnimation(wheelPick.credits, currentLevel));
		}
		
		if (wheelPick.credits != 0)
		{
			// roll up the value
			winBoxAnim.gameObject.SetActive(true);
			winBoxAnim.Play(WIN_BOX_ANIM);
			yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + wheelPick.credits, scoreLabelWrapper));
			winBoxAnim.Stop();
			winBoxAnim.gameObject.SetActive(false);
			
			BonusGamePresenter.instance.currentPayout += wheelPick.credits;
		}
		
		yield return new TIWaitForSeconds(WAIT_AFTER_PICK_REVEAL);
		
		// reveal the remaining choices
		for (int revealIndex = 0; revealIndex <= wheelPick.wins.Count; revealIndex++)
		{
			// skip the index which was won
			if (revealIndex != wheelPick.winIndex)
			{
				for (int i = 0; i < doors.Length; i++)
				{
					if (!doors[i].isRevealed)
					{
						WheelPick wheelWin = wheelPick.wins[revealIndex];

						if (wheelWin.extraData == "DALE")
						{
							doors[i].revealUnpickedDale(wheelWin.credits);
						}
						else if (!wheelWin.canContinue)
						{
							doors[i].revealUnpickedGameEnd();
						}
						else if (wheelWin.credits != 0)
						{
							doors[i].revealUnpickedValue(wheelWin.credits);
						}
						
						break;
					}
				}

				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}

		yield return new TIWaitForSeconds(WAIT_AFTER_ALL_REVEALED);
		
		// check if the player is moving to the next round or has reached the end
		if (wheelPick.canContinue)
		{
			StartCoroutine(changeLevel(currentLevel + 1));
		}
		else
		{
			BonusGamePresenter.instance.gameEnded();
		}
	}
	
	/// change the current level and resets anything that has to be reset between levels
	private IEnumerator changeLevel(GameStageEnum nextLevel)
	{
		currentLevel = nextLevel;

		// grab the next levels info now
		wheelPick = wheelOutcome.getNextEntry();

		// play the transition as long as this isn't the starting stage
		if (nextLevel != GameStageEnum.STAGE_1)
		{
			transitionAnim.gameObject.SetActive(true);
			transitionAnim.Play(TRANSITION_ANIM_NAME);

			Audio.play(INCREASE_LVL_WHOOSH_SOUND);

			// wait for the transition animation to finish
			while (transitionAnim.isPlaying)
			{
				yield return null;
			}

			transitionAnim.gameObject.SetActive(false);

			// play the next background music
			Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC, 0.0f);

			// reset the wait on reveals for the next round
			revealWait.reset();
		}

		roundLabelWrapper.text = Localize.textUpper("round_{0}_sans_colon", CommonText.formatNumber((int)nextLevel + 1));
		roundBackLabelWrapper.text = Localize.textUpper("round_{0}_sans_colon", CommonText.formatNumber((int)nextLevel + 1));

		// go through and find the dale result in the paytable info so we know it's value for the round
		foreach (WheelPick wheelResult in wheelPick.wins)
		{
			if (wheelResult.extraData == "DALE")
			{
				daleBonusLabelWrapper.text = CreditsEconomy.convertCredits(wheelResult.credits);
				daleBonusBackLabelWrapper.text = CreditsEconomy.convertCredits(wheelResult.credits);
				break;
			}
		}
		
		foreach (FlashGordonChallengeDoor door in doors)
		{
			door.resetDoor();
			door.changeDoorStyle(nextLevel);
		}

		background.mainTexture = levelBkgTextures[(int)nextLevel];

		// play the explosion for the updated value
		daleJackpotValueAnim.Play(DALE_JACKPOT_VALUE_ANIM_NAME);
		StartCoroutine(daleExplosionLeft.play());
		StartCoroutine(daleExplosionRight.play());

		inputEnabled = true;
	}

	/// Pick me animation player
	private IEnumerator pickMeAnimCallback()
	{
		int doorIndex = Random.Range(0, doors.Length);
		yield return StartCoroutine(doors[doorIndex].playPickMe());
	}

	/// Play the next voice over for the intro
	private void playNextIntroVoiceOver()
	{
		currentIntroVoNum++;
		PlayingAudio introVo = Audio.play(INTRO_VO_SOUNDS[currentIntroVoNum]);

		if (introVo != null)
		{
			introVo.addListeners(new AudioEventListener("end", introVoiceOverComplete));
		}
	}

	/// When a voice over completes check if another voice over needs to be played for the intro and play it
	private void introVoiceOverComplete(AudioEvent audioEvent, PlayingAudio audioInfo)
	{
		if (currentIntroVoNum + 1 < INTRO_VO_SOUNDS.Length)
		{
			playNextIntroVoiceOver();
		}
	}
}

