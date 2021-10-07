using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Base class for Hi03 - Mega Jackpot
 * Needs to handle sparkle mutations and re-evaluations(for the top reel jackpot)
 * Authors: Nick Reynolds & Alfred Anguiano
 */
public class Hi03 : DeprecatedMultiSlotBaseGame
{
	public GameObject dollarSpinPrefab = null;                          // The prefab to make the dollar signs with
	public GameObject[] topAnticipations = new GameObject[3];           // Reel anticipation effects for the top reels
	public GameObject scatterBurst;                                     // Reference to the scatter animation to play over
	public GameObject[] wildBanners;                                    // Static banners that are supposed to toggle on the top columns
	public GameObject[] inactiveDollars;                                // grayed out dollar bills in top 
	public GameObject[] activeDollars;                                  // dollar bills that become activated after getting SCW symbol
	public UILabel jackpotLabel2;       // To be removed when prefabs are updated.
	public UILabel jackpotAmountTxt2;   // To be removed when prefabs are updated.
	public LabelWrapperComponent jackpotLabelWrapperComponent;
	public LabelWrapperComponent jackpotAmountTxtWrapperComponent;
	public GameObject paylinePrefab;
	public GameObject meterCelebration;

	public bool playAlternateJackpotSymbol3;
	public bool doBurstAtStart = true;
	public Animator meterAnimator;
	public string jackPotOutroAnimation;
	public string jackPotIntroAnimation;
	public string jackPotDeactivateAnimation;
	public bool playMeterCelebrationAfterJackpotAnimation = false;
	public float OUTRO_ANIMATION_DELAY = 3.5f;
	public float OUTRO_ANIMATION_DURATION = 2.5f;
	public float JACKPOT_SEQEUNCE_DURATION = 7.5f;
	public float JACKPOT_AUDIO_DELAY = 0.0f;

	private GameObject instancedPayline;
	private GameObject[] dollarSigns = new GameObject[3];       // The dollar signs for the game, only can ever have 3
	private GameObject[] blueBurst = new GameObject[3];         // The blue bursts that accompy the dollar signs
	protected bool[] wildDollarsTriggered = new bool[3];            // Tracks what reel has had a wil dollar triggered on it for the top set
	private bool[] isDoingDollarSignAnimOnReel = new bool[5];   // Need to force the reel stopped callback from continuing until these anims finish so they don't play over slam stopped spins

	private TopSlotPaylineScript payline;
	private int numBottomPayouts;               // if true only the top reels have a winning payline
	public Color topLineColor = Color.blue;

	public float symbolHeightOverride;


	public Vector2 topReelPayboxSize;

	protected long JACKPOT_AMOUNT = 0L;
	public TICoroutine rollupRoutine;

	private PlayingAudio backgroundHum = null;          // Store the looped humming noise played while the reels spin
	private PlayingAudio anticipationSound = null;      // Store the anticipation sound so it can be canceled when the reels stop

	private const float DOLLAR_SIGN_FLY_UP_TIME = 1.0f;                                 // Time it takes for the dollar sign to fly up

	public string SPIN_START_SOUND = "mechreel03startCommon";                   // Mechanical starting noise of reels spinning
	public string SPINNING_SOUND = "mechreel03loopCommon";                      // Looped wurring noise of reels spinning
	public string JACKPOT_ANIM_SOUND = "JackpotAnimationMJ";                        // Sound for the jackpot presenation
	public string BONUS_SYMBOL_FANFARE_SOUND_PREFIX = "bonus_symbol_fanfare";   // Fanfares for the dollars on the bottom reels triggering
	public string JACKPOT_SYMBOL_LAND_SOUND_PREFIX = "tw_effect_land";      // Part of the name for the sound mapped landing sound of the dollar sign symbol
	public string ANTICIPATION_FANFARE_SOUND = "AnticipationRiseMJ";                // Fanfare sound for anticipation showing

