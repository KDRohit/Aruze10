using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class to handle features like Elvis03 which awards a progressive jackpot in freespins

Creation Date: 5/14/2018
Original Author: Scott Lepthien
*/
public class BuiltInProgressiveJackpotFreespinsModule : SlotModule 
{
	[System.Serializable]
	public class BuiltInProgressiveJackpotTierData
	{
		[SerializeField] public string progressiveKeyName;
		[SerializeField] public AnimationListController.AnimationInformationList showTierAnimations;
		[SerializeField] public AnimationListController.AnimationInformationList hideTierAnimations;
		[SerializeField ]private AnimationListController.AnimationInformationList pipProgressAnimations;
	}

	[SerializeField] private bool executeOnAwake = false;
	[SerializeField] private BuiltInProgressiveJackpotTierData[] jackpotTierData;
	[SerializeField] private LabelWrapperComponent[] valueLabels;
	
	[SerializeField] public AnimationListController.AnimationInformationList progJackpotWonAnimations;
	[SerializeField] public AnimationListController.AnimationInformationList progJackpotIdleAnimations;
	[SerializeField] public float jackpotRollupDuration = 2.5f; // Going to make this a set amount of time, because this number can be massive, so the standard time calculation could result in too much variation
	[SerializeField] public float rollupOverWaitTime = 0.75f;
	[SerializeField] private string jackpotRollupLoopSound = "rollup_jackpot_loop";
	[SerializeField] private string jackpotRollupTermSound = "rollup_jackpot_end";
	[Tooltip("Disable this if you want the animation portions of this module but don't want it to pay out when the game ends (if your game type awards it somewhere else)")]
	[SerializeField] private bool isPayingOutJackpotOnGameEnd = true;

	[Tooltip("Particle trail to play when progressive jackpot won")]
	[SerializeField] private AnimatedParticleEffect progJackpotWonParticleEffect;

	private bool isProgJackpotWon = false;
	private string progJackpotKey = "";
	private long progJackpotWinAmount = 0;
	private ProgressiveJackpot progressiveJackpot = null;

	protected override void OnDestroy()
	{
		unregisterValueLabelsFromProgressiveJackpot();
		base.OnDestroy();
	}

	// Unregister the labels from the progressive jackpot so that they don't update when
	// the value changes anymore
	private void unregisterValueLabelsFromProgressiveJackpot()
	{
		for (int i = 0; i < valueLabels.Length; i++)
		{
			progressiveJackpot?.unregisterLabel(valueLabels[i]);
		}
	}

	// Update all of the labels that are showing hte jackpot amount to what the amount currently is
	private void setValueLabelsToJackpotWinAmount()
	{
		for (int i = 0; i < valueLabels.Length; i++)
		{
			valueLabels[i].text = CreditsEconomy.convertCredits(progJackpotWinAmount);
		}
	} 

	// Returns the matching tier data for the passed jackpot key
	private BuiltInProgressiveJackpotTierData getProgressiveJackpotTierDataForJackpot(string progJackpotKey)
	{
		for (int i = 0; i < jackpotTierData.Length; i++)
		{
			if (jackpotTierData[i].progressiveKeyName == progJackpotKey)
			{
				return jackpotTierData[i];
			}
		}

		Debug.LogError("BuiltInProgressiveJackpotFreespinsModule.getProgressiveJackpotTierDataForJackpot() - Unable to find BuiltInProgressiveJackpotTierData for progJackpotKey = " + progJackpotKey);
		return null;
	}

	public override void Awake()
	{
		if (executeOnAwake)
		{
			StartCoroutine(executeOnSlotGameStarted(null));
		}
	}

	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return !executeOnAwake;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		if (SlotBaseGame.instance == null)
		{
			Debug.LogError("BuiltInProgressiveJackpotFreespinsModule.executeOnSlotGameStartedNoCoroutine() - SlotBaseGame.instance was null, this should never happen if this module is attached, destroying this module!");
			Destroy(this);
			yield break;
		}

