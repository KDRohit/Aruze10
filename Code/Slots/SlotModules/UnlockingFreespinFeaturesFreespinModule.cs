using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class that presents unlocking features of a freespins game based on what tier the player's
 * bet qualified them for.  You will define the features that will be presented and then the
 * tiers that will unlock those features.  First used by elvis04.
 *
 * Creation Date: 9/10/2019
 * Original Author: Scott Lepthien
 */
public class UnlockingFreespinFeaturesFreespinModule : SlotModule 
{
	[System.Serializable]
	private class UnlockingFreespinTierData
	{
		[Tooltip("Tier number from the way the tier is setup on the server, this is used to auto build the name in the form: <gameKey>_freespin_<progressiveTierNumber>")]
		[SerializeField] private int featureTierNumber;
		[Tooltip("What features are unlocked at this tier in the order they will be presented.  How they will be presented will be defined in a UnlockingFreespinFeatureData.")]
		[SerializeField] private UnlockingFreespinFeatureData.FeatureTypeEnum[] featuresTypesUnlockedForThisTier;
		[Tooltip("Allows a short delay to be added between each feature unlocking presentation for this tier.")]
		[SerializeField] private float delayBetweenShowingFeatureEffects = 0.0f;

		private List<UnlockingFreespinFeatureData> featuresUnlockedForThisTier = new List<UnlockingFreespinFeatureData>();
		
		private string featureTierKey;
		public string getFeatureTierKeyName()
		{
			if (string.IsNullOrEmpty(featureTierKey))
			{
				// build the name using the progressiveTierNumber and the game key
				featureTierKey = GameState.game.keyName + "_freespin_" + featureTierNumber;
			}

			return featureTierKey;
		}

		public void init(UnlockingFreespinFeatureData[] featureDataList)
		{
			for (int i = 0; i < featuresTypesUnlockedForThisTier.Length; i++)
			{
				UnlockingFreespinFeatureData featureData = getFeatureDataForFeatureType(featuresTypesUnlockedForThisTier[i], featureDataList);
				if (featureData != null)
				{
					featuresUnlockedForThisTier.Add(featureData);
				}
			}
		}

		public IEnumerator playEffectsForEachFeature()
		{
			for (int i = 0; i < featuresUnlockedForThisTier.Count; i++)
			{
				yield return RoutineRunner.instance.StartCoroutine(featuresUnlockedForThisTier[i].playFeatureEffects());
				
				// Only add the stagger delay if this isn't the last feature being shown
				if (delayBetweenShowingFeatureEffects > 0.0f && i < featuresUnlockedForThisTier.Count - 1)
				{
					yield return new TIWaitForSeconds(delayBetweenShowingFeatureEffects);
				}
			}
		}

		// Adjust the spin meter to reflect changes to the number of spins that this tier will apply
		// with its features.
		public void adjustSpinMeter()
		{
			for (int i = 0; i < featuresUnlockedForThisTier.Count; i++)
			{
				featuresUnlockedForThisTier[i].decrementSpinsThatWillBeAwarded();
			}
		}
		
		// Swap all features that will need to swap over to the default reelstrip to the replacements
		// before the player sees the reels, that way it looks like they transform and get new stuff
		// on them when each feature is presented
		public void swapReelsToReplacementStrips()
		{
			for (int i = 0; i < featuresUnlockedForThisTier.Count; i++)
			{
				featuresUnlockedForThisTier[i].swapToReplacementReelStrips();
			}
		}
		
		// Set the unlocked features to display as unlocked when the game starts.
		// Will play the award animations when they are actually awarded
		public IEnumerator playUnlockFeaturesAnims()
		{
			List<TICoroutine> unlockAnimCoroutines = new List<TICoroutine>();
			
			for (int i = 0; i < featuresUnlockedForThisTier.Count; i++)
			{
				unlockAnimCoroutines.Add(RoutineRunner.instance.StartCoroutine(featuresUnlockedForThisTier[i].playUnlockFeatureAnims()));
			}

			if (unlockAnimCoroutines.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(unlockAnimCoroutines));
			}
		}

		public void handleSetupSymbol(SlotSymbol symbol)
		{
			for (int i = 0; i < featuresUnlockedForThisTier.Count; i++)
			{
				featuresUnlockedForThisTier[i].handleSymbolSetup(symbol);
			}
		}

