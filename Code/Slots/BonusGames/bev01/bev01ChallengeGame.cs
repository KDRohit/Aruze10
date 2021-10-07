using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class bev01ChallengeGame : ChallengeGame
{
	public List<GameObject> shotIcons;
	public List<UILabel> shotRevealTexts;
	public List<UISprite> shotBackgrounds;
	public UILabel winLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;

	public LabelWrapper winLabelWrapper
	{
		get
		{
			if (_winLabelWrapper == null)
			{
				if (winLabelWrapperComponent != null)
				{
					_winLabelWrapper = winLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelWrapper = new LabelWrapper(winLabel);
				}
			}
			return _winLabelWrapper;
		}
	}
	private LabelWrapper _winLabelWrapper = null;
	
	public List<GameObject> bubbles;
	public List<GameObject> bubbleBackgrounds;
	public List<UILabel> bubbleMultipliers;
	public GameObject bubbleTargetArea;										// The area the current bubble gets sent to
	public GameObject bubbleShrinkToArea;									// The area the bubbles get sent to before the break through the ground
	public GameObject bubbleStopOne;										// The spot the bubble stops on before it its the winbox.
	public GameObject bubbleFinalStop;										// The final spot we want the bubble to go to.
	public GameObject oilSplat;

	// In-game animations
	public GameObject spillBurst;
	public GameObject dustCloud;
	public GameObject multiplierReveal;
	public GameObject oilFountain;
	public GameObject oilVertical;
	public GameObject revealAnim;
	public GameObject groundOilAnimation;
	
	private PickemOutcome outcome;
	private PickemPick pick;
	private GameObject currentBubble;										// The bubble that is in the center of the screen. This could be a winner!!!
	private GameObject currentBubbleBG;										// We neeed to keep this seperate because we don't want to move the text while it spins.
	private long currentMultiplier;
	private SkippableWait revealWait = new SkippableWait();
	private long multiplier = 1;
	private bool gameEnded = false;
	//private bool hasSelectedShellBefore = false;
	private bool canTouch = true;
	private PlayingAudio ambianceLoop;
	private CoroutineRepeater pickMeController;								// Class to call the pickme animation on a loop
	private CoroutineRepeater bubbleSpinController;							// Class to call the bubblespin animation on a loop

	private List<long> gameMultipliers = new List<long>();
	private List<long> playableMultipliers = new List<long>();
	private List<GameObject> playableBubbles = new List<GameObject>();		// The list of all the playable bubbles. needs to be synced with playableBubbleBGs.
	private List<GameObject> playableBubbleBGs = new List<GameObject>();	// The list of all the background associated with the playable bubbles. needs to be synced with playable Bubbles.


	// Constant Variables
	private const float INTRO_VO_DELAY = 0.6f;								// 600ms delay on the VO before the game starts.
	private const int MAX_MULTIPLIER = 100;									// The max for the range that multipliers that are not part of the outcome can be displayed as.
	private const float MIN_TIME_PICKME = 2.0f;								// Minimum time pickme animation might take to play next
	private const float MAX_TIME_PICKME = 7.0f;								// Maximum time pickme animation might take to play next
	private const float MIN_TIME_BUBBLE_SPIN = 0.5f;						// Minimum amount of time that it will be before a bubble spins again.
	private const float MAX_TIME_BUBBLE_SPIN = 3.0f;						// Maximum amount of time that it will be before a bubble spins again.
	private const float TIME_PICKME_SHAKE = 0.7f;							// The time that the bomb should shake for.
	private const float TIME_ROTATE_BUBBLE = 0.5f;							// How long it should take to rotate the current bubble left and right once.
	private const float TIME_BETWEEN_REVEALS = 0.5f;						// How long to wait between each reveal
	private const float TIME_AFTER_REVEALS = 1.0f;							// That sink in time.
	private const float TIME_MOVE_BUBBLE_TO_CENTER = 0.5f;					// After the reveals are over how long it should take to move the bubble to the center.
	private const float TIME_AFTER_SHOT = 0.5f;								// How long to wait after a missed shot.
	private const float TIME_BULLET_REVEAL = 0.5f;							// How long the animation should take before the bullet is shot.
	private const float TIME_FOR_REVEAL_FLURISH = 0.5f;						// How long to play the explostion that happens when a bullet gets revealed.
	private const float TIME_MOVE_BUBBLE_TO_SMALL_GEYSER = 0.75f;				// Time to get the bubble up the Geyser trail before it expands.
	private const float TIME_GROW_OIL_GEYSER = 1.0f;							// This number is pretty magical, because of the way particle systems scale.
	private const float TIME_OSCILATE_ON_GEYSER = 1.0f;							// Just how long it should bounce up and down for.
	private const float TIME_TO_FADE_BUBBLE = 0.25f;							// How long to fade out the multiplier bubble.
	private const float TIME_PLAY_MULTIPLIER_EXPLOSION = 1.0f;					// How long to play the explosion before starting the roll up.
	private const float TIME_AFTER_MULTIPLIER_ROLLUP = 1.0f;					// How long to show the winning amount before the reveals happen.

	// Sound names
	private const string INTRO_VO = "GSIntroVO";
	private const string PICK_ME_SOUND = "GSPickMe";
	private const string FOREST_AMBIANCE = "GSForestAmbience";
	private const string SHELL_PICK_VO = "MBPickOneOfThemThereShells";
	private const string SHELL_FIRE = "GSPickShellFireShotgun";
	private const string REVEAL_MULTIPLIER = "GSRevealMultipler";	// Spelled wrong in global data :(
	private const string PICK_LOW_MULTIPLIER_VO = "MBWoah";
	private const string PICK_MIDDLE_MULTIPLIER_VO = "GSRevealMiddlinVO";
	private const string PICK_HIGH_MULTIPLIER_VO = "GSRevealHiVO";
	private const string REVEAL_OTHERS = "reveal_others";
	private const string GUSHER_REVEAL = "GSRevealBigGusher";
	private const string SUMMARY_LOW_WIN = "GSSummaryLowVOBeverly";
	private const string SUMMARY_MIDDLE_WIN = "GSSummaryMidVOBeverly";
	private const string SUMMARY_HIGH_WIN = "GSSummaryHiVOBeverly";
	
	public override void init() 
	{
		outcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];

		/*
		JSON[] paytableBadGroupCards = null;
		JSON[] paytableCards = outcome.paytableGroups;
		foreach (JSON cardGroup in paytableCards)
		{
			if (cardGroup.getString("group_code", "") == "BAD")
			{
				paytableBadGroupCards = cardGroup.getJsonArray("cards");
			}
		}
		*/

		// Set up the list of possible numbers, because we can't have repeats.
		List<int> possibleMultiplierValues = new List<int>() {1, 2, 3, 4, 5, 6, 7, 8, 9};
		/*
		// Some Testing code so you know when you are going to hit the multiplier value.
		for (int i = 0; i < possibleMultiplierValues.Count; i++)
		{
			possibleMultiplierValues[i] += 10;
		}
		*/
		// Remove all of the extra numbers.
		while (possibleMultiplierValues.Count > bubbleMultipliers.Count)
		{
			possibleMultiplierValues.RemoveAt(Random.Range(0,possibleMultiplierValues.Count));
		}
		// Now we need to add these numbers to the labels, but we should do that randomly also so we get a good distribution.
		// While we are going through here we should find out where the final number is.
		int finalIndex = -1;	// Store this here so we can see where the final number is, and if it got set.
		for (int i = 0; i < bubbleMultipliers.Count; i++)
		{
			UILabel label = bubbleMultipliers[i];
			int randomIndex = Random.Range(0, possibleMultiplierValues.Count);
			int multiplierNumber = possibleMultiplierValues[randomIndex];
			label.text = Localize.text("{0}X", multiplierNumber);
			gameMultipliers.Add(multiplierNumber);
			possibleMultiplierValues.RemoveAt(randomIndex);
			if (multiplierNumber == outcome.finalMultiplier)
			{
				// This is the final index that we need to keep track of.
				finalIndex = i;
			}
		}
		// Make sure that the final index got set. If the data changes and the numbers can go higher than 9 this will happen.
		if (finalIndex == -1)
		{
			// Pick a position in the array and make that number the final value.
			finalIndex = Random.Range(0, bubbleMultipliers.Count);
			UILabel label = bubbleMultipliers[finalIndex];
			label.text = Localize.text("{0}X", outcome.finalMultiplier);
		}

		// Now we know which index in the bubble lists is going to be the final number so lets remove it from the list.
		GameObject winningBubble = bubbles[finalIndex];
		bubbles.RemoveAt(finalIndex);
		GameObject winningBubbleBG = bubbleBackgrounds[finalIndex];
		bubbleBackgrounds.RemoveAt(finalIndex);
		long winningMultiplier = gameMultipliers[finalIndex];
		gameMultipliers.RemoveAt(finalIndex);

		// Now that we have the actual winner of the game taken out we need to populate the near misses.
		for (int i = 0; i < outcome.entryCount - 1; i++)	// One less because we are going to add the winner to the end.
		{
			if (bubbles.Count > 0)
			{ 
				int randomIndex = Random.Range(0, bubbles.Count);
				playableBubbles.Add(bubbles[randomIndex]);
				playableBubbleBGs.Add(bubbleBackgrounds[randomIndex]);
				playableMultipliers.Add(gameMultipliers[randomIndex]);
				gameMultipliers.RemoveAt(randomIndex);
				bubbles.RemoveAt(randomIndex);
				bubbleBackgrounds.RemoveAt(randomIndex);	// Just for consistency.
			}
			else
			{
				Debug.LogError("We are trying to add more bubbles than we have.");
			}
		}
		// Add in the winning bubbles.
		playableBubbles.Add(winningBubble);
		playableBubbleBGs.Add(winningBubbleBG);
		playableMultipliers.Add(winningMultiplier);

		// Set the current bubble to be the one in the middle of the the screen (the first one in the list.)
		setNextBubble();

		currentBubble.transform.position = bubbleTargetArea.transform.position;
		currentBubble.transform.localScale = new Vector3(1.5f, 1.5f, 1.0f);

		CommonGameObject.findChild(currentBubble, "multiplierText").GetComponent<UILabel>().color = new Color(1f , 0.7f , 0f);

		// Play intro VO sound.
		Audio.play(INTRO_VO, 1.0f, 0.0f, INTRO_VO_DELAY);
		// Start the ambianceLoop.
		ambianceLoop = Audio.play(FOREST_AMBIANCE, 1, 0, 0, float.PositiveInfinity);
		winLabelWrapper.text = CreditsEconomy.convertCredits(0);

		_didInit = true;
		
		pick = outcome.getNextEntry();
		multiplier = pick.multiplier;

		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);
		bubbleSpinController = new CoroutineRepeater(MIN_TIME_BUBBLE_SPIN, MAX_TIME_BUBBLE_SPIN, bubbleSpinCallback);

		oilSplat.SetActive(false);
		spillBurst.SetActive(false);
		dustCloud.SetActive(false);
		multiplierReveal.SetActive(false);
		oilFountain.SetActive(false);
		oilVertical.SetActive(false);
		revealAnim.SetActive(false);
	}

	// Plays an animation where the bullet jumps out towards the screen while spinning around once.
	private IEnumerator pickMeCallback()
	{
		// Make sure that we actualyl have shot Icons we can play on.
		if (shotIcons.Count > 0)
		{
			// Get one of the available knocker game objects
			int shotPickMeIndex = Random.Range(0, shotIcons.Count);

			// Start the animation
			Audio.play(PICK_ME_SOUND);
			Vector3 finalThrobSize = shotIcons[shotPickMeIndex].transform.localScale * 2;
			StartCoroutine(CommonEffects.throb(shotIcons[shotPickMeIndex], finalThrobSize, TIME_PICKME_SHAKE));
			iTween.RotateBy(shotIcons[shotPickMeIndex], iTween.Hash("z", 1, "time", 1.0f, "easetype", iTween.EaseType.linear));
			yield return new WaitForSeconds(TIME_PICKME_SHAKE);
		}
	}

	private IEnumerator bubbleSpinCallback()
	{
		if (currentBubble != null && currentBubbleBG != null)
		{
			float halfDelay = TIME_ROTATE_BUBBLE / 2.0f;
			iTween.RotateBy(currentBubbleBG, iTween.Hash("z", 0.25f, "time", halfDelay, "easetype", iTween.EaseType.linear));
			yield return new TIWaitForSeconds(halfDelay);

			iTween.RotateBy(currentBubbleBG, iTween.Hash("z", -0.25f, "time", halfDelay, "easetype", iTween.EaseType.linear));
			yield return new TIWaitForSeconds(halfDelay);
		}
		else
		{
			Debug.LogWarning("Can't play bubbleSpin Animation.");
		}
	}

	protected override void Update()
	{
		base.Update();

		if (!_didInit)
		{
			return;
		}

		// We only want to be able to play the pickme animations if we actually pick stuff.
		if (!gameEnded)
		{
			bubbleSpinController.update();
			if (canTouch)
			{
				pickMeController.update();
			}
		}
	}
	
	public void shotClicked(GameObject objClicked)
	{
		if (canTouch && !gameEnded)
		{
			canTouch = false;
			StartCoroutine(animateBulletReveal(objClicked));
		}
	}

	private IEnumerator animateBulletReveal(GameObject bullet)
	{
		Audio.play(SHELL_FIRE);
		int bulletIndex = shotIcons.IndexOf(bullet);
		iTween.ScaleTo(bullet, iTween.Hash("scale", new Vector3(414, 250, 1), "time", TIME_BULLET_REVEAL, "easetype", iTween.EaseType.easeOutSine));
		iTween.RotateBy(bullet, iTween.Hash("z", 2, "time", TIME_BULLET_REVEAL, "easetype", iTween.EaseType.linear));
		revealAnim.transform.parent = bullet.transform.parent;
		revealAnim.transform.localPosition = Vector3.zero;
		yield return new TIWaitForSeconds(TIME_BULLET_REVEAL);
		revealShot(bulletIndex, false);
		StartCoroutine(beginRollupAndReveal());
		revealAnim.SetActive(true);
		yield return new TIWaitForSeconds(TIME_FOR_REVEAL_FLURISH);
		revealAnim.SetActive(false);
		canTouch = true;
	}

	// Let's do the numbers stuff, and go into the reveal.
	private IEnumerator beginRollupAndReveal()
	{
		// Play the audio specific to how big of a multiplier that you won.
		if (currentMultiplier < 3)
		{
			Audio.play(PICK_LOW_MULTIPLIER_VO, 1, 0, 0.6f);
		}
		else if (currentMultiplier < 6)
		{
			Audio.play(PICK_MIDDLE_MULTIPLIER_VO, 1, 0, 0.6f);
		}
		else
		{
			Audio.play(PICK_HIGH_MULTIPLIER_VO, 1, 0, 0.6f);
		}

		if (multiplier == 0)
		{
			StartCoroutine(missedShot());
			yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - pick.credits, BonusGamePresenter.instance.currentPayout, winLabelWrapper));
			pick = outcome.getNextEntry();
			multiplier = pick.multiplier;
		}
		else
		{
			gameEnded = true;
			if (playableBubbles.Count > 0)
			{
				Debug.LogError("We didn't make it all the way through the picks.");
			}

			Audio.play(REVEAL_MULTIPLIER);

			StartCoroutine(multiplierAnimationSequence());
		}
	}

	// The sequence that plays once the game ends.
	// The bubble that was in the ground shots through the newly formed crack
	// Floats on top of the geyser while the roll up happens for the value revealed.
	// Then the geyser gets bigger and the value hits the win box.
	// Explosions happen and the multiplier gets applied to the winnings, via rollup.
	private IEnumerator multiplierAnimationSequence()
	{
		Audio.play(GUSHER_REVEAL);

		// Start the oil geyser and move the bubble up from the ground.
		oilFountain.SetActive(true);
		oilVertical.SetActive(true);
		groundOilAnimation.SetActive(true);
		// Make the bubble smaller then bigger.
		iTween.ScaleTo(currentBubble, iTween.Hash("scale", new Vector3(0.0f, 0.0f, 1.0f), "time", TIME_MOVE_BUBBLE_TO_SMALL_GEYSER / 2, "islocal", true, "easetype", iTween.EaseType.linear));
		iTween.MoveTo(currentBubble, iTween.Hash("position", bubbleShrinkToArea.transform.position, "time", TIME_MOVE_BUBBLE_TO_SMALL_GEYSER / 2, "islocal", false, "easetype", iTween.EaseType.linear));
		yield return new TIWaitForSeconds(TIME_MOVE_BUBBLE_TO_SMALL_GEYSER / 2);
		currentBubble.transform.position = oilVertical.transform.position;
		iTween.ScaleTo(currentBubble, iTween.Hash("scale", new Vector3(1.0f, 1.0f, 1.0f), "time", TIME_MOVE_BUBBLE_TO_SMALL_GEYSER / 2, "islocal", true, "easetype", iTween.EaseType.linear));
		iTween.MoveTo(currentBubble, iTween.Hash("position", bubbleStopOne.transform.position, "time", TIME_MOVE_BUBBLE_TO_SMALL_GEYSER / 2, "islocal", false, "easetype", iTween.EaseType.linear));
		yield return new TIWaitForSeconds(TIME_MOVE_BUBBLE_TO_SMALL_GEYSER / 2);

		// Let the bubble move on top of the geyser while the roll up happens.
		iTween.PunchPosition(currentBubble, Vector3.up / 5, TIME_OSCILATE_ON_GEYSER * 2); // Let's punch a littler longer than necessary so the bob effect lasts longer, but keep the current time for the rollup.
		// Roll up the winnings from the bullet.
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - pick.credits, BonusGamePresenter.instance.currentPayout, winLabelWrapper, null, true, TIME_OSCILATE_ON_GEYSER));
		// Make the particle system bigger.
		oilFountain.SetActive(false);
		ParticleSystem oilVerticalPS = oilVertical.GetComponent<ParticleSystem>();
		if (oilVerticalPS != null)
		{
			ParticleSystem.MainModule particleSystemMainModule = oilVerticalPS.main;
			particleSystemMainModule.startSpeed = 1.9f;
			particleSystemMainModule.startSize= 0.3f;

		}
		// Change the size of of the current bubble and give it a purple hue.
		iTween.MoveTo(currentBubble, iTween.Hash("position", bubbleFinalStop.transform.position, "time", TIME_GROW_OIL_GEYSER, "islocal", false, "easetype", iTween.EaseType.linear));
		iTween.ScaleTo(currentBubble, iTween.Hash("scale", new Vector3(2.2f, 2.2f, 1.0f), "time", TIME_GROW_OIL_GEYSER, "islocal", true, "easetype", iTween.EaseType.linear));
		float age = 0.0f;
		Color[] colors = new Color[2]{Color.white, Color.magenta};
		while (age < TIME_GROW_OIL_GEYSER)
		{
			age += Time.deltaTime;
			CommonGameObject.colorUIGameObject(currentBubble, CommonColor.colorRangeSelect(age / TIME_GROW_OIL_GEYSER, colors));
		//	CommonGameObject.colorUIGameObject(currentBubble, CommonGameObject.parentsFirstSetActive(age / TIME_GROW_OIL_GEYSER, colors));
			//CommonGameObject.alphaUIGameObject(currentBubble, 1 - age/TIME_GROW_OIL_GEYSER);
			yield return null;
		}

		// Roll up the winning amount.
		BonusGamePresenter.instance.currentPayout *= multiplier;
		StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout/multiplier, BonusGamePresenter.instance.currentPayout, winLabelWrapper));

		GameObject currentText = CommonGameObject.findChild(currentBubble, "multiplierText");

		// Let's fade out the bubble.
		age = 0.0f;
		while (age < TIME_TO_FADE_BUBBLE)
		{
			age += Time.deltaTime;
			if (currentText != null)
			{
				CommonGameObject.alphaUIGameObject(currentBubble, 1 - age/TIME_TO_FADE_BUBBLE);
			}
			yield return null;
		}

		// Play the multiplier explosion
		currentBubble.SetActive(false);
		multiplierReveal.SetActive(true);
		yield return new TIWaitForSeconds(TIME_PLAY_MULTIPLIER_EXPLOSION);
		multiplierReveal.SetActive(false);
		// Fade out the tall geyser.
		 oilVertical.SetActive(false);

		// Let it all sink in
		yield return new TIWaitForSeconds(TIME_AFTER_MULTIPLIER_ROLLUP);
		
		// Play the reveals.
		StartCoroutine(revealRemainingShots());
	}

	private IEnumerator missedShot()
	{
		foreach (GameObject shotIcon in shotIcons)
		{
			CommonGameObject.setObjectCollidersEnabled(shotIcon, false);
		}

		dustCloud.SetActive(true);
		yield return new TIWaitForSeconds(TIME_AFTER_SHOT);
		dustCloud.SetActive(false);

		currentBubble.SetActive(false);
		// Get the next bubble set up.
		setNextBubble();

		// Move the bubble to the center of the screen.
		iTween.MoveTo(currentBubble, iTween.Hash("x", bubbleTargetArea.transform.position.x, "y", bubbleTargetArea.transform.position.y, "time", TIME_MOVE_BUBBLE_TO_CENTER, "islocal", false, "easetype", iTween.EaseType.linear));
		iTween.ScaleTo(currentBubble, iTween.Hash("scale", new Vector3(1.5f, 1.5f, 1.0f), "time", TIME_MOVE_BUBBLE_TO_CENTER, "islocal", true, "easetype", iTween.EaseType.linear));

		yield return new TIWaitForSeconds(TIME_MOVE_BUBBLE_TO_CENTER);

		CommonGameObject.findChild(currentBubble, "multiplierText").GetComponent<UILabel>().color = new Color(1f , 0.7f , 0f);

		foreach (GameObject shotIcon in shotIcons)
		{
			CommonGameObject.setObjectCollidersEnabled(shotIcon, true);
		}
	}
	
	private void revealShot(int arrayIndex, bool afterGameReveal)
	{
		shotIcons[arrayIndex].SetActive(false);
		CommonGameObject.setObjectCollidersEnabled(shotIcons[arrayIndex], false);

		if (multiplier != 0 && !gameEnded)
		{
			oilSplat.SetActive(true);
			oilSplat.transform.position = shotRevealTexts[arrayIndex].transform.position;
		}

		// We always want to show the numbers, because the reveal has a number on it.
		if (arrayIndex < shotRevealTexts.Count)
		{
			shotRevealTexts[arrayIndex].text = CreditsEconomy.convertCredits(pick.credits);
			shotRevealTexts[arrayIndex].gameObject.SetActive(true);
		}

		if (afterGameReveal)
		{
			// Grey out the unpick choices.
			if (arrayIndex < shotRevealTexts.Count)
			{
				shotRevealTexts[arrayIndex].color = Color.gray;
				shotBackgrounds[arrayIndex].color = Color.gray;
			}
			if(!revealWait.isSkipping)
			{
				Audio.play(REVEAL_OTHERS);
			}
		}
		else
		{
			// Add the value to the winnings.
			BonusGamePresenter.instance.currentPayout += pick.credits;
			// Remove the used shotIcon and Reveal Text from the list
			if (arrayIndex < shotRevealTexts.Count)
			{
				shotIcons.RemoveAt(arrayIndex);
				shotRevealTexts.RemoveAt(arrayIndex);
				shotBackgrounds.RemoveAt(arrayIndex);
			}
		}
	}
	
	private IEnumerator revealRemainingShots()
	{
		// All of the remaining shot Icons should be active.
		for (int i = 0; i < shotIcons.Count; i++)
		{
			pick = outcome.getNextReveal();
			revealShot(i, true);
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
		}
		
		yield return new TIWaitForSeconds(TIME_AFTER_REVEALS);

		Audio.stopSound(ambianceLoop);

		if (multiplier < 3)
		{
			Audio.play(SUMMARY_LOW_WIN);
		}
		else if (multiplier < 6)
		{
			Audio.play(SUMMARY_MIDDLE_WIN);
		}
		else
		{
			Audio.play(SUMMARY_HIGH_WIN);
		}
				
		BonusGamePresenter.instance.gameEnded();
	}

	// Sets the currentbubble and bubbleBG to the first value in the playableBubbles and playableBubbleBGs and removes them from the list.
	private void setNextBubble()
	{
		if (playableBubbles.Count > 0)
		{
			currentBubble = playableBubbles[0];
			playableBubbles.RemoveAt(0);
			if (playableBubbleBGs.Count > 0)
			{
				currentBubbleBG = playableBubbleBGs[0];
				playableBubbleBGs.RemoveAt(0);
			}
			else
			{
				Debug.LogWarning("There were not enough playableBubbleBGs, Animations requiring it won't happen.");
			}
		}
		else
		{
			// Bad things are going to happen here, but at least we will have a log of it.
			Debug.LogError("There were not enough playableBubbles");
		}
		if (playableMultipliers.Count > 0)
		{
			currentMultiplier = playableMultipliers[0];
			playableMultipliers.RemoveAt(0);
		}
		else
		{
			Debug.LogError("There were not enough playableMultipliers");
		}
	}
}

