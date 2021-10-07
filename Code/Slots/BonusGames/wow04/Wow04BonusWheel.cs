using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 Logic for the Rome (wow_04) bonus game.  The bonus game goes through a few different states that are tracked with the 
 bonus state enum.  At first, the user will have to select a pick to make.  During this time, we want to flash the
 options to them.  After making a pick, we move to the animate fireworks state in which we show activations of the bonus
 wheel.  Once the animations are done, we will move on to the state in which we are ready to press the start button.
 Next step is to change state to the wheel(s) actively going towards their expected result.  From there, the paths branch
 depending on the result.  If the user only gets credits, the credits get added to the result and that result is kicked
 back to the user, and the main game resumes.  Otherwise, we delve further into a second bonus game in which we pick
 stamps that equate to payouts. These payouts go to a summary screen.  At that stage, we also kick back out to the 
 user and return to the game.
 */
public class Wow04BonusWheel : ChallengeGame
{
	public UILabel blueResult;	// To be removed when prefabs are updated.
	public LabelWrapperComponent blueResultWrapperComponent;

	public LabelWrapper blueResultWrapper
	{
		get
		{
			if (_blueResultWrapper == null)
			{
				if (blueResultWrapperComponent != null)
				{
					_blueResultWrapper = blueResultWrapperComponent.labelWrapper;
				}
				else
				{
					_blueResultWrapper = new LabelWrapper(blueResult);
				}
			}
			return _blueResultWrapper;
		}
	}
	private LabelWrapper _blueResultWrapper = null;
	
	public WheelPickOutcomeManager managerOfResultOfChoices; ///< The manager object that controls pin selection choices. 
	public GameObject leftBonusWheel; ///< A reference to the bonus wheel on the left
	public GameObject rightBonusWheel; ///< A reference to the bonus wheel on the right
	public UILabel winAmountForPureCredits; ///< The rollup target for when the spins just generate results -  To be removed when prefabs are updated.
	public LabelWrapperComponent winAmountForPureCreditsWrapperComponent; ///< The rollup target for when the spins just generate results

	public LabelWrapper winAmountForPureCreditsWrapper
	{
		get
		{
			if (_winAmountForPureCreditsWrapper == null)
			{
				if (winAmountForPureCreditsWrapperComponent != null)
				{
					_winAmountForPureCreditsWrapper = winAmountForPureCreditsWrapperComponent.labelWrapper;
				}
				else
				{
					_winAmountForPureCreditsWrapper = new LabelWrapper(winAmountForPureCredits);
				}
			}
			return _winAmountForPureCreditsWrapper;
		}
	}
	private LabelWrapper _winAmountForPureCreditsWrapper = null;
	
	public GameObject parentOfWheelPins; ///< Immediate parent of the objects representing the pins on the wheel.
	public GameObject spinButtonEnabled; ///< The enabled version of the spin button
	public GameObject spinButtonDisabled; ///< The disabled version of the spin button
	public UILabel spinText; ///< The text over the spin button -  To be removed when prefabs are updated.
	public LabelWrapperComponent spinTextWrapperComponent; ///< The text over the spin button

	public LabelWrapper spinTextWrapper
	{
		get
		{
			if (_spinTextWrapper == null)
			{
				if (spinTextWrapperComponent != null)
				{
					_spinTextWrapper = spinTextWrapperComponent.labelWrapper;
				}
				else
				{
					_spinTextWrapper = new LabelWrapper(spinText);
				}
			}
			return _spinTextWrapper;
		}
	}
	private LabelWrapper _spinTextWrapper = null;
	
	public GameObject explosionEffectPrefab; /// The prefab for an explosion effect on the wheel pins	
	public GameObject[] highlightPos;
	
	public GameObject[] pickMeText;
	public UILabel bannerLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent bannerLabelWrapperComponent;

	public LabelWrapper bannerLabelWrapper
	{
		get
		{
			if (_bannerLabelWrapper == null)
			{
				if (bannerLabelWrapperComponent != null)
				{
					_bannerLabelWrapper = bannerLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_bannerLabelWrapper = new LabelWrapper(bannerLabel);
				}
			}
			return _bannerLabelWrapper;
		}
	}
	private LabelWrapper _bannerLabelWrapper = null;
	

