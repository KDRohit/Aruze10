using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * This class handles the Flash Gordon Free Spin Game
 * This free spin game has quite a bit of logic and animation choreography
 * There is also something slightly weird we do with rollups to prevent confusion by the user
 */
public class FlashGordonFreeSpins : FreeSpinGame 
{
	public Animation[] topPips;
	public Animation[] bottomPips;
	public UISprite[] mingEyes;
	
	public Animation[] chests;
	public Animation[] pickSparkles;
	public GameObject[] revealSparkles;
	public UILabel[] possibleWinAmounts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] possibleWinAmountsWrapperComponent;

	public List<LabelWrapper> possibleWinAmountsWrapper
	{
		get
		{
			if (_possibleWinAmountsWrapper == null)
			{
				_possibleWinAmountsWrapper = new List<LabelWrapper>();

				if (possibleWinAmountsWrapperComponent != null && possibleWinAmountsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in possibleWinAmountsWrapperComponent)
					{
						_possibleWinAmountsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in possibleWinAmounts)
					{
						_possibleWinAmountsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _possibleWinAmountsWrapper;
		}
	}
	private List<LabelWrapper> _possibleWinAmountsWrapper = null;	
	
	public UILabel treasureWinAmount;	// To be removed when prefabs are updated.
	public LabelWrapperComponent treasureWinAmountWrapperComponent;

	public LabelWrapper treasureWinAmountWrapper
	{
		get
		{
			if (_treasureWinAmountWrapper == null)
			{
				if (treasureWinAmountWrapperComponent != null)
				{
					_treasureWinAmountWrapper = treasureWinAmountWrapperComponent.labelWrapper;
				}
				else
				{
					_treasureWinAmountWrapper = new LabelWrapper(treasureWinAmount);
				}
			}
			return _treasureWinAmountWrapper;
		}
	}
	private LabelWrapper _treasureWinAmountWrapper = null;
	
	
	public GameObject treasureMapOverlay;
	public GameObject treasureMapBG;

	public GameObject flashSymbolAnim;
	public GameObject mingSymbolAnim;

	private CoroutineRepeater flashAnimController;						// Class to call the flash gordon idle animation(s) on a loop	
	private CoroutineRepeater pickMeController;						// Class to call the pickme animation on a loop	

	private int animCount = 1;
	private bool canDoAnim = true;
	private bool canDoPickme = false;
		
	private const float MIN_TIME_ANIM = 2.0f;
	private const float MAX_TIME_ANIM = 5.0f;
	private const float MIN_TIME_PICKME = 2.0f;
	private const float MAX_TIME_PICKME = 4.0f;
	
	public Animation earthAnimation;
	public Animation mingAnimation;
	public Animation flashGordonAnim;

	//private bool shouldEnd = false;
	private int mingCount = 0;
	private int flashCount = 0;
	private StandardMutation currentMutation;
	private bool currentWildWon = false;
	private bool shouldShakeEarth = false;
	private bool shouldHitMing = false;
	private bool isReadyToSpin = false;

	public GameObject top; // game object holds location of about the top of the screen
	public GameObject bottom; // game object holds location of about the bottom of the screen
	public GameObject rock; // asteroid object to clone when attacking earth
	public GameObject rocket; // rocket object to clone when attacking ming

	public GameObject rocketExplosion; // rocket explosion to activate when attacking ming
	public GameObject winBanner; // banner to activate after winning (ming defeated)
	public GameObject loseBanner; // banner to activate after losing (ming victorious)
	public UILabel loseText; // text attached to lose banner -  To be removed when prefabs are updated.
	public LabelWrapperComponent loseTextWrapperComponent; // text attached to lose banner

	public LabelWrapper loseTextWrapper
	{
		get
		{
			if (_loseTextWrapper == null)
			{
				if (loseTextWrapperComponent != null)
				{
					_loseTextWrapper = loseTextWrapperComponent.labelWrapper;
				}
				else
				{
					_loseTextWrapper = new LabelWrapper(loseText);
				}
			}
			return _loseTextWrapper;
		}
	}
	private LabelWrapper _loseTextWrapper = null;
	
	public UILabel loseTextOutline; // text outline attaches to lose banner -  To be removed when prefabs are updated.
	public LabelWrapperComponent loseTextOutlineWrapperComponent; // text outline attaches to lose banner

	public LabelWrapper loseTextOutlineWrapper
	{
		get
		{
			if (_loseTextOutlineWrapper == null)
			{
				if (loseTextOutlineWrapperComponent != null)
				{
					_loseTextOutlineWrapper = loseTextOutlineWrapperComponent.labelWrapper;
				}
				else
				{
					_loseTextOutlineWrapper = new LabelWrapper(loseTextOutline);
				}
			}
			return _loseTextOutlineWrapper;
		}
	}
	private LabelWrapper _loseTextOutlineWrapper = null;
	

	// sound constants
	private const string INTRO_SOUND = "IntroFreespinFlash";
	private const string INTRO_VO  = "FGMingAttackingMeteorsHelpMeStopHim";
	private const string BAD_WILD = "BattleMingInitiator";
	private const string WILD_SPIN = "BattleMeteorSpinAnimation";
	private const string WILD_MUTATE_SOUND = "BattleFlipWildSymbol";
	private const string BAD_WILD_METEOR_LAUNCH = "BattleMeteorLaunch";
	private const string BAD_WILD_HITS_EARTH = "BattleMeteorExplosion";
	private const string EARTH_HIT_SOUND = "BattleEarthHitVO";
	private const string PIP_DEACTIVATE_SOUND = "BattleDecrementPip";
	private const string GOOD_WILD = "BattleFlashInitiator";
	private const string ROCKET_LAUNCH_SOUND = "BattleRocketLaunch";
	private const string MONGO_HIT_SOUND = "BattleRocketExplosion";
	private const string MING_FLINCHES_SOUND = "BattleMongoHitVO";
	private const string MING_WINS_SOUND = "MGBwahahahBetterLuckNextTimeEarthling";
	private const string FLASH_WINS_SOUND = "BattleFlashWins";
	private const string TREASURE_CHEST_BG  = "BattleTreasureRoomBg";
	private const string TREASURE_CHEST_INTRO = "BattleTreasureChestVO";
	private const string TREASURE_CHEST_PICKED = "BattleRevealChest";
	private const string TREASURE_CHEST_REVEAL_OTHERS = "RevealSparkly";

	// timing constants
	private const float INTRO_WAIT_1 = 0.75f;
	private const float MUTATION_TIME = 0.25f;
	private const float WEAPON_TWEEN_TIME = 1.5f;
	private const float POST_INTRO_WAIT = 2.0f;
	private const float SYMBOL_ANIM_WAIT_1 = 0.834f;
	private const float SYMBOL_ANIM_WAIT_2 = 1.067f;
	private const float POST_BATTLE_WAIT = 1.0f;
	private const float PRE_CHEST_REVEAL_WAIT = 1.25f;
	private const float CHEST_DEACTIVATE_TIME = 0.5f;
	private const float CHEST_REVEAL_TIME = 0.75f;
	private const float CHEST_POST_REVEAL_TIME = 0.25f;
	private const float ATTACK_ANIM_WAIT_TIME = 3.5f;

	// other constants
	private const float SYMBOL_ANIM_SCALE_SCALAR = 0.275f;
	private const float WEAPON_SCALE_SCALAR = 2.0f;

	// animation name constants
	private const string EARTH_IDLE_ANIMATION = "com03_FS_earth idle_Animation";
	private const string MING_IDLE_ANIMATION = "com03_FS_Ming_idle_Animation";
	private const string FLASH_IDLE_ANIM_PREFIX = "com03_Flash_idle_Animation";
	private const string FLASH_ATTACK_ANIMATION = "com03_Flash_attack_Animation";
	private const string MING_ATTACK_ANIMATION = "com03_FS_Ming_attack_Animation";
	private const string FLASH_SYMBOL_ANIM_2 = "com03_FS_symbol_Flash_02_Animation";
	private const string FLASH_SYMBOL_ANIM_1 = "com03_FS_symbol_Flash_01_Animation";
	private const string MING_SYMBOL_ANIM_2 = "com03_FS_symbol_Ming_02_Animation";
	private const string MING_SYMBOL_ANIM_1 = "com03_FS_symbol_Ming_01_Animation";
	private const string EARTH_HIT_ANIMATION = "com03_FS_earth hit explosion_Animation";
	private const string FLASH_HIT_ANIMATION = "com03_Flash_hit_Animation";
	private const string MING_HIT_ANIMATION = "com03_FS_Ming_Hit_Animation";
	private const string MING_HEALTH_DEACTIVATE_ANIMATION = "com03_FS_MingHealth_Deactivate Animation";
	private const string FLASH_HEALTH_DEACTIVATE_ANIMATION = "com03_FS_FlashHealth_Deactivate Animation";

	GameObject chestTop, chestBody;

	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;

		// Set this game to be in endless mode and have a line for it's spin count.
		endlessMode = true;
		BonusSpinPanel.instance.spinCountLabel.text = "-";

		earthAnimation.Play(EARTH_IDLE_ANIMATION);
		mingAnimation.Play(MING_IDLE_ANIMATION);

		StartCoroutine(playOpeningAudio());
		_didInit = true;
	}

	// do opening audio, then let the game start	
	private IEnumerator playOpeningAudio()
	{
		Audio.play(INTRO_SOUND);
		yield return new TIWaitForSeconds(INTRO_WAIT_1);
		Audio.play(INTRO_VO);

		yield return new TIWaitForSeconds(POST_INTRO_WAIT);
		isReadyToSpin = true;
	}

	// override version for starting autospins
	protected override void startNextFreespin()
	{
		if (isReadyToSpin)
		{
			base.startNextFreespin();
		}
		else
		{
			StartCoroutine(waitForReadyThenAutospin());
		}
	}

	// wait until we're ready to autospin
	private IEnumerator waitForReadyThenAutospin()
	{
		while (!isReadyToSpin)
		{
			yield return null;
		}

		base.startNextFreespin();
	}

	// start animation repeater
	protected override void Awake()
	{
		base.Awake();
		flashAnimController = new CoroutineRepeater(MIN_TIME_ANIM, MAX_TIME_ANIM, animCallback);
		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickmeCallback);
	}

	// just need this function to handle the animation repeater
	protected override void Update()
	{
		base.Update();
		if (canDoAnim)
		{
			flashAnimController.update();
		}
		if (canDoPickme && _didInit)
		{
			pickMeController.update();
		}
	}


	private IEnumerator pickmeCallback()
	{
		canDoPickme = false;

		if (chests != null)
		{
			int index = Random.Range(0, chests.Length);
			if (index < chests.Length)
			{
				Animation animation = chests[index];
				if (animation != null)
				{
					animation.Play("com03_FS_PickChest_PickMe_Animation");
					animation.gameObject.SetActive(true);
					animation.Play();
					yield return new TIWaitForSeconds(animation.clip.length);
				}
			}
		}

		canDoPickme = true;
	}

	// play the next flash gordon animation, eventually looping back around
	private IEnumerator animCallback()
	{
		canDoAnim = false;
		
		flashGordonAnim.Play(FLASH_IDLE_ANIM_PREFIX + ((animCount % 3) + 1));
		animCount++;
		yield return new TIWaitForSeconds(flashGordonAnim.clip.length);
		
		canDoAnim = true;
	}

	// override version of reelsStoppedCallback to initiate all the mutations and battle stuff
	protected override void reelsStoppedCallback()
	{
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		currentMutation = null;
		if (mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase mutation in mutationManager.mutations)
			{
				if (mutation.type == "free_spin_battle_mode")
				{
					StandardMutation battleMutation = mutation as StandardMutation;
					currentWildWon = battleMutation.didWin;
					if (currentWildWon)
					{
						flashCount++;
						shouldHitMing = true;
						canDoAnim = false;
						flashGordonAnim.Play(FLASH_ATTACK_ANIMATION);
						StartCoroutine(enableIdleAnimsAfterWait());
					}
					else
					{
						mingCount++;
						shouldShakeEarth = true;
						
						mingAnimation.Play(MING_ATTACK_ANIMATION);
					}
				}
				else
				{
					currentMutation = mutation as StandardMutation;
				}
			}
		}

		StartCoroutine(doSpecialWildAnims());
	}

	// quick one-off function to allow us to continue doing flash idle animations after the attack animation is done
	private IEnumerator enableIdleAnimsAfterWait()
	{
		yield return new TIWaitForSeconds(ATTACK_ANIM_WAIT_TIME);
		canDoAnim = true;
	}

	// find the special wild symbol in the middle reel and do the animation on it
	private IEnumerator doSpecialWildAnims()
	{
		SlotReel[] reelArray = engine.getReelArray();

		if (shouldHitMing)
		{
			foreach (SlotSymbol s in reelArray[2].visibleSymbols)
			{
				if (s.name == "GW")
				{
					yield return StartCoroutine(createFlashSymbolAnim(s));
				}
			}
		}
		
		if (shouldShakeEarth)
		{
			foreach (SlotSymbol s in reelArray[2].visibleSymbols)
			{
				if (s.name == "BW")
				{
					yield return StartCoroutine(createMingSymbolAnim(s));
				}
			}
		}

		if (currentMutation == null && mutationManager.mutations.Count <= 0)
		{
			base.reelsStoppedCallback();
		}
	}

	// do the good wild symbol animations
	private IEnumerator createFlashSymbolAnim(SlotSymbol s)
	{
		GameObject flashSymbol = CommonGameObject.instantiate(flashSymbolAnim) as GameObject;
		flashSymbol.transform.parent = s.scalingSymbolPart.transform;
		flashSymbol.transform.localScale = Vector3.one * SYMBOL_ANIM_SCALE_SCALAR;
		flashSymbol.transform.localPosition = Vector3.zero;
		Animator anim = flashSymbol.GetComponent<Animator>();
		CommonGameObject.setLayerRecursively(flashSymbol, Layers.ID_SLOT_OVERLAY);

		anim.Play(FLASH_SYMBOL_ANIM_2);
		Audio.play(GOOD_WILD);
		yield return new TIWaitForSeconds(SYMBOL_ANIM_WAIT_1);
		anim.Play(FLASH_SYMBOL_ANIM_1);
		Audio.play(WILD_SPIN);
		yield return new TIWaitForSeconds(SYMBOL_ANIM_WAIT_2);

		
		StartCoroutine(launchWeapon(true));

		Destroy (flashSymbol);
	}

	// do the bad wild symbol animations
	private IEnumerator createMingSymbolAnim(SlotSymbol s)
	{
		GameObject mingSymbol = CommonGameObject.instantiate(mingSymbolAnim) as GameObject;
		mingSymbol.transform.parent = s.scalingSymbolPart.transform;
		mingSymbol.transform.localScale = Vector3.one * SYMBOL_ANIM_SCALE_SCALAR;
		mingSymbol.transform.localPosition = Vector3.zero;
		Animator anim = mingSymbol.GetComponent<Animator>();
		CommonGameObject.setLayerRecursively(mingSymbol, Layers.ID_SLOT_OVERLAY);
		
		anim.Play(MING_SYMBOL_ANIM_2);
		Audio.play(BAD_WILD);
		yield return new TIWaitForSeconds(SYMBOL_ANIM_WAIT_1);
		anim.Play(MING_SYMBOL_ANIM_1);
		Audio.play(WILD_SPIN);
		yield return new TIWaitForSeconds(SYMBOL_ANIM_WAIT_2);

		
		StartCoroutine(launchWeapon(false));

		Destroy (mingSymbol);
	}

	// launch the rocket or drop the asteroid
	private IEnumerator launchWeapon(bool goesUp)
	{
		GameObject weapon;

		float waitSkipTime = 0.0f; // might want to skip some of our tween wait time to allow the mutations to happen on time

		if (currentMutation != null)
		{
			waitSkipTime = getNumMutatedSymbols() * MUTATION_TIME;
		}
		if (goesUp)
		{
			weapon = CommonGameObject.instantiate(rocket) as GameObject;
			weapon.transform.parent = rocket.transform.parent;
			weapon.transform.localScale = rocket.transform.localScale * WEAPON_SCALE_SCALAR;
			weapon.SetActive(true);
			yield return null;
			weapon.transform.localPosition = bottom.transform.localPosition;
			yield return null;
			iTween.MoveTo(weapon, iTween.Hash("position", top.transform.localPosition, "islocal", true, "time", WEAPON_TWEEN_TIME, "easetype", iTween.EaseType.linear));
			Audio.play(ROCKET_LAUNCH_SOUND);
			yield return new TIWaitForSeconds(WEAPON_TWEEN_TIME - waitSkipTime);
		}
		else
		{
			weapon = CommonGameObject.instantiate(rock) as GameObject;
			weapon.transform.parent = rock.transform.parent;
			weapon.transform.localScale = rock.transform.localScale * WEAPON_SCALE_SCALAR;
			weapon.SetActive(true);
			yield return null;
			weapon.transform.localPosition = top.transform.localPosition;
			yield return null;
			iTween.MoveTo(weapon, iTween.Hash("position", bottom.transform.localPosition, "islocal", true, "time", WEAPON_TWEEN_TIME, "easetype", iTween.EaseType.linear));
			Audio.play(BAD_WILD_METEOR_LAUNCH);
			yield return new TIWaitForSeconds(WEAPON_TWEEN_TIME - waitSkipTime);
		}

		if (currentMutation != null)
		{
			StartCoroutine(activateWilds());

			yield return new TIWaitForSeconds(waitSkipTime);
		}
		Destroy(weapon);

		StartCoroutine(evaluateBattleResult());
	}

	// find the number of symbols that will mutate, we use this for calculating some wait times
	private int getNumMutatedSymbols()
	{
		int numSymbols = 0;
		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			if (currentWildWon)
			{
				for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
				{
					if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
					{
						numSymbols++;
					}
				}
			}
			else
			{
				for (int j = currentMutation.triggerSymbolNames.GetLength(1) - 1; j > -1; j--)
				{
					if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
					{
						numSymbols++;
					}
				}
			}
		}

		return numSymbols;
	}

	// mutate symbols to wild as rocket/asteroid go up/down respectively
	private IEnumerator activateWilds()
	{
		//Do wild process here
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			if (currentWildWon)
			{
				for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
				{
					if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
					{
						SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
						symbol.mutateTo("GW");
						Audio.play(WILD_MUTATE_SOUND);
						yield return new TIWaitForSeconds(MUTATION_TIME);
					}
				}
			}
			else
			{
				for (int j = currentMutation.triggerSymbolNames.GetLength(1) - 1; j > -1; j--)
				{
					if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
					{
						SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
						symbol.mutateTo("BW");
						Audio.play(WILD_MUTATE_SOUND);
						yield return new TIWaitForSeconds(MUTATION_TIME);
					}
				}
			}
		}
	}

	// do all the animations that happen after an attack, possibly lead to end-game
	private IEnumerator evaluateBattleResult()
	{
		for (int i = 0; i < bottomPips.Length;i++)
		{
			if (mingCount > i)
			{
				if (bottomPips[i].gameObject.activeSelf)
				{
					if (shouldShakeEarth) // earth has been hit!!
					{
						earthAnimation.Play(EARTH_HIT_ANIMATION);
						Audio.play(BAD_WILD_HITS_EARTH);

						shouldShakeEarth = false;
						yield return new TIWaitForSeconds(0.5f);
						
						flashGordonAnim.Play(FLASH_HIT_ANIMATION);
						Audio.play(EARTH_HIT_SOUND);

						yield return new TIWaitForSeconds(0.5f);

						earthAnimation.Play(EARTH_IDLE_ANIMATION);
						yield return new TIWaitForSeconds(0.5f);
						
						bottomPips[i].Play(FLASH_HEALTH_DEACTIVATE_ANIMATION);
						Audio.play(PIP_DEACTIVATE_SOUND);
						yield return new TIWaitForSeconds(bottomPips[i].clip.length);
						bottomPips[i].gameObject.SetActive(false);
					}
				}
			}
		}

		for (int j = 0; j < topPips.Length;j++) 
		{
			if (flashCount > j) 
			{
				if (topPips[j].gameObject.activeSelf)
				{
					if (shouldHitMing) // ming has been hit!! F U, Ming!!!
					{
						rocketExplosion.SetActive(true);
						Audio.play(MONGO_HIT_SOUND);
						yield return new TIWaitForSeconds(0.2f);
						mingAnimation.Play(MING_HIT_ANIMATION);
						Audio.play(MING_FLINCHES_SOUND);
						yield return new TIWaitForSeconds(0.8f);
						rocketExplosion.SetActive(false);



						float animTime = mingAnimation.clip.length;
						shouldHitMing = false;
						yield return new TIWaitForSeconds(0.2f);

						topPips[j].Play(MING_HEALTH_DEACTIVATE_ANIMATION);
						Audio.play(PIP_DEACTIVATE_SOUND);

						yield return new TIWaitForSeconds(topPips[j].clip.length);
						topPips[j].gameObject.SetActive(false);
						float extraWaitTime = animTime - 1.0f - topPips[j].clip.length;
						if (extraWaitTime > 0)
						{
							yield return new TIWaitForSeconds(extraWaitTime);
						}
						mingAnimation.Play(MING_IDLE_ANIMATION);
					}
				}
			}
		}
		
		if (mingCount >=3 || flashCount >=3) // set a flag so we don't accidentally end the game
		{
			numberOfFreespinsRemaining = 0;
			isReadyToSpin = false;
		}
			
		yield return new TIWaitForSeconds(POST_BATTLE_WAIT);

		if (flashCount >= 3) // go into treasure chest mini game
		{			
			yield return StartCoroutine(doReelsStopped());
			
			yield return _outcomeDisplayController.rollupRoutine;

			Audio.play(FLASH_WINS_SOUND);
			yield return StartCoroutine(handleEndGameBannerAnimation(winBanner));

			Audio.switchMusicKeyImmediate(TREASURE_CHEST_BG);
			treasureMapBG.SetActive(true);
			treasureMapOverlay.SetActive(true);
			treasureWinAmountWrapper.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			foreach (Animation chestAnim in chests)
			{
				chestAnim.Play("com03_FS_PickChest_Idle_Animation");
			}
			yield return new TIWaitForSeconds(0.5f);
			canDoPickme = true;
			Audio.play(TREASURE_CHEST_INTRO);
		}
		else if (mingCount >= 3) // end game after showing ming banner
		{	
			yield return StartCoroutine(doReelsStopped());

			yield return _outcomeDisplayController.rollupRoutine;

			Audio.play(MING_WINS_SOUND);
			yield return StartCoroutine(handleEndGameBannerAnimation(loseBanner, true));

			gameEnded();
		}
		else
		{	
			newReelsStoppedCallback();
		}
	}

	// activate and show the end game banner for a few seconds	
	private IEnumerator handleEndGameBannerAnimation(GameObject banner, bool isLose = false)
	{
		banner.SetActive(true);
		yield return new TIWaitForSeconds(2.0f);
		if (isLose)
		{
			loseTextWrapper.text = Localize.textUpper("game_over");
			loseTextOutlineWrapper.text = Localize.textUpper("game_over");
			yield return new TIWaitForSeconds(2.0f);
		}
		banner.SetActive(false);
	}

	// function that gets called on button press, just call the co-routine
	public void chestClicked(GameObject selectedChest)
	{
		foreach (Animation chest in chests)
		{
			CommonGameObject.setObjectCollidersEnabled(chest.gameObject, false);
		}

		StartCoroutine(chestClickedRoutine(selectedChest));
	}

	// reveal the picked treasure chest
	public IEnumerator chestClickedRoutine(GameObject selectedChest)
	{
		canDoPickme = false;
		Audio.play(TREASURE_CHEST_PICKED);
		int chestIndex = 0;
		for (int i = 0; i < chests.Length;i++)
		{
			if (chests[i].gameObject == selectedChest)
			{
				chestIndex = i;
			}
		}
		
		int revealAmount = 0;
		foreach (MutationBase baseMutation in mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;

			if (mutation.didWin)
			{
				foreach (Reveal reveal in mutation.reveals)
				{
					if (reveal.selected)
					{
						revealAmount = reveal.value;
					}
				}
			}
		}

		long multipliedReveal = revealAmount * GameState.baseWagerMultiplier;
		if (SlotBaseGame.instance != null)
		{
			multipliedReveal = multipliedReveal * SlotBaseGame.instance.multiplier;
		}
		//chests[chestIndex].spriteName = "tresureChest_open";
		//valueBG[chestIndex].gameObject.SetActive(true);
		possibleWinAmountsWrapper[chestIndex].gameObject.SetActive(true);
		possibleWinAmountsWrapper[chestIndex].text = CommonText.formatNumber(multipliedReveal);

		chests[chestIndex].Play("com03_FS_PickChest_Reveal_Animation");
		revealSparkles[chestIndex].SetActive(true);
		yield return new TIWaitForSeconds(chests[chestIndex].clip.length);

		long oldPayout = BonusGamePresenter.instance.currentPayout;
		lastPayoutRollupValue = oldPayout;
		chests[chestIndex] = null;

		yield return new TIWaitForSeconds(2.0f);

		StartCoroutine(startRollups(oldPayout, multipliedReveal));
	}

	// do the rollup for the treasure chest game
	private IEnumerator startRollups(long startValue, long multipleRevealed)
	{
		yield return new TIWaitForSeconds(0.3f);
		yield return StartCoroutine(SlotUtils.rollup(startValue, (startValue + multipleRevealed), treasureRollupCallback, true, 2.0f));

		StartCoroutine(revealRemainingChests());
	}

	// rollup callback for treasure chest minigame (needs to updated 2 labels simultaneously)
	private void treasureRollupCallback(long rollupValue)
	{
		BonusGamePresenter.instance.currentPayout = rollupValue;
		lastPayoutRollupValue = rollupValue;
		
		//Debug.Log("Win rollup to: " + rollupValue + ", last rollup: " + _lastRollupValue + ", currentTotal " + BonusGamePresenter.instance.currentPayout);
		
		BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(rollupValue);
		treasureWinAmountWrapper.text = CreditsEconomy.convertCredits(rollupValue);
	}

	// Reveal unpicked treasure chests
	private IEnumerator revealRemainingChests()
	{
		yield return new TIWaitForSeconds(PRE_CHEST_REVEAL_WAIT);
		foreach (MutationBase baseMutation in mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.didWin)
			{
				foreach (Reveal reveal in mutation.reveals)
				{
					if (!reveal.selected)
					{
						for (int i = 0; i < chests.Length;i++)
						{
							if (chests[i] != null)
							{
								Audio.play(TREASURE_CHEST_REVEAL_OTHERS);
								long revealValue = reveal.value * GameState.baseWagerMultiplier;
								if (SlotBaseGame.instance != null)
								{
									revealValue = revealValue * SlotBaseGame.instance.multiplier;
								}
								possibleWinAmountsWrapper[i].text = CreditsEconomy.convertCredits(revealValue);
								possibleWinAmountsWrapper[i].gameObject.SetActive(true);

								possibleWinAmountsWrapper[i].color = Color.grey;

								chestTop = chests[i].transform.Find("Chest/Chest top/Chest top").gameObject;
								chestBody = chests[i].transform.Find("Chest/Chest body/Chest body").gameObject;

								// deactivate these chests
								iTween.ValueTo(this.gameObject, iTween.Hash("from", chestTop.GetComponent<UISprite>().color, "to", Color.gray, "time", CHEST_DEACTIVATE_TIME, "onupdate", "OnColorUpdated"));

								yield return new TIWaitForSeconds(CHEST_DEACTIVATE_TIME);

								GameObject valueBackground = chests[i].transform.Find("valueBackground").gameObject;
								valueBackground.SetActive(true);
								iTween.ScaleFrom(valueBackground, iTween.Hash("scale", Vector3.zero, "islocal", true, "time", CHEST_REVEAL_TIME, "easetype", iTween.EaseType.easeInCubic));


								yield return new TIWaitForSeconds(CHEST_REVEAL_TIME);
								chests[i] = null;
								yield return new TIWaitForSeconds(CHEST_POST_REVEAL_TIME);
								break;
							}
						}
					}
				}
			}
		}
		
		gameEnded();
	}

	// helper function for deactivating chests
	private void OnColorUpdated(Color color)
	{
		chestTop.GetComponent<UISprite>().color = color;
		chestBody.GetComponent<UISprite>().color = color;
	}

	
	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected void newReelsStoppedCallback()
	{
		RoutineRunner.instance.StartCoroutine(doReelsStopped());
	}

	
	/**
	Handle what is performed after the reels are stopped
	*/
	protected override IEnumerator doReelsStopped(bool isAllowingContinueWhenReadyToEndSpin = true)
	{
		int subOutcomeCount = _outcome.getSubOutcomesReadOnly().Count;
		
		if (subOutcomeCount > 0)
		{
			//lets do those overlays
			yield return RoutineRunner.instance.StartCoroutine(doOverlay());
		}
		
		_lastOutcomePayout = 0;	// Default, unless there were wins.
		
		if (subOutcomeCount > 0 || engine.progressivesHit > engine.progressiveThreshold)
		{
			_lastOutcomePayout = _outcomeDisplayController.displayOutcome(_outcome, true);
		}
		else if (hasFreespinsSpinsRemaining && engine.animationCount == 0)
		{
			// Check if onOutcomeSpinBlockRelease callback is going to start the next autospin itself, in which case don't start it here
			if (!_outcomeDisplayController.isSpinBlocked())
			{
				startNextFreespin();
			}
		}
		else if (numberOfFreespinsRemaining == 0 && hasFreespinGameStarted && flashCount < 3) // if we're going to treasure chest mini-game, make sure not to end the game
		{
			gameEnded();
		}
		else
		{
			isSpinComplete = true;
		}
	}
}

