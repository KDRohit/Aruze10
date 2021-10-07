using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.Util;

public class FreespinMeterCollectModule : SlotModule
{
	// List of the meterAnimation data for the freespin meters
	[SerializeField] private List<FreespinMeterAnimationData> meterAnimations;

	// How long to delay between collecting from more than one symbol
	[SerializeField] private float nextSymbolCollectDelay;

	// How long to delay after the all the symbols have been collected to animate the hotness
	[SerializeField] private float activateMeterHotnessDelay;

	// The reevaluation data
	private ReevaluationFreespinMeter freespinMeter;

	// keep a list of the coroutines that are running so we can wait on them to end
	private List<TICoroutine> coroutineList = new List<TICoroutine>();

	// Coming back check and see if we need to reset our freespin meters after a bonus game has ended.
	protected override void OnEnable()
	{
		base.OnEnable();
		if (freespinMeter != null && freespinMeter.bonus != null && !string.IsNullOrEmpty(freespinMeter.bonus.tier))
		{
			FreespinMeterAnimationData animationData = getAnimationDataForTier(freespinMeter.bonus.tier);
			animationData.freespinCount = freespinMeter.bonus.freeSpinsReset;
			animationData.textLabel.text = CommonText.formatNumber(animationData.freespinCount);
		}
	}