	public Object whiteWheelExplosionVFX;
	private GameObject _whiteLeftWheelExplosionVFX;
	private GameObject _whiteRightWheelExplosionVFX;

	public Object sparkleTrailVFX;
	private GameObject _sparkleTrailVFX;

	public Object piePieceHeighlightVFX;
	private GameObject _piePieceHeighlightVFX;

	public UISprite wheelSprite; //Used to get the size for the swipeToSpin Feature.
	
	private List<GameObject> wheelChoices = new List<GameObject>();
	private bonusStates currentState;
	private float delayBetweenAnimations = 0.5f;
	private int wheelPickValue = 0;
	private string wheelPickMask = "000000";
	private float leftWheelPointerRestingAngle = 0; //The stop id is left pointer indexed.  Bear that in mind.
	private float rightWheelPointerRestingAngle = 0; //same for the right wheel. 
	private string wheelPointersStopID;
	private string subBonusGameType = "none";
	private WheelSpinner leftWheelSpinningObject;
	private WheelSpinner rightWheelSpinningObject;
	private GameObject[] pins;
	private bool leftWheelSpun, rightWheelSpun = false;
	
	private WheelOutcome wheelOutcome;
	private WheelPick wheelPick;
	private SkippableWait revealWait = new SkippableWait();
	
	private int playerSelection = 0;
	
	private enum bonusStates
	{
		GAME_BEGIN,
		PICK_MADE_ANIMATING,
		PICK_MADE_READY_TO_SPIN,
		WHEEL_SPIN,
		WHEEL_FINISHED_SPINNING
	}
	
	public override void init()
	{
		currentState = bonusStates.GAME_BEGIN;
		
		//Make the baseSlotGame inactive during this time
		SlotBaseGame.instance.gameObject.SetActive(false);
		int quickIndex = 0;
		
		//Connect the pins under the outcome manager to the main wheel
		pins = new GameObject[6];
		
		foreach (GameObject pin in CommonGameObject.findDirectChildren(parentOfWheelPins))
		{
			pins[quickIndex] = pin;
			quickIndex++;
		}
		
		Audio.switchMusicKey("");
		Audio.playMusic("wheel_slows_to_stop_wow04");
		
		// Begin the process to parse the results from the json provided by the server		
		WheelOutcome wheelOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.PORTAL];
		wheelPick = wheelOutcome.getNextEntry();
				
		// Get which wheels will have some results in binary format, for example 010000 means only the center red pick is displayed.
		wheelPickValue = wheelOutcome.parameter;
		wheelPickMask = getActivatedPicksAsString(wheelPickValue);

		UILabel[] leftWheelLabels = leftBonusWheel.GetComponentsInChildren<UILabel>();
		int indexOfLabel = 0;
		
		// Process the wheel wedges first from the outcome table.  Wheel starts off so that it is on its top edge, so to center, 
		// we require shifting backwards on the wedge 22.5 degrees to match SCAT expected outcomes.  The +2 here is because the label
		// provided from SCAT is the leftmost pin on both wheels.  
		foreach (JSON winLine in wheelOutcome.rounds[0].getJsonArray("wins"))
		{
			long creditLine = winLine.getInt("credits", 0) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			if (winLine.getInt("id", 0) == wheelOutcome.roundStopIDS[0])
			{
				//Angle = (Index +2) * (360/Sections of wheel) + (half of the last value)
				leftWheelPointerRestingAngle = ((indexOfLabel + 2) * 45.0f) - 22.5f ;
			}
			
			leftWheelLabels[indexOfLabel].text = CommonText.makeVertical(CreditsEconomy.convertCredits(creditLine, false));

			indexOfLabel++;
		}
				
		UILabel[] rightWheelLabels = rightBonusWheel.GetComponentsInChildren<UILabel>();
		indexOfLabel = 0;
		int redOffset = 0;
		
		// Each round here represents a different wheel. so this is now our second wheel.
		foreach (JSON nextWinLine in wheelOutcome.rounds[1].getJsonArray("wins"))
		{
			long creditLine = nextWinLine.getInt("credits", 0) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			if (nextWinLine.getInt("id", 0) == wheelOutcome.roundStopIDS[1])
			{
				//Angle = (Index +2 + redOffset) * (360/Sections of wheel) + (half of the last value)
				rightWheelPointerRestingAngle = ((indexOfLabel + 2 + redOffset) * 45.0f) - 22.5f;
			}
			
			if (creditLine != 0)
			{
				rightWheelLabels[indexOfLabel].text = CommonText.makeVertical(CreditsEconomy.convertCredits(creditLine, false));
				indexOfLabel++;
			}
			else
			{
				redOffset++;	
			}
		}
		
