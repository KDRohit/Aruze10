using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class bbh01Pickem : PickingGame<WheelOutcome> {

	enum Stage
	{
		Ammo = 0,
		Jugs = 1
	};

	// Music.
	
	private const string PICKEM_INTRO_MUSIC = "IdleBonusBBH01";
	private const string PICKEM_BG_MUSIC = "BonusBgBBH01";
	
	// Ammo pickem.

	private const float AMMO_PICK_ME_OPEN_TO_CLOSE_DUR = 1.0f;
	
	private const string AMMO_OPEN_AMMO_SOUND = "JugOpenAmmoBox";
	private const float AMMO_PICK_OPEN_TO_NUM_SHOTS_DUR = 1.0f;
	private const string AMMO_SHOW_NUM_SHOTS_SOUND = "RollupTermBaseBBH01";
	private const float AMMO_PICK_NUM_SHOTS_TO_REVEALS_DUR = 1.0f;
	
	private Color AMMO_REVEAL_GRAY_COLOR = new Color(0.33f, 0.33f, 0.33f, 1.0f);
	private string AMMO_REVEAL_AMMO_SOUND = "reveal_not_chosen";
	private float AMMO_REVEAL_AMMO_SOUND_DELAY = 1.1f;
	private const float AMMO_REVEALS_OPEN_TO_NUM_SHOTS_DUR = 1.0f;
	private const float AMMO_REVEALS_OPEN_TO_NEXT_DUR = 0.5f;
	private const float AMMO_REVEALS_TO_SPARKLE_DUR = 1.0f;
	
	private WheelOutcome wheelOutcome;
	private WheelPick wheelPick;

	private static readonly int[] POSSIBLE_NUM_SHOTS =
	{
		5,
		7,
		9
	};
	private List<int> possibleNumShots = new List<int>();
		
	// Jug pickem.

	private const string JUG_PICK_ME_SOUND = "RifleCock";
	private const float JUG_PICK_ME_SHAKE_TO_STILL_DUR = 1.0f;

	private const string JUG_SHOOT_JUG_SOUND = "RifleShot";
	private const float JUG_SHOT_TO_SHATTER_DUR = 0.12f;
	private const string JUG_SHATTER_JUG_SOUND = "JugShatter";
	private const float JUG_SHATTER_TO_FREAKIN_DUR = 0.7f;
	private const string JUG_PAPPY_FREAKIN_SOUND = "JugPappyFreakin";
	
	private const float JUG_MULTIPLIER_TO_SPARKLE_TRAIL_DUR = 0.9f;
	private const float JUG_SPARKLE_TRAIL_DUR = 1.0f;
	private const string JUG_SPARKLE_TRAIL_SOUND = "SparklyWhooshDown1";
	
	private const string JUG_SPARKLE_EXPLOSION_SOUND = "value_land";
	private const float JUG_SPARKLE_EXPLOSION_DUR = 0.8f;
	private const float JUG_CREDITS_TO_MULTIPLIED_CREDITS_DUR = 0.9f;
	
	private const float JUG_REVEALS_SHATTER_TO_NEXT_DUR = 0.4f;
	private const float JUGS_TO_END_DUR = 1.0f;
	private const string JUG_PAPPY_SUMMARY_SOUND = "JugSummaryVO";
			
	private PickemOutcome pickemOutcome;
	private PickemPick pickemPick;

	public UILabel numShotsLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent numShotsLabelWrapperComponent;

	public LabelWrapper numShotsLabelWrapper
	{
		get
		{
			if (_numShotsLabelWrapper == null)
			{
				if (numShotsLabelWrapperComponent != null)
				{
					_numShotsLabelWrapper = numShotsLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_numShotsLabelWrapper = new LabelWrapper(numShotsLabel);
				}
			}
			return _numShotsLabelWrapper;
		}
	}
	private LabelWrapper _numShotsLabelWrapper = null;
	
	public Animation pappyAnimation;
	private int numShots = 0;
	
	public UILabel instructionsLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent instructionsLabelWrapperComponent;

	public LabelWrapper instructionsLabelWrapper
	{
		get
		{
			if (_instructionsLabelWrapper == null)
			{
				if (instructionsLabelWrapperComponent != null)
				{
					_instructionsLabelWrapper = instructionsLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_instructionsLabelWrapper = new LabelWrapper(instructionsLabel);
				}
			}
			return _instructionsLabelWrapper;
		}
	}
	private LabelWrapper _instructionsLabelWrapper = null;
	

/*==========================================================================================================*\
	Init
\*==========================================================================================================*/

	public override void init()
	{
		base.init();

		Audio.playMusic(PICKEM_INTRO_MUSIC);
		Audio.switchMusicKey("");

		wheelOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		wheelPick = wheelOutcome.getNextEntry();
		
		for (int ammoIndex = 0; ammoIndex < POSSIBLE_NUM_SHOTS.Length; ammoIndex++)
		{
			possibleNumShots.Add(POSSIBLE_NUM_SHOTS[ammoIndex]);
		}

		for (int ammoIndex = 0; ammoIndex < getButtonLengthInRound(); ammoIndex++)
		{
			PickGameButtonData ammoPick = getPickGameButton(ammoIndex);
					
			ammoPick.revealNumberLabel.text = "";
			ammoPick.revealNumberOutlineLabel.text = "";
		}
		
		instructionsLabelWrapper.text = Localize.textUpper("pick_an_ammo_box");
		_didInit = true;
	}
	
/*==========================================================================================================*\
	Pickme Game
\*==========================================================================================================*/

	protected override IEnumerator pickMeAnimCallback()
	{
		if (currentStage == (int)Stage.Ammo)
		{
			yield return StartCoroutine(ammoPickMeAnimCallback());

		}
		else
		if (currentStage == (int)Stage.Jugs)
		{
			yield return StartCoroutine(jugPickMeAnimCallback());
		}
	}

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		if (currentStage == (int)Stage.Ammo)
		{
			yield return StartCoroutine(ammoPickemButtonPressedCoroutine(button));
		}
		else
		if (currentStage == (int)Stage.Jugs)
		{
			yield return StartCoroutine(jugPickemButtonPressedCoroutine(button));
		}
	}

	public override void continueToNextStage()
	{
		base.continueToNextStage();
		
		if (currentStage == (int)Stage.Jugs)
		{
			StartCoroutine(startJugsStage());
		}
	}
	
