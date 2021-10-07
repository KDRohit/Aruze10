using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Persist a visual animation state between sessions and devices
public class Aruze10PersistentVisualEffect : SlotModule
{
	#region public properties

	[Tooltip("Visual Effects divided into tiers. Each animation in the tier will be advances 1 at a time.")]
	[SerializeField] private bool isTierUseSingleTimeOnly;

	[Tooltip("Visual Effects divided into tiers. Each animation in the tier will be advances 1 at a time.")]
	[SerializeField] private List<VisualEffectTier> visualEffectTiers;

	[Tooltip("Controls if we should animated trigger symbol particle effects at the same time")]
	[SerializeField] private bool shouldSyncTriggerParticleEffects;

	[Tooltip("Audio to play when any visual effect is activated. This can be empty if you want to define different audio for each tier.")]
	[SerializeField] private AudioListController.AudioInformationList visualEffectAudio;

	[Tooltip("this will animate the max tier particle effect when special trigger symbols and in freespins")]
	[SerializeField] private bool playBonusSymbolParticleEffectInFreespins;

	[Tooltip("A list of trigger symbol names used by playBonusSymbolParticleEffectInFreespins")]
	[SerializeField] private List<string> freespinsTriggerSymbolNames;

	[Tooltip("Play outcome animation for associated symbols in sync with particle effects")] 
	[SerializeField] private bool playSymbolOutcomeAnimationsAndParticleEffectInSync;

	[Tooltip("Audio to play when any special symbol activates a bonus game")]
	[SerializeField] private SpecialTriggerSymbolAudio specialTriggerSymbolAudio;

	[Tooltip("Wait for tier animation to finish before proceeding")] [SerializeField]
	private bool waitForTierAnimationsToFinish;

#endregion

#region private properties

	private List<ReevaluationPersistentVisualEffect> cachedPersistentVisualEffectReevaluations; // store a list of reevaluations that operate on the persistent visual effect each spin
	private string VISUAL_DATA_COUNT_ON_SYMBOL_LAND_KEY =  "visual_data_count_on_symbol_land";
	private string VISUAL_DATA_RESET_ON_BONUS_NAME_KEY =  "visual_data_reset_on_bonus_name";

	private int animationLevel
	{
		get => _animationLevel;
		set
		{
			if (value >= 0 && value < numVisualEffectAnimations)
			{
				_animationLevel = value;
			}
			else if (value >= numVisualEffectAnimations)
			{
				int valueIntoHighestTier = value - numVisualEffectsExcludingHighestTier;
				int positionInHighestTier = valueIntoHighestTier % visualEffectTiers[visualEffectTiers.Count - 1].animations.Count;
				_animationLevel = positionInHighestTier + numVisualEffectsExcludingHighestTier;
			}
		}
	}
	
	private int _animationLevel;
	private int rawAnimationLevel;
	private int freeSpinsAnimationCounter;

	// store how many visual effect animations we have in the visualEffectTiers so
	// it can be used to bound animationLevel
	private int numVisualEffectAnimations
	{
		get
		{
			if (_numVisualEffectAnimations <= 0)
			{
				foreach (VisualEffectTier visualEffectTier in visualEffectTiers)
				{
					_numVisualEffectAnimations += visualEffectTier.animations.Count;
				}
			}

			return _numVisualEffectAnimations;
		}
	}
	private int _numVisualEffectAnimations;
	
	// store how many visual effect animations we have in the visualEffectTiers excluding the highest tier
	// it can be used to loop the last tier animations
	private int numVisualEffectsExcludingHighestTier
	{
		get
		{
			_numVisualEffectsExcludingHighestTier = numVisualEffectAnimations - visualEffectTiers[visualEffectTiers.Count - 1].animations.Count;
			return _numVisualEffectsExcludingHighestTier;
		}
	}
	private int _numVisualEffectsExcludingHighestTier;

#endregion

#region slotmodule overrides

	protected override void OnEnable()
	{
		// Handle playing or restoring animations here when needed so we
		// don't have the visual feature playing default animator states.
		if (!reelGame.hasFreespinGameStarted)
		{
			StartCoroutine(playIdleAnimations());
		}
	}

	public override bool needsToExecuteOnShowSlotBaseGame()
	{
		return true;
	}

