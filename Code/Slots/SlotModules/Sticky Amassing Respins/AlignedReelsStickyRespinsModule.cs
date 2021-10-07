using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMProExtensions;

/// <summary>
/// SC, Aligned reels, sticky respins module.
/// </summary>
public class AlignedReelsStickyRespinsModule : StickyAmassingRespinsModule
{
	[Header("Stage")]
	[SerializeField] protected Animator featurePaytable = null;
	[SerializeField] protected string PAYTABLE_INTRO_ANIMATION = "intro";
	[SerializeField] protected string PAYTABLE_EXIT_ANIMATION = "outro";
	[SerializeField] protected UILabel counter = null;
	[SerializeField] protected UILabel amount = null;	
	[SerializeField] protected TextMeshPro counter_TMPro = null;
	[SerializeField] protected TextMeshPro amount_TMPro = null;

	[Header("Aligned Reels")]
	[SerializeField] protected GameObject[] reelToSlide = null;
	[SerializeField] protected float TIME_SLIDE_REELS = 1.0f;
	[SerializeField] protected float[] BOTTOM_REEL_POSITION =  { -1.805f, -1.81f, -1.8f };

	[Header("SC Landing")]
	[SerializeField] protected GameObject scRevealPrefab;
	[SerializeField] protected GameObject[] majorFaceArray;
	[SerializeField] protected Vector3 FIRST_REVEAL_POSITION_DELTA;
	[Tooltip("The times that the animation switches between major symbols for the SC feature. Use FaceIndex: Row 0 is M1, Row 1 is M2 etc.")]
	[SerializeField] protected Inspector2DFloatArray SC_CYCLE_TIMES;
	[SerializeField] protected float AFTER_SYMBOL_CHANGE_WAIT_TIME = 0.0f;
	[SerializeField] protected float AFTER_REELS_STOP_WAIT_TIME = 0.0f;
	[SerializeField] protected string[] MAJOR_SC_REVEAL_NAMES = {"sc_landing M1", "sc_landing M2", "sc_landing M3", "sc_landing M4"};
	[SerializeField] protected float TIME_BETWEEN_COUNT = 0.4f;	
	[SerializeField] protected float TIME_SC_CYCLE_OVER = 0.0f;
	[SerializeField] protected float FIRST_CHANGE_SYMBOL_TO_WAIT = 0.25f;

	[Header("Rollup Animation")]
	[SerializeField] protected GameObject rollupAnimation;
	[SerializeField] protected GameObject rollUpParticle;
	[SerializeField] protected string ROLLUP_PARTICLE_STATE_NAME;
	[SerializeField] protected GameObject sparkleParticle;
	[SerializeField] protected string SPARKLE_PARTICLE_STATE_NAME;
	[SerializeField] protected float GAME_END_WAIT;
	[SerializeField] protected float TIME_MOVE_SPARKLE = 1.0f;
	[SerializeField] protected float TIME_ROLL_UP_FEATURE = 0.5f;

	[SerializeField] protected  GameObject[] objectsToFade;			// object is list will fade out at start of feature and fade back in at end
	[SerializeField] protected float TIME_FADE;						// how long the fade will take.
	[SerializeField] protected bool ANIMATE_SYMBOLS_ON_LOCK = false;


	protected bool needsToCleanUp = false;
	protected List<GameObject> scRevealsToClean;
	protected float[] reelToSlideStartingPoistion = null;
	
	protected long lastWin = 0;
	protected long lastCount = 0;
	protected bool firstSCReveal = false;
	protected bool firstHammer = true;

	private long lastCounterRollup = 0;

	private bool isInCleanUp;
	
	protected Dictionary<string, int> symbolNameToFaceIndex = new Dictionary<string, int>()
	{ 
		{ "M1", 0}, 
		{ "M2", 1}, 
		{ "M3", 2}, 
		{ "M4", 3} 
	};