	// When the game first starts, we get the saved user data from the server from the modifier_exports
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return ReelGame.activeGame.modifierExports != null;
	}

	// Get the players startup data so that we can initialize the free spin meters
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// this is where we update our freespin meters from startup data
		foreach (JSON exportJSON in ReelGame.activeGame.modifierExports)
		{
			if (exportJSON.getString("type", "") == "free_spin_meter")
			{
				ModifierExportFreeSpinMeter modifierExportFreeSpinMeter = new ModifierExportFreeSpinMeter(exportJSON);
				initializeFreespinMeters(modifierExportFreeSpinMeter);
			}
		}

		yield return StartCoroutine(animateMeterHotness());
	}

	public override bool needsToExecuteOnReelsSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSpinning()
	{
		freespinMeter = null;
		coroutineList.Clear();
		yield break;
	}

	private FreespinMeterAnimationData getAnimationDataForTier(string tier)
	{
		foreach (FreespinMeterAnimationData animationData in meterAnimations)
		{
			if (animationData.tier == tier)
			{
				return animationData;
			}
		}

		return null;
	}

	// Initialize the freespin meters with the players saved totals
	private void initializeFreespinMeters(ModifierExportFreeSpinMeter modifierExportFreeSpinMeter)
	{
		if (modifierExportFreeSpinMeter == null || modifierExportFreeSpinMeter.tiers == null || modifierExportFreeSpinMeter.tiers.Count <= 0)
		{
			return;
		}

		foreach (ModiferExportsFreeSpinMeterTier freeSpinTier in modifierExportFreeSpinMeter.tiers)
		{
			foreach (FreespinMeterAnimationData animationData in meterAnimations)
			{
				if (animationData.tier == freeSpinTier.tier)
				{
					animationData.freespinCount = freeSpinTier.free_spins;
					animationData.textLabel.text = CommonText.formatNumber(animationData.freespinCount);
				}
			}
		}

		foreach (FreespinMeterAnimationData animationData in meterAnimations)
		{
			if (animationData.freespinCount >= animationData.hotLevelThreshold)
			{
				// make sure we don't play audio for freespin meters that are already activated
				// at the start.
				animationData.didPlayHotLevelAudioActivated = true;
			}
		}
	}

	// See if we have a free spin meter reevaluation for this reel
	public override bool needsToExecuteOnReelEndRollback(SlotReel slotReel)
	{
		freespinMeter = getReevaluationFreespinMeter();

		if (freespinMeter == null || freespinMeter.meters == null || freespinMeter.meters.Count <= 0)
		{
			return false;
		}

		foreach (FreespinMeterData meterData in freespinMeter.meters)
		{
			// note that server reels are 0 based while our slotReel.reelID is 1 based, so take one off.
			if (meterData.reel == (slotReel.reelID - 1))
			{
				return true;
			}
		}

		return false;
	}

	public ReevaluationFreespinMeter getReevaluationFreespinMeter()
	{
		JSON[] reevaluationArray = reelGame.outcome.getArrayReevaluations();

		if (reevaluationArray == null || reevaluationArray.Length <= 0)
		{
			return null;
		}

		for (int i = 0; i < reevaluationArray.Length; i++)
		{
			ReevaluationBase baseReevaluation = new ReevaluationBase(reevaluationArray[i]);
			if (baseReevaluation.type == "free_spin_meter")
			{
				return new ReevaluationFreespinMeter(reevaluationArray[i]);
			}
		}

		return null;
	}

	// Use Reevaluation data to update the number of free spins the user has accumulated.
	// Note that we use visibleSymbolsBottomUp because for 0 is the top most symbol and the server sends it down the other way.
	public override IEnumerator executeOnReelEndRollback(SlotReel slotReel)
	{
		playIdleAnimations();

		foreach (FreespinMeterData meterData in freespinMeter.meters)
		{
			if (meterData.reel == (slotReel.reelID - 1))
			{
				FreespinMeterAnimationData animationData = getFreespinMeterAnimationData(meterData.tier);
				animationData.freespinCount = meterData.freeSpinsNew;
				int symbolIndex = meterData.pos;
				SlotSymbol bonusSymbol = slotReel.visibleSymbolsBottomUp[symbolIndex];
				coroutineList.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animationData.incrementAnimations)));
				coroutineList.Add(StartCoroutine(animationData.animatedParticleEffect.animateParticleEffect(bonusSymbol.transform,animationData.textLabel.transform)));
				coroutineList.Add(StartCoroutine(incrementFreespinCounter(animationData)));
				coroutineList.Add(StartCoroutine(animateBonusSymbol(bonusSymbol, animationData)));
				yield return new WaitForSeconds(nextSymbolCollectDelay);
			}
		}
	}

	// do stuff if a bonus game was triggers from the freespin meter.
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return freespinMeter != null;
	}

	// Freespin meter triggered a bonus game, so set up the multiplier and other stuff
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (freespinMeter.bonus != null && freespinMeter.bonus.payoutMultiplier > 0 &&
		    freespinMeter.bonus.wagerMultiplier > 0)
		{
			// we are headed for a bonus game, so set it up for winning
			BonusGameManager.instance.betMultiplierOverride = freespinMeter.bonus.payoutMultiplier * freespinMeter.bonus.wagerMultiplier;
			playIdleAnimations();
		}

		if (coroutineList.Count > 0)
		{
			// don't let anything happen until the animations are complete
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}

		if (freespinMeter.meters != null)
		{
			yield return new WaitForSeconds(activateMeterHotnessDelay);
			yield return StartCoroutine(animateMeterHotness());
		}
	}

	private IEnumerator animateBonusSymbol(SlotSymbol bonusSymbol, FreespinMeterAnimationData animationData)
	{
		yield return StartCoroutine(bonusSymbol.playAndWaitForAnimateAnticipation());

		// In some games, like bettie02, the Outcome animation is a little too much fun, so we have the option
		// to not play it.
		if (animationData.playOutcomeAnimationOnCollect)
		{
			yield return StartCoroutine(bonusSymbol.playAndWaitForAnimateOutcome());
		}
	}

	// Updates the correct text label with the new number of freespins
	private IEnumerator incrementFreespinCounter(FreespinMeterAnimationData animationData)
	{
		yield return new WaitForSeconds(animationData.incrementDelay);
		animationData.textLabel.text = CommonText.formatNumber(animationData.freespinCount);
	}

	// Find the animation data for this freespinmeters tier
	private FreespinMeterAnimationData getFreespinMeterAnimationData(string freespinMeterTier)
	{
		foreach (FreespinMeterAnimationData animationData in meterAnimations)
		{
			if (animationData.tier == freespinMeterTier)
			{
				return animationData;
			}
		}

		return null;
	}

	// Make the hot animations play if the number of freespins for this meter achieves our threshold
	private IEnumerator animateMeterHotness()
	{
		List<TICoroutine> animateHotnessCoroutineList = new List<TICoroutine>();

		foreach (FreespinMeterAnimationData animationData in meterAnimations)
		{
			int freespinCount = StringUtil.ParseInt(animationData.textLabel.text, 0);

			// play the hotness sound when the freespin meter hits the threshold, this is only played once when the
			// player first achieves hotness.
			if (freespinCount == animationData.hotLevelThreshold && !animationData.didPlayHotLevelAudioActivated)
			{
				if (animationData.hotLevelActivatedAudio != null && animationData.hotLevelActivatedAudio.Count > 0)
				{
					animateHotnessCoroutineList.Add(StartCoroutine(AudioListController.playListOfAudioInformation(animationData.hotLevelActivatedAudio)));
				}
			}

			// play the meter hotness animations
			if (freespinCount >= animationData.hotLevelThreshold)
			{
				animateHotnessCoroutineList.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animationData.hotAnimations)));
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(animateHotnessCoroutineList));
	}

	// Just play all the idle animations so things stop moving around all over the place
	private void playIdleAnimations()
	{
		foreach (FreespinMeterAnimationData animationData in meterAnimations)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(animationData.idleAnimations));
		}
	}

	[System.Serializable]
	public class FreespinMeterAnimationData
	{
		public string tier;
		public int freespinCount;
		public float incrementDelay;
		public LabelWrapperComponent textLabel;
		public AnimationListController.AnimationInformationList incrementAnimations;
		public int hotLevelThreshold;
		public AudioListController.AudioInformationList hotLevelActivatedAudio = new AudioListController.AudioInformationList("freespin_meter_hotness");
		public AnimationListController.AnimationInformationList hotAnimations;
		public AnimationListController.AnimationInformationList idleAnimations;
		public AnimatedParticleEffect animatedParticleEffect;
		public bool didPlayHotLevelAudioActivated;
		public bool playOutcomeAnimationOnCollect;
	}

	public class ModifierExportFreeSpinMeter
	{
		public string type;
		public List<ModiferExportsFreeSpinMeterTier> tiers;

		public ModifierExportFreeSpinMeter(JSON exportJSON)
		{
			type = exportJSON.getString("type", "");
			JSON[] tiersJSONArray = exportJSON.getJsonArray("tiers", true);
			if (tiersJSONArray != null && tiersJSONArray.Length > 0)
			{
				tiers = new List<ModiferExportsFreeSpinMeterTier>();
				foreach (JSON tierJSON in tiersJSONArray)
				{
					tiers.Add(new ModiferExportsFreeSpinMeterTier(tierJSON));
				}
			}
		}
	}

	public class ModiferExportsFreeSpinMeterTier
	{
		public string tier;
		public int free_spins;
		public int wager_multiplier;

		public ModiferExportsFreeSpinMeterTier(JSON tierJSON)
		{
			tier = tierJSON.getString("tier", "");
			free_spins = tierJSON.getInt("free_spins", 0);
			wager_multiplier = tierJSON.getInt("wager_multiplier", 0);
		}
	}
}