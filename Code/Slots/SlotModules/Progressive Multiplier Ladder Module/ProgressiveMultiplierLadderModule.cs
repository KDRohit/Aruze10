/*
	module first used on Freaki Tiki 3 ( gen81 )
*/

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProgressiveMultiplierLadderModule : SlotModule
{
	public List<Rung> rungs;
	public List<AnimationListController.AnimationInformationList> flyToLadderShow;
	public AnimationListController.AnimationInformationList toolTipIntro;
	public AnimationListController.AnimationInformationList toolTipShow;
	public AnimationListController.AnimationInformationList toolTipOutro;
	public AnimatedParticleEffect particleTrail; // as tumbles increment ladder, trails from symbols to ladder

	private bool toolTipIsShowingAtGameStart;
	private WagerRanges wagerRanges;
	private Ladder ladder;
	private List<SlotSymbol> flownSymbols;
	private bool hasUpdatedTheLadderStates;
	private long previousWager;

	public bool updateTopTierIndependently;
	public float updateTopTierDelay;

	public enum IncrementMode
	{
		None,
		BetAmount, // user adjusts bet level, top rung of ladder changes multiplier
		Symbols // symbols land, top rung increment multiplier 
	}
	public IncrementMode incrementMode;
	public string incrementSymbol = "TW";
	public bool hideUnderlyingSymbolOnFlyover;
	private List<SlotSymbol> hiddenSymbols;

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		ladder = new Ladder(rungs);

		flownSymbols = new List<SlotSymbol>();

		if (hideUnderlyingSymbolOnFlyover)
		{
			hiddenSymbols = new List<SlotSymbol>();
		}

		if (toolTipIntro != null)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(toolTipIntro));

			toolTipIsShowingAtGameStart = true;
		}

		previousWager = reelGame.currentWager;

		JSON wagerRangeData = null;
		if (reelGame.modifierExports != null && reelGame.modifierExports.Length > 0)
		{
			for (int i = 0; i < reelGame.modifierExports.Length; ++i)
			{
				wagerRangeData = reelGame.modifierExports[i].getJSON(MutationManager.TUMBLE_MULTIPLIER_JSON_KEY);
				if (wagerRangeData != null)
				{
					break;
				}
			}

		}
		else if (reelGame.reelInfo != null)
		{
			for(int i = 0; i < reelGame.reelInfo.Length; i++)
			{
				wagerRangeData = reelGame.reelInfo[i].getJSON(MutationManager.TUMBLE_MULTIPLIER_JSON_KEY);

				if(wagerRangeData != null)
				{
					break;
				}
			}			
		}
	
		if (wagerRangeData != null)
		{
			wagerRanges = new WagerRanges();

			if (wagerRangeData.hasKey("tiers"))
			{
				JSON[] tiersJson = wagerRangeData.getJsonArray("tiers");
				for (int t = 0; t < tiersJson.Length; t++)
				{
					wagerRanges.addRange(tiersJson[t].getLong("wager_min", 0), tiersJson[t].getLong("wager_max", 1), tiersJson[t].getInt("multiplier", 1));
				}
			}
		}

		if (wagerRanges == null && reelGame.isFreeSpinGame())
		{
			ProgressiveMultiplierLadderModule baseGameModule = getModuleOnBaseGame();

			if (baseGameModule != null)
			{
				wagerRanges = baseGameModule.wagerRanges.clone();
			}
		}

		if (wagerRanges == null)
		{
			Debug.LogError("Game:" + GameState.game.keyName + " missing " + MutationManager.TUMBLE_MULTIPLIER_JSON_KEY + " data in reelGame.reelInfo, check SCAT>Start Game Event Information. See gen81 for reference");
		}
		else
		{
			updateAllRungValuesByWager(); // set initial values
		}
	}

	//
	// ROLLUP
	//
	public override bool needsToExecuteOnStartPayoutRollup(long bonusPayout, long basePayout)
	{
		return true;
	}

	public override IEnumerator executeOnStartPayoutRollup(long bonusPayout, long basePayout)
	{
		ladder.playRungWin();

		yield break;
	}

	//
	// FLY TRAILS FROM TUMBLES TO NEXT RUNG
	//
	public override bool needsToExecuteBeforeCleanUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		return particleTrail != null;
	}

	public override IEnumerator executeBeforeCleanUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		Rung nextRung = ladder.getNextRung();

		if (nextRung != null)
		{
			yield return StartCoroutine(particleTrail.animateParticleEffect(symbol.transform, nextRung.rungTransform));

			if (hasUpdatedTheLadderStates == false)
			{
				hasUpdatedTheLadderStates = true;

				ladder.updateStates(mutation.tumbleCount + 1); // hilights next active rung
			}
		}
	}
	
	//
	// INCREMENT LADDER RUNG
	//
	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationPreSpin()
	{
		multiplierUpdate(); // sets here so correct multiplier before rollup		

		yield break;
	}

	public override bool needsToExecuteOnReevaluationPreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationPreReelsStopSpinning()
	{
		hasUpdatedTheLadderStates = false;

		yield break;
	}

	//
	// RESET LADDER to RUNG 1 (1x)
	//
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		ladder.updateStates(); // resets back to rung 1 active

		hasUpdatedTheLadderStates = false;

		multiplierReset();

		if (flownSymbols != null)
		{
			flownSymbols.Clear();
		}

		if (toolTipIsShowingAtGameStart == true)
		{
			toolTipIsShowingAtGameStart = false;

			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(toolTipOutro));
		}
	}

	//
	// CHECK FOR TW SYMBOLS
	//
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(flyOverIncrementSymbols());
	}

	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		yield return StartCoroutine(flyOverIncrementSymbols());
	}

	public IEnumerator flyOverIncrementSymbols()
	{
		if (incrementMode == IncrementMode.Symbols)
		{
			SlotReel[] reels = reelGame.engine.getAllSlotReels();
			for (int reelId = 0; reelId < reels.Length; reelId++)
			{
				SlotReel reel = reels[reelId];
				for (int rowIndex = reel.visibleSymbols.Length - 1; rowIndex >= 0; rowIndex--)
				{
					SlotSymbol symbol = reel.visibleSymbols[rowIndex];

					if (flownSymbols.IndexOf(symbol) > -1) // prevents incrementing more than once for same symbol
					{
						continue;
					}

					if (symbol.name.IndexOf(incrementSymbol) > -1)
					{
						flownSymbols.Add(symbol);
						
						if (hideUnderlyingSymbolOnFlyover)
						{
							symbol.gameObject.SetActive(false);
							hiddenSymbols.Add(symbol);
						}

						//this flag allows for updating the tier value independently of the flyToLadderShow timings
						if (updateTopTierIndependently)
						{
							//avoid blocking on this so we can proceed to launching the animation 
							//of the flyToLadderShow symbol
							StartCoroutine(flyOverIncrementTopTierRung());
						}
						
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(flyToLadderShow[rowIndex]));

						if (!updateTopTierIndependently)
						{
							ladder.incrementTopTierRung(1);
						}					

						if (hideUnderlyingSymbolOnFlyover && hiddenSymbols != null)
						{
							for (int i = 0; i < hiddenSymbols.Count; i++)
							{
								hiddenSymbols[i].gameObject.SetActive(true);
							}

							hiddenSymbols.Clear();
						}
					}
				}
			}
		}
	}

	// if updateTopTierIndependently is enabled, then the top tier update happens based on this 
	// potentially delayed coroutine called before the flyToLadderShow animation occurs. 
	IEnumerator flyOverIncrementTopTierRung()
	{
		if (updateTopTierDelay > 0)
		{
			yield return new WaitForSeconds(updateTopTierDelay);
		}
		
		ladder.incrementTopTierRung(1);	
	}

	//
	// WAGER ADJUSTED - SHOW TOOL TIP & UPDATE TOP RUNG in BASE GAME
	//
	public override bool needsToExecuteOnWagerChange(long currentWager)
	{
		return incrementMode == IncrementMode.BetAmount;
	}

	public override void executeOnWagerChange(long currentWager)
	{
		updateAllRungValuesByWager();

		if ( currentWager != previousWager)
		{
			StartCoroutine(showBetAmountTooltip());
		}

		previousWager = currentWager;
	}

	private void updateAllRungValuesByWager()
	{
		if (wagerRanges == null || wagerRanges.ranges.Count < 1)
		{
			return;
		}

		if(reelGame.isFreeSpinGame() && GameState.giftedBonus != null)
		{
			wagerRanges.setCurrentRange(0); // forces using lowest range
		}
		else
		{
			wagerRanges.setCurrentRange(ReelGame.activeGame.currentWager);			
		}
		
		ladder.updateAllRungValues(wagerRanges.currentRange.multiplier);
	}

	//
	// UTILS
	//
	private void multiplierUpdate()
	{
		reelGame.outcomeDisplayController.multiplierAppend = mutation.finalMultiplier;		
	}

	private void multiplierReset()
	{
		reelGame.outcomeDisplayController.multiplierAppend = 1;
	}

	private MutationTumbleMultiplier mutation
	{
		get
		{
			return reelGame.mutationManager.getMutation(MutationManager.TUMBLE_MULTIPLIER_JSON_KEY) as MutationTumbleMultiplier;
		}
	}

	private IEnumerator showBetAmountTooltip()
	{
		if (toolTipOutro == null || toolTipShow == null)
		{
			yield break;
		}

		if (toolTipIsShowingAtGameStart == true)
		{
			toolTipIsShowingAtGameStart = false;

			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(toolTipOutro));
		}
		else
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(toolTipShow));
		}		
	}

	// Helper function to get the same module attached to the base game
	private ProgressiveMultiplierLadderModule getModuleOnBaseGame()
	{
		if (SlotBaseGame.instance != null)
		{
			for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
			{
				ProgressiveMultiplierLadderModule module = SlotBaseGame.instance.cachedAttachedSlotModules[i] as ProgressiveMultiplierLadderModule;
				if (module != null)
				{
					return module;
				}
			}
		}

		return null;
	}

	private class Ladder
	{
		private List<Rung> rungs;
		private int currentTierLastUsed;

		public Ladder(List<Rung> rungs)
		{
			this.rungs = rungs;

			updateStates();
		}

		public void playRungWin()
		{
			for (int i = 0; i < rungs.Count; i++)
			{
				if (rungs[i].isActive)
				{
					rungs[i].playWin();
					return;
				}
			}
		}

		public Rung getNextRung()
		{
			if(currentTierLastUsed < rungs.Count)
			{
				return rungs[currentTierLastUsed];
			}

			return null; // is already at top rung, so no more to be a next 
		}

		public void updateStates(int currentTier = 1)
		{
			currentTierLastUsed = Mathf.Min(currentTier, rungs.Count);

			for (int i = 0; i < rungs.Count; i++)
			{
				if(rungs[i].tier == currentTier)
				{
					rungs[i].setActive();
				}
				else
				{
					rungs[i].setInactive();
				}
			}
		}

		public void updateAllRungValues(int topTierMultiplierValue)
		{
			//set top
			rungs[rungs.Count-1].setValue(topTierMultiplierValue, true);
			
			//set lower
			for (int i = 0; i < rungs.Count - 1; i++)
			{
				rungs[i].setValue();
			}
		}

		public void incrementTopTierRung(int incrementAmount)
		{
			int topTierIndex = rungs.Count - 1;
			rungs[topTierIndex].setValue(rungs[topTierIndex].getValue() + incrementAmount, true);
		}
	}

	[Serializable]
	public class Rung
	{
		public int tier;
		public Transform rungTransform;
		public TextMeshPro textField;
		public MultiLabelWrapperComponent multiLabelText;
		public AnimationListController.AnimationInformationList animOff;
		public AnimationListController.AnimationInformationList animWin;
		public AnimationListController.AnimationInformationList animOffToOn;
		public AnimationListController.AnimationInformationList animOffToFirstOn;
		public AnimationListController.AnimationInformationList animOnToOff;		
		public AnimationListController.AnimationInformationList animOnToIncrease;
		public AnimationListController.AnimationInformationList animOffToIncrease;
		public bool isActive;
		private bool firstActivation = true;

		private int currentTierLastUsed;
		private int value;

		public void setValue(int newValue = 0, bool animate = false)
		{
			if (textField == null && multiLabelText == null)
			{
				return;
			}

			if (animate && newValue != this.value)
			{
				if (isActive)
				{
					playAnimList(animOnToIncrease);
				}
				else
				{
					playAnimList(animOffToIncrease);
				}
			}

			if (newValue < 1)
			{
				value = tier;
			}
			else
			{
				value = newValue;
			}

			if (textField != null)
			{
				textField.text = Localize.text("{0}X", CommonText.formatNumber(value));
			}

			if (multiLabelText != null)
			{
				multiLabelText.text = Localize.text("{0}X", CommonText.formatNumber(value));
			}
		}

		public int getValue()
		{
			return value;
		}

		public void setActive()
		{
			if (!isActive)
			{
				//if we have animations for the first off to on state and this is the first activation, use them
				//otherwise, just the the default animOffToOn animation list
				if (firstActivation && animOffToFirstOn.Count > 0)
				{
					playAnimList(animOffToFirstOn);
					firstActivation = false;
				}
				else
				{
					playAnimList(animOffToOn);
				}			
			}

			isActive = true;
		}

		public void setInactive()
		{
			if (isActive)
			{
				playAnimList(animOnToOff);
			}
			else
			{
				playAnimList(animOff);
			}

			isActive = false;
		}

		public void playWin()
		{
			if (isActive)
			{
				playAnimList(animWin);
			}
		}

		private void playAnimList(AnimationListController.AnimationInformationList list)
		{
			RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(list));
		}
				
	}

	private class WagerRanges
	{
		public List<WagerRange> ranges = new List<WagerRange>();
		public WagerRange currentRange;

		public void addRange (long min, long max, int multiplier)
		{
			if (ranges == null)
			{
				ranges = new List<WagerRange>();
			}

			ranges.Add(new WagerRange(min, max, multiplier));
		}

		public void setCurrentRange (long currentWager)
		{
			//if the wager is higher than the highest range's max, just set and return 
			//instead of defaulting to lowest
			if (currentWager > ranges[ranges.Count - 1].max)
			{
				currentRange = ranges[ranges.Count - 1];
				return;
			}
			
			for (int i = 0; i < ranges.Count; i++)
			{
				if (ranges[i].isInRange(currentWager))
				{
					currentRange = ranges[i];
					return;
				}
			}

			currentRange = ranges[0]; // default to lowest range
		}

		public WagerRanges clone()
		{
			WagerRanges result = new WagerRanges();

			for (int i = 0; i < this.ranges.Count; i++)
			{
				result.addRange(this.ranges[i].min, this.ranges[i].max, this.ranges[i].multiplier);
			}

			result.setCurrentRange(this.currentRange.min);

			return result;
		}
	}

	private class WagerRange
	{
		public long min;
		public long max;
		public int multiplier;

		public WagerRange(long min, long max, int multiplier)
		{
			this.min = min;
			this.max = max;
			this.multiplier = multiplier;
		}

		public override string ToString()
		{
			return "min:" + min + " max:" + max + " multiplier:" + multiplier;
		}

		public bool isInRange(long value)
		{
			return value >= min && value <= max;
		}
	}
}