	// Sound Names
	protected string RESPIN_MUSIC = "";
	protected string PAYTABLE_SLIDE_SOUND = "";
	protected string PAYTABLE_EXIT_SOUND = "";
	protected string ADVANCE_COUNTER_SOUND = "";
	protected string MAJOR_SOUND_KEY_PREFIX = "scatter_pay_table_M";	
	protected string SC_SYMBOLS_LAND = "";
	protected string GAME_END_VO_SOUND = "";
	protected string GAME_END_SOUND = "ScatterWildPayTableFinalWinSparklyFlourish";
	protected string SPARKLE_TRAVEL_SOUND = "";
	protected string SPARKLE_LAND_SOUND = "";
	protected string SYMBOL_LANDED_SOUND = "";
	protected string MATCHED_SYMBOL_LOCKS_SOUND = "";
	protected float MATCHED_SYMBOL_LOCKS_SOUND_DELAY = 0.0f;	
	protected string M1_SOUND = "";
	protected string M2_SOUND = "";
	protected string M3_SOUND = "";
	protected const float STICKY_SYMBOL_Z_POS = -2.0f;
	protected const float SC_REVEAL_Z_POS = -3.0f;


	private Dictionary<GameObject, AlphaRestoreData> restoreAlphaMaps;

	public override void Awake()
	{
		base.Awake();
		scRevealsToClean = new List<GameObject>();
		reelToSlideStartingPoistion = new float[reelToSlide.Length];

	}

	protected void saveSlideReelPositions()
	{
		for (int i = 0; i < reelToSlideStartingPoistion.Length; i++)
		{
			reelToSlideStartingPoistion[i] = reelToSlide[i].transform.position.y;
		}		
	}

	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override IEnumerator executeOnPreSpin()
	{
		// if cleanup is not finished from previous spin, continue to wait before starting the next auto spin until we are done
		if (isInCleanUp)
		{
			while (isInCleanUp)
			{
				yield return null;
			}
		}
	
		yield return StartCoroutine(base.executeOnPreSpin());
	}

	public override bool needsToExecuteAfterPaylines()
	{
		bool shouldExecute = base.needsToExecuteAfterPaylines();
		return shouldExecute || needsToCleanUp || isInCleanUp;
	}

