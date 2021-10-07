using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Oz03ScarecrowChallenge : ChallengeGame
{
	[SerializeField] private GameObject meetTheScarecrowGame;
	[SerializeField] private GameObject pickACrowGame;

	[SerializeField] private UILabel winLabel;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent winLabelWrapperComponent;

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
	
	[SerializeField] private UILabel winLabelCrow;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent winLabelCrowWrapperComponent;

	public LabelWrapper winLabelCrowWrapper
	{
		get
		{
			if (_winLabelCrowWrapper == null)
			{
				if (winLabelCrowWrapperComponent != null)
				{
					_winLabelCrowWrapper = winLabelCrowWrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelCrowWrapper = new LabelWrapper(winLabelCrow);
				}
			}
			return _winLabelCrowWrapper;
		}
	}
	private LabelWrapper _winLabelCrowWrapper = null;
	
	[SerializeField] private UILabel titleLabel;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent titleLabelWrapperComponent;

	public LabelWrapper titleLabelWrapper
	{
		get
		{
			if (_titleLabelWrapper == null)
			{
				if (titleLabelWrapperComponent != null)
				{
					_titleLabelWrapper = titleLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_titleLabelWrapper = new LabelWrapper(titleLabel);
				}
			}
			return _titleLabelWrapper;
		}
	}
	private LabelWrapper _titleLabelWrapper = null;
	
	[SerializeField] private UILabel crowGameTitleLabel;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent crowGameTitleLabelWrapperComponent;

	public LabelWrapper crowGameTitleLabelWrapper
	{
		get
		{
			if (_crowGameTitleLabelWrapper == null)
			{
				if (crowGameTitleLabelWrapperComponent != null)
				{
					_crowGameTitleLabelWrapper = crowGameTitleLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_crowGameTitleLabelWrapper = new LabelWrapper(crowGameTitleLabel);
				}
			}
			return _crowGameTitleLabelWrapper;
		}
	}
	private LabelWrapper _crowGameTitleLabelWrapper = null;
	
	[SerializeField] private UILabel roundLabel;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent roundLabelWrapperComponent;

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
	
	
	[SerializeField] private UISprite[] itemGlows;
	[SerializeField] private GameObject meetScarecrowIcon;
	[SerializeField] private UILabel[] pickemValueLabels = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent[] pickemValueLabelsWrapperComponent = null;

	public List<LabelWrapper> pickemValueLabelsWrapper
	{
		get
		{
			if (_pickemValueLabelsWrapper == null)
			{
				_pickemValueLabelsWrapper = new List<LabelWrapper>();

				if (pickemValueLabelsWrapperComponent != null && pickemValueLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in pickemValueLabelsWrapperComponent)
					{
						_pickemValueLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in pickemValueLabels)
					{
						_pickemValueLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _pickemValueLabelsWrapper;
		}
	}
	private List<LabelWrapper> _pickemValueLabelsWrapper = null;	
	
	[SerializeField] private GameObject[] winAllLabels = null;							// Labels for the win all reveal
	[SerializeField] private UILabel templateWinAllLabel = null;						// Label for win all that I can grab the color off of -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent templateWinAllLabelWrapperComponent = null;						// Label for win all that I can grab the color off of

	public LabelWrapper templateWinAllLabelWrapper
	{
		get
		{
			if (_templateWinAllLabelWrapper == null)
			{
				if (templateWinAllLabelWrapperComponent != null)
				{
					_templateWinAllLabelWrapper = templateWinAllLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_templateWinAllLabelWrapper = new LabelWrapper(templateWinAllLabel);
				}
			}
			return _templateWinAllLabelWrapper;
		}
	}
	private LabelWrapper _templateWinAllLabelWrapper = null;
	
	[SerializeField] private UISprite[] revealSprites;
	[SerializeField] private UISpriteAnimator[] revealAnimators;
	[SerializeField] private GameObject fireRevealEmitterTemplate = null;
	[SerializeField] private GameObject rowTemplate;
	[SerializeField] private Transform[] rowPositions;
	
	[SerializeField] private GameObject[] crowButtons = null;
	[SerializeField] private UILabel[] crowValueLabels = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent[] crowValueLabelsWrapperComponent = null;

	public List<LabelWrapper> crowValueLabelsWrapper
	{
		get
		{
			if (_crowValueLabelsWrapper == null)
			{
				_crowValueLabelsWrapper = new List<LabelWrapper>();

				if (crowValueLabelsWrapperComponent != null && crowValueLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in crowValueLabelsWrapperComponent)
					{
						_crowValueLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in crowValueLabels)
					{
						_crowValueLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _crowValueLabelsWrapper;
		}
	}
	private List<LabelWrapper> _crowValueLabelsWrapper = null;	
	
	[SerializeField] private Animator[] crowReveals = null;
	[SerializeField] private GameObject[] crowPickMeObjects = null;						// Pick me animation containers for the crow stage
	
	private List<Oz03ScarecrowPickemItemGroup> itemGroups = new List<Oz03ScarecrowPickemItemGroup>();
	private WheelOutcome _outcome = null;
	private int currentStep = 0;
	private bool isGameOver = false;
	private bool didWinAll = false;
	private bool isInputEnabled = false;								// Tells if touch input is enabled, will be disabled after each selection
	private bool isAnimatingPickMe = false;
	private Color blueColor;
	private Color winAllLabelColor;										// Store out the win all label color so we can put it back when shown for a pick
	private List<GameObject> fireEmitters = new List<GameObject>();
	private int multiplier = 1;
	private SkippableWait revealWait = new SkippableWait();
	private CoroutineRepeater crowPickMeController;						// Class to call the pickme animation on a loop

	// Constant Variables
	private static readonly string[] stepTitles = new string[]
	{
		"pick_an_apple",
		"pick_a_hat2",
		"pick_a_bushel_of_corn",
		"pick_a_basket",
		"pick_an_emerald_to_meet"
	};
	
	private const float TIME_BETWEEN_REVEALS = .25f;								// Time between each reveal after a pick has been made.
	private const float TIME_BETWEEN_CROW_REVEALS = 0.5f;							// How long to wait between each of the crow reveals.
	private const float TIME_AFTER_REVEALS = 1.0f;									// Time after the reveals are done before moving the rows.
	private const float TIME_TO_MOVE_GROUPS = 1.0f;									// How long each group should move for before it gets to it's resting spot.
	private const float TIME_TO_STAGGER_GROUPS_BY = 0.1f;							// How long to wait between each group moving forward.
	private const float TIME_BETWEEN_ROW_SETUP = 0.15f;								// During the init how long to wait between setting each row.
	private const float TIME_CROW_FLY = 1.0f;										// Time for the animation to finish for the crow flying.
	private const float TIME_BEFORE_CROW_REVEALS = 1.0f;							// How long to wait before showing what you didn't win in stage 2.
	private const float TIME_BEFORE_ENDING_GAME = 0.5f;								// Gotta let it sink in.
	private const string WINS_ALL = "win_all";										// Localized text showed on the symbol that wins everything
	private const string MULTIPLIER_LOCALIZED_TEXT = "{0}X";						// The string that we are using to localize the multiplication display
	private const string PICK_5_TITLE = "pass_5_rounds_to_meet_the_scarecrow";		// Title of the game when the player can pick.
	private const float MIN_TIME_PICKME = 2.0f;										// Minimum time an animation might take to play next
	private const float MAX_TIME_PICKME = 7.0f;										// Maximum time an animation might take to play next
	private const float CROW_PICK_ME_ANIMATION_DURATION = 2.5f;						// Amount of time while the particle system for the crow stage pick me's is shown
	private const float DELAY_BEFORE_SHOWING_CROW_STAGE = 1.0f;						// Slight delay before going to crow stage

	// Sound names
	private const string REVEAL_WIN = "fastsparklyup1";									// Sound played when revealing wins (when you winAll)
	private const string REVEAL_OTHER = "reveal_others";								// Sound played when revealing others.
	private const string REVEAL_GAME_OVER = "MS_flame{0}";								// Formated string for what sound should play for each flame (0: index)
	private const string REVEAL_OTHER_CROWS = "reveal_others";							// Sound name played when other crows are being revealed
	private const string PICK_WIN = "fastsparklyup1";									// Sound name played whenyou pick a winning basket.
	private const string PICK_WIN_ALL = "MS_reveal_win_all_fanfare";					// Sound name played when you win everything
	private const string PICK_GAME_OVER = "MS_reveal_bad_fanfare";						// Sound name played when you click a game over.
	private const string PICK_SCARECROW_ADVANCE = "MS_advance_to_scarecrow_fanfare";	// Sound name played when you click a scarecrow
	private const string SCARECROW_BACKGROUND_MUSIC = "MS_crow_loop";					// Background music played durring stage 2 of bonus
	private const string SHOW_ROW = "reveal_others";									// Showing rows at the start of the game.
	private const string CROW_MULTIPLIER = "MS_reveal_crow_multiplied_value"; 			// Sound name played when you get the 2x crow.
	private const string CROW_CLICKED = "caw0";											// Sound name played when a crow is clicked.
	private const string ADVANCE_ROW = "MS_advance_row_fanfare";						// Sound name played when advancing rows forward.
	private const string PICKME_SOUND = "rollover_sparkly";								// The collection that is played when the pickme animation starts.

	public override void init()
	{
		_outcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as WheelOutcome;
		Audio.switchMusicKeyImmediate("MS_main_loop");
		titleLabelWrapper.text = Localize.text(stepTitles[currentStep]);
		
		// Set up the rows of items.
		// We instantiate them in code since they're all the same
		// except for the sprite that's displayed in each row.
		// Then we position and scale them based on the position transforms.
		string[] itemSpriteNames = new string[]
		{
			"apple_m",
			"hat_m",
			"corn_m",
			"basket_m",
			"emerald_m"
		};
		
		for (int i = 0; i < 5; i++)
		{
			GameObject go = CommonGameObject.instantiate(rowTemplate) as GameObject;
			
			Oz03ScarecrowPickemItemGroup itemGroup = go.GetComponent<Oz03ScarecrowPickemItemGroup>();
			
			for (int j = 0; j < 6; j++)
			{
				itemGroup.sprites[j].spriteName = itemSpriteNames[i];
				itemGroup.sprites[j].MakePixelPerfect();
			}
			
			go.transform.parent = rowTemplate.transform.parent;
			go.transform.localPosition = rowPositions[i].localPosition;
			go.transform.localScale = rowPositions[i].localScale;
			// Keep it inactive for now, until presentRows is called();
		
			if (i < 3)
			{
				// Set the shadow to appear in front of the mask for the nearest 3 rows.
				itemGroup.setShadowDepth(10);
			}
			
			itemGroups.Add(itemGroup);
		}
		
		rowTemplate.SetActive(false);
		
		blueColor = new Color(.5f, .75f, 1f);

		winAllLabelColor = templateWinAllLabelWrapper.color;

		crowPickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, crowPickMeAnimCallback);
		
		_didInit = true;
		StartCoroutine(presentRows());
	}
	
	// Present the rows one at a time.
	private IEnumerator presentRows()
	{
		foreach (Oz03ScarecrowPickemItemGroup row in itemGroups)
		{
			row.gameObject.SetActive(true);
			Audio.play(SHOW_ROW);
			yield return new WaitForSeconds(TIME_BETWEEN_ROW_SETUP);
		}
		isInputEnabled = true;
		StartCoroutine(beginPickMeAnimations());
	}
	
	protected override void Update()
	{
		base.Update();
		
		if (!_didInit)
		{
			return;
		}
		
		if (isInputEnabled)
		{
			// pulse for all steps before the crow step, if in the crow step then play the crow pick me animations
			if (currentStep < 5)
			{
				// Pulsate the glows if waiting for touch.
				setGlowAlpha(CommonEffects.pulsateBetween(.5f, 1, 5));
			}
			else
			{
				crowPickMeController.update();
			}
		}
	}
	
	private void setGlowAlpha(float alpha)
	{
		foreach (UISprite glow in itemGlows)
		{
			glow.alpha = alpha;
		}	
	}
	
	// Begin randomly picked animations on a row of 
	private IEnumerator beginPickMeAnimations()
	{
		isAnimatingPickMe = true;

		while (isInputEnabled)
		{
			int currentRandomSelectedItem = UnityEngine.Random.Range(0, 6);
			Animator itemAnimator = itemGroups[currentStep].itemAnimators[currentRandomSelectedItem];
			
			// Play the one-off pick me animation.
			itemAnimator.Play("Item Pick Me", -1, 0f);
			
			// Wait for the one-off animation to finish.
			float duration = .666f;
			float elapsed = 0f;
			while (elapsed < duration && isInputEnabled)
			{
				yield return null;
				elapsed += Time.deltaTime;
			}
			
			// Reset to the default pose.
			itemAnimator.Play("Item Idle", -1, 0f);
			
			// Wait for one second or until a touch happens.
			duration = 2f;
			elapsed = 0f;
			while (elapsed < duration && isInputEnabled)
			{
				yield return null;
				elapsed += Time.deltaTime;
			}
		}
		
		isAnimatingPickMe = false;
		
		yield return null;
	}

	/// callback for clicking on an item
	public void itemClicked(GameObject clickedItem)
	{
		if (isInputEnabled)
		{
			StartCoroutine(revealSelected(clickedItem));
		}
	}
	
	/// reveals the item the user clicked on and then the rest of the items for that round over time.
	public IEnumerator revealSelected(GameObject clickedItem)
	{
		isInputEnabled = false;
		
		// Wait for the pick me animation to finish, which should only take 1 frame.
		while (isAnimatingPickMe)
		{
			yield return null;
		}
		
		setGlowAlpha(0);

		// Figure out which item was picked.
		int index = 0;
		for (int i = 0; i < 6; i++)
		{
			itemGlows[i].GetComponent<Collider>().enabled = false;

			if (itemGlows[i].gameObject == clickedItem)
			{
				index = i;
			}
		}
		
		Audio.play(PICK_WIN);

		// Store the outcome values.
		WheelPick pick = _outcome.getNextEntry();
		List<long> winValues = getWinValues(pick);
		winValues[winValues.IndexOf(pick.credits)] = winValues[index];
		winValues[index] = pick.credits;

		// Show the reveal animation.
		pickemValueLabelsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits);
		pickemValueLabelsWrapper[index].color = Color.white;
		// Set the reveal animator to the position of the glow instead of the row item's position,
		// because the row item's pivot is set to the bottom of the sprite for animation purposes.
		revealAnimators[currentStep].transform.parent.position = itemGlows[index].transform.position;
		revealAnimators[currentStep].gameObject.SetActive(true);
		itemGroups[currentStep].sprites[index].gameObject.SetActive(false);
		yield return StartCoroutine(revealAnimators[currentStep].play());
		revealAnimators[currentStep].gameObject.SetActive(false);

		int collectAllIndex = getHighestIndex(winValues);
		
		// Every step past the first one has the possibility to win all.
		if (currentStep >= 1)
		{
			if (collectAllIndex == index)
			{
				//pickemValueLabels[index].text = Localize.textUpper(WINS_ALL);
				winAllLabels[index].SetActive(true);
				CommonGameObject.colorUIGameObject(winAllLabels[index], winAllLabelColor);
				
				setRevealSprite(index, "shoes_winall_m", true);
				
				didWinAll = true;
				Audio.play(PICK_WIN_ALL);
			}
			else
			{
				// Guess you're not the best this round.
				didWinAll = false;
			}
		}
		
		// Every step from here on out has the possibility to end the game.
		if (currentStep >= 2)
		{
			if (!pick.canContinue)
			{
				setRevealSprite(index, "end_bonus_m", true);
				// Sucks to suck/
				isGameOver = true;
				//Debug.Log("Playing: " + PICK_GAME_OVER);
				//Audio.play(PICK_GAME_OVER);
			}
		}
		
		// This is the final step before picking the crow
		if (currentStep == 4)
		{
			if (collectAllIndex != index && pick.canContinue)
			{
				setRevealSprite(index, "Scarecrow_icon_m", true);

				Audio.play(PICK_SCARECROW_ADVANCE);
			}
		}

		// Don't show the value if you won all, since all the value will be shown
		if (!didWinAll)
		{
			pickemValueLabelsWrapper[index].gameObject.SetActive(true);
		}

		// If we didn't win anything else we should start the roll up now.
		TICoroutine rollUpRoutine = null;
		if (!didWinAll)
		{
			long initialCredits = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += pick.credits;
			
			// Wait for the rollup so we can play the gameover sound afterwards
			yield return StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, updateCreditsRoll));

			if (isGameOver)
			{
				Audio.play(PICK_GAME_OVER);
			}
		}

		// Reveal the remaining picks.
		for (int i = 0; i < pick.wins.Count; i++)
		{
			if (i == index)
			{
				continue;
			}
			pickemValueLabelsWrapper[i].text = CreditsEconomy.convertCredits(pick.wins[i].credits);
			itemGroups[currentStep].sprites[i].gameObject.SetActive(false);
						
			if (isGameOver)
			{
				int soundIndex = i + 1;
				Audio.play(string.Format(REVEAL_GAME_OVER, soundIndex.ToString()));
			}
			else if (didWinAll)
			{
				Audio.play(REVEAL_WIN);
			}
			else
			{
				if(!revealWait.isSkipping)
				{
					Audio.play(REVEAL_OTHER);
				}
			}

			if (currentStep >= 1)
			{
				if (collectAllIndex == i)
				{
					//pickemValueLabels[i].text = Localize.textUpper(WINS_ALL);
					winAllLabels[i].SetActive(true);
					CommonGameObject.colorUIGameObject(winAllLabels[i], Color.grey);

					setRevealSprite(i, "shoes_winall_m", didWinAll);
				}
				else
				{
					// not a win all, so show the reveal text
					pickemValueLabelsWrapper[i].gameObject.SetActive(true);
				}
			}
			else
			{
				// all are normal reveals for the first step
				pickemValueLabelsWrapper[i].gameObject.SetActive(true);
			}

			// Set the color to be right.
			// Must set the label colors after activating them for the first time,
			// otherwise the UILabelStyler will apply its color upon activation,
			// overriding what we're trying to set here.
			pickemValueLabelsWrapper[i].color = didWinAll ? Color.white : Color.grey;
			
			if (currentStep >= 2)
			{
				if (!pick.wins[i].canContinue)
				{
					setRevealSprite(i, "end_bonus_m");
					
					if (!isGameOver)
					{
						int soundIndex = i + 1;
						Audio.play(string.Format(REVEAL_GAME_OVER, soundIndex.ToString()));
					}
				}
			}
			
			if (currentStep == 4)
			{
				if (collectAllIndex != i && pick.wins[i].canContinue)
				{
					setRevealSprite(i, "Scarecrow_icon_m", didWinAll);
				}
			}

			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
		}

		// Now the player has seen everything that they won, so lets roll up.
		if (didWinAll)
		{
			long initialCredits = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += pick.credits;
			rollUpRoutine = StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, updateCreditsRoll));
		}

		// Wait for the roll up to finish before moving on.
		while (rollUpRoutine != null && !rollUpRoutine.finished)
		{
			yield return null;
		}

		yield return new WaitForSeconds(TIME_AFTER_REVEALS);

		// Destroy any fire emitters we created.
		while (fireEmitters.Count > 0)
		{
			Destroy(fireEmitters[0]);
			fireEmitters.RemoveAt(0);
		}
		
		// Get rid of the row that was just picked. Unless the game is over because the shadow getting destroyed at the end of the game was distracting me.
		if (!isGameOver)
		{
			Destroy(itemGroups[currentStep].gameObject);
			itemGroups[currentStep] = null;
		}
		
		revealWait.reset();
		currentStep++;
		StartCoroutine(moveToNextStep());
	}
	
	// Sets one of the reveal sprites and shows it.
	private void setRevealSprite(int i, string spriteName, bool isPick = false)
	{
		revealSprites[i].gameObject.SetActive(true);
		revealSprites[i].spriteName = spriteName;
		revealSprites[i].MakePixelPerfect();
		revealSprites[i].color = (isPick ? Color.white : blueColor);

		// Create a fire emitter when revealing the flame symbol, but only if a pick, not during reveal
		if (spriteName == "end_bonus_m" && isPick)
		{
			GameObject emitter = CommonGameObject.instantiate(fireRevealEmitterTemplate) as GameObject;
			emitter.SetActive(true);
			emitter.transform.parent = fireRevealEmitterTemplate.transform.parent;
			emitter.transform.localScale = Vector3.one;
			emitter.transform.position = itemGlows[i].transform.position;
			CommonTransform.setZ(emitter.transform, fireRevealEmitterTemplate.transform.localPosition.z);
			fireEmitters.Add(emitter);
		}
	}

	/// transitions between rounds of the game
	public IEnumerator moveToNextStep()
	{
		if (!isGameOver)
		{
			for (int i = 0; i < pickemValueLabelsWrapper.Count; i++)
			{
				pickemValueLabelsWrapper[i].gameObject.SetActive(false);
			}

			for (int i = 0; i < winAllLabels.Length; i++)
			{
				winAllLabels[i].SetActive(false);
			}
			
			for (int i = 0; i < revealSprites.Length; i++)
			{
				revealSprites[i].gameObject.SetActive(false);
			}

			// if currentStep == 5 we are at the scarecrow step, otherwise doing another row of objects
			if (currentStep == 5)
			{
				// hide some of this stuff since it stays on screen for just a bit before the transition occurs
				titleLabelWrapper.gameObject.SetActive(false);
				meetScarecrowIcon.SetActive(false);

				// wait just a bit to avoid a hard cut
				yield return new TIWaitForSeconds(DELAY_BEFORE_SHOWING_CROW_STAGE);

				Audio.switchMusicKeyImmediate(SCARECROW_BACKGROUND_MUSIC);
				meetTheScarecrowGame.SetActive(false);
				pickACrowGame.SetActive(true);
				revealWait.reset();
				isInputEnabled = true;
			}
			else
			{
				titleLabelWrapper.text = Localize.textUpper(PICK_5_TITLE);
				Audio.play(ADVANCE_ROW);
				
				float extraTimeDelayed = 0;
				for (int i = 0; i < 5; i++)
				{
					if (itemGroups[i] != null)
					{
						yield return new WaitForSeconds(TIME_TO_STAGGER_GROUPS_BY * i);	// Delay each one a little, so they are staggered.
						extraTimeDelayed += TIME_TO_STAGGER_GROUPS_BY;
						itemGroups[i].startMarchAnimation();
						iTween.MoveTo(itemGroups[i].gameObject, iTween.Hash("position", rowPositions[i - currentStep].localPosition, "time", TIME_TO_MOVE_GROUPS, "islocal", true, "easetype", iTween.EaseType.linear));
						iTween.ScaleTo(itemGroups[i].gameObject, iTween.Hash("scale", rowPositions[i - currentStep].localScale, "time", TIME_TO_MOVE_GROUPS, "islocal", true, "easetype", iTween.EaseType.linear));
						// Stop the marching animation when the tweens are done.
						StartCoroutine(itemGroups[i].stopMarchAnimation(TIME_TO_MOVE_GROUPS, (i <= 3)));
					}
				}
				
				// We only want to wait for the time it took to move the first group.
				yield return new TIWaitForSeconds(TIME_TO_MOVE_GROUPS - extraTimeDelayed);
				
				roundLabelWrapper.text = string.Format("{0}/5", CommonText.formatNumber(currentStep + 1));

				// Reactivate the coliders.
				for (int i = 0; i < 6; i++)
				{
					itemGlows[i].GetComponent<Collider>().enabled = true;
				}
				isInputEnabled = true;
				// Set up the title.
				titleLabelWrapper.text = Localize.text(stepTitles[currentStep]);
				if (currentStep == 4)
				{
					meetScarecrowIcon.SetActive(true);
				}

				StartCoroutine(beginPickMeAnimations());
			}
		}
		else
		{
			yield return new WaitForSeconds(TIME_BEFORE_ENDING_GAME);
			endGame();
		}
	}

	/// Pick me animation player for the crow stage
	private IEnumerator crowPickMeAnimCallback()
	{
		int buttonIndex = Random.Range(0, crowPickMeObjects.Length);
		Audio.play(PICKME_SOUND);

		// just a particle system so turn it on for a bit then turn it off and reset it
		crowPickMeObjects[buttonIndex].SetActive(true);
		CommonEffects.playAllParticleSystemsOnObject(crowPickMeObjects[buttonIndex]);

		yield return new TIWaitForSeconds(CROW_PICK_ME_ANIMATION_DURATION);

		crowPickMeObjects[buttonIndex].SetActive(false);
		CommonEffects.stopAllParticleSystemsOnObject(crowPickMeObjects[buttonIndex]);
	}

	/// callback for clicking on a crow.
	public void crowClicked(GameObject clickedItem)
	{
		if (isInputEnabled)
		{
			isInputEnabled = false;

			// hide the instruction text
			crowGameTitleLabelWrapper.gameObject.SetActive(false);

			StartCoroutine(revealSelectedCrow(clickedItem));
		}
	}

	/// reveals the crow the user clicked on and then the rest of the crows over time.
	public IEnumerator revealSelectedCrow(GameObject clickedItem)
	{
		int index = 0;
		for (int i = 0; i < crowButtons.Length; i++)
		{
			if (crowButtons[i] == clickedItem)
			{
				index = i;
			}
		}

		Audio.play(CROW_CLICKED);
		WheelPick pick = _outcome.getNextEntry();
		if (pick.credits > 0)
		{
			crowValueLabelsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits);
		}
		else
		{
			Audio.play(CROW_MULTIPLIER);
			multiplier += pick.multiplier;
			crowValueLabelsWrapper[index].text = Localize.text(MULTIPLIER_LOCALIZED_TEXT, multiplier);
		}
		crowButtons[index].SetActive(false);
		crowReveals[index].gameObject.SetActive(true);	// This starts the fly-away animation.

		List<long> winValues = getWinValues(pick);
		// Why are we swaping these values?
		winValues[winValues.IndexOf(pick.credits)] = winValues[index];
		winValues[index] = pick.credits;

		yield return new WaitForSeconds(TIME_CROW_FLY);
		crowValueLabelsWrapper[index].gameObject.SetActive(true);

		yield return new WaitForSeconds(TIME_BEFORE_CROW_REVEALS);
		for (int i = 0; i < winValues.Count; i++)
		{
			if (i == index)
			{
				continue;
			}
			Audio.play(REVEAL_OTHER_CROWS);
			if (winValues[i] > 0)
			{
				crowValueLabelsWrapper[i].text = CreditsEconomy.convertCredits(winValues[i]);
			}
			else
			{
				// TODO: don't just add one to the multiplier here. Should be changed based on what the pick says.
				crowValueLabelsWrapper[i].text = Localize.text(MULTIPLIER_LOCALIZED_TEXT, multiplier + 1);
			}
			crowButtons[i].SetActive(false);
			crowValueLabelsWrapper[i].gameObject.SetActive(true);
			crowValueLabelsWrapper[i].color = Color.grey;

			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_CROW_REVEALS));
		}
		
		long initialCredits = BonusGamePresenter.instance.currentPayout;
		
		if (pick.credits > 0)
		{
			BonusGamePresenter.instance.currentPayout += pick.credits;
		}
		else
		{
			BonusGamePresenter.instance.currentPayout *= multiplier;
		}
		
		yield return StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, updateCreditsRoll));
		yield return new WaitForSeconds(TIME_BEFORE_ENDING_GAME);
		endGame();
	}

	private void endGame()
	{
		SlotBaseGame.instance.gameObject.SetActive(true);
		BonusGamePresenter.instance.gameEnded();
	}

	/// Rollup callback function.	
	private void updateCreditsRoll(long value)
	{
		winLabelWrapper.text = CreditsEconomy.convertCredits(value);
		winLabelCrowWrapper.text = CreditsEconomy.convertCredits(value);
	}
	
	/// extract the win values from the pick win json array
	private List<long> getWinValues(WheelPick pick)
	{
		List<long> winValues = new List<long>();

		for (int i = 0; i < pick.wins.Count; i++)
		{
			winValues.Add(pick.wins[i].credits);
			//Debug.Log("Win at index " + i + " is " + wins[i].getLong("credits", 0));
		}
		return winValues;
	}

	/// find the index of the pick that wins all of the other picks or in the case of the crow section, the pick that doubles the score.
	private int getHighestIndex(List<long> winValues)
	{
		int highest = 0;
		for (int i = 1; i < winValues.Count; i++)
		{
			if (winValues[i] > winValues[highest])
			{
				highest = i;
			}
		}

		return highest;
	}
}