	public override void executeOnShowSlotBaseGame()
	{
		// this is used when the game is mostly hidden by a bigwin effect or full screen dialog
		StartCoroutine(playIdleAnimations());
	}

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		if (!reelGame.hasFreespinGameStarted)
		{
			// Not in a bonus game so this is the real start of the game,
			// or we have returned from a bonus game.
			initCurrentAnimationLevelFromModifierExports();
			yield return StartCoroutine(playIdleAnimations());
		}
		else
		{
			// Start a counter for frees spins game animation level
			freeSpinsAnimationCounter = numVisualEffectAnimations - 1;
		}
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		cachedPersistentVisualEffectReevaluations = null;
		specialTriggerSymbolAudio.didPlayBonusWonAudio = false;
		//freespinsTriggerSymbolNames.Clear();
		//freespinsTriggerSymbolNames.Add("BN");
		yield break;
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	// This is the first time the slot outcome is available and a good place to extract
	// the reevaluations that will affect the persistent visual effect
	public override IEnumerator executePreReelsStopSpinning()
	{
		cachedPersistentVisualEffectReevaluations = getPersistentVisualEffectReevaluations();
		yield break;
	}

	public override bool needsToExecuteOnBonusGameEnded()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		if (!reelGame.hasFreespinGameStarted)
		{
			// Reset visual feature to the base tier when returning from a challenge game to the basegame
			ReevaluationPersistentVisualEffect reevaluationVisualDataResetData = getVisualDataReevaluationForAfterBonusGames();

			if (reevaluationVisualDataResetData != null)
			{
				animationLevel = reevaluationVisualDataResetData.counter;
				rawAnimationLevel = reevaluationVisualDataResetData.counter;
			}

			yield return StartCoroutine(playIdleAnimations());
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if(playBonusSymbolParticleEffectInFreespins && reelGame.hasFreespinGameStarted)
		{
			// if we are in freespins we still want to fire off particle effects for trigger symbols when the reels stop
			return true;
		}

		List<ReevaluationPersistentVisualEffect> visualDataReevaluationForOnReelsStopped = getVisualDataReevaluationForOnReelsStopped();
		return visualDataReevaluationForOnReelsStopped != null && visualDataReevaluationForOnReelsStopped.Count > 0;
	}

	// update the visual feature
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (reelGame.hasFreespinGameStarted && playBonusSymbolParticleEffectInFreespins)
		{
			yield return StartCoroutine(animateParticleEffectsForBonusSymbolsInFreepins());
			yield break;
		}

		List<TICoroutine> allCoroutines = new List<TICoroutine>();
		List<ReevaluationPersistentVisualEffect> visualDataReevaluationForOnReelsStopped = getVisualDataReevaluationForOnReelsStopped();

