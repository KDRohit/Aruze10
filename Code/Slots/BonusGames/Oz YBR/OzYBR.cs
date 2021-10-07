using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls the "Man Behind the Curtain" Oz bonus game.
*/

public class OzYBR : ChallengeGame
{
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
	
	public OzYBRPiece[] pieces;
	public UILabel[] prizeLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] prizeLabelsWrapperComponent;

	public List<LabelWrapper> prizeLabelsWrapper
	{
		get
		{
			if (_prizeLabelsWrapper == null)
			{
				_prizeLabelsWrapper = new List<LabelWrapper>();

				if (prizeLabelsWrapperComponent != null && prizeLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in prizeLabelsWrapperComponent)
					{
						_prizeLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in prizeLabels)
					{
						_prizeLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _prizeLabelsWrapper;
		}
	}
	private List<LabelWrapper> _prizeLabelsWrapper = null;	
	
	public VisualEffectComponent[] prizeSparkleEffects;
	public UIFont revealFont;
	public GameObject goalParent;
	public UILabel goalAchievedLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent goalAchievedLabelWrapperComponent;

	public LabelWrapper goalAchievedLabelWrapper
	{
		get
		{
			if (_goalAchievedLabelWrapper == null)
			{
				if (goalAchievedLabelWrapperComponent != null)
				{
					_goalAchievedLabelWrapper = goalAchievedLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_goalAchievedLabelWrapper = new LabelWrapper(goalAchievedLabel);
				}
			}
			return _goalAchievedLabelWrapper;
		}
	}
	private LabelWrapper _goalAchievedLabelWrapper = null;
	
	public UILabel goalAmountLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent goalAmountLabelWrapperComponent;

	public LabelWrapper goalAmountLabelWrapper
	{
		get
		{
			if (_goalAmountLabelWrapper == null)
			{
				if (goalAmountLabelWrapperComponent != null)
				{
					_goalAmountLabelWrapper = goalAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_goalAmountLabelWrapper = new LabelWrapper(goalAmountLabel);
				}
			}
			return _goalAmountLabelWrapper;
		}
	}
	private LabelWrapper _goalAmountLabelWrapper = null;
	
	public UILabel goalAmountLadderLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent goalAmountLadderLabelWrapperComponent;

	public LabelWrapper goalAmountLadderLabelWrapper
	{
		get
		{
			if (_goalAmountLadderLabelWrapper == null)
			{
				if (goalAmountLadderLabelWrapperComponent != null)
				{
					_goalAmountLadderLabelWrapper = goalAmountLadderLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_goalAmountLadderLabelWrapper = new LabelWrapper(goalAmountLadderLabel);
				}
			}
			return _goalAmountLadderLabelWrapper;
		}
	}
	private LabelWrapper _goalAmountLadderLabelWrapper = null;
	
	public UISprite road;
	public VisualEffectComponent roadSparkles;
	public GameObject pieceSheenPrefab;
	public float pieceSheenDelay = 5.0f;
	
	private long _goalAchieved = 0;	// The amount achieved so far for the current ladder step.
	private long _lastRollupValue = 0;
	private bool _isWaitingForTouch = true;
	private int _picksRemaining = 0;
	private float _pieceSheenTimer = 0;
	
	private List<OzYBRPiece> _unrevealedPieces = new List<OzYBRPiece>();	// Keep track of which pieces have not been revealed yet.
	private List<OzYBRPiece> _pickedPieces = new List<OzYBRPiece>();		// The pieces that were picked before revealing the results.
	private OzYBRPiece _revealingPiece = null;
	private int lastPieceNumber = 0; // We play special sounds for the first and second reveals.
	
	private List<int> _goals = new List<int>();
	private List<long> _prizes = new List<long>();
	private ThresholdLadderOutcome _ladderOutcome = null;
	private ThresholdLadderRound _round = null;				// The current round of the outcome.
	private int _roundNo = -1;
	private bool _didReachGoal = false;
	private bool _autoMode = false;	// Press space to enable auto mode to get through the game faster.
	private SkippableWait revealWait = new SkippableWait();
	
	private float[] goalY = new float[]
	{
		-335,
		-133,
		71,
		286,
		466
	};

	private float[] roadFill = new float[]
	{
		0f,
		.2f,
		.4f,
		.65f,
		.9f,
		1f
	};

	// Constant Variables
	private const string TITLE_ONE_PICK_LEFT = "select_one_more";			// The localized string on the title when you only have one pick left.
	private const string TITLE_X_PICK_LEFT = "select_{0}_emeralds";			// The localized string on the title when you have more thant 1 pick left.
	private const string X_TO_GO = "{0}_to_go";								// The localized string displaying how many more points are needed to advance.
	private const string GOAL_ACHIEVED = "goal_achieved";					// The localized string showed when the goal has been reached.
	private const string GRAYSCALE_EMERALD = "emerald_gray_m";				// The name of the sprite that is a grayscaled version of the emerald.
	private const string REGULAR_EMERALD = "emerald_color_m";				// The name of the sprite that is the regular version of the emerald.
	private const int SCALE_TO_FIT_HEIGHT = 62;
	private const int SCALE_TO_FIT_AMOUNT_LIMIT = 100000;
	private const float TIME_BETWEEN_REVEALS = 0.125f;						// Time between each of the reveals after a stage has finished.
	private const float TIME_AFTER_ROUND_END = 1.0f;						// Time after the round ends to look at results.
	private const float TIME_AFTER_GAME_END = 1.0f;							// Time to wait after the game has ended, this happens in addition to TIME_AFTER_ROUND_END.
	private const float SECOND_BRICK_SOUND_DELAY = 0.7f;					// Delay between the first and second brick sound.
	private const float TIME_TO_EXTEND_ROAD = 1.0f;							// The amount of time before the YBR reaches it's destination.
	private const float LEVEL_VO_DELAY = 0.6f;								// Time after the brick road has been laid that the LEVEL_VO plays
	private const float REVEAL_PIECE_WAIT = .5f;							// Time to wait between doing emerald value reveals
	private const float REVEAL_ROLLUP_TIME = 2.0f;							// How long the reveal rollups should take

	// Sound names
	private const string ADVANCE_ROUND = "ybr_advance_pointer";				// Name of sound played when the round advances.
	private const string BACKGROUND_MUSIC = "ybrbonusbg0";					// Name of music that plays in the background
	private const string CLICK_SOUND = "menuselect0";						// Name of sound played when a piece is clicked on.
	private const string ROLLUP_SOUND = "Ybr_Rollup_Straight";				// Name of rollup sound used in YBR, called explicitly because rollup sound is distinct from 3 roll ups.
	private const string ROLLUP_FIRST_HIT = "Ybr_Rollup_Straight_Punct1";	// Name of sound played when first emerald roll up finished. Should NOT end ROLLUP_SOUND.
	private const string ROLLUP_SECOND_HIT = "Ybr_Rollup_Straight_Punct2";	// Name of sound played when second emerald roll up finished. Should NOT end ROLLUP_sound.
	private const string ROLLUP_THIRD_HIT = "Ybr_Rollup_Straight_Term";		// Name of sound played when thrid emerald roll up finishes. SHOULD end ROLLUP_SOUND.
	private const string REVEAL_SOUND = "reel_stop5";						// Name of the sound mapped when an unpicked emerald gets revealed.
	private const string FIRST_BRICK_SOUND = "brickshuffle0";				// Name of first sound played when the brick road is falling into place.
	private const string SECOND_BRICK_SOUND = "brickshuffle0";				// Name of second sound played when the brick road is falling into place.
	private const string BRICK_REACHED_GOAL_SOUND = "rollover_sparkly";		// Name of the collection played when the bricks reach their goal.
	private const string GOAL_REACHED = "ybr_goal_reached";					// Name of sound played when the final goal is reached.
	private const string LEVEL_0_VO = "jgbestfriendsanybodyeverhad";		// Name of the level 0 voice over.
	private const string LEVEL_1_VO = "rbnotbrightbendnailmaybeslipoff";	// Name of the level 1 voice over.
	private const string LEVEL_2_VO = "tmsomemostlylionstigersbears";		// Name of the level 2 voice over.
	private const string LEVEL_3_VO = "clsomebodypulledmytail";				// Name of the level 3 voice over.
	private const string LEVEL_4_VO = "wzcomeforeward";						// Name of the level 4 voice over.

	public override void init()
	{		
		_ladderOutcome = (ThresholdLadderOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		
		// Extract the goal thresholds for each round from the global data.
		// For now we hardcode the keyName to get the ThresholdLadderGame object,
		// since it's not included in the outcome data, unfortunately.
		ThresholdLadderGame threshGame = ThresholdLadderGame.find("oz_ybr_bonus");
		
		foreach (ThresholdLadderGameRound round in threshGame.rounds)
		{
			_goals.Add(round.targetScore);
		}
		
		JSON[] progressivePools = SlotBaseGame.instance.slotGameData.progressivePools; // These pools are from highest to lowest.
		
		for (int i = 0; i < threshGame.rounds.Count; i++)
		{
			long prize = 0;
			if (progressivePools != null && progressivePools.Length > 0)
			{
				string keyName = progressivePools[i].getString("key_name", "");
				prize = SlotsPlayer.instance.progressivePools.getPoolCredits(keyName, SlotBaseGame.instance.multiplier, (keyName == _ladderOutcome.progressivePool));

			}
			else
			{
				prize = (long)(threshGame.rounds[i].basePayout * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier);
			}

			_prizes.Add(prize);
			prizeLabelsWrapper[i].text = CreditsEconomy.convertCredits(prize);
			if (prize > SCALE_TO_FIT_AMOUNT_LIMIT)
			{
				prizeLabelsWrapper[i].boxHeight = SCALE_TO_FIT_HEIGHT;
			}
		}
		
		road.fillAmount = 0;
		// Specifically set the sound for the ybr game.
		Audio.stopMusic();
		Audio.switchMusicKey(BACKGROUND_MUSIC);
		_didInit = true;
	}
	
	protected override void startGame()
	{
		base.startGame();
		startNextRound();
	}
	
#if UNITY_EDITOR
	int tmp_i = 0;
#endif
	protected override void Update()
	{
		base.Update();
		if (!_didInit)
		{
			return;
		}
		
#if UNITY_EDITOR
		// cheat to see the yellow brick road animation
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			int i = tmp_i;
			tmp_i = (tmp_i + 1) % 5;
			if (i == 0)
			{
				roadSparkles.Reset();
				roadSparkles.GetComponent<VfxAnimationSegmenter>().Reset();
			}
			StartCoroutine(extendYBR(i % 5));
		}
		
		// NOTE: Space, left arrow, right arrow, and escape are all reserved keys for user input (WebGL, Android with keyboard).
		//if (Input.GetKeyDown(KeyCode.???))
		//	_autoMode = true;
		//}
		
		if (_autoMode && _isWaitingForTouch)
		{
			// Automatically pick the next unpicked piece.
			foreach (OzYBRPiece piece in _unrevealedPieces)
			{
				if (_pickedPieces.IndexOf(piece) == -1)
				{
					pieceClicked(piece.icon.gameObject);
					break;
				}
			}
		}
#endif
		
		if (_isWaitingForTouch && pieceSheenPrefab != null)
		{
			_pieceSheenTimer -= Time.deltaTime;
			if (_pieceSheenTimer <= 0)
			{
				int randPieceIndex = Random.Range(0, _unrevealedPieces.Count-1);
				OzYBRPiece piece = _unrevealedPieces[randPieceIndex];
				VisualEffectComponent.Create(pieceSheenPrefab, piece.gameObject);
				
				_pieceSheenTimer = pieceSheenDelay;
			}
		}
	}

	private void startNextRound()
	{
		_round = _ladderOutcome.getNextEntry();
		
		if (_round == null)
		{
			// No more rounds, so end the game.
			StartCoroutine(showFinalResults());
			return;
		}
		
		_roundNo++;
		_didReachGoal = false;
		
		iTween.MoveTo(goalParent, iTween.Hash("y", goalY[_roundNo], "time", 1f, "islocal", true));
		
		resetPieces();
		
		_goalAchieved = 0;
		goalAchievedLabelWrapper.text = "0";
		goalAmountLabelWrapper.text = CommonText.formatNumber(_goals[_roundNo]);
		goalAmountLadderLabelWrapper.text = CommonText.formatNumber(_goals[_roundNo]);
		
		// Addvance pointer sound
		if (_roundNo > 0)
		{
			Audio.play(ADVANCE_ROUND);
		}
		
		revealWait.reset();
		_isWaitingForTouch = true;
	}

	private IEnumerator pieceClickedDelay(GameObject go)
	{
		if (!_isWaitingForTouch)
		{
			yield break;
		}

		OzYBRPiece piece = go.transform.parent.gameObject.GetComponent<OzYBRPiece>();
		
		// no double taps!
		if (_pickedPieces.Contains(piece))
		{
			yield break;
		}

		// wait a frame so we don't skip the first roll up
		yield return null;

		
		messageLabelWrapper.text = "";

		_pickedPieces.Add(piece);
		
		piece.icon.GetComponent<Collider>().enabled = false;
		
		// Highlight the selected piece.
		piece.EnableGlow = true;
		
		// show the sheen effect
		VisualEffectComponent.Create(pieceSheenPrefab, piece.gameObject);
		
		_picksRemaining--;
		
		switch (_picksRemaining)
		{
			case 0:
				// Time to show results.
				StartCoroutine(revealRoundResults());
				break;
				
			case 1:
				messageLabelWrapper.text = Localize.textUpper(TITLE_ONE_PICK_LEFT);
				break;
				
			case 2:
				messageLabelWrapper.text = Localize.textUpper(TITLE_X_PICK_LEFT, _picksRemaining);
				break;
		}
		
		// Play click sound
		Audio.play(CLICK_SOUND);
	}
		
	/// NGUI button callback.
	private void pieceClicked(GameObject go)
	{
		StartCoroutine(pieceClickedDelay(go));
	}
	
	/// Reveals the picks and non-picks for the round, after all picks were made.
	private IEnumerator revealRoundResults()
	{
		_isWaitingForTouch = false;

		// Show picks first.
		int roundTotal = 0;
		foreach (OzYBRPiece piece in _pickedPieces)
		{
			piece.finalValue = _round.getNextPick();
			roundTotal += piece.finalValue;
		}
		// Play the roll up sound because Ybr_Rollup_Straight_Term will end the roll up sound.
		// and we need to play the sound for all 3 rollups.
		Audio.play(ROLLUP_SOUND);
		roundTotal = 0;
		foreach (OzYBRPiece piece in _pickedPieces)
		{
			lastPieceNumber++;
			yield return StartCoroutine(revealPiece(piece, piece.finalValue));
			yield return new TIWaitForSeconds(REVEAL_PIECE_WAIT);
		}
		_pickedPieces.Clear();
		lastPieceNumber = 0;
		
		if (_didReachGoal)
		{
			// Show more of the YBR. Don't yield on this, because we want it to happen while revealing non-picks.
			StartCoroutine(extendYBR(_roundNo));
			
			// Audio Calls for bricks
			Audio.play(FIRST_BRICK_SOUND);
			Audio.play(SECOND_BRICK_SOUND, 1f, 0f, SECOND_BRICK_SOUND_DELAY);
			Audio.play(BRICK_REACHED_GOAL_SOUND, 1f, 0f, TIME_TO_EXTEND_ROAD);
			
			// Audio vo calls for round
			switch (_roundNo)
			{
				case 0:
					Audio.play(LEVEL_0_VO, 1f, 0f, TIME_TO_EXTEND_ROAD + LEVEL_VO_DELAY);
					break;
				case 1:
					Audio.play(LEVEL_1_VO, 1f, 0f, TIME_TO_EXTEND_ROAD + LEVEL_VO_DELAY);
					break;
				case 2:
					Audio.play(LEVEL_2_VO, 1f, 0f, TIME_TO_EXTEND_ROAD + LEVEL_VO_DELAY);
					break;
				case 3:
					Audio.play(LEVEL_3_VO, 1f, 0f, TIME_TO_EXTEND_ROAD + LEVEL_VO_DELAY);
					break;
				case 4:
					Audio.play(LEVEL_4_VO, 1f, 0f, TIME_TO_EXTEND_ROAD + LEVEL_VO_DELAY);
					break;
			}
		}

		// Set the labels to be in front of the icons for the rest of the reveals.
		foreach (OzYBRPiece piece in pieces)
		{
			CommonTransform.setZ(piece.labelWrapper.transform, -5);
		}

		// Show non-pick reveals next.
		while (_round.revealCount > 0)
		{
			long pick = _round.getNextReveal();
			
			yield return StartCoroutine(revealPiece(_unrevealedPieces[0], pick, true));
		}
		
		yield return new WaitForSeconds(TIME_AFTER_ROUND_END);	// A little delay to let it sink in.

		startNextRound();
	}
		
	/// Reveals a piece visually.
	private IEnumerator revealPiece(OzYBRPiece piece, long pick, bool isFinished = false)
	{
		_lastRollupValue = 0;
		
		piece.labelWrapper.color = isFinished ? Color.white : Color.green;
		
		_unrevealedPieces.Remove(piece);
				
		// Create a UILable or UISprite to be revealed, depending on the pick.
		_revealingPiece = piece;
		
		if (!isFinished)
		{
			piece.labelWrapper.text = CommonText.formatNumber(0); // Roll up starts from 0.
			piece.EnableGlow = false;
			
			piece.StartCoroutine(piece.fadeIcon());	// Don't yield on this, the rollup starts as soon as this starts.
			
			// Roll up the winnings.
			yield return StartCoroutine(rollCredits(pick));
			// Audio call on reveal
			switch (lastPieceNumber)
			{
				case 1:
					Audio.play(ROLLUP_FIRST_HIT);
					break;
				case 2:
					Audio.play(ROLLUP_SECOND_HIT);
					break;
				case 3:
					Audio.play(ROLLUP_THIRD_HIT);
					break;
				default:
					Debug.LogWarning("Revealed more pieces then expected, no sounds to play.");
					break;
			}
		}
		else
		{
			// If level is over, immediately swap the icon for the gray version instead of hiding the icon.
			piece.labelWrapper.text = CommonText.formatNumber(pick);
			_revealingPiece.icon.spriteName = GRAYSCALE_EMERALD;
			
			// Audio call on reveal
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap(REVEAL_SOUND));
			}
			
			if (!_autoMode)
			{
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}
	}
		
	private IEnumerator rollCredits(long addAmount)
	{
#if UNITY_EDITOR
		if (_autoMode)
		{
			updateCreditsRoll(addAmount);
			yield break;
		}
#endif
		// Don't play any sound in the credit roll because we are handing it in revealroundResults.
		yield return StartCoroutine(SlotUtils.rollup(0, addAmount, updateCreditsRoll, false, REVEAL_ROLLUP_TIME));
	}
	
	/// iTween callback.
	private void updateCreditsRoll(long value)
	{
		long added = value - _lastRollupValue;
		if (added < 0)
		{
			// Started a new piece rollup.
			added += _lastRollupValue;
		}
		
		_goalAchieved += (value - _lastRollupValue);
		_lastRollupValue = value;
		
		// Three labels need updating during the rollups.
		_revealingPiece.labelWrapper.text = CommonText.formatNumber(value);
		goalAchievedLabelWrapper.text = CommonText.formatNumber(_goalAchieved);
				
		if (!_didReachGoal)
		{
			if (_goalAchieved >= _goals[_roundNo])
			{
				_didReachGoal = true;
				StartCoroutine(CommonEffects.throb(goalParent, 1.5f, .5f));
				messageLabelWrapper.text = Localize.textUpper(GOAL_ACHIEVED);
				
				// Yay we achieved our goal, here is some audio.
				Audio.play(GOAL_REACHED);
			}
			else
			{
				messageLabelWrapper.text = Localize.text(X_TO_GO, CommonText.formatNumber(_goals[_roundNo] - _goalAchieved));
			}
		}
	}
	
	/// Extends the YBR to the next stage.
	private IEnumerator extendYBR(int roundNo)
	{
		VfxAnimationSegmenter segmenter = roadSparkles.GetComponent<VfxAnimationSegmenter>();
		if (segmenter != null)
		{
			segmenter.PlayNextSegment();
		}
		
		float age = 0;
		
		while (age < TIME_TO_EXTEND_ROAD)
		{
			age += Time.deltaTime;
			yield return null;
			road.fillAmount = Mathf.Lerp(roadFill[roundNo], roadFill[roundNo + 1], age / TIME_TO_EXTEND_ROAD);
		}
		
		if (_roundNo < prizeSparkleEffects.Length)
		{
			VisualEffectComponent sparkleVfx = prizeSparkleEffects[roundNo];
			if (sparkleVfx != null)
			{
				sparkleVfx.gameObject.SetActive(true);
			}
		}
	}

	/// Resets all the pieces back to normal.
	private void resetPieces()
	{
		_picksRemaining = _round.pickCount;
		messageLabelWrapper.text = Localize.textUpper(TITLE_X_PICK_LEFT, _picksRemaining);
		
		foreach (OzYBRPiece piece in pieces)
		{
			piece.icon.gameObject.SetActive(true);
			piece.icon.spriteName = REGULAR_EMERALD;
			piece.icon.alpha = 1;
			piece.icon.GetComponent<Collider>().enabled = true;
			piece.labelWrapper.text = "";
			piece.EnableGlow = false;
		}
		
		_unrevealedPieces.Clear();
		foreach (OzYBRPiece piece in pieces)
		{
			_unrevealedPieces.Add(piece);
			// Set the labels back behind the icons.
			CommonTransform.setZ(piece.labelWrapper.transform, 5);
		}
	}
		
	/// All rounds are done, show the final results.
	private IEnumerator showFinalResults()
	{
		yield return new WaitForSeconds(TIME_AFTER_GAME_END);	// A little delay to let it sink in.
		
		BonusGamePresenter.instance.currentPayout = _prizes[_ladderOutcome.winRound];
		
		BonusGamePresenter.instance.gameEnded();		
		
		yield break;
	}
}