	public LabelWrapper jackpotLabelWrapper
	{
		get
		{
			if (_jackpotLabelWrapper == null)
			{
				if (jackpotLabelWrapperComponent != null)
				{
					_jackpotLabelWrapper = jackpotLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotLabelWrapper = new LabelWrapper(jackpotLabel2);
				}
			}
			return _jackpotLabelWrapper;
		}
	}
	private LabelWrapper _jackpotLabelWrapper = null;

	public LabelWrapper jackpotAmountTxtWrapper
	{
		get
		{
			if (_jackpotAmountTxtWrapper == null)
			{
				if (jackpotAmountTxtWrapperComponent != null)
				{
					_jackpotAmountTxtWrapper = jackpotAmountTxtWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotAmountTxtWrapper = new LabelWrapper(jackpotAmountTxt2);
				}
			}
			return _jackpotAmountTxtWrapper;
		}
	}
	private LabelWrapper _jackpotAmountTxtWrapper = null;
	
	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][]
		{
			new StopInfo[] {new StopInfo(0, 0, 0)},
			new StopInfo[] {new StopInfo(1, 0, 0)},
			new StopInfo[] {new StopInfo(2, 0, 0)},
			new StopInfo[] {new StopInfo(3, 0, 0)},
			new StopInfo[] {new StopInfo(4, 0, 0)},
			new StopInfo[] {new StopInfo(0, 0, 1)},
			new StopInfo[] {new StopInfo(1, 0, 1)},
			new StopInfo[] {new StopInfo(2, 0, 1)},
		};
	}
	
	protected override void Awake()
	{
		base.Awake();

		// Create the dollar signs and hide them
		for (int i = 0; i < dollarSigns.Length; i++)
		{
			dollarSigns[i] = CommonGameObject.instantiate(dollarSpinPrefab) as GameObject;
			dollarSigns[i].SetActive(false);

			blueBurst[i] = CommonGameObject.instantiate(scatterBurst) as GameObject;
			blueBurst[i].SetActive(false);
		}

		for (int i = 0; i < isDoingDollarSignAnimOnReel.Length; ++i)
		{
			isDoingDollarSignAnimOnReel[i] = false;
		}

	}

	// override this so we can set the jackpot amount after setting the reelset
	protected override void handleSetReelSet(string reelSetKey)
	{
		base.handleSetReelSet(reelSetKey);
		PayTable payTable = PayTable.find(foregroundReelPaytableName);
		foreach (PayTable.LineWin lineWin in payTable.lineWins.Values)
		{
			if (lineWin.symbol == "SCW" && lineWin.symbolMatchCount == 3)
			{
				JACKPOT_AMOUNT = lineWin.credits;
			}
		}
		resetSlotMessage();
	}

	// setup "lines" boxes on sides
	protected override void setSpinPanelWaysToWin(string reelSetName)
	{
		initialWaysLinesNumber = slotGameData.getWinLines(reelSetName);
		SpinPanel.instance.setDualSideInfo(1, initialWaysLinesNumber, "line", "lines", showSideInfo);
	}

	private bool isWaitingForDollarAnimations()
	{
		for (int i = 0; i < isDoingDollarSignAnimOnReel.Length; ++i)
		{
			if (isDoingDollarSignAnimOnReel[i] == true)
			{
				return true;
			}
		}

		return false;
	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected override void reelsStoppedCallback()
	{
		StartCoroutine(reelStoppedCallbackCoroutine());
	}

	/// Need a coroutine here so we can delay until the dollar sign animations finish so they don't keep animating between spins
	private IEnumerator reelStoppedCallbackCoroutine()
	{
		// make sure all animations have ended so we don't end in a bad state
		while (isWaitingForDollarAnimations())
		{
			yield return null;
		}

		// turn off the hum noise
		if (backgroundHum != null)
		{
			backgroundHum.stop(0);
		}

		// turn off the anticipation sound if it was playing
		if (anticipationSound != null)
		{
			anticipationSound.stop(0);
			anticipationSound = null;
		}

		// make sure all the anticipation VFX are hidden
		hideAllAnticipationVFX();

		StartCoroutine(checkForTopReelsWin());
	}

	private IEnumerator waitForTopReelsRollup()
	{
		yield return rollupRoutine;

		if (!playMeterCelebrationAfterJackpotAnimation)
		{
			meterCelebration.SetActive(false);
		}
	}

	protected override IEnumerator prespin()
	{
		//  SCAT only allows one symbol height value for the entire game and the top reels in some games (gen27,oz07,hi03)
		// have much larger symbols causing top reels spin faster than the bottom reels which can cause rotoscoping  issues
		//  this allows us to set a different symbol height for the top symbols so they spin smooth as silk	withoug slowing down the
		// bottom reels	
		if (symbolHeightOverride > 0)
		{
			for (int i = 0; i < 3; i++)
			{
				GameObject go = getReelRootAtLayer(i, 1);
				if (go != null)
				{
					SwipeableReel swipeReel = go.GetComponent<SwipeableReel>();
					if (swipeReel != null)
					{
						swipeReel.myReel.symbolHeight = symbolHeightOverride;
					}
				}
			}

			symbolHeightOverride = 0;  // only need to do this once
		}

		if (playMeterCelebrationAfterJackpotAnimation)
		{
			meterCelebration.SetActive(false);
		}

		yield return StartCoroutine(base.prespin());

		Audio.play(SPIN_START_SOUND);
		backgroundHum = Audio.play(SPINNING_SOUND, 1, 0, 0, float.PositiveInfinity);

		// re-show any hidden symbols on the top reels
		for (int i = 0; i < 3; ++i)
		{
			engine.getSlotReelAt(i, -1, 1).visibleSymbols[0].gameObject.SetActive(true);
		}

		// hide the banners
		foreach (GameObject wildBanner in wildBanners)
		{
			wildBanner.SetActive(false);
		}

		resetDollarLightIndicators();

		hideAllBlueBursts();

		if (instancedPayline != null)
		{
			payline = null;
			Destroy(instancedPayline);
		}

		for (int i = 0; i < wildDollarsTriggered.Length; ++i)
		{
			wildDollarsTriggered[i] = false;
		}
	}

	private IEnumerator playJackpotIntroAnimation()
	{
		if (JACKPOT_AUDIO_DELAY > 0)
		{
			yield return new TIWaitForSeconds(JACKPOT_AUDIO_DELAY);
		}

		Audio.play(JACKPOT_ANIM_SOUND);

		if (!playMeterCelebrationAfterJackpotAnimation)
		{
			meterCelebration.SetActive(true);
		}

		if (meterAnimator != null)
		{
			if (engine.isSlamStopPressed)
			{
				yield return new TIWaitForSeconds(0.8f);  // give the dollar signs a chance to light up
			}
			meterAnimator.Play(jackPotIntroAnimation);
		}
		yield return null;
	}

	private IEnumerator playOutroAnimation()
	{
		if (meterAnimator != null)
		{
			yield return new TIWaitForSeconds(OUTRO_ANIMATION_DELAY);
			meterAnimator.Play(jackPotOutroAnimation);
			yield return new TIWaitForSeconds(OUTRO_ANIMATION_DURATION);
		}

		if (playMeterCelebrationAfterJackpotAnimation)
		{
			meterCelebration.SetActive(true);
		}

		yield return null;
	}

	// this is neeeded because the standard pay line display code will not show a top line win with the deprecated outcome
	// or animate its symbols
	private IEnumerator cycleTopLineWin()
	{
		yield return new TIWaitForSeconds(0.75f);   // ensure initial payline gets a little show time.
		if (payline != null)
		{
			StartCoroutine(payline.hideLineOnly());     // hides the line that is shown only once at beginning of win cycle
		}

		while (payline != null)
		{

			// animate the symbols
			for (int i = 0; i < 3; ++i)
			{
				SlotSymbol symbol = engine.getSlotReelAt(i, -1, 1).visibleSymbols[0];
				if (symbol != null)
				{
					symbol.animateOutcome();
					if (wildBanners[i].activeSelf)
					{
						wildBanners[i].SetActive(false);
						wildBanners[i].SetActive(true);
					}
				}
			}

			if (payline != null)
			{
				yield return StartCoroutine(payline.show(2.0f));        // show the custom payline
			}

			float waitTime = Mathf.Max(2.0f, numBottomPayouts * 2.0f);
			yield return new TIWaitForSeconds(waitTime);
		}
	}

	private IEnumerator checkForTopReelsWin()
	{
		JSON[] reevalInfo = outcome.getArrayReevaluations();
		// games like hi03 use reevaluations for the outcome info for "foreground" reels
		if (reevalInfo.Length > 0)
		{
			JSON[] outcomes = reevalInfo[0].getJsonArray("outcomes.1");
			//reevalInfo[0].get

			if (outcomes != null && outcomes.Length > 0)
			{
				long winCredits = 0;

				if (outcomes[0].hasKey("credits"))
				{
					winCredits = outcomes[0].getLong("credits", 0L);
				}
				else
				{
					// doesn't contain a credits entry, so look up the line win
					int winId = outcomes[0].getInt("win_id", 0);

					PayTable payTable = PayTable.find(reevalInfo[0].getString("pay_table.1", ""));
					Debug.LogWarning("WIN PAYTABLE: " + reevalInfo[0].getString("pay_table.1", ""));
					if (payTable.lineWins.ContainsKey(winId))
					{
						PayTable.LineWin lineWin = payTable.lineWins[winId];
						winCredits = lineWin.credits;
					}
				}

				bool hasJackpot = winCredits == JACKPOT_AMOUNT;

				if (hasJackpot)
				{
					yield return StartCoroutine(playJackpotIntroAnimation());
				}

				float rollupTime = 2.0f;

				// need to double check if we should play sound for this rollup, only do it if there weren't any payouts from the other reels
				numBottomPayouts = outcome.getSubOutcomesReadOnly().Count;

				if (numBottomPayouts == 0)
				{
					// need to run a rollup for this since it isn't going to get added to the bottom rollup
					long total = winCredits * multiplier * GameState.baseWagerMultiplier;
					addCreditsToSlotsPlayer(winCredits * multiplier * GameState.baseWagerMultiplier, "hi03 top payout", shouldPlayCreditsRollupSound: false);

					if (willPayoutTriggerBigWin(total) && PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BIG_WINS_KEY))
					{
						total *= 2;
					}

					rollupRoutine = StartCoroutine(SlotUtils.rollup(0, total, onPayoutRollup, true, rollupTime));
				}

				// create the top payline since we don't have top pay line support with the deprecated outcome in use
				instancedPayline = CommonGameObject.instantiate(paylinePrefab) as GameObject;
				payline = instancedPayline.GetComponent<TopSlotPaylineScript>();
				Dictionary<int, int[]> symbols = new Dictionary<int, int[]>();
				symbols.Add(0, new int[] { 1 });
				symbols.Add(1, new int[] { 1 });
				symbols.Add(2, new int[] { 1 });

				// we use a payline index of 666 so the payline  caching code does not cache this payline with a layer 0 pay line 
				// causing it to display as one pay line when the bottom reel pay line draws
				payline.init(symbols, topLineColor, this, 666, topReelPayboxSize, 1); // this makes a line show up on the top reels

				StartCoroutine(cycleTopLineWin());


				if (hasJackpot)
				{
					StartCoroutine(playOutroAnimation());
				}

				if (numBottomPayouts == 0)
				{
					// no bottom reel pay out so do own rollup
					yield return StartCoroutine(waitForTopReelsRollup());
				}
				else
				{
					// Top and bottom are going to combine payouts

					// Start a rollup to the win meter
					long jackpotAmount = winCredits * multiplier * GameState.baseWagerMultiplier;
					rollupTime = Mathf.Ceil((float)((double)jackpotAmount / SlotBaseGame.instance.betAmount)) * Glb.ROLLUP_MULTIPLIER;

					if (willPayoutTriggerBigWin(jackpotAmount) && PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BIG_WINS_KEY))
					{
						jackpotAmount *= 2;
					}

					rollupRoutine = StartCoroutine(SlotUtils.rollup(0, jackpotAmount, onPayoutRollup, true, rollupTime));

					if (meterCelebration.activeSelf || (playMeterCelebrationAfterJackpotAnimation && winCredits == JACKPOT_AMOUNT))
					{
						yield return new TIWaitForSeconds(JACKPOT_SEQEUNCE_DURATION);

						// wait for rollup to finish
						yield return rollupRoutine;

						// trigger the end rollup to move the winnings into the runningPayoutRollupValue
						yield return StartCoroutine(onEndRollup(isAllowingContinueWhenReady: false));

						if (payline != null)
						{
							StartCoroutine(payline.hide());
						}
						if (!playMeterCelebrationAfterJackpotAnimation)
						{
							meterCelebration.SetActive(false);
						}
					}
					else
					{
						// still wait on the rollup to avoid a desync
						// wait for rollup to finish
						yield return rollupRoutine;

						// trigger the end rollup to move the winnings into the runningPayoutRollupValue
						yield return StartCoroutine(onEndRollup(isAllowingContinueWhenReady: false));
					}
				}
			}
		}

		base.reelsStoppedCallback();
	}


	public override void setOutcome(SlotOutcome outcome)
	{
		// In case a banner is still active from a previous spin, for whatever the reason, let's MAKE SURE ITS INACTIVE.
		foreach (GameObject wildBanner in wildBanners)
		{
			wildBanner.SetActive(false);
		}

		base.setOutcome(outcome);
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject(), true);
	}

	/// Hide all of the anticipation effects
	protected virtual void hideAllAnticipationVFX()
	{
		for (int i = 0; i < topAnticipations.Length; ++i)
		{
			topAnticipations[i].SetActive(false);
		}
	}

	/// Tells if an anticipation effect is showing
	protected bool isAnticipationVFXShowing()
	{
		for (int i = 0; i < topAnticipations.Length; ++i)
		{
			if (topAnticipations[i].activeSelf)
			{
				return true;
			}
		}

		return false;
	}

	/// returns number of active dollar signs
	protected int getNumTriggeredDollarSigns()
	{
		int numTriggered = 0;

		for (int i = 0; i < wildDollarsTriggered.Length; ++i)
		{
			if (wildDollarsTriggered[i])
			{
				numTriggered++;
			}
		}

		return numTriggered;
	}

	/// Tells if an anticipation should trigger
	private bool shouldAnticipationTrigger()
	{
		int numWildDollarsShowing = 0;

		for (int i = 0; i < wildDollarsTriggered.Length; ++i)
		{
			if (wildDollarsTriggered[i])
			{
				numWildDollarsShowing++;
			}
		}

		return !isAnticipationVFXShowing() && numWildDollarsShowing == 2 && !engine.isSlamStopPressed;
	}

	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		if (stoppedReel.layer == 1)
		{
			if (!wildDollarsTriggered[stoppedReel.reelID - 1])
			{
				// If an SCW lands on the top reel, immediately turn the banner on.
				foreach (SlotSymbol symbol in stoppedReel.visibleSymbols)
				{
					if (symbol.name == "SCW")
					{
						wildBanners[stoppedReel.reelID - 1].SetActive(true);

						wildDollarsTriggered[stoppedReel.reelID - 1] = true;

						// turn on the indicator light
						StartCoroutine(lightDollarSignIndicator(stoppedReel.reelID - 1));

						if (playAlternateJackpotSymbol3)
						{
							// Play jackpot result sound
							if (stoppedReel.reelID < 3)
							{
								Audio.play(Audio.soundMap(JACKPOT_SYMBOL_LAND_SOUND_PREFIX + stoppedReel.reelID));
							}
							else
							{
								Audio.play(Audio.soundMap("JackpotSymbolLands3BB"));
							}
						}

						if (shouldAnticipationTrigger())
						{
							// start the anticipation animation on the top reel that hasn't landed yet
							for (int k = 0; k < wildDollarsTriggered.Length; ++k)
							{
								if (!wildDollarsTriggered[k])
								{
									topAnticipations[k].SetActive(true);
									// Play anticipation sound
									anticipationSound = Audio.play(ANTICIPATION_FANFARE_SOUND);
								}
							}
						}
					}
				}
			}

			// hide the symbol behind the banner
			if (wildDollarsTriggered[stoppedReel.reelID - 1])
			{
				engine.getSlotReelAt(stoppedReel.reelID - 1, -1, 1).visibleSymbols[0].gameObject.SetActive(false);
			}
		}

		if (stoppedReel.layer == 1 && isAnticipationVFXShowing())
		{
			// make sure the top anticipation is off
			hideAllAnticipationVFX();
		}

		if (mutationManager.mutations.Count != 0)
		{
			// Trying to delay new spins until after the animation has finished on a reel.
			// Also updated this to be a for loop instead of a foreach to not modify a list mid-search.

			// save off the previous value so we can restore it, it used to be set to false, which can cause crashes and dsyncs when slam stopping on slow devices
			for (int x = 0; x < mutationManager.mutations.Count; x++)
			{
				StandardMutation mutation = mutationManager.mutations[x] as StandardMutation;
				if (mutation.fromMutations != null && mutation.toMutations != null)
				{
					for (int i = 0; i < mutation.fromMutations.Count; i++)
					{
						if ((mutation.fromMutations[i].reel + 1) == stoppedReel.reelID && stoppedReel.layer == 0)
						{
							int topObjectIndex = 0;
							switch (stoppedReel.reelID)
							{
								case 1:
									topObjectIndex = 0;
									break;
								case 3:
									topObjectIndex = 1;
									break;
								case 5:
									topObjectIndex = 2;
									break;
							}

							// mark that this reel is animating and should block proceeding in the reelStopCallback
							isDoingDollarSignAnimOnReel[stoppedReel.reelID - 1] = true;

							// Play jackpot init sound
							Audio.play(Audio.soundMap(BONUS_SYMBOL_FANFARE_SOUND_PREFIX + (topObjectIndex + 1)));

							GameObject burstEffect = blueBurst[topObjectIndex];

							if (doBurstAtStart)
							{
								SlotSymbol symbol = engine.getVisibleSymbolsBottomUpAt(mutation.fromMutations[i].reel)[mutation.fromMutations[i].position];
								burstEffect.transform.position = symbol.gameObject.transform.position;
								burstEffect.SetActive(true);
							}

							Transform reelRoot = getReelRootsAt(mutation.fromMutations[i].reel).transform;

							// Move the dollar sign to the top
							GameObject dollarSign = dollarSigns[topObjectIndex];

							dollarSign.transform.parent = reelRoot;
							dollarSign.transform.localPosition = new Vector3(0, (mutation.fromMutations[i].position) * getSymbolVerticalSpacingAt(i, stoppedReel.layer), 0);
							dollarSign.SetActive(true);

							GameObject targetReelRoot = getReelGameObject(mutation.toMutations[i].reel, -1, 1);
							Vector3 targetPos = new Vector3(targetReelRoot.transform.position.x, targetReelRoot.transform.position.y, 0);

							// Movements aren't perfect yet, need to bugfix.
							yield return new TITweenYieldInstruction(iTween.MoveTo(dollarSign, iTween.Hash("position", targetPos, "isLocal", false, "time", DOLLAR_SIGN_FLY_UP_TIME, "easetype", iTween.EaseType.easeInOutBack)));

							// Play jackpot result sound
							if (topObjectIndex < 3)
							{
								Audio.play(Audio.soundMap(JACKPOT_SYMBOL_LAND_SOUND_PREFIX + (topObjectIndex + 1)));
							}

							if (!doBurstAtStart)
							{
								burstEffect.transform.position = targetPos;
								burstEffect.SetActive(true);
								yield return new TIWaitForSeconds(0.6f);   // give the burst a little time to get started before showing the wild banners
							}

							dollarSign.SetActive(false);

							// because of the way the reels stop, the far right top reel will stop before the animations here finish
							// so we'll hide this one here.  Also, if slam stop then hide the symbol
							if (topObjectIndex == 2 || engine.isSlamStopPressed)
							{
								engine.getSlotReelAt(topObjectIndex, -1, 1).visibleSymbols[0].gameObject.SetActive(false);
							}
							else
							{
								// cancel the spin on this reel since it doesn't matter anymore and we want to hide the symbol behind the banner
								// actual hiding will happen when the top reel actually stops
								engine.getSlotReelAt(topObjectIndex, -1, 1).stopReelSpin(-1);
							}

							wildBanners[mutation.toMutations[i].reel].SetActive(true);


							wildDollarsTriggered[topObjectIndex] = true;

							// turn on the indicator light
							StartCoroutine(lightDollarSignIndicator(topObjectIndex));

							if (shouldAnticipationTrigger())
							{
								// start the anticipation animation on the top reel that hasn't landed yet
								for (int k = 0; k < wildDollarsTriggered.Length; ++k)
								{
									if (!wildDollarsTriggered[k])
									{
										// delay slightly so we don't play over the bonus hit sound
										yield return new TIWaitForSeconds(0.75f);

										topAnticipations[k].SetActive(true);
										// Play anticipation sound
										anticipationSound = Audio.play(ANTICIPATION_FANFARE_SOUND);
									}
								}
							}

							yield return new TIWaitForSeconds(1.0f);

							burstEffect.SetActive(false);

							// mark that this animation is done
							isDoingDollarSignAnimOnReel[stoppedReel.reelID - 1] = false;
						}
					}
				}
			}
		}
	}

	/// Ensure all blue bursts are hidden when the prespin happens
	private void hideAllBlueBursts()
	{
		for (int i = 0; i < blueBurst.Length; i++)
		{
			blueBurst[i].SetActive(false);
		}
	}

	/// Reset the dollar sign indicator lights
	protected virtual void resetDollarLightIndicators()
	{
		foreach (GameObject activeDollar in activeDollars)
		{
			activeDollar.SetActive(false);
		}
		foreach (GameObject inactiveDollar in inactiveDollars)
		{
			inactiveDollar.SetActive(true);
		}
	}

	/// Handle lighting dollar sign lights
	protected virtual IEnumerator lightDollarSignIndicator(int dollarLightIndex)
	{
		//yield return new TIWaitForSeconds(0.5f);
		if (dollarLightIndex >= 0 && dollarLightIndex < activeDollars.Length)
		{
			activeDollars[dollarLightIndex].SetActive(true);
			inactiveDollars[dollarLightIndex].SetActive(false);
		}

		yield break;
	}

	protected override void resetSlotMessage()
	{
		if (jackpotLabelWrapper != null)
		{
			jackpotLabelWrapper.text = CreditsEconomy.convertCredits(multiplier * JACKPOT_AMOUNT * GameState.baseWagerMultiplier);
		}
		if (jackpotAmountTxtWrapper != null)
		{
			jackpotAmountTxtWrapper.text = CreditsEconomy.convertCredits(multiplier * JACKPOT_AMOUNT * GameState.baseWagerMultiplier);
		}

		base.resetSlotMessage();
	}
}