		long winnings = 0;
		
		while (wheelPick != null)
		{
			winnings += wheelPick.credits;
			wheelPick = wheelOutcome.getNextEntry();
		}
				
		// Next, check to see if there are additional results
		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CHALLENGE))
		{
			subBonusGameType = "atg";
		}
		
		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING))
		{
			subBonusGameType = "freespin";
		}
		
		BonusGamePresenter.portalPayout = winnings;
		BonusGamePresenter.instance.currentPayout = winnings;

		_didInit = true;
	}

	private void enableSwipeToSpin()
	{
		bool leftWheelNeeded = wheelPickMask.Substring(3).Contains('1');
		bool rightWheelNeeded = wheelPickMask.Substring(0, 3).Contains('1');
		if (leftWheelNeeded)
		{
			leftBonusWheel.AddComponent<SwipeableWheel>().init(leftBonusWheel,leftWheelPointerRestingAngle,onLeftSwipeStart, onLeftWheelStop,wheelSprite);
		}
		if (rightWheelNeeded)
		{
			rightBonusWheel.AddComponent<SwipeableWheel>().init(rightBonusWheel,rightWheelPointerRestingAngle,onRightSwipeStart,onRightWheelStop,wheelSprite);
		}
	}

	private void disableSwipeToSpin()
	{
		SwipeableWheel swipeableWheel = leftBonusWheel.GetComponent<SwipeableWheel>();
		
		if (swipeableWheel != null)
		{
			Destroy(swipeableWheel);
		}
		
		swipeableWheel = rightBonusWheel.GetComponent<SwipeableWheel>();
		
		if (swipeableWheel != null)
		{
			Destroy(swipeableWheel);
		}
	}

	private void onLeftSwipeStart()
	{
		bannerLabelWrapper.text = Localize.text("good_luck");
		playLeftWheelExplosion();
		disableSpinButton();
	}
	
	private void onRightSwipeStart()
	{
		bannerLabelWrapper.text = Localize.text("good_luck");
		playRightWheelExplosion();
		disableSpinButton();
	}

	private void onLeftWheelStop()
	{
		leftWheelSpun = true;
		highlightLeftWinBoxes();
		swipeSpinComplete();
	}
	private void onRightWheelStop()
	{
		rightWheelSpun = true;
		highlightRightWinBoxes();
		swipeSpinComplete();
	}

	private void swipeSpinComplete(){
		bool leftWheelNeeded = wheelPickMask.Substring(3).Contains('1');
		bool rightWheelNeeded = wheelPickMask.Substring(0, 3).Contains('1');
		bool bothNeeded = leftWheelNeeded && rightWheelNeeded;
		
		//Left wheel only ends if there is no right wheel spins.
		if ((bothNeeded && leftWheelSpun && rightWheelSpun) ||
			(!bothNeeded && leftWheelNeeded && leftWheelSpun) ||
			(!bothNeeded && rightWheelNeeded && rightWheelSpun))
		{
			currentState = bonusStates.WHEEL_FINISHED_SPINNING;
			StartCoroutine(endMainBonusGame());
		}
	}

	private void playLeftWheelExplosion()
	{
		if (initLeftWhiteWheelExplosion())
		{
			VisualEffectComponent vfxComp = _whiteLeftWheelExplosionVFX.GetComponent<VisualEffectComponent>();
			if (vfxComp == null)
			{
				vfxComp = _whiteLeftWheelExplosionVFX.AddComponent<VisualEffectComponent>();
				vfxComp.playOnAwake = true;
				vfxComp.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
			}
			vfxComp.Reset();
			vfxComp.Play(); //Once it's done it will just sit there idly.
		}
	}

	private void playRightWheelExplosion()
	{
		if (initRightWhiteWheelExplosion())
		{
			VisualEffectComponent vfxComp = _whiteRightWheelExplosionVFX.GetComponent<VisualEffectComponent>();
			if (vfxComp == null)
			{
				vfxComp = _whiteRightWheelExplosionVFX.AddComponent<VisualEffectComponent>();
				vfxComp.playOnAwake = true;
				vfxComp.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
			}
			vfxComp.Reset();
			vfxComp.Play(); //Once it's done it will just sit there idly.
		}
	}

	private bool initSparkleTrailAnimation()
	{
		if (sparkleTrailVFX != null)
		{
			_sparkleTrailVFX = CommonGameObject.instantiate(sparkleTrailVFX) as GameObject;
			if (_sparkleTrailVFX != null)
			{
				CommonGameObject.setLayerRecursively(_sparkleTrailVFX, Layers.ID_NGUI_OVERLAY);
			}
			else
			{
				Debug.LogError("sparkleTrailVFX could not be Instantiated");
				return false;
			}

		}
		else
		{
			Debug.LogWarning("sparkleTrailVFX is not assigned");
			return false;
		}
		return true;
	}

	private bool initPiePieceHighlight()
	{
		if (piePieceHeighlightVFX != null)
		{
			_piePieceHeighlightVFX = CommonGameObject.instantiate(piePieceHeighlightVFX) as GameObject;
			if (_piePieceHeighlightVFX != null)
			{
				CommonGameObject.setLayerRecursively(_piePieceHeighlightVFX, Layers.ID_NGUI);
			}
			else
			{
				Debug.LogError("piePieceHeighlightVFX could not be Instantiated");
				return false;
			}

		}
		else
		{
			Debug.LogWarning("piePieceHeighlightVFX is not assigned");
			return false;
		}
		return true;
	}

	//inits the LeftWheelExplosion, and attaches it to the left wheel. Returns false if something goes wrong.
	private bool initLeftWhiteWheelExplosion()
	{
		if (whiteWheelExplosionVFX != null)
		{
			_whiteLeftWheelExplosionVFX = CommonGameObject.instantiate(whiteWheelExplosionVFX) as GameObject;
			if (_whiteLeftWheelExplosionVFX != null)
			{
				CommonGameObject.setLayerRecursively( _whiteLeftWheelExplosionVFX, Layers.ID_NGUI); //I don't know what layer I should be putting this on
				_whiteLeftWheelExplosionVFX.transform.parent = leftBonusWheel.transform;
				_whiteLeftWheelExplosionVFX.transform.localPosition = new Vector3(0,0,0);

			}
			else
			{
				Debug.LogError("whiteWheelExplosionVFX could not be Instantiated");
				return false;
			}
		}
		else
		{
			Debug.LogWarning("whiteWheelExplosionVFX is not assigned");
			return false;
		}
		return true;
	}

	//inits the RightWheelExplosion, and attaches it to the right wheel. Returns false if something goes wrong.
	private bool initRightWhiteWheelExplosion()
	{
		if (whiteWheelExplosionVFX != null)
		{
			_whiteRightWheelExplosionVFX = CommonGameObject.instantiate(whiteWheelExplosionVFX) as GameObject;
			if (_whiteRightWheelExplosionVFX != null)
			{
				CommonGameObject.setLayerRecursively( _whiteRightWheelExplosionVFX, Layers.ID_NGUI); //I don't know what layer I should be putting this on
				_whiteRightWheelExplosionVFX.transform.parent = rightBonusWheel.transform;
				_whiteRightWheelExplosionVFX.transform.localPosition = new Vector3(0,0,0);

			}
			else
			{
				Debug.LogError("whiteWheelExplosionVFX could not be Instantiated");
				return false;
			}
		}
		else
		{
			Debug.LogWarning("whiteWheelExplosionVFX is not assigned");
			return false;
		}
		return true;
	}
	
	protected override void Update()
	{
		base.Update();

		switch (currentState)
		{
			case bonusStates.PICK_MADE_READY_TO_SPIN:
				break;
				
			case bonusStates.WHEEL_SPIN:
				
				if (leftWheelSpinningObject != null)
				{
					leftWheelSpinningObject.updateWheel();
				}
				if (rightWheelSpinningObject != null)
				{
					rightWheelSpinningObject.updateWheel();
				}
				break;
				
			case bonusStates.WHEEL_FINISHED_SPINNING:
				break;
		}
	}
	
	///Figure out which spin pointers are active
	private string getActivatedPicksAsString(int bitmask)
	{
		string baseConversion = System.Convert.ToString(bitmask, 2);
		//Pad out the length to 6
		while (baseConversion.Length < 6)
		{
			baseConversion = "0" + baseConversion;
		}
		return baseConversion;
	}
	
	///The trigger that sets that the user has clicked a particular selection.  We will compare the clicked
	///sprite to the registered ones and mark that first one to be cleared out
	public void clickBonusButton(GameObject clickedSprite)
	{
		if (currentState == bonusStates.GAME_BEGIN)
		{
			// Play the pick sound.
			Audio.play("RomeRevealOtherPicks");
			
			currentState = bonusStates.PICK_MADE_ANIMATING;
			
			playerSelection = 0;
			
			//Select which int corresponds to the target picked by comparing them against registered objects,
			//and take the index.
			WorldOfWheelsChoice choiceMade = clickedSprite.GetComponent<WorldOfWheelsChoice>();
			if (choiceMade != null)
			{
				playerSelection = choiceMade.index;
			}
			
			//Set up the switcheroo and execute
			managerOfResultOfChoices.makePick(wheelPickValue, playerSelection);
			
			//Display to user and remove from active list
			clickedSprite.SetActive(false);
			pickMeText[playerSelection].SetActive(false);
			foreach (GameObject choice in wheelChoices)
			{
				//The only section underneath the wheel choice itself if the sheen object at this time.
				//However, it should remove all children.  
				List<GameObject> objectsToDestroy = CommonGameObject.findDirectChildren(choice);
				foreach (GameObject objectToCleanse in objectsToDestroy)
				{
					Destroy(objectToCleanse);
				}
			}
			wheelChoices.Remove(clickedSprite);
			
			//Fire off animation sequence of moves
			StartCoroutine(displayResultsOfChoice());
		}
	}
	
	/// Triggered when the user clicks the Spin button
	public void clickSpinButton()
	{
		if (currentState == bonusStates.PICK_MADE_READY_TO_SPIN)
		{
			toggleSpinState();
			disableSwipeToSpin();
			currentState = bonusStates.WHEEL_SPIN;
			//Spin the appropriate wheels.  The latter 3 characters in the mask are blue, the front three are red.
			if (wheelPickMask.Substring(3).Contains('1'))
			{
				spinLeftWheel();
			}
			if (wheelPickMask.Substring(0, 3).Contains('1'))
			{
				StartCoroutine(spinRightWheel());
			}
			
			bannerLabelWrapper.text = Localize.text("good_luck");
			// Play the spin sound? 
		}
	}

	private void highlightRightWinBoxes(){
		for (int k = 0; k < 3; k++)
		{
			if (wheelPickMask[k] == '1')
			{
				if (initPiePieceHighlight())
				{
					_piePieceHeighlightVFX.transform.parent = highlightPos[5-k].transform;
					_piePieceHeighlightVFX.transform.localRotation = Quaternion.identity;
					_piePieceHeighlightVFX.transform.localPosition = Vector3.zero;
					_piePieceHeighlightVFX = null; 
				}
			}
		}
	}

	private void highlightLeftWinBoxes(){
		for (int k = 3; k < wheelPickMask.Length; k++)
		{
			if (wheelPickMask[k] == '1')
			{
				if (initPiePieceHighlight())
				{
					_piePieceHeighlightVFX.transform.parent = highlightPos[5-k].transform;
					_piePieceHeighlightVFX.transform.localRotation = Quaternion.identity;
					_piePieceHeighlightVFX.transform.localPosition = Vector3.zero;
					_piePieceHeighlightVFX = null; 
				}
			}
		}
	}

	private IEnumerator highlightWinBoxes()
	{
		highlightLeftWinBoxes();
		highlightRightWinBoxes();
		yield return new WaitForSeconds(1);
	}
	
	/// Transition from the main bonus game to the next state.
	public IEnumerator endMainBonusGame()
	{		
		if (!string.IsNullOrEmpty(subBonusGameType))
		{
			Audio.play("WoW_reveal_common_bonus");
		}

		yield return StartCoroutine(highlightWinBoxes());
		
		if (BonusGamePresenter.portalPayout > 0)
		{
			yield return StartCoroutine(SlotUtils.rollup(0, BonusGamePresenter.portalPayout, winAmountForPureCreditsWrapper));
		}
		
		yield return new WaitForSeconds(1);
		
		//Determine which type of thing should show up next temporarily.  Either summary, atg, or freespin
		switch (subBonusGameType)
		{
			case "freespin":
				BonusGamePresenter.instance.endBonusGameImmediately();
				BonusGameManager.instance.create(BonusGameType.GIFTING);
				BonusGameManager.instance.show();
				Audio.play("WoW_Freespin_announcer");
				break;
			case "atg":
				BonusGamePresenter.instance.endBonusGameImmediately();
				BonusGameManager.instance.create(BonusGameType.CHALLENGE);
				BonusGameManager.instance.show();
				Audio.play("WoW_ATG_announcer");
				break;
			default:
				SlotBaseGame.instance.gameObject.SetActive(true); //Make the base slot game active again b/c we are not going into any of the bonuses.
				BonusGamePresenter.instance.gameEnded();
				break;
		}
	}

	private void addSparkleTrail(Vector3 start, Vector3 finish, float time)
	{
		if (initSparkleTrailAnimation())
		{
			_sparkleTrailVFX.transform.parent = transform;
			_sparkleTrailVFX.transform.localPosition = start;
			Quaternion targetedTransform = Quaternion.LookRotation(finish - start);
			_sparkleTrailVFX.transform.rotation = targetedTransform;
			_sparkleTrailVFX.transform.localScale = new Vector3(1,1,1);
			ParticleSystem[] particleSystems = _sparkleTrailVFX.GetComponentsInChildren<ParticleSystem>();
			Color psColor = Color.red;
			if (finish.x < 0)
			{
				psColor = Color.blue;
			}
			foreach (ParticleSystem ps in particleSystems)
			{
				//Keeping the SATC particles as they are in case they do things differently, but we want everything 0'd out.
				ps.gameObject.transform.localRotation = Quaternion.identity;
				ParticleSystem.MainModule particleSystemMainModule = ps.main;
				particleSystemMainModule.startColor = psColor;
				//Set the particle system speed based on how far away it is
				//Note to self: A 1 speed will likely accurately hit about 302.586 (113.754) units away, so calculate the difference
				if (ps.name.Contains("Sparkle29_A"))
				{
					int rootMagnitude = CommonMath.fastIntSqrt(
							(
							 (int)_sparkleTrailVFX.transform.localPosition.x -
						     (int) finish.x
							) *
							(
							 (int)_sparkleTrailVFX.transform.localPosition.x -
						     (int) finish.x
							) +
							(
							 (int)_sparkleTrailVFX.transform.localPosition.y -
						     (int)finish.y
							) *
							(
							 (int)_sparkleTrailVFX.transform.localPosition.y -
						     (int)finish.y
							)
						);
					particleSystemMainModule.startSpeed = (float) rootMagnitude / 135.255f;
					//Adjust for aspect ratios lower than 4:3
					if (Camera.main.aspect < (4.0f / 3.0f) && Camera.main.aspect >= 1)
					{
						particleSystemMainModule.startSpeed = ps.main.startSpeedMultiplier / ((4.0f / 3.0f) / Camera.main.aspect);
					}
				}
			}
			_sparkleTrailVFX = null;
		}
	}

	private float playSparkleTrailAnimation()
	{
		float time = 2;
		Vector3 startPosition = pickMeText[playerSelection].transform.localPosition; //The center of the button that they pressed
		Vector3[] finalPositions = new Vector3[CommonText.countNumberOfSpecificCharacter(wheelPickMask, '1')]; //The number of ones in the string
		//Populate the final positions
		int finalPostionsIndex = 0;
		
		for (int k = 0; k < wheelPickMask.Length; k++)
		{
			if (wheelPickMask[k] == '1')
			{
				finalPositions[finalPostionsIndex] = pins[5-k].transform.localPosition;
				finalPostionsIndex++;
			}
		}
		
		foreach (Vector3 vect in finalPositions)
		{
			addSparkleTrail(startPosition, vect, time);
		}
		return .5f;

	}
	
	/// Coroutine that handles animation of a 'choice' that the user makes.
	private IEnumerator displayResultsOfChoice()
	{
		Audio.play("fireworkswhiz01");
		yield return new WaitForSeconds(playSparkleTrailAnimation());
		
		//Show the results of the wheel picks
		for (int i =0; i < wheelPickMask.Length; i++)
        {
            if (wheelPickMask[i] == '1')
            {
				StartCoroutine(explosionEffect(pins[pins.Length-i-1]));
            }
        }
		Audio.play("RomePointerArrives");
		
		//With some visual delay, pull up the choice curtains.
		for (int i = 0; i < wheelChoices.Count;i++)
		{
			yield return StartCoroutine(revealWait.wait(delayBetweenAnimations));
			if(!revealWait.isSkipping)
			{
				Audio.play("RomeRevealOtherPicks");
			}
			wheelChoices[i].SetActive(false);
			if (i >= playerSelection)
			{
				pickMeText[i+1].SetActive(false);
			}
			else
			{
				pickMeText[i].SetActive(false);
			}
		}
		wheelChoices.Clear();
		
		//Highlight the spin button
		toggleSpinState();
		enableSwipeToSpin();
		bannerLabelWrapper.text = Localize.text("spin_the_wheel");
		currentState = bonusStates.PICK_MADE_READY_TO_SPIN;
		
		Audio.play(Audio.soundMap("wheel_click_to_spin"));
		yield return null;
	}
	
	/// Coroutine invoked to trigger an explosion on a targetted wheel pin
	private IEnumerator explosionEffect(GameObject pinToTarget)
	{
		GameObject explosion = CommonGameObject.instantiate(explosionEffectPrefab, pinToTarget.transform.position, pinToTarget.transform.rotation) as GameObject;
		explosion.transform.parent = pinToTarget.transform;
		explosion.layer = pinToTarget.layer;
		CommonGameObject.setLayerRecursively(explosion, pinToTarget.layer);
		explosion.transform.localPosition = new Vector3(0, 0, -10f);
		yield return new WaitForSeconds(0.4f);
		pinToTarget.GetComponent<UISprite>().color = Color.white;
		yield return new WaitForSeconds(2.5f);
		Destroy(explosion);
	}
	
	/// Coroutine responsible for spinning the left wheel and arriving at the correct outcome.
	private void spinLeftWheel()
	{
		playLeftWheelExplosion();
		//If we have a left wheel, it always starts first.
		leftWheelSpinningObject = new WheelSpinner
		(
			leftBonusWheel, 
			leftWheelPointerRestingAngle, 
			delegate() 
			{
			//Left wheel only ends if there is no right wheel spins.
				if (!wheelPickMask.Substring(0, 3).Contains('1'))
				{
					currentState = bonusStates.WHEEL_FINISHED_SPINNING;
					StartCoroutine(endMainBonusGame());
				}
			}
		);
	}
	
	/// Coroutine responsible for spinning the right wheel and arriving at the correct outcome.
	private IEnumerator spinRightWheel()
	{
		playRightWheelExplosion();
		float constantVelocity = 0.5f;
		if (wheelPickMask.Substring(3).Contains('1'))
		{
			yield return new WaitForSeconds(0.1f);
			constantVelocity = 4.0f;
		}
		//If we have a both a left wheel and a right wheel, the right wheel spins first.
		//At the end of a right wheel result, we always shift to the next mode
		rightWheelSpinningObject = new WheelSpinner
			(
				rightBonusWheel, 
				rightWheelPointerRestingAngle, 
				delegate() 
				{
					currentState = bonusStates.WHEEL_FINISHED_SPINNING;
					StartCoroutine(endMainBonusGame());
				}
			);
		rightWheelSpinningObject.constantVelocitySeconds = constantVelocity;
		yield return null;
	}
	
	///Have a mechanism for choices to add themselves to this manager.
	public void registerPick(GameObject visualObject)
	{
		wheelChoices.Add(visualObject);
	}
	
	//Message to toggle whether or not the spin button is active and ready to detect a spin
	public void toggleSpinState()
	{
		if (!spinButtonEnabled.activeSelf)
		{
			spinButtonEnabled.SetActive(true);
			spinTextWrapper.gameObject.SetActive(true);
		}
		else
		{
			disableSpinButton();
		}
	}

	private void disableSpinButton()
	{
		spinButtonEnabled.SetActive(false);
		spinButtonDisabled.SetActive(true);
		spinTextWrapper.gameObject.SetActive(false);
	}
}


