using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for updating the current eligible jackpot based on the number of tally symbols collected. Supports personal and
 * progressive jackpot labels, but progressive jackpot initializing and granting is handled by BuiltInProgressiveFreespinModule.
 * First used by gen99 freespins game
 * Author: Caroline 4/2020
 */
public class SymbolCollectJackpotLadderModule : SlotModule
{
	[Serializable]
	public class JackpotLadderTierAnimationData
	{
		[Tooltip("Jackpot tier index, 0 is progressive, others are fixed")]
		public int tierId;

		[Tooltip("Current jackpot value label")]
		public LabelWrapperComponent jackpotAmountLabel;
		[Tooltip("Number of tally symbols needed to unlock this tier label")]
		public LabelWrapperComponent symbolsCountNeededToUnlockLabel;

		[Tooltip("Animations to play when enough tally symbols have been selected to qualify for this jackpot, should have an ambient hold state")]
		public AnimationListController.AnimationInformationList jackpotSelectedAmbientAnimations;
		[Tooltip("Animations to play when the next jackpot gets selected and this one is deselected")]
		public AnimationListController.AnimationInformationList jackpotDeselectedAnimations;
		[Tooltip("Animations to play after all upgrade animations complete if this tier was already selected, should have an ambient hold state")]
		public AnimationListController.AnimationInformationList jackpotRestoreSelectedStateAmbientAnimations;
		
		[Tooltip("Animations to play specific to this tier when upgrade symbol lands and increases the value of the jackpot, keyed by number of upgrades this round. This plays after the general upgrade animation")]
		public List<JackpotLadderUpgradeAnimationsForUpgradeCount> jackpotUpgradedAnimations;
		
		[Header("Fixed Jackpot Tier Animations")]
		[Tooltip("Animations to play when the jackpot is awarded")]
		public AnimationListController.AnimationInformationList jackpotWonIntroAnimations;
		[Tooltip("Animations to play when the jackpot rollup is complete")]
		public AnimationListController.AnimationInformationList jackpotWonOutroAnimations;

		[Tooltip("Start anchor for jackpot won particle trail")]
		public Transform jackpotWonParticleTrailStartPosition;

		[NonSerialized] public long currentJackpotValue;
		[NonSerialized] public bool isSelected;
	}

	[Serializable]
	public class JackpotLadderUpgradeAnimationsForUpgradeCount
	{
		public int upgradeCount;
		public AnimationListController.AnimationInformationList upgradeAnimations;
		[Tooltip("Delay after first upgrade value set for each subsequent upgrade label update")]
		public float updateLabelDelayPerUpgrade;
	}

	[Tooltip("Animation data for each fixed jackpot tier in the ladder")]
	[SerializeField] private List<JackpotLadderTierAnimationData> jackpotTierAnimationDataList;

	[Tooltip("General animation to play when upgrade symbol lands, eg pagoda spin in gen99. Keyed by number of upgrades this round to distinguish between single/double/etc upgrades")]
	[SerializeField] private List<JackpotLadderUpgradeAnimationsForUpgradeCount> upgradeJackpotsGeneralAnimationsByCount;

	[Tooltip("Delay when general upgrade animation kicks off before changing jackpot label values, only for first ugprade, for multiple upgrades use upgrade animation data")]
	[SerializeField] private float upgradeJackpotLabelsDelay = 1.0f;
	[Tooltip("Delay between reels when playing particle trail from tally symbols to counter")]
	[SerializeField] private float addSymbolsToTallyCounterDelayByReel = 0.3f;
	[Tooltip("Delay after symbol kicks off particle effect when playing particle trail from tally symbols to counter")]
	[SerializeField] private float incrementTallyCounterDelayBySymbol = 0.3f;

	[Tooltip("Label to track number of tally symbols landed")]
	[SerializeField] private LabelWrapperComponent symbolTallyLabel;

	[Tooltip("Particle trail from upgrade symbol to jackpot ladder")]
	[SerializeField] private AnimatedParticleEffect upgradeJackpotsParticleTrail;
	[Tooltip("Particle trail from tally symbol to counter label")]
	[SerializeField] private AnimatedParticleEffect incrementSymbolTallyParticleTrail;
	[Tooltip("Particle trail from qualifying jackpot to win box that plays when awarded")]
	[SerializeField] private AnimatedParticleEffect jackpotWonToWinBoxParticleTrail;
	
