using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Our controller for the osa05 pickem game. Clone, more or less, or the beetle bailey fight game.
public class osa05Pickem : ChallengeGame 
{

	// Our arrays in the pickem
	public GameObject[] colliderObjects;					// The sprites that the user can see
	public GameObject[] pickemAnimators;					// The unique pickem animator for each page
	public GameObject[] revealAnimations;					// The unique reveal animator for each page
	public GameObject[] gameOverAnimations;					// The wizard head bad reveal animation for each page
	public UISprite[] revealSprites;						// The flying icons when getting a multiplier
	public UILabel[] revealTexts;							// The text that appears after credits or multipliers -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealTextsWrapperComponent;							// The text that appears after credits or multipliers

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
	

	// The one off public objects for our pickem
	public GameObject cardDissapearSparkle;					// The object to dispaly when the flying card dissapears.
	public UILabel multiplierText;							// The main multiplier text below the wizard head -  To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierTextWrapperComponent;							// The main multiplier text below the wizard head

	public LabelWrapper multiplierTextWrapper
	{
		get
		{
			if (_multiplierTextWrapper == null)
			{
				if (multiplierTextWrapperComponent != null)
				{
					_multiplierTextWrapper = multiplierTextWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierTextWrapper = new LabelWrapper(multiplierText);
				}
			}
			return _multiplierTextWrapper;
		}
	}
	private LabelWrapper _multiplierTextWrapper = null;
	
	public Animation wizardHead;							// Controller for the wizard mouth
	public Animator wizardEffects;							// Controls the good/bad fire
	public ParticleSystem leftFire;							// The left outer (red) fire
	public ParticleSystem rightFire;						// The right outer (red) fire
	public GameObject fireballTrail;						// The trail used in the mulitplier sequence
	public GameObject fireballExplosion;					// The explosion for after the trail
	public UILabel winAmountLabel;							// The bottom right label -  To be removed when prefabs are updated.
	public LabelWrapperComponent winAmountLabelWrapperComponent;							// The bottom right label

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
	
	public UILabel bannerText;								// The text in the bottom center -  To be removed when prefabs are updated.
	public LabelWrapperComponent bannerTextWrapperComponent;								// The text in the bottom center

	public LabelWrapper bannerTextWrapper
	{
		get
		{
			if (_bannerTextWrapper == null)
			{
				if (bannerTextWrapperComponent != null)
				{
					_bannerTextWrapper = bannerTextWrapperComponent.labelWrapper;
				}
				else
				{
					_bannerTextWrapper = new LabelWrapper(bannerText);
				}
			}
			return _bannerTextWrapper;
		}
	}
	private LabelWrapper _bannerTextWrapper = null;
	
	public UILabelStyle grayedOutRevealStyle;				// Our swapped out reveal text
	public GameObject gameOverTextAnimation;				// The game over text animation below the wizard head

	private SkippableWait revealWait = new SkippableWait();
	private PickemOutcome pickemOutcome;
	private PickemPick pick;
	private CoroutineRepeater pickemRepeater;

	private PlayingAudio bgLoop;
	private PlayingAudio ambientLoop;

	private long localMultiplier = 1;						// The int that keeps track of the multipliers stacked in-game.

	private bool gameEnded = false;
	private bool canAnimate = true;

	private const float MIN_TIME_ANIM = 2.0f;
	private const float MAX_TIME_ANIM = 5.0f;
	private const float TIME_BETWEEN_REVEALS = 0.5f;

	private const string PICK_ITEM_LOC = "pick_an_item";
	private const string PICK_AGAIN = "pick_again";

	private const string WIZARD_HEAD_ANIM = "AnimationLoop";
	private const string WIZARD_HEAD_IDLE_ANIM = "Idle";
	private const string ICON_REVEAL_ANIM = "OSA05_PB_Pick_Icon_Reveal_Animation";
	private const string PARTICLE_IDLE_ANIM = "OSA05_PB_WizardHead_ParticleSet_Idle_Animation";
	private const string PARTICLE_GOOD_ANIM = "OSA05_PB_WizardHead_ParticleSet_LipSynching_Animation";
	private const string PARTICLE_BAD_ANIM = "OSA05_PB_WizardHead_ParticleSet_PickBad_Animation";