	public override IEnumerator executeAfterPaylinesCallback(bool winsShown)
	{
		if (isInCleanUp)
		{
			// since cleanup might call justPlayRollup which sets the isGameBusy game to false too early we must make sure cleanup is done,
			// before returning
			while (isInCleanUp)
			{
				yield return null;
			}
			yield break;
		}		
		else if (needsToCleanUp)
		{
			yield return StartCoroutine(cleanUp());
		}
		else
		{
			// fade out any objects that shouldn't show during the feature
			// for peewee01 this is the ambient characters
			restoreAlphaMaps = CommonGameObject.getAlphaRestoreDataMapsForGameObjects(objectsToFade);  // save off all the alpha data for this array of game objects, includes materials and tmpro labels
			StartCoroutine(CommonGameObject.fadeGameObjectsTo(objectsToFade, 1.0f, 0, TIME_FADE, false));  // fades out materials and tmpro labels

			Audio.switchMusicKeyImmediate(RESPIN_MUSIC);
			yield return StartCoroutine(base.executeAfterPaylinesCallback(winsShown));
			yield return new TIWaitForSeconds(AFTER_SYMBOL_CHANGE_WAIT_TIME);

			// Set the right face to be on the paytable
			foreach (GameObject face in majorFaceArray)
			{
				face.SetActive(false);
			}
			
			if (symbolNameToFaceIndex.ContainsKey(transformSymbol))
			{
				int faceIndex = symbolNameToFaceIndex[transformSymbol];
				
				if(Audio.canSoundBeMapped(MAJOR_SOUND_KEY_PREFIX + (faceIndex + 1)))
				{
					Audio.play(Audio.soundMap(MAJOR_SOUND_KEY_PREFIX + (faceIndex + 1)));
				}
				else
				{
					if (transformSymbol == "M1")
					{
						Audio.play(M1_SOUND);
					}
					else if (transformSymbol == "M2")
					{
						Audio.play(M2_SOUND);
					}
					else if (transformSymbol == "M3")
					{
						Audio.play(M3_SOUND);
					}
				}
				
				if (faceIndex >= 0 && faceIndex < majorFaceArray.Length)
				{
					majorFaceArray[faceIndex].SetActive(true);
				}
				else
				{
					Debug.LogError("majorFaceArray doesn't contain an entry for faceIndex = " + faceIndex);
				}
			}
			else
			{
				Debug.LogWarning("Not sure which face should be shown for " + transformSymbol);
			}
			
			// Slide down the reels.
			saveSlideReelPositions();   // this used to be done once in awake but wrong position would be saved if game has been scaled because of jackpot label
			startReelTweenDown(); 
			yield return new TIWaitForSeconds(TIME_SLIDE_REELS);
			endOfReelTweenDown();

			// Slide in the animator
			if (featurePaytable != null)
			{
				Audio.play(PAYTABLE_SLIDE_SOUND);
				featurePaytable.Play(PAYTABLE_INTRO_ANIMATION);
				
				// wait till we enter into the animation state
				while (!featurePaytable.GetCurrentAnimatorStateInfo(0).IsName(PAYTABLE_INTRO_ANIMATION))
				{
					yield return null;
				}
				
				// now wait for the animation to finish and go back to the idle state
				while (featurePaytable.GetCurrentAnimatorStateInfo(0).IsName(PAYTABLE_INTRO_ANIMATION))
				{
					yield return null;
				}
			}
		}
	}

	// Factoring this out into its own function allows for use to override it and do other things while moving the reels 
	// without having to rewrite the entire executeAfterPaylinesCallback()
	protected virtual void startReelTweenDown()
	{
		for (int i = 0; i < reelToSlide.Length; i++)
		{
			if (BOTTOM_REEL_POSITION.Length > i)
			{
				GameObject go = reelToSlide[i];
				iTween.MoveTo(go, iTween.Hash("y", BOTTOM_REEL_POSITION[i], "time", TIME_SLIDE_REELS, "islocal", true));
			}
			else
			{
				Debug.LogError("No reel position set for " + i);
			}
		}
	}

	// Factoring this out into its own function allows for use to override it and do other things while moving the reels 
	// without having to rewrite the entire executeAfterPaylinesCallback()
	protected virtual void startReelTweenUp()
	{
		for (int i = 0; i < reelToSlide.Length; i++)
		{
			if (BOTTOM_REEL_POSITION.Length > i)
			{
				//GameObject go = reelToSlide[i];
				iTween.MoveTo(reelToSlide[i], iTween.Hash("y", reelToSlideStartingPoistion[i], "time", TIME_SLIDE_REELS, "islocal", false));
			}
			else
			{
				Debug.LogError("No reel position set for " + i);
			}
		}
	}

	//This is incase we want to do something specific after the reels move up
	protected virtual void endOfReelTweenUp()
	{
		//Nothing to do in in the base class, override in children classes if needed
	}

	//This is incase we want to do something specific after the reels move down
	protected virtual void endOfReelTweenDown()
	{
		//Nothing to do in in the base class, override in children classes if needed
	}

	// Executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpinEnding(SlotReel stoppedReel)
	{
		// NOTE: rhw01 didn't have this, it was just calling the base class verson which returned false, but I think it's fine.
		return !firstSCReveal;
	}

	// This is called for each reel by executeOnSpecificReelStopping() if needsToExecuteOnSpinEnding(SlotReel stoppedReel) returns true
	public override void executeOnSpinEnding(SlotReel stoppedReel)
	{
		// NOTE: rhw01 didn't have this, which makes sense because needsToExecuteOnSpinEnding returned false.
		foreach (SlotSymbol symbol in stoppedReel.visibleSymbols)
		{
			if (symbol.name == transformSymbol)
			{
				Audio.play(SC_SYMBOLS_LAND);
			}
		}
	}

