using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This is the bonus game that occurs after scoring a raise in the Blondie Dagwood's Raise Bonus game.  
It applies a simple wheel outcome to the earned tokens.  Only a wheel spin is done.  No other shenanigans here,
other than maybe animating a texture.  That's probably the hardest thing to do here.
 */
public class RaiseWheel : ChallengeGame
{
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
	
	public UILabel[] WheelLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelLabelsWrapperComponent;

	public List<LabelWrapper> wheelLabelsWrapper
	{
		get
		{
			if (_wheelLabelsWrapper == null)
			{
				_wheelLabelsWrapper = new List<LabelWrapper>();

				if (wheelLabelsWrapperComponent != null && wheelLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in wheelLabelsWrapperComponent)
					{
						_wheelLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in WheelLabels)
					{
						_wheelLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _wheelLabelsWrapper;
		}
	}
	private List<LabelWrapper> _wheelLabelsWrapper = null;	
	
	public GameObject wheelParent;
	public GameObject spinParent;
	public GameObject spinButtonEnabled;
	public GameObject spinButtonDisabled;
	public UILabel spinLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent spinLabelWrapperComponent;

	public LabelWrapper spinLabelWrapper
	{
		get
		{
			if (_spinLabelWrapper == null)
			{
				if (spinLabelWrapperComponent != null)
				{
					_spinLabelWrapper = spinLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_spinLabelWrapper = new LabelWrapper(spinLabel);
				}
			}
			return _spinLabelWrapper;
		}
	}
	private LabelWrapper _spinLabelWrapper = null;
	
	public GameObject wheelStartSpinParticles;
	public GameObject particlesAroundPick;
	public GameObject dagwoodPointing;
	public GameObject dagwoodWinning;
	public UISprite wheelSprite;
	public static GameObject objectToDestroyOnLoad;
	
	private WheelSpinner _spinScript;
	private WheelOutcome _wheelOutcome;
	private const float DEGREES_PER_SLICE = 45.0f;
	private const float WHEEL_FINAL_ANGLE = 112.5f;
	private float _angleToStop;

	// Sound names
	private const string BACKGROUND_MUSIC = "BonusPortalBgBlondie";				// The background music that gets played when this game starts.
	
	/**
	 * Sample JSON response:
	 * {"events":[{"type":"slots_outcome","event":"cnDQPGF1WUCEiNvVsX5s93mH6evUL9OwMSeDBzhMMn0Cc",
	 * "outcome_type":"reel_set","outcomes":[{"outcome_type":"scatter",
	 * "outcomes":[
	 * {"outcome_type":"bonus_game",
	 * 		"outcomes":[{"outcome_type":"wheel","outcomes":[
	 * 		{"outcome_type":"bonus_game",
	 * 			"outcomes":[
	 * 			{"outcome_type":"pickem",
	 * 			"outcomes":[{"outcome_type":"bonus_game",
	 * 					"outcomes":[{"outcome_type":"wheel","win_id":"1493"}],
	 * 					"bonus_game":"com01_challenge_bonus","bonus_game_pay_table":"com01_challenge_bonus",
	 * 					"round_1_stop_id":"1493"}],"picks":["214","104","SPIN"],"reveals":["212","SPIN","105","BAD","BAD","213","103"]}],
	 * 			"bonus_game":"com01_challenge","bonus_game_pay_table":"com01_challenge","pay_table_set_id":"159"}],"win_id":"1518"}],
	 * 			"bonus_game":"com01_portal_main","bonus_game_pay_table":"com01_portal_force_challenge",
	 * 		"round_1_stop_id":"1518"}],"win_id":"42"}],"reel_set":"com01_reelset_force_outcome","reel_stops":[1,14,10,11,1],"anticipations":[3],"anticipation_pairs":{"3":4},"anticipation_sounds":[2,3,4],"anticipation_info":{"reels_landed":[2,3,4],"triggers":{"3":{"reel":4,"starting_cell":0,"height":3,"width":1}}}}],"last_action_processed":2,"ending_credits":38760}
	 */
	/// Grab and parse remaining JSON data that hadn't been parsed from the previous outcome
	public override void init() 
	{
		//Clean up previous bonus game that no longer matters
		if (objectToDestroyOnLoad != null)
		{
			Destroy(objectToDestroyOnLoad);
		}
		CommonEffects.addUIPanelReduceAlphaEffect(spinParent.GetComponent<UIPanel>(), 0.01f, 0.5f, true);
		
		//Set the current payout to what we had from the previous bonus game.
		winLabelWrapper.text = CreditsEconomy.convertCredits(BonusGamePresenter.portalPayout);
		
		//Grab the data for this wheel
		_wheelOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		
		// This wheel always has exactly one result, and one set of wheels to spin against
		WheelPick wheelAndResult = _wheelOutcome.getNextEntry();
		
		// The outcome to match is the win id in _wheelOutcome.
		// Keeping track of the min scale size so everything ends up looking uniform.
		float scaleMin = float.MaxValue;
		for (int i = 0; i < wheelAndResult.wins.Count; i++)
		{
			long wheelLabelAmount = wheelAndResult.wins[i].credits;
			wheelLabelsWrapper[i].text = CommonText.makeVertical(CreditsEconomy.convertCredits(wheelLabelAmount, false));
			scaleMin = Mathf.Min(wheelLabelsWrapper[i].transform.localScale.x, scaleMin);
		}

		//Make text size uniform
     	for (int i = 0; i < wheelAndResult.wins.Count; i++)
		{	
			wheelLabelsWrapper[i].transform.localScale = new Vector3(scaleMin, scaleMin, 1);
		}
		
		//Math: The starting wheel position is the top in between label 1 and label 8.  22.5 degrees offsets this angle, and 
		// our pointer is on the left side of the wheel (90 degrees).  Therefore, the distance of each slice * the index of what we want
		// + 112.5f is our target final resting place.				
		_angleToStop = (DEGREES_PER_SLICE * wheelAndResult.winIndex + WHEEL_FINAL_ANGLE);
		// The current payout for this game is the result from last game + how much we are going to win.
		BonusGamePresenter.instance.currentPayout = BonusGamePresenter.portalPayout + wheelAndResult.credits;
		Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC);
		_didInit = true;
	}
	
	//On Start, make sure the camera exists so we can add a swipeableWheel
	protected override void startGame()
	{
		base.startGame();
		//set up the wheel to be swipeable
		GameObject parentWheelToSpin = wheelParent.transform.parent.gameObject;
		parentWheelToSpin.AddComponent<SwipeableWheel>().init(parentWheelToSpin,_angleToStop, startSpinningTheWheel, onWheelSpinComplete, wheelSprite);
	}
	
	/// If the wheel is spinning, show it!
	protected override void Update()
	{
		base.Update();

		if (_spinScript != null)
		{
			_spinScript.updateWheel();
		}
	}
	
	//This is triggered when the Spin Button is pressed.  
	public void spinClicked()
	{
		_spinScript = new WheelSpinner(wheelParent.transform.parent.gameObject, _angleToStop, onWheelSpinComplete);
		startSpinningTheWheel();
	}
	
	public void startSpinningTheWheel()
	{
		disableSpinButton();
		wheelStartSpinParticles.SetActive(true);
		spinParent.GetComponent<UIPanel>().alpha = 1.0f;
		SwipeableWheel swipeableWheel = wheelSprite.GetComponent<SwipeableWheel>();
		if(swipeableWheel != null)
		{
			Destroy(swipeableWheel);
		}
	}
	
	private void onWheelSpinComplete()
	{
		//Rollup further and then end the game.
		StartCoroutine(finishGame());
		particlesAroundPick.SetActive(true);
		dagwoodPointing.SetActive(false);
		dagwoodWinning.SetActive(true);
	}
	
	private IEnumerator finishGame()
	{
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.portalPayout, BonusGamePresenter.instance.currentPayout, winLabelWrapper));
		yield return new WaitForSeconds(1.5f);
		BonusGamePresenter.instance.gameEnded();
	}
	
	private void disableSpinButton()
	{
		spinButtonEnabled.SetActive(false);
		spinLabelWrapper.color = new Color(0.5f, 0.5f, 0);
		spinButtonDisabled.SetActive(true);
	}
}

