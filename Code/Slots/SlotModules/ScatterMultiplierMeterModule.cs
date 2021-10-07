using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for handling rolling up a SC meter and paying out a multiplier. Inherits from ScatterCollectBaseModule to
 * leverage dynamic label setup code
 * First used in gen93 Meet the Bigfoots
 * Author: Caroline 02/2020
 */
public class ScatterMultiplierMeterModule : ScatterSymbolBaseModule
{
	[Tooltip("Incremental counter for number of SC symbols landed on a spin")]
	[SerializeField] private LabelWrapperComponent multiplierMeterLabel;
	[Tooltip("Temporary display area to show base scatter winnings before applying multiplier")]
	[SerializeField] private LabelWrapperComponent freespinsHeaderWinLabel;

	[Header("Animations")]
	[Tooltip("Animation to play when particle trail hits multiplier meter and number increments")]
	[SerializeField] private AnimationListController.AnimationInformationList meterIncrementAnimations;
	[Tooltip("Animation to play when meter rollup and base credits rollup are done, before final multiplier is applied to win box")]
	[SerializeField] private AnimationListController.AnimationInformationList meterMultiplierPayoutAnimations;

	// free spin specific animations
	[Tooltip("Animation to play when we're going to transition to a freespin game, plays before starting rollup after initial SC particle trails")]
	[SerializeField] private AnimationListController.AnimationInformationList freespinPreTransitionAnimation;
	[Tooltip("Animation to play before final spin in free spins")]
	[SerializeField] private AnimationListController.AnimationInformationList freespinsPreFinalSpinAnimations;
	[Tooltip("Animation to play after final spin before multiplier applied in free spins")]
	[SerializeField] private AnimationListController.AnimationInformationList freespinsFinalSpinPrePayoutAnimations;
	[Tooltip("Animation to play during freespin final spin payout, after multiplier applied")]
	[SerializeField] private AnimationListController.AnimationInformationList freespinsFinalSpinPayoutAnimations;
	[Tooltip("Animation to play after freespin final spin payout complete")]
	[SerializeField] private AnimationListController.AnimationInformationList freespinsFinalSpinPayoutEndAnimations;

	[Header("Particle Trails")]
	[Tooltip("Particle trail from a symbol to the multiplier meter")]
	[SerializeField] private AnimatedParticleEffect symbolToMultiplierMeterParticleEffect;
	[Tooltip("Particle trail from a symbol to the multiplier meter on last spin")]
	[SerializeField] private AnimatedParticleEffect symbolToMultiplierMeterLastSpinParticleEffect;
	[Tooltip("Particle trail from multiplier meter to pay box")]
	[SerializeField] private AnimatedParticleEffect multiplierMeterToPayBoxParticleEffect;
	[Tooltip("Particle trail from multiplier meter to jackpot display area, plays during freespins final spin before multiplier applied to base winnings")]
	[SerializeField] private AnimatedParticleEffect freespinsMultiplierMeterToDisplayAreaParticleEffect;
	[Tooltip("Particle trail from jackpot display area to win box, plays during freespins final spin after multiplier applied to base winnings")]
	[SerializeField] private AnimatedParticleEffect freespinsDisplayAreaToPayboxParticleEffect;

	[Header("Freespins Final Spin Animation")]
	[Tooltip("Symbols to reparent during free spins final spin, grouped by reel")]
	[SerializeField] private List<ReparentSymbolAndPlayAnimation.ReparentedSymbolsReelData> reparentedSymbolsPerReelAnimationData = new List<ReparentSymbolAndPlayAnimation.ReparentedSymbolsReelData>();

	[Tooltip("Reset animations for symbol parents to play after cleanup (in case we need to play animations again in same freespin instance)")]
	[SerializeField] private AnimationListController.AnimationInformationList resetReparentedSymbolsAnimationData;

	[Tooltip("Prefab used when generating placeholder objects on the reels for testing the reparent symbol animation effect. If not set, will choose a random symbol template to use instead")]
	[SerializeField] private GameObject freespinFinalSpinAnimationTestSymbolPrefab;
	