		// Look through the reevaluations that have an effect on the reel stops and apply any new visual effects
		foreach (ReevaluationPersistentVisualEffect visualEffectReevaluation in visualDataReevaluationForOnReelsStopped)
		{
			allCoroutines.Clear();

			if (visualEffectReevaluation.counter > animationLevel)
			{
				foreach (ReevaluationPersistentVisualEffect.TriggerSymbol triggerSymbol in visualEffectReevaluation.symbols)
				{
					if (animationLevel < visualEffectReevaluation.counter)
					{
						animationLevel++;
					}

					List<SlotSymbol> slotSymbols = reelGame.engine.getVisibleSymbolsBottomUpAt(triggerSymbol.reel);
					SlotSymbol slotSymbol = slotSymbols[triggerSymbol.position];
					
					if (shouldSyncTriggerParticleEffects)
					{
						allCoroutines.Add(StartCoroutine(playActivationAnimationsForAnimationLevel(animationLevel, slotSymbol)));
					}
					else
					{
						yield return StartCoroutine(playActivationAnimationsForAnimationLevel(animationLevel, slotSymbol));
					}
				}

				// make sure our current animation level is synced with the data
				animationLevel = visualEffectReevaluation.counter;
				rawAnimationLevel = visualEffectReevaluation.counter;
			}

			if (waitForTierAnimationsToFinish)
			{
				yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
			}
			else
			{
				RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
			}
		}
	}

	// Check for reset data which indicates that a special symbol has activated a bonus game,
	// in which case we may want to play a special audio hit to go with it as it lands.
	public override bool needsToExecuteOnSpinEnding(SlotReel stoppedReel)
	{
		if (reelGame.hasFreespinGameStarted)
		{
			return true;
		}

		return getVisualDataReevaluationForOnReelsStopped() != null;
	}

	// Play an audio sound when special bonus symbols land.
	// Make it extra special if they triggered a bonus game.
	public override void executeOnSpinEnding(SlotReel stoppedReel)
	{
		// in freespins we need to play audio for defined symbols
		if (reelGame.hasFreespinGameStarted)
		{
			playSpecialAudioForTriggerSymbolsInFreespins(stoppedReel);
		}
		else
		{
			playSpecialAudioForTriggerSymbols(stoppedReel);
		}
	}

	// Play special landing sounds for trigger symbols landing
	private void playSpecialAudioForTriggerSymbols(SlotReel stoppedReel)
	{
		// get reevaluations needed to determine if we should play special audio for this reel stop
		List<ReevaluationPersistentVisualEffect> visualDataReevaluationForOnReelsStopped = getVisualDataReevaluationForOnReelsStopped();
		bool shouldPlayBonusWonAudio = false;

		foreach (ReevaluationPersistentVisualEffect visualEffectReevaluation in visualDataReevaluationForOnReelsStopped)
		{
			if (visualEffectReevaluation.type == VISUAL_DATA_RESET_ON_BONUS_NAME_KEY)
			{
				// if this reevaluation type exists then we are going into a bonus game that was triggered by
				// a trigger symbol and we need to play special sounds for it.
				shouldPlayBonusWonAudio = true;
			}
		}

		foreach (ReevaluationPersistentVisualEffect visualEffectReevaluation in visualDataReevaluationForOnReelsStopped)
		{
			foreach (ReevaluationPersistentVisualEffect.TriggerSymbol triggerSymbol in visualEffectReevaluation.symbols)
			{
				if (triggerSymbol.reel == stoppedReel.reelID - 1)
				{
					playAudioForTriggerSymbol(shouldPlayBonusWonAudio);
					return;
				}
			}
		}
	}

	// Play special landing sounds for trigger symbols landing in freespins as defined
	// in freespinsTriggerSymbolNames
	private void playSpecialAudioForTriggerSymbolsInFreespins(SlotReel stoppedReel)
	{
		foreach (SlotSymbol slotSymbol in stoppedReel.visibleSymbols)
		{
			if (freespinsTriggerSymbolNames.Contains(slotSymbol.serverName))
			{
				bool shouldPlayBonusWonAudio = reelGame.outcome.hasBonusGame();
				playAudioForTriggerSymbol(shouldPlayBonusWonAudio);
				return;
			}
		}
	}

	private void playAudioForTriggerSymbol(bool shouldPlayBonusWonAudio)
	{
		if (shouldPlayBonusWonAudio &&
		    specialTriggerSymbolAudio.triggerBonusWonAudio != null &&
		    (!specialTriggerSymbolAudio.didPlayBonusWonAudio || specialTriggerSymbolAudio.shouldAlwaysPlayBonusWonAudio))
		{
			// We have reset data which means a bonus game was triggered and we should play triggerBonusWonAudio
			specialTriggerSymbolAudio.didPlayBonusWonAudio = true;
			StartCoroutine(AudioListController.playListOfAudioInformation(specialTriggerSymbolAudio.triggerBonusWonAudio));
		}
		else if (specialTriggerSymbolAudio.triggerLandedAudio != null)
		{
			// no bonus game was won, but trigger symbols landed
			StartCoroutine(AudioListController.playListOfAudioInformation(specialTriggerSymbolAudio.triggerLandedAudio));
		}
	}

	#endregion