/*==========================================================================================================*\
	Ammo Pickem
\*==========================================================================================================*/

	protected IEnumerator ammoPickMeAnimCallback()
	{
		if (numShots == 0)
		{
			PickGameButtonData ammoPick = getRandomPickMe();
			
			if (ammoPick != null)
			{
				ammoPick.animator.Play("BBH01_PappysJugs_PickingObjectBox_PickMe");
				Audio.play(AMMO_OPEN_AMMO_SOUND);
				
				yield return new WaitForSeconds(AMMO_PICK_ME_OPEN_TO_CLOSE_DUR);
				
				if ((numShots == 0) && isButtonAvailableToSelect(ammoPick.button))
				{
					ammoPick.animator.Play("BBH01_PappysJugs_PickingObjectBox_Still");
				}
			}
		}
	}
	
	protected IEnumerator ammoPickemButtonPressedCoroutine(GameObject ammoButton)
	{
		inputEnabled = false;

		pickemOutcome =
			new PickemOutcome(
				SlotOutcome.getBonusGameOutcome(
				BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame));
		
		numShots = pickemOutcome.entryCount;
		possibleNumShots.Remove(numShots);
		
		int ammoIndex = getButtonIndex(ammoButton);
		removeButtonFromSelectableList(getButtonUsingIndexAndRound(ammoIndex));

		for (int revealIndex = 0; revealIndex < getButtonLengthInRound(); revealIndex++)
		{
			PickGameButtonData revealPick = getPickGameButton(revealIndex);
			
			if (isButtonAvailableToSelect(revealPick.button))
			{
				CommonGameObject.colorGameObject(revealPick.animator.gameObject, AMMO_REVEAL_GRAY_COLOR);
			}
		}
		
		PickGameButtonData ammoPick = getPickGameButton(ammoIndex);
		
		ammoPick.animator.Play("BBH01_PappysJugs_PickingObjectBox_Reveal");
		Audio.play(AMMO_OPEN_AMMO_SOUND);
		
		yield return new WaitForSeconds(AMMO_PICK_OPEN_TO_NUM_SHOTS_DUR);
		
		ammoPick.revealNumberLabel.text = Localize.text("{0}_shots", numShots);
		ammoPick.revealNumberOutlineLabel.text = Localize.text("{0}_shots", numShots);
		
		Audio.play(AMMO_SHOW_NUM_SHOTS_SOUND);
		pickemPick = pickemOutcome.getNextEntry();
		
		yield return new WaitForSeconds(AMMO_PICK_NUM_SHOTS_TO_REVEALS_DUR);
		StartCoroutine(openRevealAmmos(ammoPick));
	}

	private IEnumerator openRevealAmmos(PickGameButtonData ammoPick)
	{
		while (possibleNumShots.Count > 0)
		{
			StartCoroutine(openRevealAmmo());
			yield return new WaitForSeconds(AMMO_REVEALS_OPEN_TO_NEXT_DUR);
		}
		
		yield return new WaitForSeconds(AMMO_REVEALS_TO_SPARKLE_DUR);
		
		// Sparkle trail from ammo box to ammo label.
		
		GameObject sparkleTrail = CommonGameObject.instantiate(bonusSparkleTrail) as GameObject;
		
		sparkleTrail.transform.parent = bonusSparkleTrail.transform.parent;
		sparkleTrail.transform.localScale = Vector3.one;
		
		sparkleTrail.transform.position = new Vector3(
			ammoPick.revealNumberLabel.gameObject.transform.position.x,
			ammoPick.revealNumberLabel.gameObject.transform.position.y,
			bonusSparkleTrail.transform.position.z);
		
		sparkleTrail.SetActive(true);
		
		// The trail to-position should use the jug pick multiplier label z-coordinate
		// to make sure the sparkle trail appears on top of Pappy and the other jugs.
		Vector3 toPos = numShotsLabelWrapper.transform.position;
		toPos.z = bonusSparkleTrail.transform.position.z;
		
		iTween.MoveTo(
			sparkleTrail,
			iTween.Hash(
			"position", toPos,
			"time", JUG_SPARKLE_TRAIL_DUR,
			"islocal", false,
			"easetype",
			iTween.EaseType.linear));
		
		Audio.play(JUG_SPARKLE_TRAIL_SOUND);
		yield return new TIWaitForSeconds(JUG_SPARKLE_TRAIL_DUR);
		
		Destroy(sparkleTrail);
		continueToNextStage();	
	}
	
	private IEnumerator openRevealAmmo()
	{
		int ammoIndex = getButtonIndex(grabNextButtonAndRemoveIt());
		PickGameButtonData ammoPick = getPickGameButton(ammoIndex);
		
		Animator ammoAnimator = ammoPick.animator;
		ammoAnimator.Play("BBH01_PappysJugs_PickingObjectBox_Reveal");
		Audio.play(Audio.soundMap(AMMO_REVEAL_AMMO_SOUND), 1.0f, 0.0f, AMMO_REVEAL_AMMO_SOUND_DELAY);
		
		int ammoNumShots = possibleNumShots[0];
		possibleNumShots.RemoveAt(0);
		
		yield return new WaitForSeconds(AMMO_REVEALS_OPEN_TO_NUM_SHOTS_DUR);
				
		ammoPick.revealNumberLabel.text = Localize.text("{0}_shots", ammoNumShots);
		ammoPick.revealNumberOutlineLabel.text = Localize.text("{0}_shots", ammoNumShots);
		
		grayOutRevealText(ammoIndex);
	}
	