	[Header("Timing Tuning")]
	[Tooltip("Small delay between each symbol's particle trail to the multiplier meter so we can see meter increment")]
	[SerializeField] private float scatterParticleTrailPerSymbolDelay = 0.05f;
	[Tooltip("Delay between reels to show scatter symbol contributions per reel")]
	[SerializeField] private float scatterParticleTrailPerReelDelay = 0.5f;
	[Tooltip("Delay after reel stop before starting scatter symbol particle trails and payout")]
	[SerializeField] private float scatterPayoutStartDelay = 0.5f;
	[Tooltip("Rollup time for base scatter payout before multiplier applied")]
	[SerializeField] private float scatterPayoutRollupTimeOverride = 0.5f;
	[Tooltip("Rollup time for base scatter payout in freespins header display")]
	[SerializeField] private float freespinsScatterPayoutRollupTimeOverride = 1.0f;

	[Tooltip("Duration override for freespin final spin")]
	[SerializeField] private float freespinFinalSpinOverrideDuration = 0.0f;

	private const string FIELD_SYMBOL_PAYOUT_MULTIPLIER = "symbol_payout_count_multiplier";
	
	private JSON symbolPayoutMultiplierJSON;
	
	// store symbol info in a quick lookup by reel position
	private Dictionary<int, List<ScatterMultiplierSymbolData>> scatterSymbolsPerReel = new Dictionary<int, List<ScatterMultiplierSymbolData>>(); //kvp: reelId, symbol data

	// coroutine lists to reduce garbage
	private List<TICoroutine> loopingScatterSymbolCoroutines = new List<TICoroutine>();
	private List<TICoroutine> particleEffectCoroutines = new List<TICoroutine>();
	
	private int currentMeterValue;
	private bool isRollingUpSymbolPayout;

	private bool isDebuggingFreespinFinalSpinAnimation; // flag to prevent spamming test button

	private class ScatterMultiplierSymbolData
	{
		public string symbolName;
		public int reelId;
		public int position;
		public long credits;
		public int incrementValue; // how much to increment the multiplier meter by when landed
	}
	
	private bool isFreespinsFinalSpin
	{
		get { return reelGame.hasFreespinGameStarted && !reelGame.hasFreespinsSpinsRemaining; }
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		currentMeterValue = 0;
		updateMeterMultiplierLabel(currentMeterValue);
		yield return StartCoroutine(base.executeOnSlotGameStarted(reelSetDataJson));
	}
	
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		// reset state
		symbolPayoutMultiplierJSON = null;
		clearPayoutSymbolsDictionary();
		particleEffectCoroutines.Clear();
		loopingScatterSymbolCoroutines.Clear();
		// don't clear meter if in freespin game
		if (!reelGame.hasFreespinGameStarted)
		{
			currentMeterValue = 0;
			updateMeterMultiplierLabel(0);
		}