	protected void rollUpCounter(long rollupValue)
	{
		if (counter != null)
		{
			counter.text = CommonText.formatNumber(rollupValue);

		}
		if (counter_TMPro != null)
		{
			counter_TMPro.text = CommonText.formatNumber(rollupValue);

		}

		if (lastCounterRollup != rollupValue)
		{
			Audio.play(ADVANCE_COUNTER_SOUND);
		}		
		lastCounterRollup = rollupValue;
	}	
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(base.executeOnReelsStoppedCallback());
		firstHammer = true;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{

		firstHammer = true;
		yield return new TIWaitForSeconds(AFTER_REELS_STOP_WAIT_TIME);

		// Count every symbol that is the SC symbol.
		int symbolCount = 0;
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
			{
				if (symbol.serverName == transformSymbol)
				{
					symbolCount++;
				}
			}
		}
		string paytableName = reelGame.currentReevaluationSpin.getPayTable();
		PayTable paytable = PayTable.find(paytableName);
		if (paytable != null)
		{
			foreach (PayTable.LineWin win in paytable.lineWins.Values)
			{
				if (win.symbolMatchCount == symbolCount)
				{
					long winAmount = win.credits * reelGame.outcome.getMultiplier() * GameState.baseWagerMultiplier * reelGame.outcomeDisplayController.multiplier;
					if (rollupAnimation != null)
					{
						rollupAnimation.SetActive(true);
					}

					float rollupTime = getCounterRollupTime(symbolCount);
					yield return StartCoroutine(SlotUtils.rollup(lastCount, symbolCount, rollUpCounter, false, rollupTime));
					if (amount != null)
					{
						amount.text = CreditsEconomy.convertCredits(winAmount);
					}
					if (amount_TMPro != null)
					{
						amount_TMPro.text = CreditsEconomy.convertCredits(winAmount);
					}

					if (rollupAnimation != null)
					{
						rollupAnimation.SetActive(false);
					}
					lastCount = symbolCount;
					lastWin = winAmount;
					break;
				}
			}
		}
		
	
		