/*==========================================================================================================*\
	Jug Pickem
\*==========================================================================================================*/

	protected IEnumerator startJugsStage()
	{
		Audio.switchMusicKey(PICKEM_BG_MUSIC);
		inputEnabled = true;

		// Sparkle explosion on the num shots label.
		
		bonusSparkleExplosion.transform.position = new Vector3(
			numShotsLabelWrapper.transform.position.x,
			numShotsLabelWrapper.transform.position.y,
			bonusSparkleExplosion.transform.position.z);
		bonusSparkleExplosion.SetActive(true);
		Audio.play(JUG_SPARKLE_EXPLOSION_SOUND);
		
		numShotsLabelWrapper.text = CommonText.formatNumber(numShots);
		instructionsLabelWrapper.text = Localize.textUpper("shoot_the_jugs");
		
		yield return new TIWaitForSeconds(JUG_SPARKLE_EXPLOSION_DUR);
		bonusSparkleExplosion.SetActive(false);
	}
	
	protected IEnumerator jugPickMeAnimCallback()
	{
		if (numShots > 0)
		{
			PickGameButtonData jugPick = getRandomPickMe();
			
			if (jugPick != null)
			{
				jugPick.animator.Play("BBH01_PappysJugs_PickingObject_Jug_PickMe");
				Audio.play(JUG_PICK_ME_SOUND);
				
				yield return new WaitForSeconds(JUG_PICK_ME_SHAKE_TO_STILL_DUR);
				
				if ((numShots > 0) && isButtonAvailableToSelect(jugPick.button))
				{
					jugPick.animator.Play("BBH01_PappysJugs_PickingObject_Jug_Still");
				}
			}
		}
	}

	private IEnumerator animatePappy()
	{
		pappyAnimation.Play("Start");
		while(pappyAnimation.isPlaying)
		{
			yield return null;
		}
		pappyAnimation.Play("Idle");
	}

	protected IEnumerator jugPickemButtonPressedCoroutine(GameObject jugButton)
	{
		inputEnabled = false;

		StartCoroutine(animatePappy());
		
		numShots--;
		numShotsLabelWrapper.text = CommonText.formatNumber(numShots);
		
		int jugIndex = getButtonIndex(jugButton);
		removeButtonFromSelectableList(jugButton);
		
		PickGameButtonData jugPick = getPickGameButton(jugIndex);
		
		Audio.play(JUG_SHOOT_JUG_SOUND);
		yield return new TIWaitForSeconds(JUG_SHOT_TO_SHATTER_DUR);
		
		// Get the multiplier and credits from the pick.
		// Note, multiplier is the multiplier bonus from this pick,
		// and currentMultiplier is the total multiplier bonus you've accumulated.
		
		int multiplier = (int)pickemPick.multiplier;
		long credits = pickemPick.credits;
		
		// Is it a multiplier or credits?
		
		if (multiplier > 0)
		{
			// The multiplier explodes out of the jug.
			
			jugPick.revealNumberLabel.text = Localize.text("plus_{0}_X", CommonText.formatNumber(multiplier));
			jugPick.revealNumberOutlineLabel.text = Localize.text("plus_{0}_X", CommonText.formatNumber(multiplier));
			
			jugPick.animator.Play("BBH01_PappysJugs_PickingObject_Jug_RevealNumber");
			
			Audio.play(JUG_SHATTER_JUG_SOUND);
			Audio.play(JUG_PAPPY_FREAKIN_SOUND, 1.0f, 0.0f, JUG_SHATTER_TO_FREAKIN_DUR);
			
			yield return new TIWaitForSeconds(JUG_MULTIPLIER_TO_SPARKLE_TRAIL_DUR);
			
			// The sparkle trail flies from the jug to the multiplier label.
			
			GameObject sparkleTrail = CommonGameObject.instantiate(bonusSparkleTrail) as GameObject;
			
			sparkleTrail.transform.parent = bonusSparkleTrail.transform.parent;
			sparkleTrail.transform.localScale = Vector3.one;
			
			sparkleTrail.transform.position = new Vector3(
				jugPick.revealNumberLabel.gameObject.transform.position.x,
				jugPick.revealNumberLabel.gameObject.transform.position.y,
				bonusSparkleTrail.transform.position.z);

			sparkleTrail.SetActive(true);
			
			// The trail to-position should use the jug pick multiplier label z-coordinate
			// to make sure the sparkle trail appears on top of Pappy and the other jugs.
			Vector3 toPos = currentMultiplierLabel.gameObject.transform.position;
			toPos.z = bonusSparkleTrail.transform.position.z;
			
			iTween.MoveTo(
				sparkleTrail,
				iTween.Hash(
					"position", toPos,
					"time", JUG_SPARKLE_TRAIL_DUR,
					"islocal", false,
					"easetype",
					iTween.EaseType.linear));
			
			Audio.play(JUG_SPARKLE_TRAIL_SOUND);
			yield return new TIWaitForSeconds(JUG_SPARKLE_TRAIL_DUR);
			
			Destroy(sparkleTrail);
			
			// Show the sparkle explosion and update the multiplier.
			
			jugPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
			jugPick.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(credits);
			
			bonusSparkleExplosion.transform.position = new Vector3(
				currentMultiplierLabel.gameObject.transform.position.x,
				currentMultiplierLabel.gameObject.transform.position.y,
				bonusSparkleExplosion.transform.position.z);
			bonusSparkleExplosion.SetActive(true);
			Audio.play(JUG_SPARKLE_EXPLOSION_SOUND);
			
			currentMultiplier += multiplier;
			currentMultiplierLabel.text = Localize.text("{0}X", CommonText.formatNumber(currentMultiplier));
			
			yield return new TIWaitForSeconds(JUG_SPARKLE_EXPLOSION_DUR);
			bonusSparkleExplosion.SetActive(false);
		}
		else		
		if (credits > 0)
		{
			// The credits explode out of the jug.
			
			jugPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
			jugPick.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(credits);
			
			jugPick.animator.Play("BBH01_PappysJugs_PickingObject_Jug_RevealNumber");
	
			Audio.play(JUG_SHATTER_JUG_SOUND);
			Audio.play(JUG_PAPPY_FREAKIN_SOUND, 1.0f, 0.0f, JUG_SHATTER_TO_FREAKIN_DUR);
		}
		else
		{
			jugPick.animator.Play("BBH01_PappysJugs_PickingObject_Jug_RevealShell");
		}

		// Finish animating the credits.
		
		if (credits > 0)
		{
			// Is there a multiplier?
			
			if (currentMultiplier > 1)
			{
				// Wait.
				
				if (multiplier == 0)
				{
					yield return new TIWaitForSeconds(JUG_CREDITS_TO_MULTIPLIED_CREDITS_DUR);
				}
				
				// Show the sparkle explosion and multiply the credits.
				
				bonusSparkleExplosion.transform.position = new Vector3(
					jugPick.revealNumberLabel.gameObject.transform.position.x,
					jugPick.revealNumberLabel.gameObject.transform.position.y,
					bonusSparkleExplosion.transform.position.z);
				bonusSparkleExplosion.SetActive(true);
				Audio.play(JUG_SPARKLE_EXPLOSION_SOUND);
				
				credits *= currentMultiplier;
				
				jugPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
				jugPick.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(credits);
				
			}
			
			// Rollup the score.
						
			StartCoroutine(
				animateScore(
					BonusGamePresenter.instance.currentPayout,
					BonusGamePresenter.instance.currentPayout + credits));
	
			// Is there a multiplier?
			
			if (currentMultiplier > 1)
			{
				// Hide the sparkle explosion.
				
				yield return new TIWaitForSeconds(JUG_SPARKLE_EXPLOSION_DUR);
				bonusSparkleExplosion.SetActive(false);
			}
		
			// Update the current payout.
			
			BonusGamePresenter.instance.currentPayout += credits;
		}
		
		if (numShots > 0)
		{
			pickemPick = pickemOutcome.getNextEntry();
			inputEnabled = true;
		}
		else
		{
			StartCoroutine(shatterRevealJugs());
		}
	}

	protected IEnumerator shatterRevealJugs()
	{
		PickemPick pickemReveal = pickemOutcome.getNextReveal();
	
		while (pickemReveal != null)
		{
			shatterRevealJug(pickemReveal);
			
			yield return StartCoroutine(revealWait.wait(JUG_REVEALS_SHATTER_TO_NEXT_DUR));
			pickemReveal = pickemOutcome.getNextReveal();
		}

		yield return new TIWaitForSeconds(JUGS_TO_END_DUR);
		
		Audio.play(JUG_PAPPY_SUMMARY_SOUND);
		Audio.switchMusicKeyImmediate(""); // force the music off so the summary music starts right away
		BonusGamePresenter.instance.gameEnded();
	}
	
	protected void shatterRevealJug(PickemPick pickemReveal)
	{
		int jugIndex = getButtonIndex(grabNextButtonAndRemoveIt());
		PickGameButtonData jugPick = getPickGameButton(jugIndex);
		
		CommonGameObject.colorGameObject(jugPick.animator.gameObject, AMMO_REVEAL_GRAY_COLOR); // This doesn't do anything.
		if (!revealWait.isSkipping)
		{
			Audio.play (JUG_SHATTER_JUG_SOUND);
		}
		
		// Get the multiplier and credits.
		
		long multiplier = pickemReveal.multiplier;
		long credits = pickemReveal.credits;
		
		// Is it a multiplier or credits?
		
		if (multiplier > 0)
		{
			// The multiplier explodes out of the jug.
			
			jugPick.revealNumberLabel.text = Localize.text("plus_{0}_X", CommonText.formatNumber(multiplier));
			jugPick.revealNumberOutlineLabel.text = Localize.text("plus_{0}_X", CommonText.formatNumber(multiplier));
			
			jugPick.animator.Play("BBH01_PappysJugs_PickingObject_Jug_RevealNumber");
		}
		else if (credits > 0)
		{	
			jugPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
			jugPick.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(credits);
			
			jugPick.animator.Play("BBH01_PappysJugs_PickingObject_Jug_RevealNumber");
		}
		
		grayOutRevealText(jugIndex);
	}
}