		if (isFreespinsFinalSpin)
		{
			yield return StartCoroutine(doFreespinsFinalSpinAnimations());
		}
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return isFreespinsFinalSpin && freespinFinalSpinOverrideDuration > 0;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		if (freespinFinalSpinOverrideDuration > 0)
		{
			// extend the spin time of the final spin for dramatic effect
			yield return new TIWaitForSeconds(freespinFinalSpinOverrideDuration);
		}
	}
	
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();
		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				if (reevaluation.getString("type", "").Equals(FIELD_SYMBOL_PAYOUT_MULTIPLIER))
				{
					symbolPayoutMultiplierJSON = reevaluation;
					break;
				}
			}
		}

		if (symbolPayoutMultiplierJSON == null)
		{
			yield break;
		}

		JSON[] symbolsJSON = symbolPayoutMultiplierJSON.getJsonArray("symbols");
		if (symbolsJSON == null)
		{
			yield break;
		}

		parsePayoutSymbolsDictionary(symbolsJSON);

		if (symbolPayoutMultiplierJSON == null || symbolToMultiplierMeterParticleEffect == null)
		{
			yield break;
		}

		if (scatterPayoutStartDelay > 0)
		{
			yield return new TIWaitForSeconds(scatterPayoutStartDelay);
		}

		// animate meter increment per reel
		long preFeatureMultiplierCredits = 0L;
		for (int i = 0; i < reelGame.engine.getAllSlotReels().Length; i++)
		{
			if (scatterSymbolsPerReel.ContainsKey(i))
			{
				foreach (ScatterMultiplierSymbolData scatterMultiplierSymbolData in scatterSymbolsPerReel[i])
				{
					long credits = scatterMultiplierSymbolData.credits * reelGame.multiplier * GameState.baseWagerMultiplier;
					preFeatureMultiplierCredits += credits;
					int pos = scatterMultiplierSymbolData.position;
					List<SlotSymbol> symbolsOnReel = reelGame.engine.getVisibleSymbolsBottomUpAt(i);
					if (pos >= 0 && pos < symbolsOnReel.Count)
					{
						SlotSymbol startSymbol = symbolsOnReel[pos];
						particleEffectCoroutines.Add(StartCoroutine(playMultiplierMeterIncrement(startSymbol, scatterMultiplierSymbolData.incrementValue)));
						particleEffectCoroutines.Add(StartCoroutine(playSymbolOutcome(startSymbol)));
						// offset particle trails very slightly so meter will appear to roll up instead of jump to new value
						if (scatterParticleTrailPerSymbolDelay > 0)
						{
							yield return new WaitForSeconds(scatterParticleTrailPerSymbolDelay);
						}
					}
					else
					{
						Debug.LogErrorFormat("Invalid position {0} for scatter payout symbol {1}", pos, scatterMultiplierSymbolData.symbolName);
					}
				}

				if (scatterParticleTrailPerReelDelay > 0)
				{
					yield return new WaitForSeconds(scatterParticleTrailPerReelDelay);
				}
			}
		}
		
		// check if we're going to transition to a bonus game
		TICoroutine freespinPreTransitionAnimationCoroutine = null;
		if (!reelGame.hasFreespinGameStarted && reelGame.outcome.hasBonusGame())
		{
			freespinPreTransitionAnimationCoroutine = StartCoroutine(AnimationListController.playListOfAnimationInformation(freespinPreTransitionAnimation));
		}

		// rollup base value pre-multiplier
		// skip if in free spins since that has unique flow
		if (!reelGame.hasFreespinGameStarted)
		{
			yield return StartCoroutine(reelGame.rollupCredits(0, preFeatureMultiplierCredits,
				ReelGame.activeGame.onPayoutRollup, isPlayingRollupSounds:true, 
				specificRollupTime:scatterPayoutRollupTimeOverride, allowBigWin: false));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(particleEffectCoroutines));

		// animate final payout
		isRollingUpSymbolPayout = true;
		long finalPayout = currentMeterValue * preFeatureMultiplierCredits;
		
		if (isFreespinsFinalSpin)
		{
			yield return StartCoroutine(doFreeSpinsFinalMultiplierPayout(preFeatureMultiplierCredits, finalPayout));
		}
		else if (!reelGame.hasFreespinGameStarted)
		{
			yield return StartCoroutine(doBaseGameFinalMultiplierPayout(preFeatureMultiplierCredits, finalPayout));
		}
		
		isRollingUpSymbolPayout = false;

		// hold off on rolling up credits if we have more credits coming
		if (!reelGame.hasFreespinGameStarted && !reelGame.outcome.hasBonusGame())
		{
			// in freespins credits are added at the end
			reelGame.addCreditsToSlotsPlayer(finalPayout, "symbol payout multiplier reward",
				shouldPlayCreditsRollupSound: false);
		}

		if (freespinPreTransitionAnimationCoroutine != null)
		{
			yield return freespinPreTransitionAnimationCoroutine;
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(loopingScatterSymbolCoroutines));
	}

	private IEnumerator playSymbolOutcome(SlotSymbol symbol)
	{
		if (symbol == null || symbol.animator == null)
		{
			yield break;
		}
		
		// may be playing anticipation, stop it first
		if (symbol.animator.isAnimating)
		{
			symbol.haltAnimation();
		}
		
		yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());

	}

	private IEnumerator doBaseGameFinalMultiplierPayout(long baseWinnings, long finalPayout)
	{		
		// 1) animate multiplier meter to show we're applying it
		if (meterMultiplierPayoutAnimations != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(meterMultiplierPayoutAnimations));
		}

		// 2) particle trail from multiplier meter to win box
		if (multiplierMeterToPayBoxParticleEffect != null)
		{
			yield return StartCoroutine(multiplierMeterToPayBoxParticleEffect.animateParticleEffect());
		}

		// 3) rollup the difference between our final value and the base winnings we already rolled up
		yield return StartCoroutine(reelGame.rollupCredits(0, finalPayout - baseWinnings,
			ReelGame.activeGame.onPayoutRollup, true, allowBigWin: false));
	}

	private IEnumerator doFreeSpinsFinalMultiplierPayout(long baseWinnings, long finalPayout)
	{
		// 1) animate pre multiplier applied animation
		if (freespinsFinalSpinPrePayoutAnimations != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(freespinsFinalSpinPrePayoutAnimations));
		}

		// 2) animate multiplier meter to show we're applying it now
		if (meterMultiplierPayoutAnimations != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(meterMultiplierPayoutAnimations));
		}

		// 3) rollup jackpot area temp display to show base winnings (might include per symbol particle trail?)
		if (freespinsHeaderWinLabel != null)
		{
			yield return StartCoroutine(SlotUtils.rollup(0, baseWinnings, freespinsHeaderWinLabel, specificRollupTime:freespinsScatterPayoutRollupTimeOverride));
		}

		// 4) particle trail from multiplier meter to jackpot display area
		if (freespinsMultiplierMeterToDisplayAreaParticleEffect != null)
		{
			yield return StartCoroutine(freespinsMultiplierMeterToDisplayAreaParticleEffect.animateParticleEffect());
		}

		// 5) apply multiplier and rollup display area value to new final payout
		if (freespinsHeaderWinLabel != null)
		{
			yield return StartCoroutine(SlotUtils.rollup(baseWinnings, finalPayout, freespinsHeaderWinLabel));
		}

		// 6) animate display area to show final payout amount
		TICoroutine finalSpinPayoutAnimationCoroutine = null;
		if (freespinsFinalSpinPayoutAnimations != null)
		{
			finalSpinPayoutAnimationCoroutine =
				StartCoroutine(AnimationListController.playListOfAnimationInformation(freespinsFinalSpinPayoutAnimations));
		}

		// 7) particle trail from display area to win box
		if (freespinsDisplayAreaToPayboxParticleEffect != null)
		{
			yield return StartCoroutine(freespinsDisplayAreaToPayboxParticleEffect.animateParticleEffect());
		}

		// 8) rollup to final value
		yield return StartCoroutine(reelGame.rollupCredits(0, finalPayout,
			ReelGame.activeGame.onPayoutRollup, true, allowBigWin: false));
		// 9) if animate in not finished, wait before animate out
		if (finalSpinPayoutAnimationCoroutine != null)
		{
			yield return finalSpinPayoutAnimationCoroutine;
		}

		// 10) on rollup complete animations
		if (freespinsFinalSpinPayoutEndAnimations != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(freespinsFinalSpinPayoutEndAnimations));
		}
	}

	private IEnumerator doFreespinsFinalSpinAnimations(bool isDebug = false)
	{
		// final spin, do special stuff
		TICoroutine preFinalSpinAnim = StartCoroutine(AnimationListController.playListOfAnimationInformation(freespinsPreFinalSpinAnimations));

		if (reparentedSymbolsPerReelAnimationData.Count > 0)
		{
			// do reparent on all reels
			if (isDebug)
			{
#if UNITY_EDITOR
				yield return StartCoroutine(ReparentSymbolAndPlayAnimation.doTestSymbolReparentOnReels(reelGame, reparentedSymbolsPerReelAnimationData, freespinFinalSpinAnimationTestSymbolPrefab));
#endif
			}
			else
			{
				yield return StartCoroutine(ReparentSymbolAndPlayAnimation.doSymbolReparentOnReels(reelGame, reparentedSymbolsPerReelAnimationData));
			}

			// once all symbol falling animations are complete, hide before resuming spin to prevent jumps
			if (reelGame.engine != null)
			{
				List<SlotSymbol> symbolsToCleanup = reelGame.engine.getAllSymbolsOnReels();
				foreach (SlotSymbol symbol in symbolsToCleanup)
				{
					symbol.cleanUp();
				}
			}
		}
			
		// make sure animation complete before moving on
		yield return preFinalSpinAnim;
		
		if (resetReparentedSymbolsAnimationData != null)
		{
			// reset parent object states in case we want to play animation again
			// play after all other animations complete to avoid jumping effect
			if (resetReparentedSymbolsAnimationData != null)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(resetReparentedSymbolsAnimationData));
			}
		}
	}

	private void loopSymbolPayoutOutcomeAnimations()
	{
		for (int i = 0; i < reelGame.engine.getAllSlotReels().Length; i++)
		{
			if (scatterSymbolsPerReel.ContainsKey(i))
			{
				List<SlotSymbol> symbolsOnReel = reelGame.engine.getVisibleSymbolsBottomUpAt(i);
				// get symbol at position and kick off animation
				foreach (ScatterMultiplierSymbolData scatterMultiplierSymbolData in scatterSymbolsPerReel[i])
				{
					int pos = scatterMultiplierSymbolData.position;
					if (pos >= 0 && pos < symbolsOnReel.Count)
					{
						SlotSymbol symbol = symbolsOnReel[pos];
						string symbolName = scatterMultiplierSymbolData.symbolName;
						if (symbol.serverName.Equals(symbolName))
						{
							// found a match
							loopingScatterSymbolCoroutines.Add(StartCoroutine(loopSymbolOutcome(symbol)));
						}
					}
					else
					{
						Debug.LogErrorFormat("invalid position {0} for scatter payout symbol {1}", pos, scatterMultiplierSymbolData.symbolName);
					}
				}
			}
		}
	}
	
	private IEnumerator loopSymbolOutcome(SlotSymbol symbol)
	{
		while (isRollingUpSymbolPayout)
		{
			yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		}
	}

	// animate particle trail from symbol to multiplier meter and increment counter
	private IEnumerator playMultiplierMeterIncrement(SlotSymbol symbol, int incrementAmount)
	{
		if (multiplierMeterLabel == null || 
		    symbolToMultiplierMeterParticleEffect == null || 
		    symbol == null ||
		    symbol.gameObject == null || incrementAmount <= 0)
		{
			yield break;
		}

		AnimatedParticleEffect particleEffect = symbolToMultiplierMeterParticleEffect; 
		if (isFreespinsFinalSpin && symbolToMultiplierMeterLastSpinParticleEffect != null)
		{
			particleEffect = symbolToMultiplierMeterLastSpinParticleEffect;
		}
		yield return StartCoroutine(particleEffect.animateParticleEffect(symbol.transform));
		
		// set new value and play inc animation
		currentMeterValue += incrementAmount;
		updateMeterMultiplierLabel(currentMeterValue);
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(meterIncrementAnimations));
	}

	private void parsePayoutSymbolsDictionary(JSON[] payoutJSONArray)
	{
		if (payoutJSONArray == null)
		{
			return;
		}

		for (int i = 0; i < payoutJSONArray.Length; i++)
		{
			int reelId = payoutJSONArray[i].getInt("reel", -1);
			int pos = payoutJSONArray[i].getInt("pos", -1);
			string symbolName = payoutJSONArray[i].getString("symbol", "");
			long credits = payoutJSONArray[i].getLong("credits", -1);
			int incrementValue = payoutJSONArray[i].getInt("multiplier_increase", 0);
			if (reelId < 0 || pos < 0 || string.IsNullOrEmpty(symbolName) || credits < 0)
			{
				Debug.LogWarning("Invalid payout symbol data!");
				continue;
			}

			if (!scatterSymbolsPerReel.ContainsKey(reelId))
			{
				scatterSymbolsPerReel[reelId] = new List<ScatterMultiplierSymbolData>();
			}

			ScatterMultiplierSymbolData scatterMultiplierSymbolData = new ScatterMultiplierSymbolData
			{
				symbolName = symbolName,
				reelId = reelId,
				position = pos,
				credits = credits,
				incrementValue = incrementValue
			};
			scatterSymbolsPerReel[reelId].Add(scatterMultiplierSymbolData);
		}
	}

	private void clearPayoutSymbolsDictionary()
	{
		if (scatterSymbolsPerReel == null)
		{
			return;
		}

		foreach (int key in scatterSymbolsPerReel.Keys)
		{
			scatterSymbolsPerReel[key].Clear();
		}
	}

	private void updateMeterMultiplierLabel(int value)
	{
		if (multiplierMeterLabel == null)
		{
			return;
		}

		multiplierMeterLabel.text = CommonText.formatNumber(value);
	}

#if UNITY_EDITOR
	public IEnumerator debugFreespinFinalSpinAnimation()
	{
		if (isDebuggingFreespinFinalSpinAnimation)
		{
			yield break;
		}
		
		isDebuggingFreespinFinalSpinAnimation = true;
		yield return StartCoroutine(doFreespinsFinalSpinAnimations(true));
		isDebuggingFreespinFinalSpinAnimation = false;
	}

	public void cleanupDebugFreespinFinalSpinSymbols()
	{
		if (!isDebuggingFreespinFinalSpinAnimation)
		{
			ReparentSymbolAndPlayAnimation.cleanupTestSymbols();
		}
	}
#endif
}