	private const string INTRO_VO = "TRIntroVO";
	private const string BONUS_BG = "TRBonusBg";
	private const string SMOKE_BG = "TRSmoke";
	private const string PICK_ME = "TRPickme";
	private const string SUMMARY_VO = "TRSummary";
	private const string MULTIPLIER_TRAVEL = "TRMultiplierTravel";
	private const string MULTIPLIER_CREDIT = "TRMultiplyCredit";
	private const string ANGRY_WIZARD = "TRRevealAngryWizard";
	private const string REVEAL_SPECIAL = "TRRevealSpecial";
	private const string REVEAL_CREDIT = "TRRevealCredit";
	private const string REVEAL_X = "TRRevealAdvanceX";
	private const string IGNITE_SPECIAL = "TRIgniteSpecial";
	private const string TINMAN_VO_1 = "WZStepForwardTinMan";
	private const string TINMAN_VO_2 = "WZIAwardYouATestimonial";
	private const string LION_VO_1 = "WZAndYouLionWell";
	private const string LION_VO_2 = "WZIAwardYouAMedal";
	private const string SCARECROW_VO_1 = "WZAndYouScarecrow";
	private const string SCARECROW_VO_2 = "WZIAwardYouADiploma";
	private const string DOROTHY_VO_1 = "WZAndYouWhippersnapper";
	private const string DOROTHY_VO_2 = "WZOnlyWayDorothyKansasMeTakeHer";
	private const string DO_NOT_AROUSE_ME = "WZDoNotArouseWrathGreatPowerfulOz";

	public override void init()
	{
		// The reveals are set to true, let's set them false first. Yeah, I could just do it in the prefab, but whatever, don't hassle me asshole.
		for (int i = 0; i < revealSprites.Length;i++)
		{
			revealSprites[i].gameObject.SetActive(false);
		}

		bgLoop = Audio.play(BONUS_BG, 1, 0, 0, float.PositiveInfinity);
		ambientLoop = Audio.play(SMOKE_BG, 1, 0, 0, float.PositiveInfinity);
		bannerTextWrapper.text = Localize.text(PICK_ITEM_LOC);
		StartCoroutine(beginBeginningWizard());

		pickemRepeater = new CoroutineRepeater(MIN_TIME_ANIM, MAX_TIME_ANIM, animCallback);
		pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		pick = pickemOutcome.getNextEntry();
		_didInit = true;
	}

	private IEnumerator beginBeginningWizard()
	{
		Audio.play(INTRO_VO);
		wizardHead.Play(WIZARD_HEAD_ANIM);
		yield return new TIWaitForSeconds(5.5f);
		wizardHead.Play(WIZARD_HEAD_IDLE_ANIM);
	}

	// play the next idle animation, eventually looping back around to the first one
	private IEnumerator animCallback()
	{
		int pickemIndex = Random.Range(0,14);
		if (canAnimate && !gameEnded && !revealTextsWrapper[pickemIndex].gameObject.activeSelf)
		{
			CommonGameObject.alphaGameObject(revealAnimations[pickemIndex], 0);
			pickemAnimators[pickemIndex].SetActive(true);
			Audio.play(PICK_ME);
			yield return new TIWaitForSeconds(0.75f);
			pickemAnimators[pickemIndex].SetActive(false);
			CommonGameObject.alphaGameObject(revealAnimations[pickemIndex], 1);
		}
	}
	
	protected override void Update()
	{
		base.Update();
		if (!gameEnded && canAnimate && _didInit)
		{
			pickemRepeater.update();
		}
	}

	// Callback from clicking an icon.
	public void pickSelected(GameObject icon)
	{
		bannerTextWrapper.text = "";
		int arrayIndex = System.Array.IndexOf(colliderObjects, icon);
		StartCoroutine(revealSinglePick(arrayIndex));
		canAnimate = false;
	}