		if (!reelGame.hasReevaluationSpinsRemaining)
		{			
			Audio.play(GAME_END_SOUND);
			yield return new TIWaitForSeconds(GAME_END_WAIT);
			Audio.play(GAME_END_VO_SOUND);
			
			// Move the sparkle particle
			if (sparkleParticle != null)
			{
				Audio.play(SPARKLE_TRAVEL_SOUND);
				Vector3 sparklePaticleStart = sparkleParticle.transform.position;
				sparkleParticle.gameObject.SetActive(true);

				// Play the sparkle animation, if there is one
				Animator sparkleAnimator = sparkleParticle.GetComponent<Animator>();
				if(sparkleAnimator != null)
				{
					sparkleAnimator.Play(SPARKLE_PARTICLE_STATE_NAME);
					CommonAnimation.waitForAnimDur(sparkleAnimator);
				}

				yield return new TITweenYieldInstruction(
					iTween.MoveTo(sparkleParticle.gameObject, iTween.Hash(
					"position", SpinPanel.instance.winningsAmountLabel.transform.position,
					"time", TIME_MOVE_SPARKLE,
					"islocal", false,
					"easetype", iTween.EaseType.easeInQuad)));	

				foreach (ParticleSystem ps in sparkleParticle.GetComponents<ParticleSystem>())
				{
					if (ps != null)
					{
						ps.Clear();
					}
				}
				Audio.play(SPARKLE_LAND_SOUND);
				sparkleParticle.gameObject.SetActive(false);
				sparkleParticle.transform.position = sparklePaticleStart;
			}
			
			if (rollUpParticle != null)
			{
				rollUpParticle.transform.position = SpinPanel.instance.winningsAmountLabel.transform.position;
				rollUpParticle.SetActive(true);

				// Play the rollup animation, if there isone
				Animator rollUpAnimator = rollUpParticle.GetComponent<Animator>();
				if(rollUpAnimator)
				{
					rollUpAnimator.Play(ROLLUP_PARTICLE_STATE_NAME);
					CommonAnimation.waitForAnimDur(rollUpAnimator);
				}
			}
			yield return StartCoroutine(SlotUtils.rollup(0, lastWin, reelGame.onPayoutRollup, true, TIME_ROLL_UP_FEATURE, true, false));
			
			needsToCleanUp = true;
		}		
	}

	protected virtual float getCounterRollupTime(int symbolCount)
	{
		return TIME_BETWEEN_COUNT * (symbolCount - lastCount) * (reelGame.reevaluationSpinsRemaining / 3);
	}

	protected virtual IEnumerator cleanUp()
	{
		isInCleanUp = true;
		needsToCleanUp = false;
		firstHammer = true;
		PlaylistInfo playlistInfo = PlaylistInfo.find(ADVANCE_COUNTER_SOUND);
		if (playlistInfo != null)
		{
			// This should start over everytime.
			playlistInfo.reset();
		}
		if (rollUpParticle != null)
		{
			rollUpParticle.gameObject.SetActive(false);
		}
		
		foreach (GameObject go in scRevealsToClean)
		{
			Destroy(go);
		}
		scRevealsToClean.Clear();
		
		if (featurePaytable != null)
		{
			featurePaytable.Play(PAYTABLE_EXIT_ANIMATION);
			Audio.play(PAYTABLE_EXIT_SOUND);
			// wait till we enter into the animation state
			while (!featurePaytable.GetCurrentAnimatorStateInfo(0).IsName(PAYTABLE_EXIT_ANIMATION))
			{
				yield return null;
			}
			
			// now wait for the animation to finish and go back to the idle state
			while (featurePaytable.GetCurrentAnimatorStateInfo(0).IsName(PAYTABLE_EXIT_ANIMATION))
			{
				yield return null;
			}
		}

		// Slide down the reels.
		startReelTweenUp();
		yield return new TIWaitForSeconds(TIME_SLIDE_REELS);
		endOfReelTweenUp();

		// Set the outcome back to the basic one and fade the reels back in.
		reelGame.setOutcome(reelGame.outcome);
		reelGame.clearReevaluationSpins();
		BonusGameManager.instance.finalPayout = lastWin;
		yield return StartCoroutine(restoreSymbols());
		
		
		if (reelGame.outcome.getJsonSubOutcomes().Length > 0)
		{
			// We have paylines in the final outcome.
			reelGame.skipPaylines = false;
			yield return StartCoroutine(reelGame.handleNormalReelStop());
			reelGame.skipPaylines = true;
		}
		else
		{
			// There won't be a roll up.
			reelGame.outcomeDisplayController.justPlayRollup(reelGame.outcome);
		}
		// Put the music back to where it was.
		Audio.switchMusicKeyImmediate(Audio.soundMap("prespin_idle_loop", reelGame.engine.gameData.keyName));

		if (counter != null)
		{
			counter.text = CommonText.formatNumber(0);
		}
		if (counter_TMPro != null)
		{
			counter_TMPro.text = CommonText.formatNumber(0);
		}		


		if (amount != null)
		{
			amount.text = CreditsEconomy.convertCredits(0);
		}
		if (amount_TMPro != null)
		{
			amount_TMPro.text = CreditsEconomy.convertCredits(0);
		}			

		lastCount = 0;
		lastWin = 0;

		yield return StartCoroutine(CommonGameObject.fadeGameObjectsToOriginalAlpha(restoreAlphaMaps, TIME_FADE));

		isFeatureInProgress = false;	

		isInCleanUp = false;
	}

	protected override IEnumerator changeSymbolTo(SlotSymbol symbol, string name)
	{
		playSCRevealOn(symbol.gameObject);
		if (firstSCReveal)
		{
			yield return new TIWaitForSeconds(FIRST_CHANGE_SYMBOL_TO_WAIT);
		}
		if (symbol != null)
		{
			if (symbol.name != name)
			{
				symbol.mutateTo(transformSymbol);

				if (firstSCReveal && ANIMATE_SYMBOLS_ON_LOCK)
				{
					symbol.animateOutcome();
				}
			}
		}
		yield break;
	}

	protected virtual void playSCRevealOn(GameObject go)
	{
		if (go != null)
		{
			if (scRevealPrefab != null)
			{
				GameObject reveal = CommonGameObject.instantiate(scRevealPrefab) as GameObject;
				scRevealsToClean.Add(reveal); // Keep track of it because it needs to be cleaned up one the SC effect is over.
				if (!firstSCReveal)
				{
					reveal.transform.parent = go.transform;				
					Vector3 animatorPos = Vector3.zero;
					animatorPos.z = SC_REVEAL_Z_POS;
					reveal.transform.localPosition = animatorPos;
				}
				else
				{
					reveal.transform.position = go.transform.position + FIRST_REVEAL_POSITION_DELTA;
				}
				if (firstHammer || firstSCReveal)
				{
					Audio.play(MATCHED_SYMBOL_LOCKS_SOUND, 1, 0, MATCHED_SYMBOL_LOCKS_SOUND_DELAY);
				}
				if (!firstSCReveal)
				{
					firstHammer = false;
				}
			}
			else
			{
				Debug.LogWarning("No SC reveal found.");
			}
		}
		else
		{
			Debug.LogError("Trying to attach a reveal onto a null gameObject.");
		}
	}

	/// <summary>
	/// Playes the SC symbol effect when it lands.
	/// Note: If it can find a SC_mutate symbol it will mutate to it first.
	/// </summary>
	/// <returns>The SC symbol effect.</returns>
	/// <param name="symbol">Symbol.</param>
	protected override IEnumerator playSCSymbolEffect(SlotSymbol symbol)
	{		
		firstSCReveal = true; 
		
		if (symbol != null)
		{			
			// Is the animation on this symbol, or do we need to mutate first

			// peewee01 has a seperate SC symbol each with it's own unique animator for the reveal in the form
			// of M1_SC, M2_SC, so we need to check for that type
			if (reelGame.findSymbolInfo(transformSymbol + "_SC") != null) 
			{
				symbol.mutateTo(transformSymbol + "_SC", null, false, true);
				yield return null;
			}
			else if (reelGame.findSymbolInfo ("SC_mutate") != null) 
			{ 
				// TODO: This check above works for now, but a more complete test will be:
				// if(scAnimator.HasState(scAnimation, -1))
				// HasState isn't available until a later version of Unity3d.
				// We found it, so mutate

				// Mutate it imediately to the one with the animations.
				symbol.mutateTo ("SC_mutate", null, false, true);
				yield return null;
			}

			if (symbol.isFlattenedSymbol)
			{
				symbol.mutateTo("SC", null, false, true);
				yield return null;
			}

			Animator scAnimator = symbol.gameObject.GetComponentInChildren<Animator>();
			string scAnimation = null;
			if (scAnimator != null)
			{
				int cycleIndex = -1;
				if (symbolNameToFaceIndex.ContainsKey(transformSymbol))
				{
					cycleIndex = symbolNameToFaceIndex[transformSymbol];
					
					if (cycleIndex >= 0 && cycleIndex < MAJOR_SC_REVEAL_NAMES.Length)
					{
						scAnimation = MAJOR_SC_REVEAL_NAMES[cycleIndex];
						
						if (cycleIndex < majorFaceArray.Length)
						{
							majorFaceArray[cycleIndex].SetActive(true);
						}
						else
						{
							Debug.LogError("majorFaceArray doesn't contain an entry for cycleIndex = " + cycleIndex);
						}
					}
					else
					{
						Debug.LogError("MAJOR_SC_REVEAL_NAMES doesn't contain an entry for cycleIndex = " + cycleIndex);
					}
				}
				else
				{
					Debug.LogWarning("Not sure which SC animation should be played for " + transformSymbol);
				}
				
				if (scAnimation != null)
				{
					scAnimator.Play(scAnimation);
					scAnimator.speed = 1;
					
					// wait till we enter into the animation state
					if (SYMBOL_CYCLE_SOUND != "")
					{
						if (SC_CYCLE_TIMES != null && cycleIndex >= 0 && cycleIndex < SC_CYCLE_TIMES.Length)
						{
							foreach (float delay in SC_CYCLE_TIMES[cycleIndex])
							{
								Audio.play(SYMBOL_CYCLE_SOUND, 1.0f, 0.0f, delay);
							}
						}
						else
						{
							Debug.LogWarning("Can't play " + SYMBOL_CYCLE_SOUND + " because " + cycleIndex + " isn't in bounds of " + SC_CYCLE_TIMES);
						}
					}
					
					while (!scAnimator.GetCurrentAnimatorStateInfo(0).IsName(scAnimation))
					{
						yield return null;
					}
					
					// now wait for the animation to finish and go back to the idle state
					while (scAnimator.GetCurrentAnimatorStateInfo(0).IsName(scAnimation))
					{
						yield return null;
					}
					
					if (SYMBOL_SELECTED_SOUND != "")
					{
						Audio.play(SYMBOL_SELECTED_SOUND, 1.0f, 0, TIME_SC_CYCLE_OVER);
					}
				}
			}
			else
			{
				Debug.LogError("No animator found on " + symbol.name);
			}
		}
		else
		{
			Debug.LogError("Couldn't find SC symbol.");
		}
	}
	
	public override void executeOnReleaseSymbolInstance(SymbolAnimator animator)
	{
		base.executeOnReleaseSymbolInstance(animator);
		if (animator != null && animator.info != null && SlotSymbol.getShortServerNameFromName(animator.symbolInfoName) == "SC")
		{
			// Put the SC symbol back to it's defualt state.
			animator.gameObject.SetActive(false);
			animator.gameObject.SetActive(true);
		}
	}
	
	// executeOnChangeSymbolToSticky() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnChangeSymbolToSticky()
	{
		return true;
	}
	
	public override IEnumerator executeOnChangeSymbolToSticky(SlotSymbol symbol, string name)
	{
		// Add a stuck symbol
		SlotSymbol newSymbol = reelGame.createStickySymbol(name, symbol.index, symbol.reel);
		SymbolAnimator symbolAnimator = newSymbol.animator;
		if (symbolAnimator != null)
		{
			// This is a custom symbol
			Vector3 animatorPos = symbol.transform.localPosition;
			animatorPos.z = STICKY_SYMBOL_Z_POS;
			symbolAnimator.transform.localScale = Vector3.one;
			symbolAnimator.transform.localPosition = animatorPos;
			symbolAnimator.skipSymbolCaching = true;

			// This is the first symbol and it has the animation built in.
			if (!firstSCReveal)
			{
				playSCRevealOn(symbolAnimator.gameObject);
				if (ANIMATE_SYMBOLS_ON_LOCK)
				{
					symbol.getAnimator().deactivate(); // deactivate the original symbol so we can see the sticky symbol animate without the original symbol still being there
					symbolAnimator.playAnimation(symbol.info.outcomeAnimation, true);
				}
			}
			else
			{
				firstSCReveal = false;
			}
		}
		else
		{
			Debug.LogWarning("No symbol found for " + name);
		}
		yield break;
	}
}