	#region TransformSymbolsFromTriggerSymbol
	[Header("Transform Symbols From Trigger Symbol (Optional)")]
	[Space(10)]
	[Tooltip("Delay after trigger symbol lands so idle animation can play, before transformations start")]
	[SerializeField] private float triggerSymbolPreTransformPlayIdleDelay = 0.5f;
	[Tooltip("Delay after trigger symbol reveals which symbol type will be transformed, before transformations start")]
	[SerializeField] private float triggerSymbolRevealPreTransformDelay = 0.5f;
	[Tooltip("Delay between reels when transforming symbols")]
	[SerializeField] private float transformingSymbolDelayByReel = 0.3f;
	[Tooltip("Delay between individual symbols when transforming symbols")]
	[SerializeField] private float transformingSymbolDelayBySymbol = 0.0f;
	
	[Tooltip("Mutation effect that gets instantiated over the transforming symbol before mutating")]
	[SerializeField] private GameObject transformingSymbolsEffectPrefab;
	[Tooltip("Mutation effect sound key for the intermediate display before mutating")]
	[SerializeField] private AudioListController.AudioInformationList transformingSymbolsEffectSounds;
	[Tooltip("Mutation effect sound key for the final mutation")]

	[SerializeField] private AudioListController.AudioInformationList finalMutationSounds;
	[Tooltip("Delay after mutation effect is instantiated before mutating the symbol underneath")]
	[SerializeField] private float transformingEffectMutateSymbolDelay = 0.0f;
	[Tooltip("Delay increment to wait for Post Trigger Symbol Transform Callback to finish")]
	
	private string mutateTriggerSymbolToSuffix = "_TW";
	private string mutateTriggerSymbolHoldSuffix = "_TW_Mutated";
	private StandardMutation transformingSymbolsMutation;

	private Dictionary<int, List<StandardMutation.ReplacementCell>> transformingSymbolsByReel = new Dictionary<int, List<StandardMutation.ReplacementCell>>();

	private List<TICoroutine> transformingSymbolsCoroutines = new List<TICoroutine>();
	
	private GameObjectCacher transformingEffectCacher;
	private List<GameObject> spawnedTransformingEffects = new List<GameObject>();
	#endregion

	// parsed data for each trigger symbol, keyed by reel
	private Dictionary<int, List<TriggerSymbolData>> triggerSymbolsData = new Dictionary<int, List<TriggerSymbolData>>();
	private Dictionary<int, JackpotTierModificationData> jackpotTierModifications = new Dictionary<int, JackpotTierModificationData>(); //kvp: tierId, data

	private const string modifierExportFreespinReevaluator = "{0}_freespin_reevaluator";

	private int currentTallySymbolCount; // total count of tally symbols collected
	private int jackpotUpgradeCount; // number of jackpot upgrades to apply this spin
	private int tallySymbolIncrementCount; // tally symbols collected this spin
	private List<JackpotTierRewardData> jackpotRewardDataList = new List<JackpotTierRewardData>(); // jackpots to award when freespin game ends, only for fixed jackpots

	private JSON reevaluatorData;

	// cached coroutine lists to avoid re-allocating constantly
	private List<TICoroutine> upgradeAnimationCoroutines = new List<TICoroutine>();
	private List<TICoroutine> restoreSelectedStateAnimationCoroutines = new List<TICoroutine>();
	private List<TICoroutine> upgradeSymbolAnimationCoroutines = new List<TICoroutine>();
	private List<TICoroutine> updateTallyCountAnimationCoroutines = new List<TICoroutine>();
	private List<TICoroutine> tallySymbolAnimationCoroutines = new List<TICoroutine>();

	private class TriggerSymbolData
	{
		public int reel;
		public int position;
		public int upgradeAmount; // how much this symbol upgrades the jackpots by (eg 1 level, 2 levels)
		public int tallyAmount; // how much this symbol contributes to the tally
	}

	private class JackpotTierModificationData
	{
		public int tierIndex;
		public long newJackpotValue;
		public bool isSelected;
		public int requiredSymbolCount;
	}

	private class JackpotTierRewardData
	{
		public int tierIndex;
		public long amountRewarded;
	}

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		// initialize jackpot ladder tiers
		if (SlotBaseGame.instance == null)
		{
			// games with progressive jackpots are not giftable, so we don't need to worry about gifted freespins case
			// thus we can assume the basegame must exist
			Debug.LogError("SlotBaseGame instance was null, probably was gifted freespins which shouldn't happen for game " + GameState.game.keyName);
			return;
		}

