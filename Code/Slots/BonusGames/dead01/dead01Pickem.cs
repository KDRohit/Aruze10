using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Pickem class for the dead01 game. Really art intensive, and extremely rigid setup for the second portion of the pickem.
// The first odd thing I had to do was force tinting of the materials used in the second half in order to 'gray' out for the sequence.
// The second odd thing is a secondary text overlay, used in both pickems. The first is to ensure its above the depth of everything else.
// The second half uses it to ensure we can actually display the reveals, since the animation won't let me activate it normally.

public class dead01Pickem : ChallengeGame {

	public GameObject screen1;								// Main screen for the first screen
	public GameObject screen2;								// Main screen for the second screen

	public GameObject smoke;								// Smoke on the second screen, which needs to be disabled on crappy devices.
	public GameObject endsBonusLabel;						// Overlay font that needs to be disabled on the second screen.
	public GameObject[] colliderObjects;					// Pickem objects on the first screen
	public GameObject[] pickemAnimationObjects;				// The objects that hold the pickem animations.
	public UISprite[] pageSprites;							// References to the sprites so we can gray them out.
	public UILabel[] revealTexts;							// Credit texts to modify with our pickem data. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealTextsWrapperComponent;							// Credit texts to modify with our pickem data.

	public List<LabelWrapper> revealTextsWrapper
	{
		get
		{
			if (_revealTextsWrapper == null)
			{
				_revealTextsWrapper = new List<LabelWrapper>();

				if (revealTextsWrapperComponent != null && revealTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealTextsWrapperComponent)
					{
						_revealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealTexts)
					{
						_revealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealTextsWrapper;
		}
	}
	private List<LabelWrapper> _revealTextsWrapper = null;	
	
	public UILabel[] pageRevealTexts;						// Credit text on pages is different, so I grab it here. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] pageRevealTextsWrapperComponent;						// Credit text on pages is different, so I grab it here.

	public List<LabelWrapper> pageRevealTextsWrapper
	{
		get
		{
			if (_pageRevealTextsWrapper == null)
			{
				_pageRevealTextsWrapper = new List<LabelWrapper>();

				if (pageRevealTextsWrapperComponent != null && pageRevealTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in pageRevealTextsWrapperComponent)
					{
						_pageRevealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in pageRevealTexts)
					{
						_pageRevealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _pageRevealTextsWrapper;
		}
	}
	private List<LabelWrapper> _pageRevealTextsWrapper = null;	
	
	public UILabel[] monsterForegroundRevealTexts;			// The reveal credit/mult. text on the monster screen -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] monsterForegroundRevealTextsWrapperComponent;			// The reveal credit/mult. text on the monster screen

	public List<LabelWrapper> monsterForegroundRevealTextsWrapper
	{
		get
		{
			if (_monsterForegroundRevealTextsWrapper == null)
			{
				_monsterForegroundRevealTextsWrapper = new List<LabelWrapper>();

				if (monsterForegroundRevealTextsWrapperComponent != null && monsterForegroundRevealTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in monsterForegroundRevealTextsWrapperComponent)
					{
						_monsterForegroundRevealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in monsterForegroundRevealTexts)
					{
						_monsterForegroundRevealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _monsterForegroundRevealTextsWrapper;
		}
	}
	private List<LabelWrapper> _monsterForegroundRevealTextsWrapper = null;	
	
	public GameObject[] monsterForegroundPages;				// The copy of pages on the foreground area.
	public UILabel winAmountLabel;							// Our main win amount label. -  To be removed when prefabs are updated.
	public LabelWrapperComponent winAmountLabelWrapperComponent;							// Our main win amount label.

	public LabelWrapper winAmountLabelWrapper
	{
		get
		{
			if (_winAmountLabelWrapper == null)
			{
				if (winAmountLabelWrapperComponent != null)
				{
					_winAmountLabelWrapper = winAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winAmountLabelWrapper = new LabelWrapper(winAmountLabel);
				}
			}
			return _winAmountLabelWrapper;
		}
	}
	private LabelWrapper _winAmountLabelWrapper = null;
	
	public UILabel pageCountRemaining;						// We start with 0 pages, and count up till we have 3, and go on.  -  To be removed when prefabs are updated.
	public LabelWrapperComponent pageCountRemainingWrapperComponent;						// We start with 0 pages, and count up till we have 3, and go on. 

	public LabelWrapper pageCountRemainingWrapper
	{
		get
		{
			if (_pageCountRemainingWrapper == null)
			{
				if (pageCountRemainingWrapperComponent != null)
				{
					_pageCountRemainingWrapper = pageCountRemainingWrapperComponent.labelWrapper;
				}
				else
				{
					_pageCountRemainingWrapper = new LabelWrapper(pageCountRemaining);
				}
			}
			return _pageCountRemainingWrapper;
		}
	}
	private LabelWrapper _pageCountRemainingWrapper = null;
	
	public UILabelStyle revealPageGrayedOut;				// UILabelStyle to swap out for the reveal sequence

	public Animator[] monsterAnimators;						// Left arm/Face/Right Arm animatiors to call for anmiation purposes
	public Animation vortexAnimator;						// The vortex in the bg is disabled until modified.
	public GameObject[] selectableMonsterPieces;			// Our objects with colliders on the monster
	public UILabel[] monsterRevealTexts;					// The texts associated with the animated reveal on the monster. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] monsterRevealTextsWrapperComponent;					// The texts associated with the animated reveal on the monster.

	public List<LabelWrapper> monsterRevealTextsWrapper
	{
		get
		{
			if (_monsterRevealTextsWrapper == null)
			{
				_monsterRevealTextsWrapper = new List<LabelWrapper>();

				if (monsterRevealTextsWrapperComponent != null && monsterRevealTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in monsterRevealTextsWrapperComponent)
					{
						_monsterRevealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in monsterRevealTexts)
					{
						_monsterRevealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _monsterRevealTextsWrapper;
		}
	}
	private List<LabelWrapper> _monsterRevealTextsWrapper = null;	
	
	public UILabel[] monsterRevealShadowTexts;				// The shadow texts on the monster. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] monsterRevealShadowTextsWrapperComponent;				// The shadow texts on the monster.

	public List<LabelWrapper> monsterRevealShadowTextsWrapper
	{
		get
		{
			if (_monsterRevealShadowTextsWrapper == null)
			{
				_monsterRevealShadowTextsWrapper = new List<LabelWrapper>();

				if (monsterRevealShadowTextsWrapperComponent != null && monsterRevealShadowTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in monsterRevealShadowTextsWrapperComponent)
					{
						_monsterRevealShadowTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in monsterRevealShadowTexts)
					{
						_monsterRevealShadowTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _monsterRevealShadowTextsWrapper;
		}
	}
	private List<LabelWrapper> _monsterRevealShadowTextsWrapper = null;	
	
	public UILabel[] monsterPageRevealTexts;				// The page texts associated with the animated reveal on the monster. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] monsterPageRevealTextsWrapperComponent;				// The page texts associated with the animated reveal on the monster.

	public List<LabelWrapper> monsterPageRevealTextsWrapper
	{
		get
		{
			if (_monsterPageRevealTextsWrapper == null)
			{
				_monsterPageRevealTextsWrapper = new List<LabelWrapper>();

				if (monsterPageRevealTextsWrapperComponent != null && monsterPageRevealTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in monsterPageRevealTextsWrapperComponent)
					{
						_monsterPageRevealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in monsterPageRevealTexts)
					{
						_monsterPageRevealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _monsterPageRevealTextsWrapper;
		}
	}
	private List<LabelWrapper> _monsterPageRevealTextsWrapper = null;	
	
	public UILabel[] monsterPageRevealShadowTexts;			// The page shadow texts on the monster. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] monsterPageRevealShadowTextsWrapperComponent;			// The page shadow texts on the monster.

	public List<LabelWrapper> monsterPageRevealShadowTextsWrapper
	{
		get
		{
			if (_monsterPageRevealShadowTextsWrapper == null)
			{
				_monsterPageRevealShadowTextsWrapper = new List<LabelWrapper>();

				if (monsterPageRevealShadowTextsWrapperComponent != null && monsterPageRevealShadowTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in monsterPageRevealShadowTextsWrapperComponent)
					{
						_monsterPageRevealShadowTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in monsterPageRevealShadowTexts)
					{
						_monsterPageRevealShadowTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _monsterPageRevealShadowTextsWrapper;
		}
	}
	private List<LabelWrapper> _monsterPageRevealShadowTextsWrapper = null;	
	
	public Material[] monsterMaterials;						// Materials used for each part of the monster.
	public UILabelStyle revealCreditGrayedOut;				// Our label style to swap out on the monster.

	private bool[] hasPageBeenSelected = new bool[15];		// Just a bool array to keep track of selected pages, since there's so much that can change per selection.

	private SkippableWait revealWait = new SkippableWait();
	private PickemOutcome pickemOutcome;
	private WheelOutcome monsterOutcome;
	private PickemPick pick;
	private WheelPick monsterPick;
	private CoroutineRepeater pickemRepeater;

	private PlayingAudio treeAmbianceLoop;

	private bool gameEnded = false;
	private bool game2Ended = false;
	private bool canAnimate = true;

	private float soundCounter = -1;						// Counter used to trigger incantations on the second screen.
	private int pageFoundCount;								// Page counter used for the first screen pickem.

	// Audio keys
	private const string REVEAL_PAGE = "DEDRevealPages";
	private const string LEVEL1_BG = "DestroyEvilDead2Level1";
	private const string LEVEL2_BG = "DestroyEvilDead2Level2";
	private const string INTRO_VO = "DEDIntroVO";
	private const string PICKME_SFX = "DEDPickMe";
	private const string PAGE_1_VO = "ANNosferatos1";
	private const string PAGE_2_VO = "ANAlAmmemnon1";
	private const string PAGE_3_VO = "ANKanda1";
	private const string FINAL_KANDA = "ANKanda3";
	private const string TREE_TRANSITION = "DEDTransitionToTree";
	private const string TREE_VO = "DEDTreeVO";
	private const string REVEAL_CREDIT = "DEDRevealCredit";
	private const string REVEAL_HENRIETTA = "DEDRevealHenrietta";
	private const string REVEAL_HENRIETTA_VO = "DEDRevealHenriettaVO";
	private const string INCANTATION_VO = "DEDincantationVO";
	private const string MULTIPLIER_AMBIANCE = "PickAMultiplierAmbienceLoop";
	private const string REVEAL_MULTIPLIER = "DEDRevealMultiplier";
	private const string FINAL_PICK_VO = "DEDFinalPickVO";
	private const string REVEAL_CHAINSAW = "DEDRevealChainsaw";
	private const string PICK_TOP_MULTIPLIER = "DEDPickTopMultiplier";
	private const string SUMMARY_VO = "SummaryBonusEvilDead2";

	// Animation keys
	private const string REVEAL_SPELL_ANIM = "Dead01_PickingBonus_PickObject_RevealSpellPage_Animation";
	private const string REVEAL_CREDIT_ANIM = "Dead01_PickingBonus_PickObject_RevealNumber_Animation";
	private const string REVEAL_END_ANIM = "Dead01_PickingBonus_PickObject_RevealEndBonus_Animation";
	private const string REVEAL_SPELL_LOOP_ANIM = "Dead01_PickingBonus_PickObject_RevealSpellPageGlowLoop_Animation";
	private const string PICKEM_ANIM = "Dead01_PickingBonus_PickObject_PickMe_Animation";
	private const string PICKME_LEFT_HAND = "Dead01_MonsterPick_PickMe_Lf_Hand";
	private const string PICKME_FACE = "Dead01_MonsterPick_PickMe_face";
	private const string PICKME_RIGHT_HAND = "Dead01_MonsterPick_PickMe_Rt_Hand";
	private const string MONSTER_INTRO = "Dead01_MonsterPick_Intro";
	private const string MONSTER_OUTRO = "Dead01_MonsterPick_Outtro";
	private const string MONSTER_IDLE_LEFT_HAND = "Dead01_MonsterPick_Idle_Lf_Hand";
	private const string MONSTER_IDLE_FACE = "Dead01_MonsterPick_Idle_face";
	private const string MONSTER_IDLE_RIGHT_HAND = "Dead01_MonsterPick_Idle_Rt_Hand";
	private const string MONSTER_STILL_LEFT_HAND = "Dead01_MonsterPick_Still_Lf_Hand";
	private const string MONSTER_STILL_FACE = "Dead01_MonsterPick_Still_face";
	private const string MONSTER_STILL_RIGHT_HAND = "Dead01_MonsterPick_Still_Rt_Hand";
	private const string MONSTER_REVEAL_NUMBER_LEFT_HAND = "Dead01_MonsterPick_RevealedNumber_Lf_Hand";
	private const string MONSTER_REVEAL_NUMBER_FACE = "Dead01_MonsterPick_RevealedNumber_face";
	private const string MONSTER_REVEAL_NUMBER_RIGHT_HAND = "Dead01_MonsterPick_RevealedNumber_Rt_Hand";
	private const string MONSTER_REVEAL_PAGE_LEFT_HAND = "Dead01_MonsterPick_RevealedPage_Lf_Hand";
	private const string MONSTER_REVEAL_PAGE_FACE = "Dead01_MonsterPick_RevealedPage_face";
	private const string MONSTER_REVEAL_PAGE_RIGHT_HAND = "Dead01_MonsterPick_RevealedPage_Rt_Hand";

	private const float MIN_TIME_ANIM = 2.0f;
	private const float MAX_TIME_ANIM = 5.0f;
	private const float TIME_BETWEEN_REVEALS = 0.25f;

	public override void init()
	{
		pickemRepeater = new CoroutineRepeater(MIN_TIME_ANIM, MAX_TIME_ANIM, animCallback);
		pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		pick = pickemOutcome.getNextEntry();

		Audio.switchMusicKeyImmediate(LEVEL1_BG);
		Audio.play(INTRO_VO);

		// Let's ensure the materials on the monster are gray first.
		for (int i = 0; i < 3; i++)
		{
			monsterMaterials[i].SetColor ("_EmisColor", Color.gray);
		}

		_didInit = true;
	}

	// play the next pickem animation. This controls the pickem on the first AND second screens.
	private IEnumerator animCallback()
	{
		int pickemIndex = Random.Range(0,14);

		if (canAnimate && !gameEnded && !hasPageBeenSelected[pickemIndex])
		{
			Audio.play(PICKME_SFX);
			pickemAnimationObjects[pickemIndex].GetComponent<Animation>().Play(PICKEM_ANIM);
		}

		if (canAnimate && gameEnded && !game2Ended)
		{
			pickemIndex = Random.Range(0,3);
			if (pickemIndex == 0)
			{
				monsterAnimators[pickemIndex].Play(PICKME_LEFT_HAND);
			}
			else if (pickemIndex == 1)
			{
				monsterAnimators[pickemIndex].Play(PICKME_FACE);
			}
			else if (pickemIndex == 2)
			{
				monsterAnimators[pickemIndex].Play(PICKME_RIGHT_HAND);
			}

			yield return new TIWaitForSeconds(0.5f);

			if (pickemIndex == 0)
			{
				monsterAnimators[pickemIndex].Play(MONSTER_IDLE_LEFT_HAND);
			}
			else if (pickemIndex == 1)
			{
				monsterAnimators[pickemIndex].Play(MONSTER_IDLE_FACE);
			}
			else
			{
				monsterAnimators[pickemIndex].Play(MONSTER_IDLE_RIGHT_HAND);
			}
		}

		yield return new TIWaitForSeconds(0.5f);
	}

	// Our update controls the pickem repeater, and an audio cue in the second screen.
	protected override void Update()
	{
		base.Update();
		if ((!gameEnded || !game2Ended) && canAnimate && _didInit)
		{
			pickemRepeater.update();
		}

		if (soundCounter > 0 && canAnimate)
		{
			soundCounter -= Time.deltaTime;
			if (soundCounter < 0)
			{
				Audio.play(INCANTATION_VO);
				soundCounter = 2.0f;
			}
		}
	}

	// Basic page selected callback from the first pickem screen.
	public void pageSelected(GameObject page)
	{
		if (!canAnimate)
		{
			return;
		}

		canAnimate = false;
		CommonGameObject.setObjectCollidersEnabled(page, false);
		int arrayIndex = System.Array.IndexOf(colliderObjects, page);
		hasPageBeenSelected[arrayIndex] = true;
		StartCoroutine(revealSinglePick(arrayIndex));
	}

	private IEnumerator revealSinglePick(int arrayIndex)
	{
		BonusGamePresenter.instance.currentPayout += pick.credits;

		if (pick.groupId == "tornado")
		{
			// Page was found! Let's increase the page count, update the text, play the anim, and the appropriate sounds.
			pageFoundCount++;
			pageCountRemainingWrapper.text = (3 - pageFoundCount).ToString();
			pickemAnimationObjects[arrayIndex].GetComponent<Animation>().Play(REVEAL_SPELL_ANIM);
			pageRevealTextsWrapper[arrayIndex].text = CreditsEconomy.convertCredits(pick.credits);
			Audio.play(REVEAL_PAGE);
			yield return new TIWaitForSeconds(1.0f);

			if (pageFoundCount == 1)
			{
				Audio.play(PAGE_1_VO);
			}
			else if (pageFoundCount == 2)
			{
				Audio.play(PAGE_2_VO);
			}
			else if (pageFoundCount == 3)
			{
				// 3 pages means endgame. Let's start that unique sequence.
				Audio.play(PAGE_3_VO);
				yield return new TIWaitForSeconds(0.5f);
				Audio.play(TREE_TRANSITION);
				Audio.switchMusicKeyImmediate(LEVEL2_BG);
				Audio.play(TREE_VO, 1, 0, 1.7f);
				soundCounter = 2.5f;
			}

			if (pick.credits != 0)
			{
				yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - pick.credits , BonusGamePresenter.instance.currentPayout, winAmountLabelWrapper, true, 0.25f));
			}

			pickemAnimationObjects[arrayIndex].GetComponent<Animation>().Play(REVEAL_SPELL_LOOP_ANIM);

			if (pageFoundCount == 3)
			{
				// Let's prep the next game and do the reveals, instead of getting the next pick.
				SlotOutcome challengeBonus = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, pick.bonusGame);
				monsterOutcome = new WheelOutcome(challengeBonus);
				StartCoroutine(revealAllPicks(false));
			}
			else
			{
				// Get the next pick and allow pickem anims to occur again.
				pick = pickemOutcome.getNextEntry();
				canAnimate = true;
			}
		}
		else if (pick.groupId == "shark")
		{
			// End game sequence was hit. Play the sound and VO, rollup, and final reveals.
			gameEnded = true;
			Audio.play(REVEAL_HENRIETTA);
			Audio.play(REVEAL_HENRIETTA_VO);
			pickemAnimationObjects[arrayIndex].GetComponent<Animation>().Play(REVEAL_END_ANIM);
			yield return new TIWaitForSeconds(0.1f);
			yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - pick.credits , BonusGamePresenter.instance.currentPayout, winAmountLabelWrapper, true, 0.25f));
			StartCoroutine(revealAllPicks(true));
		}
		else
		{
			// Just a credit was hit. update what needs to be updated and get the next pick.
			Audio.play(REVEAL_CREDIT);
			pickemAnimationObjects[arrayIndex].GetComponent<Animation>().Play(REVEAL_CREDIT_ANIM);
			revealTextsWrapper[arrayIndex].text = CreditsEconomy.convertCredits(pick.credits);
			yield return new TIWaitForSeconds(0.1f);
			yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - pick.credits , BonusGamePresenter.instance.currentPayout, winAmountLabelWrapper, true, 0.25f));
			pick = pickemOutcome.getNextEntry();
			canAnimate = true;
		}
	}

	// The things we need to ensure are toggled correctly before going into the second screen.
	private void beginSecondGameSection()
	{
		// Art requested this be disabled on crappy devices, so we're disabling it.
		if (!MobileUIUtil.isCrappyDevice && smoke != null)
		{
			smoke.SetActive(false);
		}

		if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
		{
			BonusGameManager.instance.wings.forceShowSecondaryChallengeWings(true);
		}

		gameEnded = true;
		//vortexAnimator.gameObject.SetActive(false);
		vortexAnimator.Play();
		endsBonusLabel.SetActive(false);
		screen1.SetActive(false);
		screen2.SetActive(true);
		treeAmbianceLoop = Audio.play(MULTIPLIER_AMBIANCE, 1, 0, 0, float.PositiveInfinity);

		// Make sure we start on the intro sequences. It'll autoplay the credit sequences otherwise.
		for (int i = 0; i < 3; i++)
		{
			monsterAnimators[i].Play(MONSTER_INTRO);
			StartCoroutine(beginIdleAnimations());
		}
	}

	private IEnumerator beginIdleAnimations()
	{
		yield return new TIWaitForSeconds(1.0f);
		for (int i = 0; i < 3; i++)
		{
			if (i == 0)
			{
				monsterAnimators[i].Play(MONSTER_IDLE_LEFT_HAND);
			}
			else if (i == 1)
			{
				monsterAnimators[i].Play(MONSTER_IDLE_FACE);
			}
			else
			{
				monsterAnimators[i].Play(MONSTER_IDLE_RIGHT_HAND);
			}
		}

		canAnimate = true;
	}

	// Reveal method for the first pickem screen.
	private IEnumerator revealAllPicks(bool gameEnded)
	{
		for (int i = 0; i < colliderObjects.Length; i++)
		{
			CommonGameObject.setObjectCollidersEnabled(colliderObjects[i], false);
		}
		
		yield return new TIWaitForSeconds(1.0f);
		
		for (int i = 0; i < hasPageBeenSelected.Length; i++)
		{
			if (!hasPageBeenSelected[i])
			{
				// We make sure the page sprite is grayed out all the time, in case.
				UISprite pageSprite = pageSprites[i].GetComponent<UISprite>();
				if (pageSprite != null)
				{
					pageSprite.color = Color.gray;
				}

				pick = pickemOutcome.getNextReveal();

				if (pick.groupId == "tornado")
				{
					// Reveal was a page, let's update the text and gray out that reveal credit as well.
					pickemAnimationObjects[i].GetComponent<Animation>().Play(REVEAL_SPELL_ANIM);
					pageRevealTextsWrapper[i].text = CreditsEconomy.convertCredits(pick.credits);
					UILabelStyler labelStyler = pageRevealTextsWrapper[i].gameObject.GetComponent<UILabelStyler>();
					if (labelStyler != null)
					{
						labelStyler.style = revealPageGrayedOut;
						labelStyler.updateStyle();
					}
				}
				else if (pick.groupId == "shark")
				{
					// Just play the end bonus anim here.
					pickemAnimationObjects[i].GetComponent<Animation>().Play(REVEAL_END_ANIM);
				}
				else
				{
					// Basic credit revealed, let's update that text now.
					pickemAnimationObjects[i].GetComponent<Animation>().Play(REVEAL_CREDIT_ANIM);
					revealTextsWrapper[i].text = CreditsEconomy.convertCredits(pick.credits);
					UILabelStyler labelStyler = revealTextsWrapper[i].gameObject.GetComponent<UILabelStyler>();
					if (labelStyler != null)
					{
						labelStyler.style = revealPageGrayedOut;
						labelStyler.updateStyle();
					}
				}
				
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}
		
		yield return new TIWaitForSeconds(1.0f);

		if (gameEnded)
		{
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			beginSecondGameSection();
		}
	}

	// Hey, look at that , we clicked a monster piece!
	public void monsterClicked(GameObject monsterPiece)
	{
		if (!canAnimate)
		{
			return;
		}

		canAnimate = false;
		CommonGameObject.setObjectCollidersEnabled(monsterPiece, false);
		int arrayIndex = System.Array.IndexOf(selectableMonsterPieces, monsterPiece);

		Audio.stopSound(treeAmbianceLoop);
		Audio.play(FINAL_KANDA);

		for (int i = 0; i < 3; i++)
		{
			if (i == 0)
			{
				monsterAnimators[i].Play(MONSTER_STILL_LEFT_HAND);
			}
			else if (i == 1)
			{
				monsterAnimators[i].Play(MONSTER_STILL_FACE);
			}
			else
			{
				monsterAnimators[i].Play(MONSTER_STILL_RIGHT_HAND);
			}
		}

		monsterPick = monsterOutcome.getNextEntry();

		if (monsterPick.credits != 0)
		{
			// Only credits were found, let's add those up and update the texts.
			Audio.play(REVEAL_MULTIPLIER);
			BonusGamePresenter.instance.currentPayout += monsterPick.credits;
			StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - monsterPick.credits , BonusGamePresenter.instance.currentPayout, winAmountLabelWrapper, true, 0.25f));
			monsterRevealTextsWrapper[arrayIndex].text = CreditsEconomy.convertCredits(monsterPick.credits);
			monsterRevealShadowTextsWrapper[arrayIndex].text = CreditsEconomy.convertCredits(monsterPick.credits);
		}
		else
		{
			// Ooooo, multiplier was found. let's start the audio queues and update the count.
			Audio.play(FINAL_PICK_VO);
			Audio.play(REVEAL_CHAINSAW, 1, 0, 0.5f);
			Audio.play(REVEAL_CREDIT);
			BonusGamePresenter.instance.currentPayout *= monsterPick.multiplier;
			monsterPageRevealTextsWrapper[arrayIndex].text = monsterPick.multiplier.ToString() + "x";
			monsterPageRevealShadowTextsWrapper[arrayIndex].text = monsterPick.multiplier.ToString() + "x";
			StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout / monsterPick.multiplier, BonusGamePresenter.instance.currentPayout, winAmountLabelWrapper, true, 0.25f));
		}

		// Now we play the animations based on which piece was selected. They're so unique, this has to be kinda lengthy.
		if (arrayIndex == 0)
		{
			if (monsterPick.credits != 0)
			{
				monsterAnimators[0].Play(MONSTER_REVEAL_NUMBER_LEFT_HAND);
			}
			else
			{
				monsterAnimators[0].Play(MONSTER_REVEAL_PAGE_LEFT_HAND);
			}
		}
		else if (arrayIndex == 1)
		{
			if (monsterPick.credits != 0)
			{
				monsterAnimators[1].Play(MONSTER_REVEAL_NUMBER_FACE);
			}
			else
			{
				monsterAnimators[1].Play(MONSTER_REVEAL_PAGE_FACE);
			}
		}
		else
		{
			if (monsterPick.credits != 0)
			{
				monsterAnimators[2].Play(MONSTER_REVEAL_NUMBER_RIGHT_HAND);
			}
			else
			{
				monsterAnimators[2].Play(MONSTER_REVEAL_PAGE_RIGHT_HAND);
			}
		}

		StartCoroutine(revealRemainingMonsterPiecesAndEndGame(arrayIndex));
	}

	// After selection, let's cycle through the rest and show what we need to show.
	private IEnumerator revealRemainingMonsterPiecesAndEndGame(int arrayIndex)
	{	
		// Give the initial sequence some time first....
		yield return new TIWaitForSeconds(2.0f);

		int winsIndex = 0;
		
		if (monsterPick.winIndex == winsIndex)
		{
			winsIndex++;
		}

		for (int i = 0; i < 3;i++)
		{
			if (i != arrayIndex)
			{
				// Let's set each material to black to ensure it looks 'grayed out'. Then update the text accordingly.
				monsterMaterials[i].SetColor ("_EmisColor", Color.black);

				if(!revealWait.isSkipping)
				{
					Audio.play("reveal_others");
				}
				if (monsterPick.wins[winsIndex].credits != 0)
				{
					monsterForegroundRevealTextsWrapper[i].text = CreditsEconomy.convertCredits(monsterPick.wins[winsIndex].credits);
				}
				else
				{
					monsterForegroundRevealTextsWrapper[i].text = monsterPick.wins[winsIndex].multiplier.ToString() + "x";
					monsterForegroundPages[i].SetActive(true);
				}

				monsterForegroundRevealTextsWrapper[i].gameObject.SetActive(true);

				winsIndex++;
				if (monsterPick.winIndex == winsIndex)
				{
					winsIndex++;
				}

				yield return new TIWaitForSeconds(0.5f);
			}
		}

		yield return new TIWaitForSeconds(0.3f);

		// Now let's remove the texts and set the color back to normal.
		for (int i = 0; i < 3; i++)
		{
			monsterForegroundRevealTextsWrapper[i].gameObject.SetActive(false);
			monsterForegroundPages[i].SetActive(false);
			monsterMaterials[i].SetColor ("_EmisColor", Color.gray);
		}

		// Play the outro and get the hell out!
		for (int i = 0; i < 3; i++)
		{
			monsterAnimators[i].Play(MONSTER_OUTRO);
		}
		//vortexAnimator.gameObject.SetActive(true);
		//vortexAnimator.Play();
		Audio.play(PICK_TOP_MULTIPLIER);
		// Give it some time, play the song, and get the fuck out.
		yield return new TIWaitForSeconds(5.5f);
		Audio.play(SUMMARY_VO);
		BonusGamePresenter.instance.gameEnded();
	}
}

