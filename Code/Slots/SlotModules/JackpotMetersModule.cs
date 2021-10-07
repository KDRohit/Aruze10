using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for handling fixed value jackpots that are awarded via some mechanism - initially pip counts that are part of
 * SC symbol data set in a mutation (non_persistent_jackpot_meter) and this awards credits from the jackpots 
 *
 * Author : Shaun Peoples <speoples@zynga.com>
 * First Use : Orig001
 */
public class JackpotMetersModule :  SlotModule
{
	enum JackpotType
	{
		Fixed,
		Progressive
	}
	
	//Used to ensure that the labels, animations, and jackpot data are properly tied together without the hassle of needing to index a bunch of seperate lists
	[System.Serializable]
	private class JackpotContainer
	{
		//We will want a way to match mutation data since the jackpots may not come down in order or with numerically parsable keys in the future
		public string keyName = "";
		public JackpotType jackpotType;
		public List<JackpotTier> jackpotTiers = new List<JackpotTier>();

		[System.Serializable]
		public class JackpotTier
		{
			public string jackpotTierKey;
			public string associatedSymbolServerName;
			//Related animations for the jackpot ui elements
			public AnimationListController.AnimationInformation activatedAnimationState;
			public AnimationListController.AnimationInformationList winIntroAnimations;
			public AnimationListController.AnimationInformationList winOutroAnimations;
			public AnimationListController.AnimationInformationList deselectedAnimations;
			public AnimationListController.AnimationInformationList pipProgressAnimations;
			public LabelWrapperComponent jackpotLabel;
			
			public bool isAbbreviatingJackpotLabel;
			public string pipOnAnimationName;
			public string pipOffAnimationName;
			public string pipAquiredAnimationName;

			[System.NonSerialized] public int lastPips;
			[System.NonSerialized] public int requiredPips;
			[System.NonSerialized] public int currentPips;
			[System.NonSerialized] public long credits;
			[System.NonSerialized] public bool wasAwarded = false;
			[System.NonSerialized] public List<SlotSymbol> triggeringSymbols = new List<SlotSymbol>();
		}
	}
	
	[SerializeField] private List<JackpotContainer> jackpotContainers = new List<JackpotContainer>();
	[SerializeField] private List<AudioListController.AudioInformationList> jackpotSymbolLandingSounds = new List<AudioListController.AudioInformationList>();
	[SerializeField] private AudioListController.AudioInformationList jackpotSymbolWinSounds = new AudioListController.AudioInformationList();
	
	[Tooltip("Particle trail from a symbol to the multiplier meter")]
	[SerializeField] private AnimatedParticleEffect symbolToJackpotBoxAnimatedParticleEffect;

	[SerializeField] private bool animateScatterOnRollbackEnd;
	[SerializeField] private float preWinAnimationsDelay = 0.5f;
	[SerializeField] private bool shouldLoopSymbolAnimationsOnRollup = false;
	private bool symbolsDonePlaying = false;

	//The tracked mutation
	private StandardMutation jackpotMutation = null;
	private JackpotContainer.JackpotTier activePJPTier;
	
	private int numberOfJackpotSymbolsThisSpin = 0;
	private bool hasRollupFinished = false;
	private bool isRollingUp = false;
	private bool isDataInitalized = false;
	[SerializeField] private float postRollupWait;

	protected override void OnEnable()
	{
		base.OnEnable();
		resetAnimationStates();
	}

	private void resetAnimationStates()
	{
		foreach (JackpotContainer jackpot in jackpotContainers)
		{
			foreach(JackpotContainer.JackpotTier jackpotTier in jackpot.jackpotTiers)
			{
				StartCoroutine(jackpotTier.wasAwarded
					? AnimationListController.playAnimationInformation(jackpotTier.activatedAnimationState)
					: AnimationListController.playListOfAnimationInformation(jackpotTier.deselectedAnimations));
				
				jackpotTier.triggeringSymbols.Clear();
				jackpotTier.lastPips = 0;
			}
		}
	}

