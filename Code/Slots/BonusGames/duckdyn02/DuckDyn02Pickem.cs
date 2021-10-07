using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DuckDyn02Pickem : PickingGame<WheelOutcome> 
{
	public GameObject intialStagesBGSet;
	public UILabel finalWinAmountText;	// To be removed when prefabs are updated. -  To be removed when prefabs are updated.
	public LabelWrapperComponent finalWinAmountTextWrapperComponent;	// To be removed when prefabs are updated.

	public LabelWrapper finalWinAmountTextWrapper
	{
		get
		{
			if (_finalWinAmountTextWrapper == null)
			{
				if (finalWinAmountTextWrapperComponent != null)
				{
					_finalWinAmountTextWrapper = finalWinAmountTextWrapperComponent.labelWrapper;
				}
				else
				{
					_finalWinAmountTextWrapper = new LabelWrapper(finalWinAmountText);
				}
			}
			return _finalWinAmountTextWrapper;
		}
	}
	private LabelWrapper _finalWinAmountTextWrapper = null;
		
	public ParticleSystem[] leftParticles;
	public ParticleSystem[] rightParticles;
	public float FINISH_REVEAL_DUR = 0.25f;
	
	
	[SerializeField] private GameObject boatBackgroundAnimation = null;			// Reference to the object that contains the boat movement animation objects
	[SerializeField] private UILabel animalRoundNumberText = null;			// Text for the current round number during animal rounds -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent animalRoundNumberTextWrapperComponent = null;			// Text for the current round number during animal rounds

	public LabelWrapper animalRoundNumberTextWrapper
	{
		get
		{
			if (_animalRoundNumberTextWrapper == null)
			{
				if (animalRoundNumberTextWrapperComponent != null)
				{
					_animalRoundNumberTextWrapper = animalRoundNumberTextWrapperComponent.labelWrapper;
				}
				else
				{
					_animalRoundNumberTextWrapper = new LabelWrapper(animalRoundNumberText);
				}
			}
			return _animalRoundNumberTextWrapper;
		}
	}
	private LabelWrapper _animalRoundNumberTextWrapper = null;
	
	[SerializeField] private UILabel animalRoundNumberShadow = null;		// Text shadow for the current round number text -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent animalRoundNumberShadowWrapperComponent = null;		// Text shadow for the current round number text

	public LabelWrapper animalRoundNumberShadowWrapper
	{
		get
		{
			if (_animalRoundNumberShadowWrapper == null)
			{
				if (animalRoundNumberShadowWrapperComponent != null)
				{
					_animalRoundNumberShadowWrapper = animalRoundNumberShadowWrapperComponent.labelWrapper;
				}
				else
				{
					_animalRoundNumberShadowWrapper = new LabelWrapper(animalRoundNumberShadow);
				}
			}
			return _animalRoundNumberShadowWrapper;
		}
	}
	private LabelWrapper _animalRoundNumberShadowWrapper = null;
	
	[SerializeField] private UILabel animalInstructionText = null;			// Text instructing the user of what they should currently be doing during animal rounds -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent animalInstructionTextWrapperComponent = null;			// Text instructing the user of what they should currently be doing during animal rounds

	public LabelWrapper animalInstructionTextWrapper
	{
		get
		{
			if (_animalInstructionTextWrapper == null)
			{
				if (animalInstructionTextWrapperComponent != null)
				{
					_animalInstructionTextWrapper = animalInstructionTextWrapperComponent.labelWrapper;
				}
				else
				{
					_animalInstructionTextWrapper = new LabelWrapper(animalInstructionText);
				}
			}
			return _animalInstructionTextWrapper;
		}
	}
	private LabelWrapper _animalInstructionTextWrapper = null;
	
	[SerializeField] private UILabel animalInstructionShadow = null;		// Text shadow for the instruction text for the round -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent animalInstructionShadowWrapperComponent = null;		// Text shadow for the instruction text for the round

	public LabelWrapper animalInstructionShadowWrapper
	{
		get
		{
			if (_animalInstructionShadowWrapper == null)
			{
				if (animalInstructionShadowWrapperComponent != null)
				{
					_animalInstructionShadowWrapper = animalInstructionShadowWrapperComponent.labelWrapper;
				}
				else
				{
					_animalInstructionShadowWrapper = new LabelWrapper(animalInstructionShadow);
				}
			}
			return _animalInstructionShadowWrapper;
		}
	}
	private LabelWrapper _animalInstructionShadowWrapper = null;
	
	[SerializeField] private GameObject revealPrefab = null;				// Reveal VFX prefab which is added dynamically
	[SerializeField] private GameObject gatorIconPrefab = null;				//  Gator icon prefab which denotes that the pick game was lost
	[SerializeField] private GameObject uncleSiIconPrefab = null;			//  Uncle Si icon profab which denotes that the player has won all values for that round
	[SerializeField] private GameObject revealAllPrefab = null;				// Reveal VFX when the user won all of the values during a round
	[SerializeField] private UISpriteAnimator potSplashAnim = null;			// Pot splashing animation shown when making the pick in the miss kay bonus round
	[SerializeField] private Animation rollupFireAnim = null;				// Fire effects taht play when the rollup is starting up
	
	private const float TIME_PICK_ME_ANIM = 1.5f;				// Time the pick me animation takes to complete half the animation, i.e. scaling up or scaling down
	private const float TIME_BEFORE_NEXT_STAGE = 0.5f;				// Wait time before moving to the next stage / summary dialog
	private const float SINGLE_REVEAL_ANIM_HALF_LENGTH = 0.7f;		// The time a single reveal animation takes
	private const float ALL_REVEAL_ANIM_HALF_LENGTH = 0.4f;			// The time the reveal all animation takes
	private const float PUT_CRITTER_IN_POT_DURATION = 0.75f;		// Time it takes to put a critter in the pot during the Miss Kay round	
	private const float PICKME_TIME = 1.0f;
	private const float PRE_REVEAL_WAIT = 0.5f;
	private const float POST_REVEAL_WAIT_LONG = 2.0f;
	private const string FIRE_ROLL_UP_FLY_OVER_ANIM = "DD02_PickBonus_win roll up_Animation";
	private const string FIRE_ROLL_UP_LOOP_ANIM = "DD02_PickBonus_win roll loop_Animation";
	
	private const string PICKME_ANIM = "pickme";
	private const string PICKME_STILL = "DD02_picking object_Still";
	private const string TYPE_1_REVEAL_END = "DD02_picking object_reveal end";
	private const string TYPE_1_REVEAL_NUMBER = "DD02_picking object_reveal number";
	private const string TYPE_1_REVEAL_END_GRAY = "DD02_picking object_not selected end";
	
	private const string CRAWFISH_INTRO_VO_SOUND = "CrawfishIntroVO";			// Intro VO for hte crawfish round
	private const string FROG_INTRO_VO_SOUND = "CrawfishRevealFrogVO";			// Intro VO for the frog round
	private const string BASS_INTRO_VO_SOUND = "CrawfishRevealBassVO";			// Intro VO for the bass round
	private const string DUCK_INTRO_VO_SOUND = "CrawfishRevealDuckVO";			// Intro VO for the duck round
	private const string MISS_KAY_COOKING_INTRO_VO_SOUND = "CrawfishPotPickVO";	// Intro VO for the miss kay round
	private readonly string[] INTRO_VO_SOUNDS = {CRAWFISH_INTRO_VO_SOUND, FROG_INTRO_VO_SOUND, BASS_INTRO_VO_SOUND, DUCK_INTRO_VO_SOUND, MISS_KAY_COOKING_INTRO_VO_SOUND};

	private const string REACHED_MISS_KAY_ROUND_SOUND = "PortalRevealBonusDuck02";	// Sound for getting to the bonus miss kay round  
	private const string POT_BOIL_LOOP_SOUND = "CrawfishCauldronLoop";				// Sound for the pot boiling
	private PlayingAudio boatIdleSound = null;									// Stored out sound for the boat idle
	private PlayingAudio boilSound = null;									// Stored out sound for the boat idle
	private const string BOAT_MOVE_SOUND = "CrawfishOutboardMove";					// Boat sound when moving
	private const string BOAT_IDLE_SOUND = "CrawfishOutboardIdle";					// Boat idle sound
	
	private const string BONUS_ROUND_BG_MUSIC = "CrawfishStoveBg";					// Music that plays when you go into the bonus miss kay round
	
	private const string REVEAL_INGREDIENT_SOUND = "RevealIngredient";				// Sound ingredient makes when choosen
	private const string REVEAL_INGRED_SPLASH_SOUND = "RevealSplash";				// Sound ingredient makes when it hits the pot
	
	private const string REVEAL_WIN_ALL_SOUND = "CrawfishRevealWinAll";				// Sound played when the win all is picked
	private const string PICKME_SOUND = "CrawfishPickMe";
	private const string REVEAL_SFX = "CrawfishRevealOthers";
	private const string LOSE_SFX = "CrawfishRevealBad";
	private const string LOSE_VO = "CrawfishPooperVO";
	private const string REVEAL_CREDITS_SOUND = "CrawfishRevealCredit";				// Sound played when credits are revealed	
	
	private WheelPick currentRoundPick;
	private int realPickIndex;
	private int lastStageNumber;
	
	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();
		
		lastStageNumber = stageObjects.Length - 1;
					
		currentRoundPick = outcome.entries[currentStage];
		
		StartCoroutine(showAnimalRound(currentStage));	
	}
	
	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(button);
		realPickIndex = pickIndex;
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);
		GameObject slot = pickButtonData.go;
		
		yield return null;		// ensure the current click doesn't cause the reveals to skip by waiting a frame
		
		if (currentRoundPick.canContinue || currentStage == lastStageNumber)   
		{
			if (isCollectAll(currentRoundPick, currentRoundPick.winIndex))
			{
				button.gameObject.SetActive(false);
				
				GameObject revealAllEffect = NGUITools.AddChild(slot, revealAllPrefab);
				revealAllEffect.SetActive(true);
				yield return new TIWaitForSeconds(ALL_REVEAL_ANIM_HALF_LENGTH);
				
				Audio.play(REVEAL_WIN_ALL_SOUND);
				
				addIcon(uncleSiIconPrefab, slot);
				
				yield return new TIWaitForSeconds(ALL_REVEAL_ANIM_HALF_LENGTH);
				revealAllEffect.SetActive(false);	
				
				BonusGamePresenter.instance.currentPayout += currentRoundPick.credits;
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentRoundPick.credits, BonusGamePresenter.instance.currentPayout));
			}		
			else 
			{
				if (currentStage == lastStageNumber)
				{
					Audio.play(REVEAL_INGREDIENT_SOUND);
					
					//button.depth = 1;
					if (pickButtonData.imageReveal != null)
					{
						GameObject go = pickButtonData.imageReveal.gameObject;
						if (go != null)
						{
							iTween.MoveTo(go, iTween.Hash("position", potSplashAnim.gameObject.transform.position, "isLocal", false, "time", PUT_CRITTER_IN_POT_DURATION));
							iTween.RotateTo(go, iTween.Hash("rotation", new Vector3(.0f, .0f, 90.0f), "isLocal", true, "time", PUT_CRITTER_IN_POT_DURATION, "easetype", iTween.EaseType.linear));
							iTween.ScaleTo(go, iTween.Hash("scale", go.transform.localScale / 3.0f, "isLocal", true, "time", PUT_CRITTER_IN_POT_DURATION, "easetype", iTween.EaseType.linear));
							yield return new TIWaitForSeconds(PUT_CRITTER_IN_POT_DURATION);
							go.SetActive(false);
						}
					}
					
					Audio.play(REVEAL_INGRED_SPLASH_SOUND);
					
					StartCoroutine(playRollupFireAnim());
					yield return StartCoroutine(potSplashAnim.play());
				}				
				
				GameObject revealEffect = NGUITools.AddChild(slot, revealPrefab);
				
				if (currentRoundPick.multiplier == 0)
				{
					Audio.play(REVEAL_CREDITS_SOUND);
					pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentRoundPick.credits);
					pickButtonData.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(currentRoundPick.credits);
					pickButtonData.animator.Play(TYPE_1_REVEAL_NUMBER);
					BonusGamePresenter.instance.currentPayout += currentRoundPick.credits;
					yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentRoundPick.credits, BonusGamePresenter.instance.currentPayout));
				}
				else
				{
					Audio.play(REVEAL_SFX);
					int multiplier = currentRoundPick.multiplier + 1;
					
					pickButtonData.revealNumberLabel.text = Localize.text("{0}X", CommonText.formatNumber(multiplier));
					pickButtonData.revealNumberOutlineLabel.text = Localize.text("{0}X", CommonText.formatNumber(multiplier));
					pickButtonData.animator.Play(TYPE_1_REVEAL_NUMBER);
					BonusGamePresenter.instance.currentPayout *= multiplier;
					yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout/multiplier, BonusGamePresenter.instance.currentPayout));
				}				

				yield return new TIWaitForSeconds(SINGLE_REVEAL_ANIM_HALF_LENGTH);		
				revealEffect.SetActive(true);
				yield return new TIWaitForSeconds(SINGLE_REVEAL_ANIM_HALF_LENGTH);	
				revealEffect.SetActive(false);	
			}
			
			yield return StartCoroutine(endStage(currentStage == lastStageNumber));
			
			//inputEnabled = true;
		}
		else
		{
			// game endig pick the gator!
			pickButtonData.animator.Play(TYPE_1_REVEAL_END);
			GameObject revealEffect = NGUITools.AddChild(slot, revealPrefab);
			revealEffect.SetActive(true);
			yield return new TIWaitForSeconds(SINGLE_REVEAL_ANIM_HALF_LENGTH);
			
			addIcon(gatorIconPrefab, slot);
			
			Audio.play(LOSE_SFX);
			
			yield return new TIWaitForSeconds(SINGLE_REVEAL_ANIM_HALF_LENGTH);
			revealEffect.SetActive(false);
			
			// game ends have values in this game
			if (currentRoundPick.credits != 0)
			{
				// animate the score changing
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + currentRoundPick.credits));
				BonusGamePresenter.instance.currentPayout += currentRoundPick.credits;
			}
			
			Audio.play(LOSE_VO);		
			
			yield return StartCoroutine(endStage(true));			
		}
		
		yield return new TIWaitForSeconds(TIME_BEFORE_NEXT_STAGE);
	}
	
	private void addIcon(GameObject prefab, GameObject parentObj, bool drawGray = false)
	{
		GameObject icon = NGUITools.AddChild(parentObj, prefab);
		icon.transform.localPosition = new Vector3(.0f, 85.0f, .0f);
		icon.SetActive(true);
		
		if (drawGray)
		{
			UISprite sprite = icon.GetComponentInChildren<UISprite>();
			if (sprite != null)
			{
				sprite.color = Color.gray;	
			}	
			LabelWrapperComponent label = icon.GetComponentInChildren<LabelWrapperComponent>();
			if (label != null)
			{
				label.color = Color.gray;
			}				
		}	
	}	
	
	/// Show an animal round
	private IEnumerator showAnimalRound(int round)
	{
		int keyNumber = round + 1;
		
		animalInstructionTextWrapper.text = Localize.text("duckdyn02_challenge_round" + keyNumber);		
		animalInstructionShadowWrapper.text = Localize.text("duckdyn02_challenge_round" + keyNumber);
		
		animalRoundNumberTextWrapper.text = Localize.text("round_{0}", keyNumber);
		animalRoundNumberShadowWrapper.text = Localize.text("round_{0}", keyNumber);
				
		// hide round objects before we play the boat transition
		animalRoundNumberTextWrapper.gameObject.SetActive(false);
		animalRoundNumberShadowWrapper.gameObject.SetActive(false);
		animalInstructionTextWrapper.gameObject.SetActive(false);
		animalInstructionShadowWrapper.gameObject.SetActive(false);
		
		if (stageObjects.Length > round && stageObjects[round])   
		{
			stageObjects[round].SetActive(false);
		}		
		
		// play the boat transtion
		yield return StartCoroutine(playBoatMoveTransition());
		
		// show the objects for the next round
		animalRoundNumberTextWrapper.gameObject.SetActive(true);
		animalRoundNumberShadowWrapper.gameObject.SetActive(true);
		animalInstructionTextWrapper.gameObject.SetActive(true);
		animalInstructionShadowWrapper.gameObject.SetActive(true);
		
		if (stageObjects.Length > round && stageObjects[round])
		{
			stageObjects[round].SetActive(true);
		}

		inputEnabled = true;	
		
		Audio.play(INTRO_VO_SOUNDS[round]);
	}	
	
	private IEnumerator endStage(bool endGame = false)
	{
		yield return new TIWaitForSeconds(PRE_REVEAL_WAIT);
		
		grayoutAnimName = "";
		yield return StartCoroutine(finishRevealingPicks(FINISH_REVEAL_DUR));

		// give a little extra time so that we can see the results for longer.
		yield return new TIWaitForSeconds(POST_REVEAL_WAIT_LONG);

		if (endGame)
		{
			Audio.stopMusic(); 
			if (boilSound != null)
			{
				Audio.stopSound(boilSound);
			}
			if (boatIdleSound != null)
			{
				Audio.stopSound(boatIdleSound);
			}			
			Audio.switchMusicKey("");
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			// transition to the next stage
			if (currentStage < lastStageNumber)
			{
				continueToNextStage();		// this advances currentStage
				if (currentStage < lastStageNumber)
				{
					StartCoroutine(showAnimalRound(currentStage));
				}
				else
				{
					setupMissKayRoundAudio();
				}
			}
			
			if (currentStage == lastStageNumber)
			{
				intialStagesBGSet.SetActive(false);
				currentWinAmountTextWrapperNew = finalWinAmountTextWrapper;
				currentWinAmountTextWrapperNew.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			}		
			currentRoundPick = outcome.entries[currentStage];			
		}
	}
	
	/// Play all the animations for the boat moving
	private IEnumerator playBoatMoveTransition()
	{
		Animation[] animations = boatBackgroundAnimation.GetComponentsInChildren<Animation>();
		
		// Turn off the boat idle noise
		if (boatIdleSound == null)
		{
			boatIdleSound = Audio.play(BOAT_IDLE_SOUND);
		}
		
		Audio.play(BOAT_MOVE_SOUND);
		
		foreach (Animation animation in animations)
		{
			animation.Play();
		}
		
		bool isPlaying = true;
		
		// wait for all animations to finish
		while (isPlaying)
		{
			yield return null;
			
			foreach (Animation animation in animations)
			{
				if (animation.isPlaying)
				{
					isPlaying = true;
					break;
				}
				else
				{
					isPlaying = false;
				}
			}
		}
	}	
	
	/// Check if the current result index in pick.wins array is a collect all.   
	private bool isCollectAll(WheelPick pick, int index)
	{
		long winAmount = 0;
		long unWonAmount = 0;
		long winMax = 0;
		
		winAmount = pick.wins[index].credits;
		winMax =  winAmount;
		
		int i = 0;
		foreach (WheelPick card in pick.wins)
		{
			long lCredit = card.credits;
			
			if (i == index)
			{
				i++;
				continue;
			}
			if (lCredit == 0)
			{
				continue;
			}
			unWonAmount += lCredit;
			
			if (lCredit > winMax)
			{
				winMax = lCredit;
			}
			
			i++;
		}
		
		return (winAmount == winMax) && (winAmount == unWonAmount);
	}	
	
	/// Show the final round which is Miss Kay cooking
	private void setupMissKayRoundAudio()
	{
		// Turn off the boat idle noise
		if (boatIdleSound != null)
		{
			Audio.stopSound(boatIdleSound);
		}
		
		Audio.play(INTRO_VO_SOUNDS[lastStageNumber]);
		boilSound = Audio.play(POT_BOIL_LOOP_SOUND);
		
		Audio.switchMusicKeyImmediate(BONUS_ROUND_BG_MUSIC, 0.0f);	
		inputEnabled = true;
	}	
	
	/// Animation that plays during the rollup of miss key bonus stage
	private IEnumerator playRollupFireAnim()
	{
		rollupFireAnim.gameObject.SetActive(true);
		rollupFireAnim.Play(FIRE_ROLL_UP_FLY_OVER_ANIM);
		
		while (rollupFireAnim.isPlaying)
		{
			yield return null;
		}
		
		rollupFireAnim.Play(FIRE_ROLL_UP_LOOP_ANIM);
	}	
	
	protected override void finishRevealingPick(PickGameButtonData buttonPickEmData)
	{	
		GameObject button = buttonPickEmData.button;			
		int pickIndex = getButtonIndex(button);
					
		if (pickIndex == realPickIndex)
		{
			return;
		}
		
		if (pickIndex == currentRoundPick.winIndex)
		{
			pickIndex = realPickIndex;    // this button was already revealed when picked and showed the win, so now show it's data in the winindex location
		}

		if (!revealWait.isSkipping) 
		{
			Audio.play (REVEAL_SFX);
		}

		string revealStr = TYPE_1_REVEAL_END_GRAY;
		UILabel label = buttonPickEmData.extraLabel;
		UILabel glowLabel = buttonPickEmData.revealNumberOutlineLabel;
		bool showGator = !currentRoundPick.wins[pickIndex].canContinue;


		if (isCollectAll(currentRoundPick, currentRoundPick.winIndex))
		{
			revealStr = TYPE_1_REVEAL_NUMBER;
			label = buttonPickEmData.revealNumberLabel;
			glowLabel = buttonPickEmData.revealNumberOutlineLabel;
			
			GameObject revealFX = NGUITools.AddChild(buttonPickEmData.go, revealPrefab);
			if (revealFX != null)
			{
				revealFX.SetActive(true);
			}				
		}
			
		if (currentStage != lastStageNumber && (showGator || isCollectAll(currentRoundPick, pickIndex)) )
		{
			label.text = "";
			glowLabel.text = "";
			label = null;
			if (showGator)
			{
				addIcon(gatorIconPrefab, buttonPickEmData.go, true);
			}
			else
			{
				addIcon(uncleSiIconPrefab, buttonPickEmData.go, true);
			}
		}
		
		if (label != null)
		{
			if (currentRoundPick.wins[pickIndex].multiplier == 0)
			{
				long credits = currentRoundPick.wins[pickIndex].credits;
				label.text = CreditsEconomy.convertCredits(credits);
			}
			else
			{
				int multiplier = currentRoundPick.wins[pickIndex].multiplier + 1;
				label.text = Localize.text("{0}X", CommonText.formatNumber(multiplier));						
			}
			if (glowLabel != null)
			{
				glowLabel.text = label.text;
			}
		}
		
		buttonPickEmData.animator.Play(revealStr);	
	}
		
}