#region helper methods

	// get all the reevaluations acting on the persistent visual effect data
	private List<ReevaluationPersistentVisualEffect> getPersistentVisualEffectReevaluations()
	{
		List<ReevaluationPersistentVisualEffect> persistentVisualEffectReevals = new List<ReevaluationPersistentVisualEffect>();

		JSON[] arrayReevaluations = ReelGame.activeGame.outcome.getArrayReevaluations();
		for (int i = 0; i < arrayReevaluations.Length; i++)
		{
			string reevalType = arrayReevaluations[i].getString("type", "");
			if (reevalType == VISUAL_DATA_COUNT_ON_SYMBOL_LAND_KEY || reevalType == VISUAL_DATA_RESET_ON_BONUS_NAME_KEY)
			{
				persistentVisualEffectReevals.Add(new ReevaluationPersistentVisualEffect(arrayReevaluations[i]));
			}
		}

		return persistentVisualEffectReevals;
	}

	private List<ReevaluationPersistentVisualEffect> getVisualDataReevaluationForOnReelsStopped()
	{
		List<ReevaluationPersistentVisualEffect> visualDataReevaluationForOnReelsStopped = new List<ReevaluationPersistentVisualEffect>();

		foreach (ReevaluationPersistentVisualEffect reevaluation in cachedPersistentVisualEffectReevaluations)
		{
			if (reevaluation.type == VISUAL_DATA_COUNT_ON_SYMBOL_LAND_KEY || reevaluation.type == VISUAL_DATA_RESET_ON_BONUS_NAME_KEY)
			{
				visualDataReevaluationForOnReelsStopped.Add(reevaluation);
			}
		}

		return visualDataReevaluationForOnReelsStopped;
	}

	private ReevaluationPersistentVisualEffect getVisualDataReevaluationForAfterBonusGames()
	{
		foreach (ReevaluationPersistentVisualEffect reevaluation in cachedPersistentVisualEffectReevaluations)
		{
			if (reevaluation.type == VISUAL_DATA_RESET_ON_BONUS_NAME_KEY)
			{
				return reevaluation;
			}
		}

		return null;
	}

	private IEnumerator animateParticleEffectsForBonusSymbolsInFreepins()
	{
		
		List<TICoroutine> allCoroutines = new List<TICoroutine>();
		
		// animate particle effects in freespins.
		foreach (SlotSymbol slotSymbol in reelGame.engine.getAllVisibleSymbols())
		{
			if (freespinsTriggerSymbolNames.Contains(slotSymbol.serverName))
			{
				int freeSpinsAnimationLevel = freeSpinsAnimationCounter;
				if (freeSpinsAnimationLevel >= numVisualEffectAnimations)
				{
					int valueIntoHighestTier = freeSpinsAnimationLevel - numVisualEffectsExcludingHighestTier;
					int positionInHighestTier = valueIntoHighestTier % visualEffectTiers[visualEffectTiers.Count - 1].animations.Count;
					freeSpinsAnimationLevel = positionInHighestTier + numVisualEffectsExcludingHighestTier;
				}
				
				if (shouldSyncTriggerParticleEffects)
				{
					allCoroutines.Add(StartCoroutine(playActivationAnimationsForAnimationLevel(freeSpinsAnimationLevel, slotSymbol)));
				}
				else
				{
					yield return StartCoroutine(playActivationAnimationsForAnimationLevel(freeSpinsAnimationLevel, slotSymbol));
				}
				
				freeSpinsAnimationCounter ++;
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
	}

	private IEnumerator playActivationAnimationsForAnimationLevel(int myAnimationLevel, SlotSymbol triggerSymbol)
	{
		int calculatedTier = getCalculatedTierLevel(myAnimationLevel);

		if (visualEffectTiers == null || calculatedTier >= visualEffectTiers.Count)
		{
			yield break;
		}

		VisualEffectTier visualEffectTierData = null;
		VisualEffectAnimationData visualEffectAnimationData = getVisualEffectAnimationDataForAnimationLevel(myAnimationLevel,ref visualEffectTierData);
		bool isProceed = isTierUseSingleTimeOnly == false ? true : 
						 visualEffectTierData != null && visualEffectTierData.isTierUsed == false ? true : 
						 false;

		if (isProceed && visualEffectAnimationData != null && visualEffectAnimationData.activate != null & visualEffectAnimationData.activate.Count > 0)
		{
			List<TICoroutine> visualEffectCoroutines = new List<TICoroutine>();
			
			if(playSymbolOutcomeAnimationsAndParticleEffectInSync)
			{
				visualEffectCoroutines.Add(StartCoroutine(triggerSymbol.playAndWaitForAnimateOutcome()));
			}

			// --- Send a animated particle effect from the trigger symbol if we have enough to correspond with the increase in animation level.
			// respect the blocking as the particle effect flys to the thing it is activating
			if (triggerSymbol != null && visualEffectTiers[calculatedTier].tierAnimatedParticleEffect != null)
			{
				yield return StartCoroutine(visualEffectTiers[calculatedTier].tierAnimatedParticleEffect.animateParticleEffect(triggerSymbol.transform, visualEffectAnimationData.particleTargetTransform));
			}

			// --- play audio that goes with any visual effect activating.
			if (visualEffectAudio != null && visualEffectAudio.audioInfoList.Count > 0)
			{
				visualEffectCoroutines.Add(StartCoroutine(AudioListController.playListOfAudioInformation(visualEffectAudio)));
			}

			// --- play audio that goes is specific to this visual visual effect tier activating.
			if (visualEffectTiers[calculatedTier].tierAudio != null && visualEffectTiers[calculatedTier].tierAudio.Count > 0)
			{
				visualEffectCoroutines.Add(StartCoroutine(AudioListController.playListOfAudioInformation(visualEffectTiers[calculatedTier].tierAudio)));
			}

			// --- play the actual visual effect
			visualEffectCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(visualEffectAnimationData.activate)));
			if(visualEffectAnimationData.activate.Count>0)
            {
				visualEffectTierData.isTierUsed = true;
			}
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(visualEffectCoroutines));
		}
	}

	// Find a visual effect within the tiers that correspond to the animationLevel
	private VisualEffectAnimationData getVisualEffectAnimationDataForAnimationLevel(int myAnimationLevel, ref VisualEffectTier visualEffectTierData)
	{
		int tierAnimationIndex = myAnimationLevel;
		foreach (VisualEffectTier visualEffectTier in visualEffectTiers)
		{

			if (tierAnimationIndex < visualEffectTier.animations.Count)
			{
				visualEffectTierData = visualEffectTier;
				return visualEffectTier.animations[tierAnimationIndex];
			}

			tierAnimationIndex -= visualEffectTier.animations.Count;
		}

		return null;
	}

	// Play all the idle animations from the highest level starting from the top animationLevel
	// back down to the lowest.
	private IEnumerator playIdleAnimations()
	{
		if (visualEffectTiers == null || visualEffectTiers.Count < 1)
		{
			yield break;
		}

		stopAllAnimators();
		
		int calculatedTierLevel = getCalculatedTierLevel(animationLevel);
		
		int calculatedAnimationLevel = 0;
		if (rawAnimationLevel >= numVisualEffectAnimations)
		{
			calculatedAnimationLevel = getCalculatedAnimationLevel(numVisualEffectAnimations - 1);
		}
		else
		{
			calculatedAnimationLevel = getCalculatedAnimationLevel(animationLevel);
		}
		
		// Since different tiers can contain the same animators with different states,
		// we have to loop backwards from the top tier to make sure we only play the
		// highest level of animation state for any one animator.
		// Note that tierLevel will never exceed the number of visualEffectTiers already
		Dictionary<Animator, bool> didAnimate = new Dictionary<Animator, bool>();
		List<TICoroutine> allCoroutines = new List<TICoroutine>();
		for (int i = calculatedTierLevel; i >= 0; i--)
		{
			VisualEffectTier tier = visualEffectTiers[i];

			// starting from the top tier we use the calculatedAnimationLevel since
			// that represents our highest level of animation within the top tier.
			int startingAnimationLevel = calculatedAnimationLevel;

			// we are looping backward through the tiers. For the first tier where i == our starting tier
			// we start at the calculatedAnimationLevel. After that we start at the highest level in the
			// next tier down.
			if (i < calculatedTierLevel)
			{
				startingAnimationLevel = tier.animations.Count - 1;
			}

			for (int j = startingAnimationLevel; j >= 0; j--)
			{
				if (tier.animations != null && tier.animations.Count > 0)
				{
					VisualEffectAnimationData tierAnimation = tier.animations[j];
					if (tierAnimation.idle != null & tierAnimation.idle.Count > 0)
					{
						bool shouldAnimate = true;
						foreach (AnimationListController.AnimationInformation animationInformation in tierAnimation.idle.animInfoList)
						{
							if (didAnimate.ContainsKey(animationInformation.targetAnimator))
							{
								shouldAnimate = false;
								break;
							}
						}

						if (shouldAnimate)
						{
							allCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(tierAnimation.idle)));

							foreach (AnimationListController.AnimationInformation animationInformation in tierAnimation.idle.animInfoList)
							{
								if (!didAnimate.ContainsKey(animationInformation.targetAnimator))
								{
									didAnimate.Add(animationInformation.targetAnimator, true);
								}
							}
						}
					}
				}
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
	}

	private void stopAllAnimators()
	{
		foreach (VisualEffectTier visualEffectTier in visualEffectTiers)
		{
			foreach (VisualEffectAnimationData animationData in visualEffectTier.animations)
			{
				stopAllAnimatorsInAnimationInformationList(animationData.idle);
				stopAllAnimatorsInAnimationInformationList(animationData.activate);
			}
		}
	}

	private void stopAllAnimatorsInAnimationInformationList(AnimationListController.AnimationInformationList animationInformationList)
	{
		foreach (AnimationListController.AnimationInformation animationInformation in animationInformationList.animInfoList)
		{
			animationInformation.targetAnimator.StopPlayback();
		}
	}


	private void initCurrentAnimationLevelFromModifierExports()
	{
		animationLevel = 0;
		rawAnimationLevel = 0;

		// get the animation level from server data
		foreach (JSON exportJSON in reelGame.modifierExports)
		{
			if (exportJSON.getString("type", "") == "visual_data_count_on_symbol_land")
			{
				ModiferExportsVisualCount modifierExportVisualCount = new ModiferExportsVisualCount(exportJSON);
				animationLevel = modifierExportVisualCount.counter;
				rawAnimationLevel = modifierExportVisualCount.counter;
			}
		}
	}

	private int getCalculatedTierLevel(int myAnimationLevel)     
	{
		int calculatedTier = 0;
		int tierAnimationIndex = myAnimationLevel;
		foreach (VisualEffectTier visualEffectTier in visualEffectTiers)
		{
			if (tierAnimationIndex < visualEffectTier.animations.Count)
			{
				return calculatedTier;
			}

			if (calculatedTier + 1 < visualEffectTiers.Count)
			{
				calculatedTier++;
				tierAnimationIndex -= visualEffectTier.animations.Count;
			}
		}
		return calculatedTier;
	}

	private int getCalculatedAnimationLevel(int myAnimationLevel)
	{
		int calculatedTier = 0;
		int calculatedAnimationIndex = myAnimationLevel;
		
		foreach (VisualEffectTier visualEffectTier in visualEffectTiers)    
		{
			if (calculatedAnimationIndex < visualEffectTier.animations.Count)
			{
				return calculatedAnimationIndex;
			}

			if (calculatedTier + 1 < visualEffectTiers.Count)
			{
				calculatedTier++;
				calculatedAnimationIndex -= visualEffectTier.animations.Count;
			}
		}
		return calculatedAnimationIndex;
	}

