using UnityEngine;
using System.Collections;

public class SnorkelVsBeetleBonus : ChallengeGame
{
	public GameObject inputBlocker;								// Game object that puts a collider over the whole scene so no input can happen.
	public UILabel scoreLabel;									// label holding the players score -  To be removed when prefabs are updated.
	public LabelWrapperComponent scoreLabelWrapperComponent;									// label holding the players score

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
	
	public UILabel multiplierLabel;								// Label holding the current multiplier amount. -  To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierLabelWrapperComponent;								// Label holding the current multiplier amount.

	public LabelWrapper multiplierLabelWrapper
	{
		get
		{
			if (_multiplierLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierLabelWrapper = multiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierLabelWrapper = new LabelWrapper(multiplierLabel);
				}
			}
			return _multiplierLabelWrapper;
		}
	}
	private LabelWrapper _multiplierLabelWrapper = null;
	
	public GameObject buttonPrefab;								// The buttons that got populated in the game.
	public Vector3 buttonSpacing = new Vector3(224, -224);		// Amount of space between each button
	public Vector2int gridSize = new Vector2int(4, 5);			// The grid that we are going to be putting buttons into.
	public GameObject buttonRevealVfxPrefab;					// The vfx that gets played when a button is selected.
	public UISprite fightingIcon;								// The icon that goes on the button when a fight is happening.
	public UISprite fightLostIcon;								// The icon that goes on the button when a fight is lost. If it's won a number shows up.
	public UISprite buttonBackgroundIcon;						// The normal button background
	public GameObject scoreVfxParent;							// The light that goes from left to right while the score is being added up.
	public GameObject multiplierSparkleTrailVfxPrefab;			// The trail and ball that go from the multiplier to the buttons.
	public GameObject multiplierSparklePopVfxPrefab;			// The explosion vfx that pops when it lands on a button.
	
	// fight sprites
	public UISprite beetleSprite;					// Sprite of Beetle
	public UISprite snorkleSprite;					// Sprite of Snorkle
	public UISprite fightCloudSprite1;				// One of the fight clouds used in the fight sequence
	public UISprite fightCloudSprite2;				// One of the fight clouds used in the fight sequence
	public UISprite fightWinSprite;					// Displayed when you win
	public UISprite fightLoseSprite;				// Displayed when you lose
	public UILabel fightWinMultiplierLabel;			// The multiplier that beetle holds up -  To be removed when prefabs are updated.
	public LabelWrapperComponent fightWinMultiplierLabelWrapperComponent;			// The multiplier that beetle holds up

	public LabelWrapper fightWinMultiplierLabelWrapper
	{
		get
		{
			if (_fightWinMultiplierLabelWrapper == null)
			{
				if (fightWinMultiplierLabelWrapperComponent != null)
				{
					_fightWinMultiplierLabelWrapper = fightWinMultiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_fightWinMultiplierLabelWrapper = new LabelWrapper(fightWinMultiplierLabel);
				}
			}
			return _fightWinMultiplierLabelWrapper;
		}
	}
	private LabelWrapper _fightWinMultiplierLabelWrapper = null;
	
	public UILabel gameOver;						// The game over sprite with Snorkle and Beetle. -  To be removed when prefabs are updated.
	public LabelWrapperComponent gameOverWrapperComponent;						// The game over sprite with Snorkle and Beetle.

	public LabelWrapper gameOverWrapper
	{
		get
		{
			if (_gameOverWrapper == null)
			{
				if (gameOverWrapperComponent != null)
				{
					_gameOverWrapper = gameOverWrapperComponent.labelWrapper;
				}
				else
				{
					_gameOverWrapper = new LabelWrapper(gameOver);
				}
			}
			return _gameOverWrapper;
		}
	}
	private LabelWrapper _gameOverWrapper = null;
	

	private PickemOutcome _pickemOutcome;
	private long _multiplier = 1;
	private bool _buttonsEnabled = true;
	private GameObject[] slots;
	private SkippableWait revealWait = new SkippableWait();

	private enum AnimationStage						// The possible stages that the sprites can be in.
	{
		DEFAULT = 0,
		CLOUD1,
		CLOUD2,
		WIN,
		LOSS
	}

	// Constant Variables
	private const string FIGHT_PICK = "FIGHT";							// The name of the fight pick.
	private const string MULTIPLIER_LOCALIZED_TEXT = "{0}X";			// The string that we are using to localize the multiplication display
	private const float TIME_BETWEEN_FIGHT_FRAMES = 0.2f;				// The amount of time between each of the frames in the fight sequence.
	private const float TIME_AFTER_FIGHT_FRAMES = 1.0f;					// The amount of time to wait after the fight has finished.
	private const float TIME_TO_WAIT_AFTER_GAME_END = 2.0f;				// The amount of time to wait after the game has ended to soak it all in.
	private const float TIME_BETWEEN_REVEALS = 0.5f;					// The amount of time between each reveal that you missed.
	private const float TIME_FROM_FIGHT_TO_MULTIPLIER = 0.5f;			// The amount of time to get from the fight to the multipiler for the vfx.
	private const float TIME_FROM_MULTIPLIER_TO_PICK = 1.0f;			// The amount of time to get from the multiplier to the pick for the vfx.
	private const float TIME_DELAY_BEFORE_SHOWING_VALUE = 0.6f;			// The amount of time to wait before showing the number value so the shader is set up.
	private const int MAX_NUMBER_OF_FIGHT_FRAMES = 10;					// The max number of frames that we want to show for each fight.
	private const int MIN_NUMBER_OF_FIGHT_FRAMES = 5;					// The min number of frames that we want to show for each fight.
	private const int NUMBER_OF_FIGHT_STAGES = 2;						// The number of stage that can be switched to for the fight sequence
	// Sound Names
	private const string FIGHT_ADVANCE = "FightAdvanceX";				// Name of the sound played when you move to the next fighting stage.
	private const string PICKEM_PRESSED = "SparklyCardFlip";			// Name of the sound played when a pickem button is pressed.
	private const string REVEAL_NOT_CHOSEN = "reveal_not_chosen";		// Name of mapped sound played when revealing the missed pickem choice.
	private const string SARGE_WINS = "SargeWins";						// Name of the sound played when Sarge wins.
	private const string START_FIGHT1 = "FightBell";					// Name of sound played at the begining of the fight sequence.
	private const string START_FIGHT2 = "SargeVsBeetle";				// Name of sound played at the begining of the fight sequence.
	private const string VALUE_MOVE = "value_move";						// Name of the mapped sound once the multiplier particle starts to move.
	private const string VALUE_LAND = "value_land";						// Name of the mapped sound once the multiplier particle move has finished.


	public override void init() 
	{
		_pickemOutcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as PickemOutcome;
		multiplierLabelWrapper.text = Localize.text(MULTIPLIER_LOCALIZED_TEXT, 1);
		fightWinMultiplierLabelWrapper.text = Localize.text(MULTIPLIER_LOCALIZED_TEXT, 1);
		scoreLabelWrapper.text = "0";

		// spawn all the buttons
		int cols = gridSize.x;
		int rows = gridSize.y;
		slots = new GameObject[cols*rows];
		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Vector3 position = new Vector3(buttonSpacing.x * c, buttonSpacing.y * r, 0) + buttonPrefab.transform.localPosition;
				GameObject button = CommonGameObject.instantiate(buttonPrefab) as GameObject;
				int i = r * cols + c;
#if UNITY_EDITOR
				button.name = "Slot " + i;
#endif
				button.transform.parent = buttonPrefab.transform.parent;
				button.transform.localScale = buttonPrefab.transform.localScale;
				button.transform.localPosition = position;
				button.SetActive(true);

				slots[i] = button;
			}
		}
		toggleFightStage(AnimationStage.DEFAULT);

		_didInit = true;
	}

	public void pickemButtonPressed(GameObject buttonObj)
	{
		if (!inputEnabled)
		{
			return;
		}

		Audio.play(PICKEM_PRESSED);
		StartCoroutine(pickemButtonPressedCoroutine(buttonObj));
	}

	private IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		GameObject slot = buttonObj.transform.parent.gameObject;
		PickemPick pick = _pickemOutcome.getNextEntry();
		
		yield return StartCoroutine(revealSlot(slot, pick, true));
		
		if (_pickemOutcome.entryCount == 0)
		{
			yield return StartCoroutine(revealRemainingSlots());
		}
	}

	private IEnumerator revealRemainingSlots()
	{
		inputEnabled = false;

		foreach (GameObject slot in slots)
		{
			GameObject button = slot.transform.Find("Button").gameObject;
			if (!button.activeSelf)
			{
				continue;	// button already used, move to next one
			}

			PickemPick pick = _pickemOutcome.getNextReveal();
			yield return StartCoroutine(revealSlot(slot, pick, false));
			inputEnabled = false;

			Audio.play(Audio.soundMap(REVEAL_NOT_CHOSEN));
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
		}

		yield return new WaitForSeconds(TIME_TO_WAIT_AFTER_GAME_END);

		BonusGamePresenter.instance.gameEnded();
	}

	// Toggles the remaining slots into a greyed out mode, or back into their normal mode.
	private IEnumerator toggleRemainingSlots(bool toShow)
	{
		foreach (GameObject slot in slots)
		{
			GameObject button = slot.transform.Find("Button").gameObject;
			if (!button.activeSelf)
			{
				continue;	// button already used, move to next one
			}
			UISprite background = button.transform.Find("Button").GetComponent<UISprite>();
			background.color = (toShow) ? Color.white : Color.grey;
		}
		yield return null;
	}

	private IEnumerator revealSlot(GameObject slot, PickemPick pick, bool isPick)
	{
		if (pick == null)
		{
			yield break;
		}

		inputEnabled = false;

		GameObject button = slot.transform.Find("Button").gameObject;
		GameObject reveal = slot.transform.Find("Reveal").gameObject;
		LabelWrapperComponent label = reveal.transform.Find("Label").GetComponent<LabelWrapperComponent>();
		UISprite background = reveal.transform.Find("Background").GetComponent<UISprite>();

		button.SetActive(false);
		reveal.SetActive(true);
		long value = 0;

		// Show the fight pick specifically because it doesn't have a text label.
		if (pick.pick == FIGHT_PICK)
		{
			background.spriteName = fightingIcon.spriteName;
			label.gameObject.SetActive(false);
		}
		else
		{
			value = long.Parse(pick.pick) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			label.text = CreditsEconomy.convertCredits(value);
		}
		
		if (isPick)
		{
			VisualEffectComponent vfx = VisualEffectComponent.Create(buttonRevealVfxPrefab, slot);
			// We need to add a slight delay so it looks right with the layering and shaders.
			if (vfx != null)
			{
				reveal.SetActive(false);
				yield return new WaitForSeconds(TIME_DELAY_BEFORE_SHOWING_VALUE);
				reveal.SetActive(true);
			}
			while (vfx != null)
			{
				yield return null;
			}

			if (pick.pick == FIGHT_PICK)
			{
				bool beetleWinsFight = _pickemOutcome.entryCount > 0;
				yield return StartCoroutine(fightAnimation(beetleWinsFight));

				if (!beetleWinsFight)
				{
					background.spriteName = fightLostIcon.spriteName;
				}
				else
				{
					label.text = Localize.text(MULTIPLIER_LOCALIZED_TEXT, 1);
					background.spriteName = buttonBackgroundIcon.spriteName;
					label.gameObject.SetActive(true);
				}
			}
			else
			{
				long total = value * _multiplier;

				// if there's a multiplier, animate the score being multiplied
				if (_multiplier > 1)
				{
					yield return StartCoroutine(animateApplyScoreMultiplier(label, total));
				}
					
				// Introduced a slight delay here so the click of the button doesn't immediately force the rollup to stop.
				yield return null;

				// animate the score changing
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout+total));

				BonusGamePresenter.instance.currentPayout += total;
				//Debug.LogWarning("You scored " + value + "x" + _multiplier + " points for a total of " + BonusGamePresenter.instance.currentPayout);
			}
		}
		else
		{
			background.color = Color.gray;
			label.color = Color.gray;
		}

		inputEnabled = true;
	}

	// Note: These game objects are not getting toggled on and off anymore because it was showing up blank for a split second.
	// Toggles the stage of the fight that we are in.
	// Stage 0 = off, Snorkel and Beetle are visible.
	// Stage 1 = FightCloud1 on. 2 off.
	// Stage 2 = FightCloud2 on. 1 off.
	// Stage 3 = victory sprite, all of the other sprites are not visible.
	// Stage 4 = Loss sprite, all of the other sprites are not visible.
	private void toggleFightStage(AnimationStage stage)
	{
		Vector3 offScreen = new Vector3(0, 5000, 0);
		switch (stage)
		{
			case AnimationStage.DEFAULT:
				fightCloudSprite1.gameObject.transform.localPosition = offScreen;
				fightCloudSprite2.gameObject.transform.localPosition = offScreen;
				fightWinSprite.gameObject.transform.localPosition = offScreen;
				fightLoseSprite.gameObject.transform.localPosition = offScreen;
				beetleSprite.gameObject.transform.localPosition = Vector3.zero;
				snorkleSprite.gameObject.transform.localPosition = Vector3.zero;
				fightWinMultiplierLabelWrapper.gameObject.SetActive(false);
				gameOverWrapper.gameObject.SetActive(false);
				break;
			case AnimationStage.CLOUD1:
				fightCloudSprite1.gameObject.transform.localPosition = Vector3.zero;
				fightCloudSprite2.gameObject.transform.localPosition = offScreen;
				fightWinSprite.gameObject.transform.localPosition = offScreen;
				fightLoseSprite.gameObject.transform.localPosition = offScreen;
				beetleSprite.gameObject.transform.localPosition = offScreen;
				snorkleSprite.gameObject.transform.localPosition = offScreen;
				break;
			case AnimationStage.CLOUD2:
				fightCloudSprite1.gameObject.transform.localPosition = offScreen;
				fightCloudSprite2.gameObject.transform.localPosition = Vector3.zero;
				fightWinSprite.gameObject.transform.localPosition = offScreen;
				fightLoseSprite.gameObject.transform.localPosition = offScreen;
				beetleSprite.gameObject.transform.localPosition = offScreen;
				snorkleSprite.gameObject.transform.localPosition = offScreen;
				break;
			case AnimationStage.WIN:
				fightCloudSprite1.gameObject.transform.localPosition = offScreen;
				fightCloudSprite2.gameObject.transform.localPosition = offScreen;
				fightWinSprite.gameObject.transform.localPosition = Vector3.zero;
				fightLoseSprite.gameObject.transform.localPosition = offScreen;
				beetleSprite.gameObject.transform.localPosition = offScreen;
				snorkleSprite.gameObject.transform.localPosition = offScreen;
				fightWinMultiplierLabelWrapper.gameObject.SetActive(true);
				break;
			case AnimationStage.LOSS:
				fightCloudSprite1.gameObject.transform.localPosition = offScreen;
				fightCloudSprite2.gameObject.transform.localPosition = offScreen;
				fightWinSprite.gameObject.transform.localPosition = offScreen;
				fightLoseSprite.gameObject.transform.localPosition = Vector3.zero;
				beetleSprite.gameObject.transform.localPosition = offScreen;
				snorkleSprite.gameObject.transform.localPosition = offScreen;
				gameOverWrapper.gameObject.SetActive(true);
				break;
			default:
				Debug.LogWarning("Trying to go to a stage that isn't defined.");
				break;
		}
	}

	private IEnumerator fightAnimation(bool win)
	{
		Audio.play(START_FIGHT1);
		Audio.play(START_FIGHT2);
		yield return StartCoroutine(toggleRemainingSlots(false));
		// Start the fight sequence.
		for (int i = 0; i < Random.Range(MIN_NUMBER_OF_FIGHT_FRAMES, MAX_NUMBER_OF_FIGHT_FRAMES); i++)
		{
			switch (i % NUMBER_OF_FIGHT_STAGES)
			{
				case 0:
					toggleFightStage(AnimationStage.CLOUD1);
					break;
				case 1:
					toggleFightStage(AnimationStage.CLOUD2);
					break;
			}
			yield return new WaitForSeconds(TIME_BETWEEN_FIGHT_FRAMES);
		}
		// The fight is over, show the winner.
		toggleFightStage(AnimationStage.DEFAULT);
		if (win)
		{
			toggleFightStage(AnimationStage.WIN);
			Audio.play(FIGHT_ADVANCE);
		}
		else
		{
			toggleFightStage(AnimationStage.LOSS);
			Audio.play(SARGE_WINS);
		}
		yield return new WaitForSeconds(TIME_AFTER_FIGHT_FRAMES);
		// Turn back on everything after the win.
		if (win)
		{
			yield return StartCoroutine(animateUpdateScoreMultiplier());
			toggleFightStage(AnimationStage.DEFAULT);
		}
		yield return StartCoroutine(toggleRemainingSlots(true));
	}

	// Animates the VFX going from the fight to the multiplier and then changes the value.
	private IEnumerator animateUpdateScoreMultiplier()
	{
		_multiplier++;
		//Debug.LogWarning("Multiplier is now " + _multiplier);

		GameObject vfxParent = this.gameObject;
		VisualEffectComponent vfx = VisualEffectComponent.Create(multiplierSparkleTrailVfxPrefab, vfxParent);
		
		if (vfx == null)
		{
			yield break;
		}
		
		vfx.transform.position = fightWinMultiplierLabelWrapper.transform.position;
		// Make the trail point in the right direction.
		Vector3 startPosition = vfx.transform.localPosition;
		Vector3 endPosition = vfxParent.transform.worldToLocalMatrix.MultiplyPoint3x4(multiplierLabelWrapper.transform.position);
		Vector3 delta = endPosition - startPosition;
		TweenPosition.Begin(vfx.gameObject, TIME_FROM_FIGHT_TO_MULTIPLIER, endPosition);
		vfx.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x));

		Audio.play(VALUE_MOVE);

		yield return new WaitForSeconds(TIME_FROM_FIGHT_TO_MULTIPLIER);

		Audio.play(VALUE_LAND);

		vfx.Finish();
		
		VisualEffectComponent popVfx = VisualEffectComponent.Create(multiplierSparklePopVfxPrefab, multiplierLabelWrapper.transform.parent.gameObject);
		popVfx.transform.localPosition = multiplierLabelWrapper.transform.localPosition;

		multiplierLabelWrapper.text = Localize.text(MULTIPLIER_LOCALIZED_TEXT, _multiplier);
	}
	
	// Animates the VFX going from the multiplier to the label.
	private IEnumerator animateApplyScoreMultiplier(LabelWrapperComponent label, long newValue)
	{
		GameObject vfxParent = this.gameObject;
		VisualEffectComponent vfx = VisualEffectComponent.Create(multiplierSparkleTrailVfxPrefab, vfxParent);

		if (vfx == null)
		{
			yield break;
		}

		vfx.transform.position = multiplierLabelWrapper.transform.position;
		// Make the trail point in the right direction.
		Vector3 startPosition = vfx.transform.localPosition;
		Vector3 endPosition = vfxParent.transform.worldToLocalMatrix.MultiplyPoint3x4(label.transform.position);
		Vector3 delta = endPosition - startPosition;
		TweenPosition.Begin(vfx.gameObject, TIME_FROM_MULTIPLIER_TO_PICK, endPosition);
		vfx.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x));

		Audio.play(VALUE_MOVE);

		yield return new WaitForSeconds(TIME_FROM_MULTIPLIER_TO_PICK);

		Audio.play(VALUE_LAND);

		vfx.Finish();

		VisualEffectComponent.Create(multiplierSparklePopVfxPrefab, label.transform.parent.gameObject);

		label.text = CreditsEconomy.convertCredits(newValue);
	}

	private IEnumerator animateScore(long startScore, long endScore)
	{
		// play vfx
		VisualEffectComponent scoreVfx = null;
		if (scoreVfxParent != null)
		{
			scoreVfx = scoreVfxParent.GetComponentInChildren<VisualEffectComponent>();
		}
		if (scoreVfx != null)
		{
			scoreVfx.Play();
		}

		yield return StartCoroutine(SlotUtils.rollup(startScore, endScore, scoreLabelWrapper));

		//yield return new WaitForSeconds(0.5f);
	}

	private bool inputEnabled
	{
		get
		{
			return _buttonsEnabled;
		}
		set
		{
			_buttonsEnabled = value;

			// start/stop color tween of buttons
			enableButtonColorTweens(_buttonsEnabled);
			// enable/disable collider covering the whole gamed
			inputBlocker.SetActive(!_buttonsEnabled);
		}
	}

	private void enableButtonColorTweens(bool enabled)
	{
		foreach (GameObject buttonObj in slots)
		{
			TweenColor[] tweens = buttonObj.GetComponentsInChildren<TweenColor>();
			foreach (TweenColor tween in tweens)
			{
				tween.enabled = enabled;
			}
		}
	}
}

