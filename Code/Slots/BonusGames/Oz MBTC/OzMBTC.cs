using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls the "Man Behind the Curtain" Oz bonus game.
*/

public class OzMBTC : ChallengeGame
{
	public GameObject[] stages;
	public GameObject needlePivot;
	public UILabel messageLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent messageLabelWrapperComponent;

	public LabelWrapper messageLabelWrapper
	{
		get
		{
			if (_messageLabelWrapper == null)
			{
				if (messageLabelWrapperComponent != null)
				{
					_messageLabelWrapper = messageLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_messageLabelWrapper = new LabelWrapper(messageLabel);
				}
			}
			return _messageLabelWrapper;
		}
	}
	private LabelWrapper _messageLabelWrapper = null;
	
	public UILabel[] winningsAmountLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] winningsAmountLabelsWrapperComponent;

	public List<LabelWrapper> winningsAmountLabelsWrapper
	{
		get
		{
			if (_winningsAmountLabelsWrapper == null)
			{
				_winningsAmountLabelsWrapper = new List<LabelWrapper>();

				if (winningsAmountLabelsWrapperComponent != null && winningsAmountLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in winningsAmountLabelsWrapperComponent)
					{
						_winningsAmountLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in winningsAmountLabels)
					{
						_winningsAmountLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _winningsAmountLabelsWrapper;
		}
	}
	private List<LabelWrapper> _winningsAmountLabelsWrapper = null;	
	
	public GameObject[] meterIcons;		///< The icons on the meter. Must be defeined from left to right visually.
	public VisualEffectComponent[] meterIconVfx;	// visual effects for each meter icon
	public GameObject[] pieces;
	public Transform revealedParent;
	public GameObject[] wheelValueSizers;
	public UILabel[] wheelValueLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelValueLabelsWrapperComponent;

	public List<LabelWrapper> wheelValueLabelsWrapper
	{
		get
		{
			if (_wheelValueLabelsWrapper == null)
			{
				_wheelValueLabelsWrapper = new List<LabelWrapper>();

				if (wheelValueLabelsWrapperComponent != null && wheelValueLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in wheelValueLabelsWrapperComponent)
					{
						_wheelValueLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in wheelValueLabels)
					{
						_wheelValueLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _wheelValueLabelsWrapper;
		}
	}
	private List<LabelWrapper> _wheelValueLabelsWrapper = null;	
	
	public UISprite[] wheelGlows;
	public GameObject[] wheelGlowVfxAnchors;
	public GameObject wheelGlowVfxPrefab;
	public GameObject wheelGlowWinVfxPrefab;
	public UIFont revealFont;
	public UISprite leverSprite;
	public UISprite leverGlow;
	public UILabel tapLevelLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent tapLevelLabelWrapperComponent;

	public LabelWrapper tapLevelLabelWrapper
	{
		get
		{
			if (_tapLevelLabelWrapper == null)
			{
				if (tapLevelLabelWrapperComponent != null)
				{
					_tapLevelLabelWrapper = tapLevelLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_tapLevelLabelWrapper = new LabelWrapper(tapLevelLabel);
				}
			}
			return _tapLevelLabelWrapper;
		}
	}
	private LabelWrapper _tapLevelLabelWrapper = null;
	
	public GameObject handleParent;
	public GameObject lightsParent;
	public GameObject pieceSheenPrefab;
	public float pieceSheenDelay = 2.0f;
	public GameObject meterVfxPrefab;
	public UIAtlas MBTCAtlas;			// The atlas for the sprites that get added during the reveals.
	
	private bool _isWaitingForTouch = true;
	private bool _autoMode = false;	///< Press space to enable auto mode to get through the game faster.
	
	private List<GameObject> _unrevealedPieces = new List<GameObject>();	///< Keep track of which pieces have not been revealed yet.
	private List<int> _unusedMeters = new List<int>();					///< The meter stops that haven't been used yet.
	private UISprite _revealingPiece = null;
	private SkippableWait revealWait = new SkippableWait();
	
	private PickemOutcome _pickemOutcome = null;	///< Pickem stage outcome.
	private WheelPick _wheelPick = null;
	
	private float _pieceSheenTimer = 0;

	// Constant variables
	private static float[] METER_ANGLES = new float[] { 65, 35, -35, -65, 0 };	// The fifth value is the center "GAME OVER" one.
	private static string[] METER_LOCALIZATIONS = new string[]
	{
		"heart_pays_{0}",
		"diploma_pays_{0}",
		"courage_pays_{0}",
		"slippers_pays_{0}"
	};
	private const string PICK_AGAIN = "pick_again";											// Localized key for pick again.
	private const int GAMEOVER_INDEX = 4;													// The index of the gameover angle.
	private const string OPENING_TITLE = "highlight_all_four_objects_to_open_the_curtain";	// The localized string of the title at the start of the game.
	private const string GAME_OVER = "game_over";											// The localized string key for gameover.
	private const float TIME_BETWEEN_REVEALS = 0.125f;
	private const float TIME_METER_MOVE = 0.5f;												// How long the meter moves back and forth before making a decision.
	private const float TIME_METER_FINISH = 1.0f;											// Amount of time it takes for the meter to get to it's final position after all of the moving has ended.
	private const float TIME_REVEAL_ANIMATION = 0.5f;										// Amount of time to let the reveal animation play for.
	private const float TIME_LEVER_PULLED_DOWN = 1.0f;										// The amount of time it takes for the lever to be pulled down.
	private const float TIME_LEVER_PULLED_RIGHT = 0.5f;										// The amount of time it takes for the lever to be pulled right.
	private const float TIME_BETWEEN_WHEEL_LIGHTS = 0.5f;									// The amount of time between each of the values to light up in stage 2.
	private const float TIME_AFTER_STAGE_TWO_ENDS = 1.0f;									// How much time to wait after the game has ended to "let is all sink in"
	private const float STAGE_TWO_STARTUP_TIME = 0.5f;										// Amount of time to wait after both levers have been pulled while the machine "warms up";
	private const float METER_REVEAL_SOUND_DELAY = 1.0f;									// The amount of time to wait before playing the medal, diploma, heart, or slippers sound after the reveal 
	// Sound Names
	private const string OPENING_SOUND = "wziamozthegreatandpowerful";				// Sound name played at the start of the game.
	private const string PICK_SOUND = "MBC_pick_hat";								// Sound name played when a piece is clicked.
	private const string REVEAL_NUMBER = "MBC_reveal_number";						// Sound name played when a piece is revealed.
	private const string METER_SOUND = "MBC_scalemeter_short";						// Sound name played while the meter is moving left and right.
	private const string GAMEOVER_PICKED = "ww_laughs";								// Sound name played when the gameover tile has been picked.
	private const string HEART_REVEALED_SOUND = "pmwhatuneedisatestimonial";		// Name of sound played when the heart is picked.
	private const string DIPLOMA_REVEALED_SOUND = "pmwhatuneedisadiploma";			// Name of sound played when the diploma is picked.
	private const string MEDAL_REVEALED_SOUND = "pmwhatuneedisamedal";				// Name of sound played when the medal is picked.
	private const string SLIPPERS_REVEALED_SOUND = "pmwhatuneedisaconsultation";	// Name of sound played when the slippers are picked
	private const string REVEAL_OTHERS = "reveal_others";							// Name of mapped sound when the unpicked reveals happen.
	private const string STAGE_TWO_BACKGROUND_MUSIC = "MBC_curtain_bg";				// Name of background sound played when stage 2 starts.
	private const string STAGE_TWO_OPENING_SOUND = "MBC_pull_curtain";				// Name of sound played at the start of stage 2.
	private const string LEVER_PULLED_DOWN = "MBC_lever_front_back";				// Sound name played when lever is pulled down.
	private const string LEVER_PULLED_RIGHT = "MBC_lever_left_right";				// Sound name played when lever is pulled right.
	private const string ELECTIRCAL_BUZZING = "MBC_arc_left";						// Name of sound played while the lights in stage 2 are lighting up.
	private const string FINAL_LIGHT_SOUND = "MBC_reveal_gauge_flourish";			// Sound name played when the final stop is reached in stage 2.
	private string[] LIGHTSOUNDS = new string[]
	{
		"MBC_gaugelight_hi_L",
		"MBC_gaugelight_hi_R",
		"MBC_gaugelight_mid_R",
		"MBC_gaugelight_lo_R",
		"MBC_gaugelight_lo_L",
		"MBC_gaugelight_mid_L"
	};
	
	public override void init() 
	{
		foreach (GameObject piece in pieces)
		{
			_unrevealedPieces.Add(piece);
		}

		for (int i = 0; i < meterIcons.Length; i++)
		{
			_unusedMeters.Add(i);
			
			// Hide the meter icons by default.
			meterIcons[i].SetActive(false);
		}

		updateCreditsRoll(0); //Make sure the score shows zero.
		
		_pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];

		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CREDIT))
		{
			// The player will get to the second phase of this game, which is the "wheel".
			WheelOutcome wheelOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CREDIT];
			_wheelPick = wheelOutcome.getNextEntry();
		}

		// Message is different in mobile than web now, by decree of art.
		messageLabelWrapper.text = Localize.textUpper(OPENING_TITLE);
		
		// Pay no attention to the man behind the hat!
		Audio.play(OPENING_SOUND);

		_didInit = true;
	}
		
	protected override void Update()
	{
		base.Update();
		if (!_didInit)
		{
			return;
		}
		
		// NOTE: Space, left arrow, right arrow, and escape are all reserved keys for user input (WebGL, Android with keyboard).
		//if (Input.GetKeyDown(KeyCode.???))
		//{
		//	_autoMode = true;
		//}
		
		if (_autoMode && _isWaitingForTouch)
		{
			if (stages[0].activeSelf)
			{
				// Automatically pick the next unpicked piece in stage one.
				pieceClicked(_unrevealedPieces[0]);
			}
			else if (stages[1].activeSelf)
			{
				// Automatically click the lever.
				leverClicked();
			}
		}
		
		// while waiting for the player to pick, periodically show a sheen over a random unrevealed piece
		if (_isWaitingForTouch && _unrevealedPieces != null && _unrevealedPieces.Count > 0)
		{
			_pieceSheenTimer -= Time.deltaTime;
			
			if (_pieceSheenTimer <= 0)
			{
				int rand = Random.Range(0, _unrevealedPieces.Count - 1);
				GameObject piece = _unrevealedPieces[rand];
				VisualEffectComponent vfx = VisualEffectComponent.Create(pieceSheenPrefab, piece);
				vfx.transform.localScale = new Vector3(4, 4, 1);
				vfx.transform.localPosition = new Vector3(0.02f, 0, 0);
				_pieceSheenTimer = pieceSheenDelay;
			}
		}
	}

	/// NGUI button callback.
	private void pieceClicked(GameObject go)
	{
		if (!_isWaitingForTouch)
		{
			return;
		}
		
		messageLabelWrapper.text = "";

		// Let's see what the next pick is...
		PickemPick pick = _pickemOutcome.getNextEntry();
		
		StartCoroutine(revealPiece(go, pick, false));
		
		Audio.play(PICK_SOUND);
	}
		
	/// Reveals a piece visually.
	private IEnumerator revealPiece(GameObject go, PickemPick pick, bool isFailed)
	{
		_isWaitingForTouch = false;
		
		_unrevealedPieces.Remove(go);
		
		// Darken the remaining pieces while input is disabled.
		colorUnrevealedPieces(CommonColor.colorFromHex("CCCCCC"));
		
		// Create a UILable or UISprite to be revealed, depending on the pick.
		_revealingPiece = go.GetComponent<UISprite>();
				
		UISprite revealSprite = null;
		
		if (pick.isBonus || pick.isGameOver)
		{
			GameObject iconObj = new GameObject();
			iconObj.layer = Layers.ID_NGUI;
			iconObj.transform.parent = revealedParent;
			iconObj.transform.localScale = Vector3.one;
			iconObj.transform.position = go.transform.position;
			CommonTransform.setZ(iconObj.transform, 0);	// The covering icon is at -15 z, so we need to set this to 0.

			revealSprite = iconObj.AddComponent<UISprite>();
			
			// Create a meter sprite. Do this even if the pick is a gameover, so the meter can land on GAME OVER before knowing what it is.
			revealSprite.spriteName = "graphic_card_gauge";
			revealSprite.atlas = MBTCAtlas;
			
			revealSprite.MakePixelPerfect();
		}
		
		if (!isFailed)
		{
			if (!pick.isBonus && !pick.isGameOver)
			{
				// If not a meter pick, create the credit label immediately.
				createPickLabel(go, pick);
				Audio.play(REVEAL_NUMBER);
			}

			if (_autoMode)
			{
				StartCoroutine(rollCredits(pick.credits));				
			}
			else
			{
				// Animate the covering piece away.
				iTween.ValueTo(gameObject, iTween.Hash("from", 1, "to", 0, "time", TIME_REVEAL_ANIMATION, "onupdate", "updateRevealingAlpha"));
				iTween.ScaleTo(go, iTween.Hash("scale", go.transform.localScale * 1.5f, "time", TIME_REVEAL_ANIMATION, "easetype", iTween.EaseType.linear));
		
				if (!(pick.isBonus || pick.isGameOver))
				{
					// Roll up the winnings, but don't yield on it, since we're yielding on the piece animation duration below.
					StartCoroutine(rollCredits(pick.credits));
				}

				// Wait for the animation to finish.
				yield return new WaitForSeconds(TIME_REVEAL_ANIMATION);

				go.SetActive(false);

				if (pick.isBonus || pick.isGameOver)
				{
					// Play meter sound.
					Audio.play (METER_SOUND);
					
					// Also animate the meter stuff for bonus and gameover picks.
					// Randomly choose a meter stop from the unused ones.
					int stop = GAMEOVER_INDEX;	// gameover uses stop 4, which is the center.
					if (pick.isBonus)
					{
						stop = _unusedMeters[Random.Range(0, _unusedMeters.Count)];
						_unusedMeters.Remove(stop);
					}
					// First go to a random left/right rotation to psych-out the player.
					// Do some math to get some motion that looks cool.
					float firstAngleMax = METER_ANGLES[stop] + 45 * CommonMath.randomSign;
					float firstAngle = Mathf.Clamp(firstAngleMax, -75, 75);
					float secondAngle = Mathf.Clamp(firstAngleMax + 90 * Mathf.Sign(METER_ANGLES[stop] - firstAngleMax), -75, 75);
					
					if (meterVfxPrefab != null && stages.Length >= 1 && stages[0] != null)
					{
						VisualEffectComponent.Create(meterVfxPrefab, stages[0]);
					}
							
					iTween.RotateTo(needlePivot, iTween.Hash("z", firstAngle, "time", TIME_METER_MOVE, "easetype", iTween.EaseType.easeOutQuad));
					yield return new WaitForSeconds(TIME_METER_MOVE);

					iTween.RotateTo(needlePivot, iTween.Hash("z", secondAngle, "time", TIME_METER_MOVE, "easetype", iTween.EaseType.easeInOutQuad));
					yield return new WaitForSeconds(TIME_METER_MOVE);

					// Bring the needle to it's final resting point.
					iTween.RotateTo(needlePivot, iTween.Hash("z", METER_ANGLES[stop], "time", TIME_METER_FINISH, "easetype", iTween.EaseType.easeOutElastic));
					yield return new WaitForSeconds(TIME_METER_FINISH);
			
					if (pick.isBonus)
					{
						if (stop < meterIconVfx.Length && meterIconVfx[stop] != null)
						{
							meterIconVfx[stop].gameObject.SetActive(true);
						}
						
						// Light up the stopped item and play the sound associated with it.
						// "What you need is a swift kick in the ass!"
						meterIcons[stop].SetActive(true);
						messageLabelWrapper.text = Localize.textUpper(METER_LOCALIZATIONS[stop], CreditsEconomy.convertCredits(pick.credits));
						
						// Play the terminator. 
						Audio.play(REVEAL_NUMBER);
						
						switch (stop)
						{
							case 0: //heart
								Audio.play(HEART_REVEALED_SOUND, 1, 0, 1f); 
								revealSprite.spriteName = "graphic_card_heart";
								break;
							case 1: //diploma
								Audio.play(DIPLOMA_REVEALED_SOUND, 1, 0, 1f); 
								revealSprite.spriteName = "graphic_card_scroll";
								break;
							case 2: //medal
								Audio.play(MEDAL_REVEALED_SOUND, 1, 0, 1f); 
								revealSprite.spriteName = "graphic_card_medal";
								break;
							case 3: //slippers
								Audio.play(SLIPPERS_REVEALED_SOUND, 1, 0, 1f); 
								revealSprite.spriteName = "graphic_card_shoes";
								break;
						}
						
						revealSprite.MakePixelPerfect();
					}
					else
					{
						revealSprite.spriteName = "graphic_card_witch";
						revealSprite.MakePixelPerfect();
						// Play witch sound.
						Audio.play(GAMEOVER_PICKED);
					}

					// For meter picks, create the credit label after the meter is done moving.
					createPickLabel(go, pick);
				
					// Roll up the winnings.
					yield return StartCoroutine(rollCredits(pick.credits));
				}
			}

			// Figure out what to do next.
			if (pick.isGameOver)
			{
				// Game over. Reveal the remaining pieces before showing final score.
				messageLabelWrapper.text = Localize.textUpper(GAME_OVER);

				while (_pickemOutcome.revealCount > 0)
				{
					PickemPick reveal = _pickemOutcome.getNextReveal();
					// Play reveal other sound.
					if(!revealWait.isSkipping)
					{
						Audio.play(REVEAL_OTHERS);
					}
					yield return StartCoroutine(revealPiece(_unrevealedPieces[0], reveal, true));
				}
				yield return new WaitForSeconds(1);	// Let it sink in before ending.
				BonusGamePresenter.instance.gameEnded();
			}
			else if (_pickemOutcome.entryCount == 0)
			{
				SlotOutcome challengeBonus = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, pick.bonusGame);
				if (challengeBonus != null)
				{
					WheelOutcome wheelOutcome = new WheelOutcome(challengeBonus);
					_wheelPick = wheelOutcome.getNextEntry();
				}
				// If there are no more picks after this one, then we're heading to stage 2 of this game - the wheel.
				stages[0].SetActive(false);
				stages[1].SetActive(true);

				// Set up the possible win value labels.
				for (int i = 0; i < _wheelPick.wins.Count; i++)
				{
					wheelValueLabelsWrapper[i].text = CreditsEconomy.convertCredits(_wheelPick.wins[i].baseCredits);
					wheelGlows[i].alpha = 0;
				}
				
				// Start Sounds for next phase.
				Audio.stopMusic();
				Audio.switchMusicKey(STAGE_TWO_BACKGROUND_MUSIC);
				Audio.play(STAGE_TWO_OPENING_SOUND);
			}
			else
			{
				messageLabelWrapper.text = Localize.textUpper(PICK_AGAIN);
			}
		}
		else
		{
			// If game is over, just immediately hide the covering sprite and don't do anything else.
			// but don't put point labels on bonus or gameover pieces
			if (!pick.isBonus && !pick.isGameOver)
			{
				createPickLabel(go, pick, isFailed);
			}
			else
			{
				revealSprite.color =  Color.grey;
			}
			go.SetActive(false);
			_revealingPiece = null;
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
		}

		if (!isFailed)
		{
			colorUnrevealedPieces(Color.white);
		}
		// Don't give up the lock if the game is over.
		if (!isFailed)
		{
			_isWaitingForTouch = true;
		}
	}
	
	// Create a credits value label. Sometimes this is immediate, sometimes it's after the meter is done.
	private void createPickLabel(GameObject go, PickemPick pick, bool isFailed = false)
	{
		GameObject labelObj = new GameObject();
		labelObj.layer = Layers.ID_NGUI;
		labelObj.transform.parent = revealedParent;
		labelObj.transform.localScale = Vector3.one;
		labelObj.transform.position = go.transform.position;
		CommonTransform.setZ(labelObj.transform, -10);
		
		UILabel revealLabel = labelObj.AddComponent<UILabel>();
		revealLabel.shrinkToFit = true;
		revealLabel.maxLineCount = 1;
		revealLabel.lineWidth = 325;
		revealLabel.color = CommonColor.colorFromHex("FFDD00");
		revealLabel.effectStyle = UILabel.Effect.Outline;
		revealLabel.effectColor = CommonColor.colorFromHex("2e063a");
		revealLabel.effectDistance = new Vector2(2, 2);
		revealLabel.font = revealFont;
		revealLabel.text = CreditsEconomy.convertCredits(pick.credits);
		revealLabel.depth = 5;
		revealLabel.MakePixelPerfect();
		
		if (isFailed)
		{
			revealLabel.color = Color.grey;
		}
		
	}
	
	private IEnumerator rollCredits(long addAmount)
	{
		long oldValue = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.currentPayout += addAmount;
		
		if (_autoMode)
		{
			updateCreditsRoll(BonusGamePresenter.instance.currentPayout);
		}
		else
		{
			yield return StartCoroutine(SlotUtils.rollup(oldValue, BonusGamePresenter.instance.currentPayout, updateCreditsRoll));
		}
	}
	
	/// Updates the labels athat store the current winning amount
	private void updateCreditsRoll(long value)
	{
		foreach (LabelWrapper label in winningsAmountLabelsWrapper)
		{
			label.text = CreditsEconomy.convertCredits(value);
		}
	}

	/// NGUI button callback for stage 2.
	void leverClicked()
	{
		if (!_isWaitingForTouch)
		{
			return;
		}
		_isWaitingForTouch = false;		
		StartCoroutine(showWheelResults());
	}
	
	/// Shows the wheel results with flare!!
	private IEnumerator showWheelResults()
	{
		// Animate the winning options.
		
		// Animate the lever so it looks like it was pulled down.
		iTween.ScaleTo(leverSprite.gameObject, iTween.Hash("y", 358, "time", TIME_LEVER_PULLED_DOWN));
		iTween.MoveTo(leverSprite.gameObject, iTween.Hash("y", -816, "time", TIME_LEVER_PULLED_DOWN, "islocal", true));
		leverGlow.gameObject.SetActive(false);
		tapLevelLabelWrapper.gameObject.SetActive(false);
		// Play the lever sound.
		Audio.play(LEVER_PULLED_DOWN);
		
		yield return new WaitForSeconds(TIME_LEVER_PULLED_DOWN);
		
		// Start the flashing lights!
		lightsParent.SetActive(true);
		PlayingAudio audioArc = Audio.play(ELECTIRCAL_BUZZING, 1, 0, 0, float.PositiveInfinity);
		
		yield return new WaitForSeconds(STAGE_TWO_STARTUP_TIME);
		
		// Move the other weird handle over.
		iTween.RotateTo(handleParent, iTween.Hash("z", -42, "time", TIME_LEVER_PULLED_RIGHT, "islocal", true));
		Audio.play(LEVER_PULLED_RIGHT);
		yield return new WaitForSeconds(TIME_LEVER_PULLED_RIGHT);

		// Highlight the numbers in rotation. Go around twice then make the pick.
		int steps = wheelValueSizers.Length * 2 + _wheelPick.winIndex;
		for (int i = 0; i < steps; i++)
		{
			int index = i % wheelValueSizers.Length;
			
			wheelGlows[index].alpha = 1f;
			VisualEffectComponent.Create(wheelGlowVfxPrefab, wheelGlowVfxAnchors[index]);
			
			// Throb the size of the value text.
			yield return StartCoroutine(CommonEffects.throb(wheelValueSizers[index], 2.0f, TIME_BETWEEN_WHEEL_LIGHTS));
			
			wheelGlows[index].alpha = 0f;
			
			// Play the appropriate light sound.
			if (i == steps - 1)
			{
				Audio.play(FINAL_LIGHT_SOUND);
			}
			else
			{
				Audio.play(LIGHTSOUNDS[index]);
			}
		}
		// Stop the arcing sound.
		Audio.stopSound(audioArc);
				
		// Highlight the winning value by making it larger permanently.
		wheelGlows[_wheelPick.winIndex].alpha = 1f;
		VisualEffectComponent.Create(wheelGlowWinVfxPrefab, wheelGlowVfxAnchors[_wheelPick.winIndex]);
		iTween.ScaleTo(wheelValueSizers[_wheelPick.winIndex], iTween.Hash("scale", Vector3.one * 2f, "time", TIME_BETWEEN_WHEEL_LIGHTS * .5f));

		yield return StartCoroutine(rollCredits(_wheelPick.credits));
		
		yield return new WaitForSeconds(TIME_AFTER_STAGE_TWO_ENDS);
		
		BonusGamePresenter.instance.gameEnded();
	}

	/// iTween update callback for fading the picked piece to reveal the thing under it.
	private void updateRevealingAlpha(float alpha)
	{
		_revealingPiece.alpha = alpha;
	}

	/// Set the color of all the unrevealed pieces.
	private void colorUnrevealedPieces(Color color)
	{
		foreach (GameObject piece in _unrevealedPieces)
		{
			UISprite sprite = piece.GetComponent<UISprite>();
			sprite.color = color;
		}
	}
	
}