#endregion

#region data class

	[System.Serializable]
	public class VisualEffectTier
	{
		[Tooltip("Informational name to divide things up, not used for anything.")]
		public bool isTierUsed=false; // not used, but useful to make the list of these look nice in the inspector

		[Tooltip("Informational name to divide things up, not used for anything.")]
		public string tierName; // not used, but useful to make the list of these look nice in the inspector

		[Tooltip("Animations to activate as the animation level ramps up. These are played one at a time.")]
		public List<VisualEffectAnimationData> animations;

		[Tooltip("Audio to play when any animation in this tier is activated")]
		public AudioListController.AudioInformationList tierAudio;

		[Tooltip("Animated Particle Effect to play to the particleTargetTransform playing activate animations in this teir")]
		public AnimatedParticleEffect tierAnimatedParticleEffect;
	}

	[System.Serializable]
	public class VisualEffectAnimationData
	{
		[Tooltip("Where to send the particle effect when activating this animation")]
		public Transform particleTargetTransform;

		[Tooltip("Plays this when an animation first activates")]
		public AnimationListController.AnimationInformationList activate;

		[Tooltip("Plays this when restoring animations")]
		public AnimationListController.AnimationInformationList idle;
	}

	[System.Serializable]
	public class SpecialTriggerSymbolAudio
	{
		[Tooltip("Audio to play when any special symbol activates a bonus game")]
		public AudioListController.AudioInformationList triggerBonusWonAudio;

		[Tooltip("Audio to play when any special symbol activates a bonus game")]
		public AudioListController.AudioInformationList triggerLandedAudio;

		[Tooltip("Normally we only play bonus won audio once, check this to always play it for each symbol that lands a triggerSymbol and wins a bonus game.")]
		public bool shouldAlwaysPlayBonusWonAudio;

		[HideInInspector]
		public bool didPlayBonusWonAudio;
	}

#endregion

#region modifier_exports
	public class ModiferExportsVisualCount
	{
		public string type; //visual_data_count_on_symbol_land,
		public int counter;
		public string counterKey; //"gen98_pick"

		public ModiferExportsVisualCount(JSON visualCountJSON)
		{
			type = visualCountJSON.getString("type", "");
			counter = (int) visualCountJSON.getInt("counter", 0);
			counterKey = visualCountJSON.getString("counter_key", "");
		}
	}
#endregion
}