		private static UnlockingFreespinFeatureData getFeatureDataForFeatureType(UnlockingFreespinFeatureData.FeatureTypeEnum featureType, UnlockingFreespinFeatureData[] featureDataList)
		{
			for (int i = 0; i < featureDataList.Length; i++)
			{
				UnlockingFreespinFeatureData currentFeature = featureDataList[i];

				if (currentFeature.featureType == featureType)
				{
					return currentFeature;
				}
			}
			
			Debug.LogError("UnlockingFreespinTierData.getFeatureDataForFeatureType() - Unable to find data for featureType = " + featureType + "; returning NULL!");
			return null;
		}
	}

	[System.Serializable]
	public class UnlockingFreespinFeatureData
	{
		public enum FeatureTypeEnum
		{
			Standard = 0,
			ExtraFreespins = 1,
			ExtraWilds = 2,
			Added2XWilds = 3
		}

		[Tooltip("The type of feature this is, used when determining the feature list that a tier in UnlockingFreespinTierData will unlock")]
		[SerializeField] public FeatureTypeEnum featureType;
		[Tooltip("Animations that get played when the freespins starts so that the features that should be unlocked for the bet amount already show up colored in")]
		[SerializeField] private AnimationListController.AnimationInformationList unlockFeatureAnims;
		[Tooltip("Animations that get played when this feature is awarded")]
		[SerializeField] private AnimationListController.AnimationInformationList awardFeatureAnims;
		[Tooltip("Particle effect played when this feature is awarded")]
		[SerializeField] private AnimatedParticleEffect awardFeatureParticleEffect;
		[Tooltip("Particle effect start location if awardFeatureParticleEffect is hooked up")]
		[SerializeField] private Transform particleEffectStartLocation;
		[Tooltip("Delay before the freespins are incremented after the Anims and particle effects are started, that will allow the increment to sync with the right timing for the effects")]
		[SerializeField] private float incrementFreespinsDelay = 0.0f;
		[Tooltip("Number of freespins that will be decremented off the starting amount and then awarded to the player during the effects part of revealing this feature")]
		[SerializeField] private int numberOfFreeSpinsAwarded;
		[Tooltip("Data about how initial reel strips will be replaced to hide added wild feature changes until they are presented.")]
		[SerializeField] private ReelStripReplacementData[] reelStripReplacements;
		[Tooltip("If the reels were swapped using reelStripReplacements this delay determines how long until they are swapped back to what they should be for this version of freespins. " +
				"Use this to sync the swap back with the animations which present the additional wilds appearing on the reels")]
		[SerializeField] private float restoreOriginalReelStripsDelay = 0.0f;

		[SerializeField] private string standardWildSymbolName = "WD";
		[SerializeField] private string twoTimesMultiplierWildSymbolName = "W2";

		private ReelGame reelGame;
		
		protected const string SPINS_ADDED_INCREMENT_SOUND_KEY = "freespin_spins_added_increment";

		public void init(ReelGame reelGame)
		{
			this.reelGame = reelGame;
		}

		// Adjust the spin meter to reflect changes to the number of spins that this tier will apply
		// with its features.
		public void decrementSpinsThatWillBeAwarded()
		{
			// If this is a feature that changes the spin count then go ahead and decrement the spins
			if (numberOfFreeSpinsAwarded > 0)
			{
				reelGame.numberOfFreespinsRemaining -= numberOfFreeSpinsAwarded;
			}
		}
		
		// Swaps out reels with strip replacements which will then be swapped back during the animations.
		// That way it can look like WD / 2X WD symbols are being added to the reels
		public void swapToReplacementReelStrips()
		{
			for (int i = 0; i < reelStripReplacements.Length; i++)
			{
				ReelStripReplacementData currentReplacementData = reelStripReplacements[i];
				currentReplacementData.swapToReplacementStrip(reelGame);

				if (featureType == FeatureTypeEnum.Added2XWilds)
				{
					// Need to make sure that we revert any 2X wild symbols which were already on the reels
					// before we did the replacement, so that you don't see 2X WD symbols that were part of the
					// initial reelset
					currentReplacementData.convert2XWildsToStandardWildsOnReel(reelGame, twoTimesMultiplierWildSymbolName, standardWildSymbolName);
				}
			}
		}

		public void handleSymbolSetup(SlotSymbol symbol)
		{
			if (featureType == FeatureTypeEnum.Added2XWilds)
			{
				if (symbol.serverName == twoTimesMultiplierWildSymbolName)
				{
					symbol.mutateTo(standardWildSymbolName, playVfx: false, skipAnimation: true);
				}
			}
		}

		public IEnumerator playUnlockFeatureAnims()
		{
			if (unlockFeatureAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(unlockFeatureAnims));
			}
		}

		public IEnumerator playFeatureEffects()
		{
			List<TICoroutine> effectsCoroutines = new List<TICoroutine>();
			if (awardFeatureAnims.Count > 0)
			{
				effectsCoroutines.Add(RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(awardFeatureAnims)));
			}

			if (awardFeatureParticleEffect != null && particleEffectStartLocation != null)
			{
				effectsCoroutines.Add(RoutineRunner.instance.StartCoroutine(awardFeatureParticleEffect.animateParticleEffect(particleEffectStartLocation)));
			}

			if (featureType == FeatureTypeEnum.ExtraFreespins && numberOfFreeSpinsAwarded > 0)
			{
				effectsCoroutines.Add(RoutineRunner.instance.StartCoroutine(incrementFreespinCountAfterDelay(numberOfFreeSpinsAwarded)));
			}
			
			if (reelStripReplacements.Length > 0)
			{
				effectsCoroutines.Add(RoutineRunner.instance.StartCoroutine(restoreOriginalReelStripsAfterDelay()));
			}

			if (effectsCoroutines.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(effectsCoroutines));
			}
		}

		private IEnumerator restoreOriginalReelStripsAfterDelay()
		{
			if (restoreOriginalReelStripsDelay > 0.0f)
			{
				yield return new TIWaitForSeconds(restoreOriginalReelStripsDelay);
			}
			
			for (int i = 0; i < reelStripReplacements.Length; i++)
			{
				reelStripReplacements[i].restoreOriginalReelStrip(reelGame);
			}
		}

		private IEnumerator incrementFreespinCountAfterDelay(int numberOfFreeSpins)
		{
			if (incrementFreespinsDelay > 0.0f)
			{
				yield return new TIWaitForSeconds(incrementFreespinsDelay);
			}

			incrementFreespinCount(numberOfFreeSpins);
		}

		private void incrementFreespinCount(int numberOfFreeSpins)
		{
			Audio.playSoundMapOrSoundKey(SPINS_ADDED_INCREMENT_SOUND_KEY);
			reelGame.numberOfFreespinsRemaining += numberOfFreeSpins;
		}
	}

	[System.Serializable]
	public class ReelStripReplacementData
	{
		[SerializeField] private int reelIndex;
		[Tooltip("The name of the strip that will be swapped in for the given reelIndex when the game starts and before the feature is awarded.  " +
				"Shouldn't include gamekey which will be auto added so final format will be: <game_key>_<stripReplacementNameWithoutGameKey>")]
		[SerializeField] private string stripReplacementNameWithoutGameKey;

		public void swapToReplacementStrip(ReelGame reelGame)
		{
			SlotReel reel = reelGame.engine.getSlotReelAt(reelIndex);
			if (reel != null && !string.IsNullOrEmpty(stripReplacementNameWithoutGameKey))
			{
				ReelStrip newStrip = ReelStrip.find(GameState.game.keyName + "_" + stripReplacementNameWithoutGameKey);
				if (newStrip != null)
				{
					reel.setReplacementStrip(newStrip);
				}
			}
		}

		public void restoreOriginalReelStrip(ReelGame reelGame)
		{
			SlotReel reel = reelGame.engine.getSlotReelAt(reelIndex);
			reel.setReplacementStrip(null);
		}

		public void convert2XWildsToStandardWildsOnReel(ReelGame reelGame, string doubleWdSymbolName, string standardWdSymbolName)
		{
			SlotReel reel = reelGame.engine.getSlotReelAt(reelIndex);
			for (int i = 0; i < reel.symbolList.Count; i++)
			{
				SlotSymbol currentSymbol = reel.symbolList[i];
				if (currentSymbol != null && currentSymbol.serverName == doubleWdSymbolName)
				{
					currentSymbol.mutateTo(standardWdSymbolName, playVfx: false, skipAnimation: true);
				}
			}
		}
	}

	[Tooltip("List of features that can be unlocked for each tier in featureTierList")]
	[SerializeField] private UnlockingFreespinFeatureData[] featureDataList;
	[Tooltip("A tier list that will use featureDataList to dynamically build a list of features that the tier will present as being unlocked")]
	[SerializeField] private UnlockingFreespinTierData[] featureTierList;

	private UnlockingFreespinTierData selectedFeatureTier = null;
	private bool areFeatureTierEffectsPlayed = false;
	private bool hasReachedSlotGameStarted = false;
	private bool isDataInited = false;
	
	private const string FORCE_RESPIN_CHEAT_TIER_ENDING = "_force_respin";
	
	// executeOnSlotGameStarted() section
	// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		hasReachedSlotGameStarted = true;

		// Extract the feature tier here if it hasn't happened yet
		extractSelectedFeatureTier();

		if (selectedFeatureTier != null)
		{
			// Go ahead and adjust the spin meter now so that the spin count will look right
			// before we award the extra spins
			selectedFeatureTier.adjustSpinMeter();
			
			// Also swap over to replacement strips for reels that will then swap back to the actual
			// strips for the current version of the game as the features are revealed
			selectedFeatureTier.swapReelsToReplacementStrips();

			// Put the unlocked features into their unlocked states
			yield return StartCoroutine(selectedFeatureTier.playUnlockFeaturesAnims());
		}
		
		yield break;
	}
	
	private void initData()
	{
		if (!isDataInited)
		{
			for (int i = 0; i < featureDataList.Length; i++)
			{
				featureDataList[i].init(reelGame);
			}

			for (int i = 0; i < featureTierList.Length; i++)
			{
				featureTierList[i].init(featureDataList);
			}

			isDataInited = true;
		}
	}

	private void extractSelectedFeatureTier()
	{
		initData();
		
		if (selectedFeatureTier == null)
		{
			FreeSpinGame freeSpinGame = reelGame as FreeSpinGame;
			string tierName = "";
			if (freeSpinGame != null)
			{
				tierName = freeSpinGame.freeSpinsOutcomes.getBonusGamePayTableName();
				//Debug.Log("UnlockingFreespinFeaturesFreespinModule.executeOnSlotGameStartedNoCoroutine() - tierName = " + tierName);
				selectedFeatureTier = getTierDataForFeatureTier(tierName);
			}

			if (selectedFeatureTier == null)
			{
				Debug.LogWarning("UnlockingFreespinFeaturesFreespinModule.executeOnSlotGameStartedNoCoroutine() - Unable to find tier data for tierName = " + tierName);
			}
		}
	}
	
	// executePreReelsStopSpinning() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels stop spinning (after the outcome has been set)
	public override bool needsToExecutePreReelsStopSpinning()
	{
		// This will only happen on the first spin
		return !areFeatureTierEffectsPlayed;
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		areFeatureTierEffectsPlayed = true;

		if (selectedFeatureTier != null)
		{
			yield return StartCoroutine(selectedFeatureTier.playEffectsForEachFeature());
		}
	}

	private UnlockingFreespinTierData getTierDataForFeatureTier(string tierName)
	{
		// Check if the name is from a cheat key and adjust the name.
		// This feels safer than doing a contains check which would be
		// more likely to accidentally be handled incorrectly.
		tierName = tierName.Replace(FORCE_RESPIN_CHEAT_TIER_ENDING, "");

		for (int i = 0; i < featureTierList.Length; i++)
		{
			UnlockingFreespinTierData currentTier = featureTierList[i];

			if (tierName == currentTier.getFeatureTierKeyName())
			{
				return currentTier;
			}
		}

		return null;
	}
	
	// executeAfterSymbolSetup() section
	// Functions in this section are called once a symbol has been setup.
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return !hasReachedSlotGameStarted;
	}
	
	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		extractSelectedFeatureTier();
		
		if (selectedFeatureTier != null)
		{
			selectedFeatureTier.handleSetupSymbol(symbol);
		}
	}
}