		// Look for the basegame BuiltInProgressiveJackpotBaseGameModule since that had to be used to
		// determine what bet level and by that what progressive jackpot we qualify for, we need this
		// info even if we didn't win it so we can display the running total of the jackpot in the labels
		// in freespins.
		for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
		{
			BuiltInProgressiveJackpotBaseGameModule module = SlotBaseGame.instance.cachedAttachedSlotModules[i] as BuiltInProgressiveJackpotBaseGameModule;
			if (module != null)
			{
				progJackpotKey = module.getCurrentJackpotTierKey();
			}
		}

		progressiveJackpot = ProgressiveJackpot.find(progJackpotKey);
		if (progressiveJackpot == null)
		{
			Debug.LogError("BuiltInProgressiveJackpotFreespinsModule.executeOnSlotGameStartedNoCoroutine() - Couldn't find progJackpotKey = " + progJackpotKey);
			Destroy(this);
			yield break;
		}

		foreach (BuiltInProgressiveJackpotTierData jackpotTier in jackpotTierData)
		{
			jackpotTier.progressiveKeyName = string.Format(jackpotTier.progressiveKeyName, GameState.game.keyName);
		}

		// Play animations which will show the correct tier
		List<TICoroutine> tierHideAndShowCoroutines = new List<TICoroutine>();
		BuiltInProgressiveJackpotTierData tierData = getProgressiveJackpotTierDataForJackpot(progJackpotKey);
		for (int i = 0; i < jackpotTierData.Length; i++)
		{
			BuiltInProgressiveJackpotTierData currentData = jackpotTierData[i];
			if (currentData == tierData)
			{
				if (currentData.showTierAnimations.Count > 0)
				{
					tierHideAndShowCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(currentData.showTierAnimations)));
				}
			}
			else
			{
				if (currentData.hideTierAnimations.Count > 0)
				{
					tierHideAndShowCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(currentData.hideTierAnimations)));
				}
			}
		}

		if (tierHideAndShowCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(tierHideAndShowCoroutines));
		}

		// determine if we have data for a won jackpot, and if so then we'll need to display
		// that we've won at the end
		// @NOTE : (Scott Lepthien) This will only work if we can reach the base game, so wouldn't work for
		// gifted spins, but games where you can win a progressive as a feature of the game have been
		// explicitly banned from being offered as gifted games.
		JSON progJackpotWonJson = SlotBaseGame.instance.outcome.getProgressiveJackpotWinJson();
		if (progJackpotWonJson != null)
		{
			isProgJackpotWon = true;
			progJackpotWinAmount = progJackpotWonJson.getLong("running_total", 0);
		}
		else
		{
			isProgJackpotWon = false;
		}

		// Register the value lables to update with the jackpot value, will unhook and set the final
		// value if the player wins the progressive jackpot
		for (int i = 0; i < valueLabels.Length; i++)
		{
			progressiveJackpot.registerLabel(valueLabels[i]);
		}
	}

	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return isProgJackpotWon && isPayingOutJackpotOnGameEnd;
	}

	public override IEnumerator executeOnFreespinGameEnd()
	{
		// Unregister the text from the jackpot since we are going to assign it the final value won
		unregisterValueLabelsFromProgressiveJackpot();
		setValueLabelsToJackpotWinAmount();

		if (progJackpotWonAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(progJackpotWonAnimations));
		}

		if (progJackpotWonParticleEffect != null)
		{
			yield return StartCoroutine(progJackpotWonParticleEffect.animateParticleEffect());
		}

		// Do the rollup
		long currentWinnings = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.currentPayout += progJackpotWinAmount;

		yield return StartCoroutine(SlotUtils.rollup(
			start: currentWinnings,
			end: currentWinnings + progJackpotWinAmount,
			tmPro: BonusSpinPanel.instance.winningsAmountLabel,
			playSound: true,
			specificRollupTime: jackpotRollupDuration,
			shouldSkipOnTouch: true,
			shouldBigWin: false,
			rollupOverrideSound: jackpotRollupLoopSound,
			rollupTermOverrideSound: jackpotRollupTermSound));

		// Restore animations to idle state once rollup is complete
		if (progJackpotIdleAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(progJackpotIdleAnimations));
		}

		yield return new TIWaitForSeconds(rollupOverWaitTime);
	}
}
