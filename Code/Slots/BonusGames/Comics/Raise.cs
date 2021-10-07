using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Raise : ChallengeGame 
{
	public EnvelopePackage[] buttonSelections;
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
	
	public UILabel pickAnEnvelopeText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent pickAnEnvelopeTextWrapperComponent;

	public LabelWrapper pickAnEnvelopeTextWrapper
	{
		get
		{
			if (_pickAnEnvelopeTextWrapper == null)
			{
				if (pickAnEnvelopeTextWrapperComponent != null)
				{
					_pickAnEnvelopeTextWrapper = pickAnEnvelopeTextWrapperComponent.labelWrapper;
				}
				else
				{
					_pickAnEnvelopeTextWrapper = new LabelWrapper(pickAnEnvelopeText);
				}
			}
			return _pickAnEnvelopeTextWrapper;
		}
	}
	private LabelWrapper _pickAnEnvelopeTextWrapper = null;
	
	public GameObject envelope_credits_prefab;
	public GameObject envelope_end_prefab;
	public GameObject envelope_raise_prefab;
	public GameObject bonusGameWinIcon1;
	public GameObject bonusGameWinIcon2;
	public GameObject bonusGameFailIcon1;
	public GameObject bonusGameFailIcon2;
	public GameObject particleWinBox;
	[SerializeField] protected float minPickMeTime = 1.5f;							// Minimum time an animation might take to play next
	[SerializeField] protected float maxPickMeTime = 4.0f;							// Maximum time an animation might take to play next
	
	private bool acceptInput;
	private bool moveToWheelBonus;
	private SkippableWait revealWait = new SkippableWait();
	protected CoroutineRepeater pickMeController;
	private List<EnvelopePackage> unselectedButtons = new List<EnvelopePackage>();

	// Constant Variables
	private const float TIME_PICK_SFX_DELAY = 0.25f;					// The delay between picking and playing the pick sound.
	private const float TIME_PICK_VO_DELAY = 1.5f;						// The delay between picking and playing the voice over.
	private const float TIME_TO_WAIT_BETWEEN_REVEALS = 0.5f;			// The amount of time to wait between each reveal.
	private const float TIME_BEFORE_REVEALS = 0.5f;						// The amount of time to wait before starting the reveals.
	private const float TIME_AFTER_REVEALS = 0.5f;						// The amount of time to wait after the reveals end.
	private const string GAME_OVER = "game_over";						// Localized text string for gameover.
	// Sound Names
	private const string YOU_WANT_A_RAISE = "dtYouWantARaise";			// The sound effect that gets played at the start of the game.
	private const string PICK_ENVELOPE = "DagwoodPicksEnvelope";		// The sound effect that gets played when you pick an envelope.
	private const string GET_RAISE_SFX = "DagwoodGetsRaise";			// The sounds effect that gets played when you pick a raise.
	private const string GET_RAISE_VO = "dtWhyCertainlyMyBoy";			// The voice over that gets played when you pick a raise
	private const string GET_GAMEOVER_SFX = "DagwoodRaiseFail";			// The sound effect that gets played when you pick a losing pick
	private const string GET_GAMEOVER_VO = "dtNoRaiseDagwood";			// The voice over that gets played when you pick a losing pick
	private const string REVEAL_NOT_CHOSEN = "reveal_not_chosen";		// The name of the sound mapped to be played when revealing the missised choices.

	
	private PickemOutcome _outcome;
	/*
		The following represents the effects of the bonus game for Blondie in which a character tries to get a raise through a pick-em game.
		Sample output from the backend: 
		{"events":[{"type":"slots_outcome","event":"TAhrrPCadc01bho8NSsTSW7wqe2YLl06Cb1zdWro03Wd0","outcome_type":"reel_set",
			"outcomes":[{"outcome_type":"scatter","outcomes":[{"outcome_type":"bonus_game","outcomes":[{"outcome_type":"wheel","outcomes":[{"outcome_type":"bonus_game",
				"outcomes":[{
					"outcome_type":"pickem",
					"picks":["104","103","BAD"],
					"reveals":["105","BAD","212","SPIN","213","SPIN","214"]}],
				"bonus_game":"com01_challenge",
				"bonus_game_pay_table":"com01_challenge",
				"pay_table_set_id":"159"}],
				"win_id":"1518"}],
				"bonus_game":"com01_portal_main",
				"bonus_game_pay_table":"com01_portal_force_challenge",
			"round_1_stop_id":"1518"}],
			"win_id":"42"}],
			"reel_set":"com01_reelset_force_outcome",
		"reel_stops":[1,14,10,11,1],"anticipations":[3],"anticipation_pairs":{"3":4},"anticipation_sounds":[2,3,4],"anticipation_info":{"reels_landed":[2,3,4],
		"triggers":{"3":{"reel":4,"starting_cell":0,"height":3,"width":1}}}},
		{"type":"leveled_up","event":"30z6I1XlMZID6dAWhI2dm4xnfuH5ulv6j5kUywA4Ij1f6","level":2}],"last_action_processed":2,"ending_credits":15480}
	 */
	public override void init() 
	{
		_outcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		acceptInput = true;
		moveToWheelBonus = false;
		Color[] colorsToSwapBetween = new Color[] { Color.green, Color.red, Color.blue, Color.white};
		CommonEffects.addOscillateTextColorEffect(pickAnEnvelopeTextWrapper, colorsToSwapBetween, 0.01f);

		foreach (EnvelopePackage envelop in buttonSelections)
		{
			unselectedButtons.Add(envelop);
		}

		pickMeController = new CoroutineRepeater(minPickMeTime, maxPickMeTime, pickMeAnimCallback);
		
		Audio.play(YOU_WANT_A_RAISE);
		
		_didInit = true;
	}

	/// Default overridable mehtod for Unity update, base handles updating the controller for pick me animations
	protected override void Update()
	{
		// Play the pickme animation.
		if (acceptInput && _didInit)
		{
			pickMeController.update();
		}

		base.Update();
	}

	/// Pick me animation player
	/// Make sure the pick me animation transitions to the idle animation.
	/// Initialize pickMeAnimName and pickMeSoundName if they're not the default names.
	protected virtual IEnumerator pickMeAnimCallback()
	{
		if (acceptInput && unselectedButtons.Count > 0)
		{
			int pickmeIndex = Random.Range(0, unselectedButtons.Count);
			EnvelopePackage pickmeObject = unselectedButtons[pickmeIndex];
			
			if (pickmeObject != null)
			{
				yield return StartCoroutine(pickmeObject.playPickMeAnimation());
			}
		}
	}
	
	// This callback is called by all Envelope objects when clicked and active.  Clicking one disables the others until complete.
	private void pickAnEnvelope(GameObject selectedEnvelope)
	{
		if (acceptInput)
		{
			Audio.play(PICK_ENVELOPE);
			toggleEnvelopes(false);
			GameObject parentOfEnvelope = selectedEnvelope.transform.parent.gameObject;
			EnvelopePackage packageToGrab = parentOfEnvelope.GetComponent<EnvelopePackage>();
			unselectedButtons.Remove(packageToGrab);
			LabelWrapper partneredText = packageToGrab.winAmountWrapper;
			
			selectedEnvelope.SetActive(false);
			if (partneredText != null)
			{
				PickemPick selection = _outcome.getNextEntry();
				if (selection.isBonus) 
				{
					SlotOutcome com01_challenge_bonus = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, selection.bonusGame);
					BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new WheelOutcome(com01_challenge_bonus);
					pickAnEnvelopeTextWrapper.gameObject.SetActive(false);
					moveToWheelBonus = true;
					Audio.play(GET_RAISE_SFX, 1, 0, TIME_PICK_SFX_DELAY);
					Audio.play(GET_RAISE_VO, 1, 0, TIME_PICK_VO_DELAY);
					if (envelope_raise_prefab != null)
					{
						GameObject raiseClick = CommonGameObject.instantiate(envelope_raise_prefab) as GameObject;
						raiseClick.layer = selectedEnvelope.layer;
						raiseClick.transform.parent = packageToGrab.effectCorrector.transform;
					}
					else
					{
						Debug.LogError("Missing prefab!");
					}
					packageToGrab.raiseLabelWrapper.gameObject.SetActive(true);
				}
				else if (selection.isGameOver)
				{
					pickAnEnvelopeTextWrapper.text = Localize.textUpper(GAME_OVER);
					Audio.play(GET_GAMEOVER_SFX, 1, 0, TIME_PICK_SFX_DELAY);
					Audio.play(GET_GAMEOVER_VO, 1, 0, TIME_PICK_VO_DELAY);
					if (envelope_raise_prefab != null)
					{
						GameObject failClick = CommonGameObject.instantiate(envelope_end_prefab) as GameObject;
						failClick.layer = selectedEnvelope.layer;
						failClick.transform.parent = packageToGrab.effectCorrector.transform;
					}
					else
					{
						Debug.LogError("Missing prefab!"); 
					}
					packageToGrab.endLabelWrapper.gameObject.SetActive(true);
				}
				else
				{
					particleWinBox.SetActive(true);
					packageToGrab.revealWinAmount(selection.credits);
					StartCoroutine(envelopeCreditRollup(selection.credits, packageToGrab));
					packageToGrab.shineEffect.gameObject.SetActive(false);
				}
				// The game ends if either of these are picked.
				if (selection.isBonus || selection.isGameOver)
				{
					StartCoroutine(revealOtherPicks());
				}
			}
			else
			{
				Debug.LogWarning("Could not find partnered text field.");
			}
		}
	}
	
	/// Roll up the credits delta between the current and ending values, then return control to the user.
	private IEnumerator envelopeCreditRollup(long bonusCredits, EnvelopePackage package)
	{
		if (envelope_credits_prefab != null)
		{
			GameObject creditsClick = CommonGameObject.instantiate(envelope_credits_prefab) as GameObject;
			creditsClick.layer = package.gameObject.layer;
			creditsClick.transform.parent = package.effectCorrector.transform;
		}
		else
		{
			Debug.LogError("Missing prefab!");
		}
		
		yield return null; // wait at least one frame before rolling up so the touch input doesn't cancel the rollup
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + bonusCredits, winLabelWrapper));
		BonusGamePresenter.instance.currentPayout += bonusCredits;
		particleWinBox.SetActive(false);
		toggleEnvelopes(true);
		yield return null;
	}

	private void toggleEnvelopes(bool enable)
	{
		for (int i = 0; i < buttonSelections.Length; i++)
		{
			UISprite spriteToColor = buttonSelections[i].envelopeSprite;
			if (spriteToColor != null)
			{
				spriteToColor.color = enable? Color.white : Color.grey;
			}
			buttonSelections[i].enablingSheen = enable;
		}
		acceptInput = enable;
	}
	
	//Invoke when an end condition is met, then move on either to the bonus wheel or back into the main game
	private IEnumerator revealOtherPicks()
	{
		yield return new WaitForSeconds(TIME_BEFORE_REVEALS);
		for (int i = 0; i < buttonSelections.Length; i++)
		{
			if (buttonSelections[i].envelopeSprite.gameObject.activeSelf) 
			{
				PickemPick thisPick = _outcome.getNextReveal();
				if (thisPick != null)
				{
					if (thisPick.isBonus) 
					{
						if(bonusGameWinIcon1.activeSelf) 
						{
							bonusGameWinIcon2.transform.localPosition = buttonSelections[i].gameObject.transform.localPosition;
							bonusGameWinIcon2.SetActive(true);
						}
						else
						{
							bonusGameWinIcon1.transform.localPosition = buttonSelections[i].gameObject.transform.localPosition;
							bonusGameWinIcon1.SetActive(true);
						}
					}
					else if (thisPick.isGameOver)
					{
						if(bonusGameFailIcon1.activeSelf) 
						{
							bonusGameFailIcon2.transform.localPosition = buttonSelections[i].gameObject.transform.localPosition;
							bonusGameFailIcon2.SetActive(true);
						}
						else
						{
							bonusGameFailIcon1.transform.localPosition = buttonSelections[i].gameObject.transform.localPosition;
							bonusGameFailIcon1.SetActive(true);
						}
					}
					else
					{
						buttonSelections[i].stripAnimation();
						buttonSelections[i].revealWinAmount(thisPick.credits);
					}
					Audio.play(Audio.soundMap(REVEAL_NOT_CHOSEN));
					yield return StartCoroutine(revealWait.wait(TIME_TO_WAIT_BETWEEN_REVEALS));
				}
			}
		}
		yield return new WaitForSeconds(0.5f);
		endGame();
	}
	
	private void endGame()
	{
		if (moveToWheelBonus)
		{
			BonusGamePresenter.portalPayout = BonusGamePresenter.instance.currentPayout;
			RaiseWheel.objectToDestroyOnLoad = BonusGamePresenter.instance.gameObject;
			BonusGameManager.instance.create(BonusGameType.PORTAL);
			BonusGameManager.instance.show();
		}
		else
		{
			BonusGamePresenter.instance.gameEnded();
		}
	}
	
}