		JSON[] modifierExports = SlotBaseGame.instance.modifierExports;
		foreach (JSON modifierExport in modifierExports)
		{
			// handle fixed jackpots
			string key = string.Format(modifierExportFreespinReevaluator, GameState.game.keyName);
			JSON freespinReevaluator = modifierExport.getJSON(key);
			if (freespinReevaluator != null)
			{
				JSON[] jackpots = freespinReevaluator.getJsonArray("jackpots");
				foreach (JSON jackpotTierData in jackpots)
				{
					setupJackpotTierData(jackpotTierData);
				}
				// we found the reevaluator, no need to keep checking modifier exports
				break;
			}
		}

		currentTallySymbolCount = 0;
		symbolTallyLabel.text = CommonText.formatNumber(currentTallySymbolCount);
		
		if (transformingSymbolsEffectPrefab != null)
		{
			transformingEffectCacher = new GameObjectCacher(this.gameObject, transformingSymbolsEffectPrefab);
		}
	}
	
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		tallySymbolIncrementCount = 0;
		jackpotUpgradeCount = 0;
		reevaluatorData = null;
		restoreSelectedStateAnimationCoroutines.Clear();
		clearTriggerSymbolsDictionary();
		clearJackpotModificationDataDictionary();
		
		if(transformingSymbolsMutation != null)
		{
			transformingSymbolsMutation = null;
			if (transformingEffectCacher != null)
			{
				foreach (GameObject spawnedEffect in spawnedTransformingEffects)
				{
					transformingEffectCacher.releaseInstance(spawnedEffect);
				}
			}
			spawnedTransformingEffects.Clear();
		}
		
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		transformingSymbolsMutation = getTransformingSymbolsMutation();
		reevaluatorData = getJackpotModificationReevaluator();
		return reevaluatorData != null;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// convert JSON outcome into various data structures
		parseJackpotModificationData(reevaluatorData);

		yield return StartCoroutine(applyJackpotModificationData());
		yield return StartCoroutine(doTriggerSymbols());
		yield return StartCoroutine(tallySymbolIncrementEffects());
		
		// update selected state now that tally counter is updated
		foreach (JackpotLadderTierAnimationData jackpotTier in jackpotTierAnimationDataList)
		{
			if (!jackpotTierModifications.TryGetValue(jackpotTier.tierId, out JackpotTierModificationData modificationData))
			{
				continue;
			}
			
			if (modificationData.isSelected && !jackpotTier.isSelected)
			{
				jackpotTier.isSelected = true;
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotTier.jackpotSelectedAmbientAnimations));
			}
			else if (!modificationData.isSelected && jackpotTier.isSelected)
			{
				jackpotTier.isSelected = false;
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotTier.jackpotDeselectedAnimations));
			}
		}
	}

	#region TransformSymbolsFromTriggerSymbolModule
	private IEnumerator doTriggerSymbols()
	{
		//run the transform animations if necessary
		if (transformingSymbolsMutation != null &&
		    transformingSymbolsMutation.twTriggeredSymbolList != null &&
		    transformingSymbolsMutation.twTriggeredSymbolList.Count > 0 &&
		    transformingSymbolsMutation.leftRightWildMutateSymbolList != null &&
		    transformingSymbolsMutation.leftRightWildMutateSymbolList.Count > 0)
		{
			//set all the TW symbols to idle anim on land
			for (int i = 0; i < transformingSymbolsMutation.twTriggeredSymbolList.Count; i++)
			{
				yield return StartCoroutine(doTriggerSymbolIdleAnimation(transformingSymbolsMutation.twTriggeredSymbolList[i]));
			}

			if (triggerSymbolPreTransformPlayIdleDelay > 0)
			{
				yield return new TIWaitForSeconds(triggerSymbolPreTransformPlayIdleDelay);
			}
			
			for (int i = 0; i < transformingSymbolsMutation.twTriggeredSymbolList.Count; i++)
			{
				yield return StartCoroutine(doTriggerSymbolReveal(transformingSymbolsMutation.twTriggeredSymbolList[i]));

				if (triggerSymbolRevealPreTransformDelay > 0)
				{
					yield return new TIWaitForSeconds(triggerSymbolRevealPreTransformDelay);
				}

				yield return StartCoroutine(doTriggerSymbolTransformingSymbols(transformingSymbolsMutation.leftRightWildMutateSymbolList[i]));
			}
		}
	}
	
	private IEnumerator doTriggerSymbolIdleAnimation(StandardMutation.ReplacementCell triggerSymbol)
	{
		int reel = triggerSymbol.reelIndex;
		int position = triggerSymbol.symbolIndex;
		SlotReel slotReel = reelGame.engine.getSlotReelAt(reel);

		if (slotReel == null || position < 0 || position >= slotReel.visibleSymbols.Length)
		{
			yield break;
		}
		
		SlotSymbol symbol = slotReel.visibleSymbolsBottomUp[position];
		if (symbol != null)
		{
			//allow symbol to run its idle animation
			symbol.mutateTo(symbol.serverName);
			symbol.animator.playAnticipation(symbol);
		}
	}
	
	private IEnumerator doTriggerSymbolReveal(StandardMutation.ReplacementCell triggerSymbol)
	{
		int reel = triggerSymbol.reelIndex;
		int position = triggerSymbol.symbolIndex;
		string toSymbol = triggerSymbol.replaceSymbol;
		SlotReel slotReel = reelGame.engine.getSlotReelAt(reel);
		if (slotReel != null && position >= 0 && position < slotReel.visibleSymbols.Length)
		{
			SlotSymbol symbol = slotReel.visibleSymbolsBottomUp[position];
			if (symbol != null)
			{		
				// transform to trigger symbol variant to do reveal animation
				symbol.mutateTo(toSymbol + mutateTriggerSymbolToSuffix);
				// play reveal
				yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());

				if (finalMutationSounds.Count > 0)
				{
					StartCoroutine(AudioListController.playListOfAudioInformation(finalMutationSounds));
				}
				symbol.mutateTo(toSymbol + mutateTriggerSymbolHoldSuffix);
			}
		}
		else
		{
			Debug.LogError("Couldn't find trigger symbol " + triggerSymbol.replaceSymbol);
		}
	}

	private IEnumerator doTriggerSymbolTransformingSymbols(List<StandardMutation.ReplacementCell> transformingSymbols)
	{
		parseTransformingSymbolListByReel(transformingSymbols);
		transformingSymbolsCoroutines.Clear();

		foreach (SlotReel slotReel in reelGame.engine.getAllSlotReels())
		{
			if (transformingSymbolsByReel.ContainsKey(slotReel.reelID - 1))
			{
				foreach (StandardMutation.ReplacementCell transformingSymbol in transformingSymbolsByReel[slotReel.reelID - 1])
				{
					int position = transformingSymbol.symbolIndex;
					string toSymbol = transformingSymbol.replaceSymbol;
					if (position >= 0 && position < slotReel.visibleSymbols.Length)
					{
						SlotSymbol symbol = slotReel.visibleSymbolsBottomUp[position];
						if (symbol != null)
						{
							transformingSymbolsCoroutines.Add(StartCoroutine(doTargetSymbolMutation(symbol, toSymbol)));
							if (transformingSymbolDelayBySymbol > 0)
							{
								yield return new TIWaitForSeconds(transformingSymbolDelayBySymbol);
							}
						}
					}
					else
					{
						Debug.LogError("Invalid transforming symbol data for symbol at " + slotReel.reelID + "," + position);
					}
				}
				if (transformingSymbolDelayByReel > 0)
				{
					yield return new TIWaitForSeconds(transformingSymbolDelayByReel);
				}
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(transformingSymbolsCoroutines));
	}

	private IEnumerator doTargetSymbolMutation(SlotSymbol symbol, string toSymbol)
	{
		// start effect, delay, then mutate
		if (transformingEffectCacher != null)
		{
			GameObject transformingEffectInstance = transformingEffectCacher.getInstance();
			transformingEffectInstance.transform.SetParent(symbol.reel.getReelGameObject().transform);
			transformingEffectInstance.transform.position = symbol.getSymbolWorldPosition();
			transformingEffectInstance.transform.localScale = symbol.transform.localScale;

			transformingEffectInstance.SetActive(true);
			
			if (transformingSymbolsEffectSounds.Count > 0)
			{
				StartCoroutine(AudioListController.playListOfAudioInformation(transformingSymbolsEffectSounds));
			}
			
			spawnedTransformingEffects.Add(transformingEffectInstance);
		}

		if (transformingEffectMutateSymbolDelay > 0)
		{
			yield return new TIWaitForSeconds(transformingEffectMutateSymbolDelay);
		}
		 
		symbol.mutateTo(toSymbol);
	}

	private StandardMutation getTransformingSymbolsMutation()
	{
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null &&
		    reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation currentMutation = baseMutation as StandardMutation;
				if (currentMutation != null && currentMutation.type == "trigger_pick_replace_multi")
				{
					return currentMutation;
				}
			}
		}

		return null;
	}

	private void parseTransformingSymbolListByReel(List<StandardMutation.ReplacementCell> transformingSymbolList)
	{
		transformingSymbolsByReel.Clear();
		
		foreach (StandardMutation.ReplacementCell transformingSymbol in transformingSymbolList)
		{
			if (!transformingSymbolsByReel.ContainsKey(transformingSymbol.reelIndex))
			{
				transformingSymbolsByReel[transformingSymbol.reelIndex] = new List<StandardMutation.ReplacementCell>();
			}
			transformingSymbolsByReel[transformingSymbol.reelIndex].Add(transformingSymbol);
		}
	}
	
	#endregion

	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return jackpotRewardDataList.Count > 0;
	}

	public override IEnumerator executeOnFreespinGameEnd()
	{
		yield return StartCoroutine(grantJackpots());
	}

	private IEnumerator applyJackpotModificationData()
	{
		// if there are upgrades, do upgrade tiers animations, update label values on a delay so we can time with spinning anim for gen99
		if (jackpotUpgradeCount > 0)
		{
			yield return StartCoroutine(upgradeJackpotTiers(jackpotUpgradeCount));
		}
	}
	
	// update tally counter
	private IEnumerator tallySymbolIncrementEffects()
	{
		if (tallySymbolIncrementCount > 0)
		{
			yield return StartCoroutine(updateTallyCountAndDoSymbolEffects());
		}
	}

	private IEnumerator updateTallyCountAndDoSymbolEffects()
	{
		updateTallyCountAnimationCoroutines.Clear();

		// get upgrade symbols and play animations on those
		SlotReel[] reels = reelGame.engine.getAllSlotReels();
		foreach (SlotReel reel in reels)
		{
			if (triggerSymbolsData.ContainsKey(reel.reelID - 1))
			{
				updateTallyCountAnimationCoroutines.Add(StartCoroutine(playAnimationsForTallySymbolsOnReel(reel, triggerSymbolsData[reel.reelID - 1])));
				yield return new TIWaitForSeconds(addSymbolsToTallyCounterDelayByReel);
			}
		}
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(updateTallyCountAnimationCoroutines));
	}

	private IEnumerator upgradeJackpotTiers(int upgradeCount)
	{
		upgradeAnimationCoroutines.Clear();
		// play general upgrade animation (eg spinning pagoda for gen99), distinguish animations by upgrade count
		JackpotLadderUpgradeAnimationsForUpgradeCount upgradeAnimations = getUpgradeAnimationsForUpgradeCount(upgradeJackpotsGeneralAnimationsByCount, upgradeCount);
		if (upgradeAnimations != null)
		{
			upgradeAnimationCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(upgradeAnimations.upgradeAnimations)));
		}

		// get upgrade symbols and play animations on those
		SlotReel[] reels = reelGame.engine.getAllSlotReels();
		foreach (SlotReel reel in reels)
		{
			if (triggerSymbolsData.ContainsKey(reel.reelID - 1))
			{
				upgradeAnimationCoroutines.Add(StartCoroutine(playAnimationsForUpgradeSymbolsOnReel(reel, triggerSymbolsData[reel.reelID - 1])));
			}
		}

		// update values on the jackpot tiers and play animations
		foreach (JackpotLadderTierAnimationData jackpotTier in jackpotTierAnimationDataList)
		{
			if (jackpotTierModifications.ContainsKey(jackpotTier.tierId))
			{
				JackpotTierModificationData modificationData = jackpotTierModifications[jackpotTier.tierId];
				if (jackpotTier.currentJackpotValue != modificationData.newJackpotValue)
				{
					upgradeAnimationCoroutines.Add(StartCoroutine(upgradeJackpotLabelsForUpgradeCount(upgradeCount, jackpotTier.currentJackpotValue, modificationData.newJackpotValue, upgradeAnimations, jackpotTier)));
					jackpotTier.currentJackpotValue = modificationData.newJackpotValue;
				}
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(upgradeAnimationCoroutines));
		
		// restore jackpot tier states, eg for gen99 we disable selected states while pagoda is spinning and restore them after
		// should have less fanfare than when the tier is initially selected so we use a different animation hook
		foreach (JackpotLadderTierAnimationData jackpotTier in jackpotTierAnimationDataList)
		{
			if (jackpotTier.isSelected)
			{
				restoreSelectedStateAnimationCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotTier.jackpotRestoreSelectedStateAmbientAnimations)));
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(restoreSelectedStateAnimationCoroutines));
	}

	// play specific upgrade animation based on upgrade count, and update label n times with some delay
	private IEnumerator upgradeJackpotLabelsForUpgradeCount(int upgradeCount, long currentValue, long finalValue, JackpotLadderUpgradeAnimationsForUpgradeCount upgradeAnimationData, JackpotLadderTierAnimationData jackpotTier)
	{
		List<long> intermediateValues = getIntermediateUpgradeValues(currentValue, finalValue, upgradeCount);
		
		// start upgrade animation
		TICoroutine upgradeAnimation = null;
		if (upgradeAnimationData != null)
		{
			upgradeAnimation = StartCoroutine(AnimationListController.playListOfAnimationInformation(upgradeAnimationData.upgradeAnimations));
		}

		// initial delay before updating labels
		if (upgradeJackpotLabelsDelay > 0)
		{
			yield return new TIWaitForSeconds(upgradeJackpotLabelsDelay);
		}
		
		for (int i = 0; i < intermediateValues.Count; i++)
		{
			if (upgradeAnimationData != null)
			{
				updateJackpotTierLabel(jackpotTier, intermediateValues[i]);
				if (upgradeAnimationData.updateLabelDelayPerUpgrade > 0)
				{
					yield return new TIWaitForSeconds(upgradeAnimationData.updateLabelDelayPerUpgrade);
				}
			}
		}

		if (upgradeAnimation != null)
		{
			yield return upgradeAnimation;
		}
	}

	private void updateJackpotTierLabel(JackpotLadderTierAnimationData jackpotTier, long value)
	{
		if (jackpotTier.tierId == 0)
		{
			// progressive jackpot shouldn't be multiplied by reelgame multiplier
			jackpotTier.jackpotAmountLabel.text = CreditsEconomy.convertCredits(value);
		}
		else
		{
			jackpotTier.jackpotAmountLabel.text = CreditsEconomy.convertCredits(value * reelGame.multiplier);
		}

	}

	private IEnumerator playAnimationsForUpgradeSymbolsOnReel(SlotReel reel, List<TriggerSymbolData> triggerSymbols)
	{
		upgradeSymbolAnimationCoroutines.Clear();
		foreach (TriggerSymbolData triggerSymbol in triggerSymbols)
		{
			if (triggerSymbol.upgradeAmount > 0 && triggerSymbol.position > 0 && triggerSymbol.position < reel.visibleSymbols.Length)
			{
				SlotSymbol upgradeSymbol = reel.visibleSymbolsBottomUp[triggerSymbol.position];
				if (upgradeSymbol != null)
				{
					// make sure we don't cut off anticipation animation
					while (upgradeSymbol.isAnimatorDoingSomething)
					{
						yield return null;
					}
					upgradeSymbolAnimationCoroutines.Add(StartCoroutine(upgradeSymbol.playAndWaitForAnimateOutcome()));
					if (upgradeJackpotsParticleTrail != null)
					{
						upgradeSymbolAnimationCoroutines.Add(StartCoroutine(upgradeJackpotsParticleTrail.animateParticleEffect(upgradeSymbol.transform)));
					}
				}
			}

			yield return StartCoroutine(Common.waitForCoroutinesToEnd(upgradeSymbolAnimationCoroutines));
		}
	}

	private IEnumerator playAnimationsForTallySymbolsOnReel(SlotReel reel, List<TriggerSymbolData> triggerSymbols)
	{
		tallySymbolAnimationCoroutines.Clear();
		foreach (TriggerSymbolData triggerSymbol in triggerSymbols)
		{
			if (triggerSymbol.tallyAmount > 0 && triggerSymbol.position >= 0 && triggerSymbol.position < reel.visibleSymbols.Length)
			{
				SlotSymbol tallySymbol = reel.visibleSymbolsBottomUp[triggerSymbol.position];
				if (tallySymbol != null)
				{
					tallySymbolAnimationCoroutines.Add(StartCoroutine(tallySymbol.playAndWaitForAnimateOutcome()));
				}
			}
		}
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(tallySymbolAnimationCoroutines));
		
		tallySymbolAnimationCoroutines.Clear();
		foreach (TriggerSymbolData triggerSymbol in triggerSymbols)
		{
			if (triggerSymbol.tallyAmount > 0 && triggerSymbol.position >= 0 && triggerSymbol.position < reel.visibleSymbols.Length)
			{
				SlotSymbol tallySymbol = reel.visibleSymbolsBottomUp[triggerSymbol.position];
				if (tallySymbol != null)
				{
					tallySymbolAnimationCoroutines.Add(StartCoroutine(playIncrementTallyCounterParticleTrailAndUpdateLabel(tallySymbol, triggerSymbol.tallyAmount)));
				}
			}
		}
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(tallySymbolAnimationCoroutines));
	}

	private IEnumerator playIncrementTallyCounterParticleTrailAndUpdateLabel(SlotSymbol tallySymbol, int tallyAmount)
	{
		if (incrementSymbolTallyParticleTrail != null)
		{
			yield return StartCoroutine(incrementSymbolTallyParticleTrail.animateParticleEffect(tallySymbol.transform));
			currentTallySymbolCount += tallyAmount;
			symbolTallyLabel.text = CommonText.formatNumber(currentTallySymbolCount);
		}
	}

	private IEnumerator grantJackpots()
	{
		foreach (JackpotTierRewardData rewardData in jackpotRewardDataList)
		{
			JackpotLadderTierAnimationData jackpotTier = getJackpotTierAnimationData(rewardData.tierIndex);
			if (jackpotTier != null)
			{
				// play reward animation and begin rollup
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotTier.jackpotWonIntroAnimations));
				if (jackpotWonToWinBoxParticleTrail != null)
				{
					yield return StartCoroutine(jackpotWonToWinBoxParticleTrail.animateParticleEffect(jackpotTier.jackpotWonParticleTrailStartPosition));
				}
				yield return StartCoroutine(reelGame.rollupCredits(0, rewardData.amountRewarded * reelGame.multiplier, ReelGame.activeGame.onPayoutRollup, true, allowBigWin: false));
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotTier.jackpotWonOutroAnimations));
			}
		}
		jackpotRewardDataList.Clear();
	}

	private JackpotLadderTierAnimationData getJackpotTierAnimationData(int tierId)
	{
		foreach (JackpotLadderTierAnimationData jackpotTier in jackpotTierAnimationDataList)
		{
			if (jackpotTier.tierId == tierId)
			{
				return jackpotTier;
			}
		}

		return null;
	}

	private JSON getJackpotModificationReevaluator()
	{
		foreach (JSON reevaluator in reelGame.outcome.getArrayReevaluations())
		{
			if (reevaluator.getString("type", "") == "jackpot_modification")
			{
				return reevaluator;
			}
		}

		return null;
	}

	private void parseJackpotModificationData(JSON jackpotModificationReevaluator)
	{
		// parse trigger symbols
		triggerSymbolsData.Clear();
		JSON[] triggerSymbols = jackpotModificationReevaluator.getJsonArray("trigger_symbols");
		if (triggerSymbols != null)
		{
			foreach (JSON triggerSymbolJson in triggerSymbols)
			{
				int reelId = triggerSymbolJson.getInt("reel", -1);
				int pos = triggerSymbolJson.getInt("position", -1);
				int tally = triggerSymbolJson.getInt("wild", 0);
				int upgrade = triggerSymbolJson.getInt("upgrade", 0);
				if (reelId >= 0 && pos >= 0)
				{
					tallySymbolIncrementCount += tally;

					TriggerSymbolData triggerSymbol = new TriggerSymbolData
					{
						reel = reelId,
						position = pos,
						tallyAmount = tally,
						upgradeAmount = upgrade
					};

					if (!triggerSymbolsData.ContainsKey(reelId))
					{
						triggerSymbolsData[reelId] = new List<TriggerSymbolData>();
					}

					triggerSymbolsData[reelId].Add(triggerSymbol);
				}
				else
				{
					Debug.LogError("Invalid trigger symbol position data");
				}
			}
		}

		jackpotUpgradeCount = jackpotModificationReevaluator.getInt("upgrade", 0);

		// parse jackpot modifications
		JSON[] jackpots = jackpotModificationReevaluator.getJsonArray("jackpots");
		foreach (JSON jackpotUpgradeData in jackpots)
		{
			int index = jackpotUpgradeData.getInt("index", -1);
			long newAmount = jackpotUpgradeData.getLong("amount", 0);
			bool isSelected = jackpotUpgradeData.getBool("selected", false);
			int requiredCount = jackpotUpgradeData.getInt("required", 0);

			if (index >= 0)
			{
				JackpotTierModificationData jackpotTierModification = new JackpotTierModificationData
				{
					tierIndex = index,
					newJackpotValue = newAmount,
					isSelected = isSelected,
					requiredSymbolCount = requiredCount
				};
				jackpotTierModifications[index] = jackpotTierModification;
			}
			else
			{
				Debug.LogError("Missing or invalid jackpot tier index for modification info");
			}
		}

		// parse reward info
		JSON rewardJson = jackpotModificationReevaluator.getJSON("reward");
		if (rewardJson != null)
		{
			string type = rewardJson.getString("type", "");
			int index = rewardJson.getInt("index", -1);
			long amount = rewardJson.getLong("amount", 0);
			if (index >= 0)
			{
				// fixed jackpots only, progressive jackpot handled by BuiltInProgressiveFreespinsModule
				if (type == "fixed_jackpot")
				{
					if (amount > 0 && index > 0)
					{
						jackpotRewardDataList.Add(new JackpotTierRewardData
						{
							tierIndex = index,
							amountRewarded = amount
						});
					}
				}
			}
			else
			{
				Debug.LogError("Missing or invalid jackpot tier index for reward info");
			}
		}
	}

	private void setupJackpotTierData(JSON tierData)
	{
		// initialization stuff for fixed and progressive jackpots
		int index = tierData.getInt("index", -1);
		long fixedValue = tierData.getLong("amount", 0);
		int counterValueToUnlock = tierData.getInt("required", 0);
		if (index >= 0)
		{
			JackpotLadderTierAnimationData jackpotTier = getJackpotTierAnimationData(index);
			if (jackpotTier != null)
			{
				jackpotTier.symbolsCountNeededToUnlockLabel.text = CommonText.formatNumber(counterValueToUnlock);
				
				// populate amount values for fixed jackpots, progressive values get updated automatically
				if (fixedValue > 0)
				{
					jackpotTier.currentJackpotValue = fixedValue;
					jackpotTier.jackpotAmountLabel.text = CreditsEconomy.convertCredits(fixedValue * reelGame.multiplier);
				}
				else
				{
					// is progressive, populate from progressive jackpot data
					ProgressiveJackpot progressiveJackpot = getProgressiveJackpotFromBasegame();
					if (progressiveJackpot != null)
					{
						jackpotTier.currentJackpotValue = progressiveJackpot.pool;
						jackpotTier.jackpotAmountLabel.text = CreditsEconomy.convertCredits(jackpotTier.currentJackpotValue);
					}
				}
			}
			else
			{
				Debug.LogError("Failed to find jackpot tier animation data for index " + index);
			}
		}
		else
		{
			Debug.LogError("Missing or invalid jackpot tier index when setting up jackpots");
		}
	}

	private void clearTriggerSymbolsDictionary()
	{
		foreach (int key in triggerSymbolsData.Keys)
		{
			triggerSymbolsData[key].Clear();
		}
	}

	private void clearJackpotModificationDataDictionary()
	{
		jackpotTierModifications.Clear();
	}

	private ProgressiveJackpot getProgressiveJackpotFromBasegame()
	{
		string key = null;
		for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
		{
			BuiltInProgressiveJackpotBaseGameModule module = SlotBaseGame.instance.cachedAttachedSlotModules[i] as BuiltInProgressiveJackpotBaseGameModule;
			if (module != null)
			{
				key = module.getCurrentJackpotTierKey();
				break;
			}
		}

		if (!string.IsNullOrEmpty(key))
		{
			return ProgressiveJackpot.find(key);
		}

		return null;
	}

	private JackpotLadderUpgradeAnimationsForUpgradeCount getUpgradeAnimationsForUpgradeCount(List<JackpotLadderUpgradeAnimationsForUpgradeCount> upgradeAnimationsByCount, int upgradeCount)
	{
		foreach (JackpotLadderUpgradeAnimationsForUpgradeCount upgradeAnimationsForCount in upgradeAnimationsByCount)
		{
			if (upgradeAnimationsForCount.upgradeCount == upgradeCount)
			{
				return upgradeAnimationsForCount;
			}
		}

		return null;
	}

	private List<long> getIntermediateUpgradeValues(long initialValue, long finalValue, int numberOfUpgrades)
	{
		List<long> intermediateValues = new List<long>();
		long stepSize = (finalValue - initialValue) / numberOfUpgrades;
		long intermediateValue = initialValue;
		for (int i = 0; i < numberOfUpgrades; i++)
		{
			intermediateValue += stepSize;
			intermediateValues.Add(intermediateValue);
		}
		
		// ensure final value is always correct in case of rounding errors
		intermediateValues[intermediateValues.Count - 1] = finalValue;
		
		return intermediateValues;
	}
}