	// When the game first starts, we get the saved user data from the server from the modifier_exports
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		bool shouldExecute = reelGame.modifierExports != null;

		if (!shouldExecute)
		{
			Debug.LogWarning("There are no applicable modifier exports in reelgame for JackpotMetersModule");
		}
		return shouldExecute;
	}

	// Get the players startup data so that we can initialize the free spin meters
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		JSON[] jackpotModifiersExportJson = getJackpotModifiersExportJson();

		if (jackpotModifiersExportJson == null)
		{
			yield break;
		}

		foreach (JSON jackpotJson in jackpotModifiersExportJson)
		{
			string keyName = jackpotJson.getString("key_name", System.String.Empty);

			JackpotContainer jackpotContainer = getJackpotContainerWithKey(keyName);

			if (jackpotContainer == null)
			{
				Debug.LogWarning("jackpotContainer is null for keyName:" + keyName);
				continue;
			}

			yield return StartCoroutine(initializeJackpotContainer(jackpotContainer, jackpotJson));
		}

		isDataInitalized = true;
	}

	// Fixed Jackpot is buried in a with a key that varies by game, but we can find it
	// using the type within the JSON.
	private JSON[] getJackpotModifiersExportJson()
	{
		List<JSON> returnValues = new List<JSON>();
		foreach (JSON modifierExportJSON in reelGame.modifierExports)
		{
			List<string> modifierExportKeys = modifierExportJSON.getKeyList();

			foreach (string key in modifierExportKeys)
			{
				if (string.IsNullOrEmpty(key))
				{
					continue;
				}
				
				JSON[] modifierExportContentJsonArray = modifierExportJSON.getJsonArray(key);

				if (modifierExportContentJsonArray == null)
				{
					continue;
				}
					
				foreach (JSON modifier in modifierExportContentJsonArray)
				{
					//type is currently limited to "fixed" or "pjp"
					string type = modifier.getString("type", ""); 
					if (type == "fixed" || type == "pjp")
					{
						returnValues.Add(modifier);
					}
				}
			}
		}

		return returnValues.ToArray();
	}
	
	private IEnumerator initializeJackpotContainer(JackpotContainer jackpotContainer, JSON jackpotJSON)
	{
		//select the active tiers:
		//if it's a PJP then just get the current progressive jackpot key from the base game
		//otherwise, grab all the tiers associated with the fixed jackpot - at this point, will be 1 tier per fixed jackpot
		activePJPTier = getCurrentProgressiveJackpotTier(jackpotContainer);

		List<JackpotContainer.JackpotTier> activeTiers = (activePJPTier != null) ? new List<JackpotContainer.JackpotTier>(){activePJPTier} : jackpotContainer.jackpotTiers;

		foreach(JackpotContainer.JackpotTier jackpotTier in activeTiers)
		{
			jackpotTier.requiredPips = int.Parse(jackpotJSON.getString("required_pips", ""));

			//this isn't visible in the non-freespins game, but the data appears in the freespins reevaluations
			if (jackpotJSON.hasKey("current_meter"))
			{
				jackpotTier.currentPips = jackpotJSON.getInt("current_meter", 0);
			}
			jackpotTier.credits = jackpotJSON.getLong("credits", 0);

			
			if (jackpotTier.jackpotLabel != null && jackpotContainer.jackpotType != JackpotType.Progressive)
			{
				//From the design doc:
				//JACKPOT DISPLAY = (JACKPOT BASE VALUE x WAGER MULTIPLIER) + CONTRIBUTION BUCKET
				//this also needed the economy multiplier attached to it
				jackpotTier.jackpotLabel.text = getJackpotContainerLabel(jackpotTier);
			}

			if (jackpotTier.pipProgressAnimations == null)
			{
				yield break;
			}

			for (int i = 0; i < jackpotTier.currentPips; ++i)
			{
				AnimationListController.playAnimationInformation(jackpotTier.pipProgressAnimations.animInfoList[i]);
			}
		}
	}

	private JackpotContainer.JackpotTier getCurrentProgressiveJackpotTier(JackpotContainer jackpotContainer)
	{
		JackpotContainer.JackpotTier currentTier = null;

		if (jackpotContainer.jackpotType != JackpotType.Progressive)
		{
			return null;
		}
		
		string pjpJackpotKey = getCurrentProgressiveJackpotKey();

		if (string.IsNullOrEmpty(pjpJackpotKey))
		{
			return null;
		}
			
		foreach (JackpotContainer.JackpotTier jackpotTier in jackpotContainer.jackpotTiers)
		{
			if (jackpotTier.jackpotTierKey == pjpJackpotKey)
			{
				currentTier = jackpotTier;
				break;
			}
		}
		return currentTier;
	}

	private string getCurrentProgressiveJackpotKey()
	{
		string progJackpotKey = "";
		foreach (SlotModule cacheduleModule in SlotBaseGame.instance.cachedAttachedSlotModules)
		{
			BuiltInProgressiveJackpotBaseGameModule module = cacheduleModule as BuiltInProgressiveJackpotBaseGameModule;
			if (module != null)
			{
				progJackpotKey = module.getCurrentJackpotTierKey();
			}
		}

		return ProgressiveJackpot.find(progJackpotKey).keyName;
	}

	private JackpotContainer getJackpotContainerWithKey(string jackpotKey)
	{
		foreach (JackpotContainer jackpotContainer in jackpotContainers)
		{
			//this just gets the key from the jackpot_meters entry, not the same as the jackpot name in 
			//progressives
			if (jackpotContainer.keyName == jackpotKey)
			{
				return jackpotContainer;
			}
		}
		return null;
	}

	//Update the jackpot ui when coming back from a freespins game
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}
	
	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		if (!reelGame.isFreeSpinGame())
		{
			return;
		}

		if (SlotBaseGame.instance != null)
		{
			copyValuesFromBasegame();
		}
		else
		{
			getValuesForGiftedFreespinsGame();
		}

		//Update the jackpot ui items
		foreach (JackpotContainer jackpot in jackpotContainers)
		{
			foreach (JackpotContainer.JackpotTier jackpotTier in jackpot.jackpotTiers)
			{
				if (jackpotTier.jackpotLabel != null && jackpot.jackpotType != JackpotType.Progressive)
				{
					jackpotTier.jackpotLabel.text = getJackpotContainerLabel(jackpotTier);
				}
			}
		}
	}

	//Update the active jackpots on wager change
	public override bool needsToExecuteOnWagerChange(long currentWager)
	{
		return true;
	}
	
	public override void executeOnWagerChange(long currentWager)
	{
		if (!isDataInitalized)
		{
			return;
		}

		foreach (JackpotContainer jackpot in jackpotContainers)
		{
			foreach (JackpotContainer.JackpotTier jackpotTier in jackpot.jackpotTiers)
			{
				if (jackpot.jackpotType != JackpotType.Progressive)
				{
					jackpotTier.jackpotLabel.text = getJackpotContainerLabel(jackpotTier);
				}
			}
		}
	}

	//Update the Jackpot balances once we get the spin outcome back
	public override bool needsToExecutePreReelsStopSpinning()
	{
		if (reelGame.mutationManager == null || reelGame.mutationManager.mutations == null || reelGame.mutationManager.mutations.Count <= 0)
		{
			return (jackpotMutation != null);
		}
		
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			if (baseMutation.type == "non_persistent_jackpot_meter")
			{
				jackpotMutation = baseMutation as StandardMutation;
			}
		}
		return (jackpotMutation != null);
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		//Update the jackpot ui items
		foreach (JackpotContainer jackpot in jackpotContainers)
		{
			if (!jackpotMutation.jackpotMeters.ContainsKey(jackpot.keyName))
			{
				continue;
			}
			
			foreach (JackpotContainer.JackpotTier jackpotTier in jackpot.jackpotTiers)
			{
				foreach (KeyValuePair<string, StandardMutation.JackpotMeter> keyValuePair in jackpotMutation.jackpotMeters)
				{
					if (keyValuePair.Value.keyName != jackpot.keyName)
					{
						continue;
					}
					
					jackpotTier.currentPips = keyValuePair.Value.currentPips;
					jackpotTier.requiredPips = keyValuePair.Value.requiredPips;
					
					jackpotTier.wasAwarded = keyValuePair.Value.shouldAward;

					//progressive jackpots get their values populated from one of the progressivejackpot systems
					if (jackpot.jackpotType != JackpotType.Progressive)
					{
						jackpotTier.credits = keyValuePair.Value.baseAmount;
					}
				}
				
				if (jackpotTier.jackpotLabel != null && jackpot.jackpotType != JackpotType.Progressive)
				{
					jackpotTier.jackpotLabel.text = getJackpotContainerLabel(jackpotTier);
				}
			}
		}
		
		yield break;
	}

	//Award Jackpot once reels are finished spinning
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return jackpotMutation != null;
	}
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		//slight pause to allow for a more natural timing
		yield return new TIWaitForSeconds(preWinAnimationsDelay);

		List<SlotSymbol> visibleSymbols = reelGame.engine.getAllVisibleSymbols(); 
		foreach (SlotSymbol symbol in visibleSymbols)
		{
			if (!symbol.isScatterSymbol)
			{
				continue;
			}
			
			foreach (JackpotContainer jackpotContainer in jackpotContainers)
			{
				foreach (JackpotContainer.JackpotTier t in jackpotContainer.jackpotTiers)
				{
					if (t.associatedSymbolServerName == symbol.serverName)
					{
						t.triggeringSymbols.Add(symbol);
					}
				}
			}
		}
		
		foreach (JackpotContainer jackpotContainer in jackpotContainers)
		{
			switch (jackpotContainer.jackpotType)
			{
				case JackpotType.Progressive:
				{
					if (activePJPTier == null)
					{
						activePJPTier = getCurrentProgressiveJackpotTier(jackpotContainer);
					}
					
					//adjust all the pip counts on the jackpots
					if(activePJPTier.currentPips != activePJPTier.lastPips && activePJPTier.currentPips > 0)
					{
						for (int i = activePJPTier.lastPips; i < activePJPTier.currentPips && activePJPTier.pipProgressAnimations != null; ++i)
						{
							//animate the symbol that is linked to this pip being lit
							if (activePJPTier.triggeringSymbols.Count > 0)
							{
								int triggeringSymbolCount = activePJPTier.triggeringSymbols.Count;
								SlotSymbol triggeringSymbol = activePJPTier.triggeringSymbols[triggeringSymbolCount - 1];
								triggeringSymbol.animateAnticipation();
								yield return StartCoroutine(symbolToJackpotBoxAnimatedParticleEffect.animateParticleEffect(triggeringSymbol.transform, activePJPTier.jackpotLabel.transform));
								activePJPTier.triggeringSymbols.Remove(triggeringSymbol);
							}
						
							//animate the pip, range guard just in case
							if (i < activePJPTier.pipProgressAnimations.animInfoList.Count)
							{
								activePJPTier.pipProgressAnimations.animInfoList[i].ANIMATION_NAME = activePJPTier.pipAquiredAnimationName;
								yield return StartCoroutine(AnimationListController.playAnimationInformation(activePJPTier.pipProgressAnimations.animInfoList[i]));
							}
						}
						activePJPTier.lastPips = activePJPTier.currentPips;
					}
					activePJPTier.triggeringSymbols.Clear();
				
					//jackpot not awarded, avoid further processing
					if (!activePJPTier.wasAwarded)
					{
						continue;
					}
					
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(activePJPTier.winIntroAnimations));
					yield return StartCoroutine(AudioListController.playListOfAudioInformation(jackpotSymbolWinSounds));
					
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(activePJPTier.winOutroAnimations));

					//turn off all the pips because the jackpot has been won
					if (activePJPTier.pipProgressAnimations != null)
					{
						for (int i = 0; i < activePJPTier.requiredPips; ++i)
						{
							//animate the pip, range guard just in case
							if (i < activePJPTier.pipProgressAnimations.animInfoList.Count)
							{
								activePJPTier.lastPips = 0;
								activePJPTier.pipProgressAnimations.animInfoList[i].ANIMATION_NAME = activePJPTier.pipOffAnimationName;
							}
						}
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(activePJPTier.pipProgressAnimations));
					}
					break;
				}
				
				case JackpotType.Fixed:
				{
					foreach (JackpotContainer.JackpotTier jackpotTier in jackpotContainer.jackpotTiers)
					{
						if (jackpotTier.currentPips != jackpotTier.lastPips && jackpotTier.currentPips > 0)
						{
							//adjust all the pip counts on the jackpots
							for (int i = jackpotTier.lastPips; i < jackpotTier.currentPips && jackpotTier.pipProgressAnimations != null; ++i)
							{
								//animate the symbol that is linked to this pip being lit
								if (jackpotTier.triggeringSymbols.Count > 0)
								{
									int triggeringSymbolCount = jackpotTier.triggeringSymbols.Count;
									SlotSymbol triggeringSymbol = jackpotTier.triggeringSymbols[triggeringSymbolCount - 1];
									triggeringSymbol.animateAnticipation();
									yield return StartCoroutine(symbolToJackpotBoxAnimatedParticleEffect.animateParticleEffect(triggeringSymbol.transform, jackpotTier.jackpotLabel.transform));
									jackpotTier.triggeringSymbols.Remove(triggeringSymbol);
								}
							
								//animate the pip, range guard just in case
								if (i < jackpotTier.pipProgressAnimations.animInfoList.Count)
								{
									jackpotTier.pipProgressAnimations.animInfoList[i].ANIMATION_NAME = jackpotTier.pipAquiredAnimationName;
									yield return StartCoroutine(AnimationListController.playAnimationInformation(jackpotTier.pipProgressAnimations.animInfoList[i]));
								}
							}
							jackpotTier.lastPips = jackpotTier.currentPips;
						}
						jackpotTier.triggeringSymbols.Clear();

						//jackpot not awarded, so avoid further processing
						if (!jackpotTier.wasAwarded)
						{
							continue;
						}

						StandardMutation.JackpotMeter wonJackpotMeter = jackpotMutation.jackpotMeters[jackpotContainer.keyName];
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotTier.winIntroAnimations));
						yield return StartCoroutine(AudioListController.playListOfAudioInformation(jackpotSymbolWinSounds));

						if (wonJackpotMeter.baseAmount <= 0)
						{
							Debug.LogError("HOW?! SHOULD BE A JACKPOT > 0!");
						}
						
						//fixed jackpot won
						if (wonJackpotMeter != null && wonJackpotMeter.baseAmount > 0)
						{
							hasRollupFinished = false;
							long creditsAwarded = wonJackpotMeter.baseAmount * reelGame.multiplier;
							yield return StartCoroutine(rollupWinnings(creditsAwarded));
							hasRollupFinished = true;
						
							//Run through the list of win animations
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotTier.winOutroAnimations));
						
							//turn all pips off
							if (jackpotTier.pipProgressAnimations != null)
							{
								for (int i = 0; i < jackpotTier.requiredPips; ++i)
								{
									if (i < jackpotTier.pipProgressAnimations.animInfoList.Count)
									{
										jackpotTier.lastPips = 0;
										jackpotTier.pipProgressAnimations.animInfoList[i].ANIMATION_NAME = jackpotTier.pipOffAnimationName;
									}
								}
								yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotTier.pipProgressAnimations));
							}

							//Reset jackpot label to the base amount
							if (jackpotTier.jackpotLabel != null)
							{
								//Have to make sure to set this back to 0 so on wager change it doesn't repopulate with the old value.
								jackpotTier.jackpotLabel.text = getJackpotContainerLabel(jackpotTier);
							}
						}
					}
					break;
				}
			}
		}
	}
	
	private IEnumerator rollupWinnings(long creditsAwarded)
	{
		if (jackpotSymbolWinSounds != null)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(jackpotSymbolWinSounds));
		}
		
		//Wait for the rollup to finish animating
		yield return StartCoroutine(reelGame.rollupCredits(0,
			creditsAwarded,
			ReelGame.activeGame.onPayoutRollup,
			isPlayingRollupSounds: true,
			specificRollupTime: 0.0f,
			shouldSkipOnTouch: true,
			allowBigWin: false));
		
		yield return new TIWaitForSeconds(postRollupWait);
	}

	//Reset the win tracking variables
	public override bool needsToExecuteOnPreSpin()
	{
		return (jackpotMutation != null);
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		jackpotMutation = null;
		numberOfJackpotSymbolsThisSpin = 0;
		yield return null;
	}

	// Helper function to get the same module attached to the base game
	private JackpotMetersModule getBaseGameModule()
	{
		if (SlotBaseGame.instance == null)
		{
			return null;
		}
		
		foreach (SlotModule mod in SlotBaseGame.instance.cachedAttachedSlotModules)
		{
			JackpotMetersModule module = mod as JackpotMetersModule;
			if (module != null)
			{
				return module;
			}
		}

		return null;
	}

	// Copy game init values over to freespins
	private bool copyValuesFromBasegame()
	{
		JackpotMetersModule baseGameModule = getBaseGameModule();
		if (baseGameModule == null)
		{
			return false;
		}

		for (int i = 0; i < baseGameModule.jackpotContainers.Count; i++)
		{
			JackpotContainer jackpotContainer = baseGameModule.jackpotContainers[i];
			for (int k = 0; k < jackpotContainer.jackpotTiers.Count; k++)
			{
				JackpotContainer.JackpotTier jackpotTier = jackpotContainer.jackpotTiers[k];
				JackpotContainer.JackpotTier tier = jackpotContainers[i].jackpotTiers[k];
				tier.requiredPips = jackpotTier.requiredPips;
				tier.currentPips = jackpotTier.currentPips;

				//progressive jackpot values come from the BuiltInProgressiveJackpot Modules
				if (jackpotContainer.jackpotType != JackpotType.Progressive)
				{
					tier.credits = jackpotTier.credits;
				}
				
				tier.lastPips = 0;
			}
		}

		return true;
	}

	// in normal freespins we get values from basegame, but for gifted freespins, values are in reelInfo
	private void getValuesForGiftedFreespinsGame()
	{
		JSON[] reelInfo = reelGame.reelInfo;
		if (reelInfo == null)
		{
			return;
		}

		foreach (JSON json in reelInfo)
		{
			if (json.getString("type", "") != "freespin_background")
			{
				continue;
			}
			
			JSON fixedJackpots = json.getJSON("non_persistent_jackpot_meter");
			JSON[] jackpotMeters = json.getJsonArray("jackpot_meters");
			if (fixedJackpots == null || jackpotMeters == null)
			{
				break;
			}
			
			foreach (JSON jackpotMeter in jackpotMeters)
			{
				string jackpotKey = jackpotMeter.getString("key_name", "");

				if (string.IsNullOrEmpty(jackpotKey))
				{
					
					continue;
				}
				
				JackpotContainer jackpotContainer = getJackpotContainerWithKey(jackpotKey);
				if (jackpotContainer != null)
				{
					StartCoroutine(initializeJackpotContainer(jackpotContainer, jackpotMeter));
				}
			}
		}
	}

	private string getJackpotContainerLabel(JackpotContainer.JackpotTier jackpotTier)
	{
		if (jackpotTier != null)
		{
			long totalJackpotAmount = (jackpotTier.credits * reelGame.multiplier);
			
			return jackpotTier.isAbbreviatingJackpotLabel ? CreditsEconomy.multiplyAndFormatNumberAbbreviated(totalJackpotAmount) 
				: CreditsEconomy.convertCredits(totalJackpotAmount);
		}

		Debug.LogError("jackpotContainer was null!");
		return string.Empty;
	}
}