	private IEnumerator revealSinglePick(int arrayIndex)
	{
		// Let's hide the pickem for this selection, then play the reveal animation.
		pickemAnimators[arrayIndex].SetActive(false);
		CommonGameObject.alphaGameObject(revealAnimations[arrayIndex], 1);
		revealAnimations[arrayIndex].SetActive(true);
		revealAnimations[arrayIndex].GetComponent<Animation>().Play(ICON_REVEAL_ANIM);

		for (int i = 0; i < colliderObjects.Length; i++)
		{
			CommonGameObject.setObjectCollidersEnabled(colliderObjects[i].gameObject, false);
		}

		if (pick.credits != 0 && pick.groupId != "FIGHT")
		{
			Audio.play(REVEAL_CREDIT);
		}

		yield return new TIWaitForSeconds(0.5f);

		// Now let's do what we need to based on the pick itself.
		if (pick.credits != 0 && pick.groupId != "FIGHT")
		{
			// Let's reveal the text and add it up.
			revealTextsWrapper[arrayIndex].gameObject.SetActive(true);
			revealTextsWrapper[arrayIndex].text = CreditsEconomy.convertCredits(pick.credits);
			BonusGamePresenter.instance.currentPayout += (pick.credits * localMultiplier);
			if (localMultiplier == 1)
			{
				// This is the basic sequence, sans multipliers.
				yield return new TIWaitForSeconds(0.5f);
				yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - (pick.credits * localMultiplier), BonusGamePresenter.instance.currentPayout, winAmountLabelWrapper));
				pick = pickemOutcome.getNextEntry();
				canAnimate = true;
				bannerTextWrapper.text = Localize.text(PICK_AGAIN);
				for (int i = 0; i < colliderObjects.Length; i++)
				{
					if (!revealTextsWrapper[i].gameObject.activeSelf)
					{
						CommonGameObject.setObjectCollidersEnabled(colliderObjects[i].gameObject, true);
					}
				}
			}
			else
			{	
				// Otherwise, we do the fireball sequence.
				StartCoroutine(startFireballSequence(arrayIndex));
			}
		}
		else if (pick.groupId == "FIGHT" && pick.multiplier != 0)
		{
			// This is a fight, where we reveal the sprite and start the multiplier sequence.
			Audio.play(REVEAL_SPECIAL);
			localMultiplier += pick.multiplier;

			revealSprites[arrayIndex].gameObject.SetActive(true);

			if (localMultiplier == 2)
			{
				revealSprites[arrayIndex].spriteName = "Heart_Icon";
			}
			else if (localMultiplier == 3)
			{
				revealSprites[arrayIndex].spriteName = "Diploma_Icon";
			}
			else if (localMultiplier == 4)
			{
				revealSprites[arrayIndex].spriteName = "Medal_Icon";
			}
			else if (localMultiplier == 5)
			{
				revealSprites[arrayIndex].spriteName = "Slippers_Icon";
			}

			StartCoroutine(startMultiplierSequence(arrayIndex));
		}
		else
		{
			Audio.play(REVEAL_SPECIAL);
			localMultiplier += 1;

			revealSprites[arrayIndex].gameObject.SetActive(true);

			if (localMultiplier == 2)
			{
				revealSprites[arrayIndex].spriteName = "Heart_Icon";
			}
			else if (localMultiplier == 3)
			{
				revealSprites[arrayIndex].spriteName = "Diploma_Icon";
			}
			else if (localMultiplier == 4)
			{
				revealSprites[arrayIndex].spriteName = "Medal_Icon";
			}
			else
			{
				revealSprites[arrayIndex].spriteName = "Slippers_Icon";
			}
			// First, we fly the sprite to the multiplier itself.
			yield return new TIWaitForSeconds(0.5f);
			iTween.MoveTo(revealSprites[arrayIndex].gameObject, iTween.Hash("position", multiplierTextWrapper.gameObject.transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
			yield return new TIWaitForSeconds(1.0f);
			multiplierTextWrapper.gameObject.SetActive(false);
			PlayingAudio wizardInitialClip = null;
			wizardHead.Play(WIZARD_HEAD_ANIM);
			if (localMultiplier == 2)
			{
				wizardInitialClip = Audio.play(TINMAN_VO_1);
				if (wizardInitialClip != null)
				{
					yield return new TIWaitForSeconds(wizardInitialClip.audioInfo.clip.length/1.5f);
					Audio.play(IGNITE_SPECIAL);
				}
			}
			else if (localMultiplier == 3)
			{
				wizardInitialClip = Audio.play(SCARECROW_VO_1);
				if (wizardInitialClip != null)
				{
					yield return new TIWaitForSeconds(wizardInitialClip.audioInfo.clip.length/1.5f);
					Audio.play(IGNITE_SPECIAL);
				}
			}
			else if (localMultiplier == 4)
			{
				wizardInitialClip = Audio.play(LION_VO_1);
				if (wizardInitialClip != null)
				{
					yield return new TIWaitForSeconds(wizardInitialClip.audioInfo.clip.length/1.5f);
					Audio.play(IGNITE_SPECIAL);
				}
			}
			else
			{
				wizardInitialClip = Audio.play(DOROTHY_VO_1);
				if (wizardInitialClip != null)
				{
					yield return new TIWaitForSeconds(wizardInitialClip.audioInfo.clip.length/1.5f);
					Audio.play(IGNITE_SPECIAL);
				}
			}
			revealSprites[arrayIndex].gameObject.SetActive(false);
			// This is our end game scene. Mess with the particles, fire off the VO and mouth, the reveals, and get the hell out.
			wizardEffects.Play(PARTICLE_BAD_ANIM);
			Audio.play(ANGRY_WIZARD);
			revealTextsWrapper[arrayIndex].gameObject.SetActive(true);
			revealTextsWrapper[arrayIndex].text = "";
			multiplierTextWrapper.text = "";
			gameOverTextAnimation.SetActive(true);
			gameEnded = true;
			gameOverAnimations[arrayIndex].SetActive(true);
			gameOverAnimations[arrayIndex].GetComponent<Animation>().Play();
			yield return new TIWaitForSeconds(0.5f);
			StartCoroutine(poorWizardVOSequence());
		
			StartCoroutine(revealAllPicks());
		}
	}

	private IEnumerator poorWizardVOSequence()
	{
		wizardHead.Play(WIZARD_HEAD_ANIM);
		Audio.play(DO_NOT_AROUSE_ME);
		yield return new TIWaitForSeconds(3.5f);
		if (wizardHead != null)
		{
			wizardHead.Play(WIZARD_HEAD_IDLE_ANIM);
		}
	}

	private IEnumerator startMultiplierSequence(int arrayIndex)
	{
		// First, we fly the sprite to the multiplier itself.
		yield return new TIWaitForSeconds(0.5f);
		iTween.MoveTo(revealSprites[arrayIndex].gameObject, iTween.Hash("position", multiplierTextWrapper.gameObject.transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
		yield return new TIWaitForSeconds(1.0f);
		multiplierTextWrapper.gameObject.SetActive(false);
		PlayingAudio wizardInitialClip = null;
		PlayingAudio wizardSecondaryClip = null;
		wizardHead.Play(WIZARD_HEAD_ANIM);

		// After we hid the multiplier and started the mouth moving, let's play the first clip, then play the second and wait as long as needed.
		// Also, these are too many off audio clips below to really necessitate moving them into const string, imo.
		if (localMultiplier == 2)
		{
			wizardInitialClip = Audio.play(TINMAN_VO_1);
			if (wizardInitialClip != null)
			{
				yield return new TIWaitForSeconds(wizardInitialClip.audioInfo.clip.length/1.5f);
				Audio.play(IGNITE_SPECIAL);
				wizardSecondaryClip = Audio.play(TINMAN_VO_2);
			}
		}
		else if (localMultiplier == 3)
		{
			wizardInitialClip = Audio.play(SCARECROW_VO_1);
			if (wizardInitialClip != null)
			{
				yield return new TIWaitForSeconds(wizardInitialClip.audioInfo.clip.length/1.5f);
				Audio.play(IGNITE_SPECIAL);
				wizardSecondaryClip = Audio.play(SCARECROW_VO_2);
			}
		}
		else if (localMultiplier == 4)
		{
			wizardInitialClip = Audio.play(LION_VO_1);
			if (wizardInitialClip != null)
			{
				yield return new TIWaitForSeconds(wizardInitialClip.audioInfo.clip.length/1.5f);
				Audio.play(IGNITE_SPECIAL);
				wizardSecondaryClip = Audio.play(LION_VO_2);
			}
		}
		else if (localMultiplier == 5)
		{
			wizardInitialClip = Audio.play(DOROTHY_VO_1);
			if (wizardInitialClip != null)
			{
				yield return new TIWaitForSeconds(wizardInitialClip.audioInfo.clip.length/1.5f);
				Audio.play(IGNITE_SPECIAL);
				wizardSecondaryClip = Audio.play(DOROTHY_VO_2);
			}
		}
		Audio.play(REVEAL_X);
		
		//This is in a separate sequence as we want the VO to overlap with the next selections, if possible
		StartCoroutine(makeWizardTalkForADuration(wizardSecondaryClip));
		
		StartCoroutine(sparklesEverywhere());
		//Change mult text
		revealSprites[arrayIndex].gameObject.SetActive(false);
		multiplierTextWrapper.gameObject.SetActive(true);
		multiplierTextWrapper.text = Localize.text("{0}X", localMultiplier);
		revealTextsWrapper[arrayIndex].gameObject.SetActive(true);
		revealTextsWrapper[arrayIndex].text = "+" + Localize.text("{0}X", pick.multiplier);
		pick = pickemOutcome.getNextEntry();
		canAnimate = true;
		bannerTextWrapper.text = Localize.text(PICK_AGAIN);

		for (int i = 0; i < colliderObjects.Length; i++)
		{
			if (!revealTextsWrapper[i].gameObject.activeSelf)
			{
				CommonGameObject.setObjectCollidersEnabled(colliderObjects[i].gameObject, true);
			}
		}

		checkForAllPicksSelected();
	}

	private IEnumerator sparklesEverywhere()
	{
		cardDissapearSparkle.SetActive(true);
		yield return new TIWaitForSeconds(0.5f);
		cardDissapearSparkle.SetActive(false);
	}

	// Just used for the final VO during the multiplier sequence
	private IEnumerator makeWizardTalkForADuration(PlayingAudio currentClip)
	{
		if (currentClip != null)
		{
			wizardEffects.Play(PARTICLE_GOOD_ANIM);
			yield return new TIWaitForSeconds(currentClip.audioInfo.clip.length/2);
		}
		wizardEffects.Play(PARTICLE_IDLE_ANIM);
		wizardHead.Play(WIZARD_HEAD_IDLE_ANIM);
	}

	// A check after every pick to see if we've selected everything, and need to force end the game.
	private void checkForAllPicksSelected()
	{
		bool allPicksSelected = true;
		for (int i = 0; i < revealTextsWrapper.Count; i++)
		{
			if (!revealTextsWrapper[i].gameObject.activeSelf)
			{
				allPicksSelected = false;
			}
		}

		if (allPicksSelected)
		{
			Audio.play(SUMMARY_VO);
			Audio.stopSound(bgLoop);
			Audio.stopSound(ambientLoop);
			BonusGamePresenter.instance.gameEnded();
		}
	}

	// Pretty straightforward tweening of the fireball, and reveal of the multiplier.
	private IEnumerator startFireballSequence(int arrayIndex)
	{
		fireballTrail.SetActive(true);
		fireballTrail.transform.position = multiplierTextWrapper.gameObject.transform.position;
		Audio.play(MULTIPLIER_TRAVEL);
		iTween.MoveTo(fireballTrail, iTween.Hash("position", revealAnimations[arrayIndex].transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
		yield return new TIWaitForSeconds(1.0f);
		fireballTrail.SetActive(false);
		fireballExplosion.transform.position = revealAnimations[arrayIndex].transform.position;
		fireballExplosion.SetActive(true);
		Audio.play(MULTIPLIER_CREDIT);
		yield return new TIWaitForSeconds(0.5f);
		revealTextsWrapper[arrayIndex].text = CreditsEconomy.convertCredits(pick.credits * localMultiplier);
		fireballExplosion.SetActive(false);
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - (pick.credits * localMultiplier), BonusGamePresenter.instance.currentPayout, winAmountLabelWrapper));
		pick = pickemOutcome.getNextEntry();
		canAnimate = true;
		bannerTextWrapper.text = Localize.text(PICK_AGAIN);

		for (int i = 0; i < colliderObjects.Length; i++)
		{
			if (!revealTextsWrapper[i].gameObject.activeSelf)
			{
				CommonGameObject.setObjectCollidersEnabled(colliderObjects[i].gameObject, true);
			}
		}

		checkForAllPicksSelected();
	}

	// Basic reveals for the pickem.
	private IEnumerator revealAllPicks()
	{
		for (int i = 0; i < colliderObjects.Length; i++)
		{
			CommonGameObject.setObjectCollidersEnabled(colliderObjects[i].gameObject, false);
		}
		
		yield return new TIWaitForSeconds(1.0f);
		
		for (int i = 0; i < revealTextsWrapper.Count; i++)
		{
			if (!revealTextsWrapper[i].gameObject.activeSelf)
			{
				revealAnimations[i].GetComponent<Animation>().Play(ICON_REVEAL_ANIM);
				pick = pickemOutcome.getNextReveal();
				yield return new TIWaitForSeconds(0.15f);
				UILabelStyler labelStyle = revealTextsWrapper[i].gameObject.GetComponent<UILabelStyler>();
				if (labelStyle != null)
				{
					labelStyle.style = grayedOutRevealStyle;
					labelStyle.updateStyle();
				}

				// Show the credit, multiplier, or fake reveal.
				if (pick.credits != 0 && pick.groupId != "FIGHT")
				{
					revealTextsWrapper[i].gameObject.SetActive(true);
					revealTextsWrapper[i].text = CreditsEconomy.convertCredits(pick.credits);
				}
				else if (pick.groupId == "FIGHT" && pick.multiplier != 0)
				{
					revealTextsWrapper[i].gameObject.SetActive(true);
					revealTextsWrapper[i].text = "+" + Localize.text("{0}X", pick.multiplier);
				}
				else
				{
					// Never show the game over anims in the reveals. Just make them look like win choices.
					revealTextsWrapper[i].gameObject.SetActive(true);
					revealTextsWrapper[i].text = "+1X";
				}
				
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}
		
		yield return new TIWaitForSeconds(1.0f);

		Audio.play(SUMMARY_VO);
		Audio.stopSound(bgLoop);
		Audio.stopSound(ambientLoop);
		BonusGamePresenter.instance.gameEnded();
	}
}

