using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This module serves two functions.
// 1. Collects symbols with values when they land on the reels.
// 2. Upgrades the value of all symbols on the reel when a special upgrade symbol lands.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Jan 13, 2020
// games : billions02
//
public class ScatterCollectBonusGameUpgradeModule : ScatterSymbolBaseModule
{
	// collect symbol label
	[SerializeField] private LabelWrapperComponent collectSymbolLabel;

	// names of symbols that collect
	[SerializeField] private List<string> collectSymbolNames;

	[Tooltip("Animations to play when upgrade symbol is collected")]
	[SerializeField] private AnimationListController.AnimationInformationList upgradeSymbolAnimations;

	[Tooltip("Particle Effect to play when collecting the upgrade symbol")]
	[SerializeField] private AnimatedParticleEffect symbolUpgradeCollectParticleEffect;

	[Tooltip("Particle Effect to play when upgrading symbols")]
	[SerializeField] private AnimatedParticleEffect symbolUpgradeBurstParticleEffect;

	[Tooltip("add delay before starting to upgrade all the symbols")]
	[SerializeField] private float startUpgradingSymbolDelay;

	[Tooltip("add delay between each symbol as they are upgraded")]
	[SerializeField] private float eachSymbolUpgradeDelay = 0.0f;

	[Tooltip("particle effect when collecting the symbols credit values")]
	[SerializeField] private AnimatedParticleEffect symbolCollectAnimatedParticleEffect;

	[Tooltip("Control time for collecting credit value rollups. Use 0 for default rollup times")]
	[SerializeField] private float specificRollupTime;

	// This value is determined from the symbol values collected in the basegame and is the value
	// of all the symbols on the reels that have a value.
	private long scatterSymbolValue;

	// symbol value upgrade list
	private List<SymbolValueUpgrade> symbolValueUpgrades = new List<SymbolValueUpgrade>();

	// reward symbol list
	private List<RewardSymbol> rewardSymbols = new List<RewardSymbol>();

	// a list of symbols values rewards being collected so we can roll them up in the correct order
	private List<long> symbolCreditAwardList = new List<long>();

	// coroutines that run as we collect symbols so we can wait until everything is done before the next spin
	List<TICoroutine> allCoroutines = new List<TICoroutine>();

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	// Init the symbol value, the collectSymbolLabel and the symbolCreditMap so we can
	// update symbol values in ScatterCollectBaseModule
	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		scatterSymbolValue = getCarryOverWinFromFreespinsOutcome();
		collectSymbolLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(scatterSymbolValue * reelGame.multiplier, shouldRoundUp: false);

		foreach (string symbolName in collectSymbolNames)
		{
			symbolCreditMap.Add(symbolName, scatterSymbolValue);
		}

		didInit = true;
	}

	private long getCarryOverWinFromFreespinsOutcome()
	{
		if (reelGame.freeSpinsOutcomes != null)
		{
			return reelGame.freeSpinsOutcomes.getCarryOverWin();
		}

		return 0L;
	}

	// When the reels stop, we need to upgrade any symbols that need it, and award the player
	// any values they have won on the reels
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		extractSymbolValueUpgrades();
		extractRewardSymbols();
		return (symbolValueUpgrades != null && symbolValueUpgrades.Count > 0) || (rewardSymbols != null && rewardSymbols.Count > 0);
	}

	public void extractSymbolValueUpgrades()
	{
		symbolValueUpgrades.Clear();
		JSON outcomeJson = reelGame.outcome.getJsonObject();

		if (outcomeJson.hasKey("symbol_value_upgrade"))
		{
			JSON[] symbolValueUpgradeJson = outcomeJson.getJsonArray("symbol_value_upgrade");

			foreach (JSON objectJson in symbolValueUpgradeJson)
			{
				symbolValueUpgrades.Add(new SymbolValueUpgrade(objectJson));
			}
		}
	}

	public void extractRewardSymbols()
	{
		rewardSymbols.Clear();
		JSON outcomeJson = reelGame.outcome.getJsonObject();

		if (outcomeJson.hasKey("reward_symbols"))
		{
			JSON[] rewardSymbolsJson = outcomeJson.getJsonArray("reward_symbols");

			foreach (JSON objectJson in rewardSymbolsJson)
			{
				rewardSymbols.Add(new RewardSymbol(objectJson));
			}
		}
	}

	// Upgrade the symbols and then award symbol values to the player
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(upgradeSymbols());
		yield return StartCoroutine(collectRewardSymbols());
	}

	// Use the data from the server to upgrade the value of all the symbols on the reels.
	private IEnumerator upgradeSymbols()
	{
		if (symbolValueUpgrades == null || symbolValueUpgrades.Count == 0)
		{
			yield break;
		}

		SlotReel[] slotReelArray = reelGame.engine.getReelArray();

		foreach (SymbolValueUpgrade symbolValueUpgrade in symbolValueUpgrades)
		{
			allCoroutines.Clear();

			// get the symbol and animate it to show it is being collected
			SlotReel slotReel = slotReelArray[symbolValueUpgrade.reel];
			SlotSymbol upgradeSlotSymbol = slotReel.visibleSymbolsBottomUp[symbolValueUpgrade.pos];
			allCoroutines.Add(StartCoroutine(upgradeSlotSymbol.playAndWaitForAnimateOutcome()));

			// animate trail from upgrade symbol to the credit value
			if (symbolUpgradeCollectParticleEffect != null)
			{
				yield return StartCoroutine(symbolUpgradeCollectParticleEffect.animateParticleEffect(upgradeSlotSymbol.transform));
			}

			// assign the new symbol value contained in afterCredits from the upgrade symbol landing
			scatterSymbolValue = symbolValueUpgrade.afterCredits;
			collectSymbolLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(scatterSymbolValue * reelGame.multiplier, shouldRoundUp: false);

			// play animations to celebrate the upgraded symbol
			allCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(upgradeSymbolAnimations)));

			// update the values in the symbolCreditMap so that the base module can properly
			// set the labels for symbols as they appear on the reels
			foreach (string symbolName in collectSymbolNames)
			{
				symbolCreditMap[symbolName] = scatterSymbolValue;
			}

			if (startUpgradingSymbolDelay > 0.0f)
			{
				yield return new WaitForSeconds(startUpgradingSymbolDelay);
			}

			// now actually upgrade each of the symbols that are currently on the reels
			foreach (SlotSymbol slotSymbol in reelGame.engine.getAllVisibleSymbols())
			{
				if (symbolCreditMap.ContainsKey(slotSymbol.serverName))
				{
					allCoroutines.Add(StartCoroutine(slotSymbol.playAndWaitForAnimateOutcome()));

					if (symbolUpgradeBurstParticleEffect != null)
					{
						yield return StartCoroutine(symbolUpgradeBurstParticleEffect.animateParticleEffect(slotSymbol.transform));
					}
					setSymbolLabel(slotSymbol);

					if (eachSymbolUpgradeDelay > 0.0f)
					{
						yield return new WaitForSeconds(eachSymbolUpgradeDelay);
					}
				}
			}

			yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
		}
	}

	private IEnumerator collectRewardSymbols()
	{
		if (rewardSymbols == null)
		{
			yield break;
		}

		SlotReel[] slotReelArray = reelGame.engine.getReelArray();

		// loop through the reward symbols and animate and award credits to the player
		foreach (RewardSymbol rewardSymbol in rewardSymbols)
		{
			allCoroutines.Clear();

			// get the symbol and animate it to show it is being collected
			SlotReel slotReel = slotReelArray[rewardSymbol.reel];
			SlotSymbol slotSymbol = slotReel.visibleSymbolsBottomUp[rewardSymbol.pos];
			allCoroutines.Add(StartCoroutine(slotSymbol.playAndWaitForAnimateOutcome()));

			// animate a trail to the win meter from the symbol
			if (symbolCollectAnimatedParticleEffect != null)
			{
				yield return StartCoroutine(symbolCollectAnimatedParticleEffect.animateParticleEffect(slotSymbol.transform));
			}

			long creditsAwarded = scatterSymbolValue *reelGame.multiplier;

			// roll up the awarded credits
			allCoroutines.Add(
				StartCoroutine(SlotUtils.rollup(
					start: BonusGamePresenter.instance.currentPayout,
					end: BonusGamePresenter.instance.currentPayout + creditsAwarded,
					tmPro: BonusSpinPanel.instance.winningsAmountLabel,
					specificRollupTime: specificRollupTime
				)));

			BonusGamePresenter.instance.currentPayout += creditsAwarded;
			FreeSpinGame.instance.setRunningPayoutRollupValue(BonusGamePresenter.instance.currentPayout);

			yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
		}
	}

	public class SymbolValueUpgrade
	{
		public int reel;
		public int pos;
		public string symbol;
		public long beforeCredits; // old symbol credit value from before upgrade symbol landing
		public long afterCredits; // new symbol credit value from the upgrade symbol landing

		public SymbolValueUpgrade(JSON json)
		{
			reel = json.getInt("reel", -1);
			pos = json.getInt("pos", -1);
			symbol = json.getString("symbol", "");
			beforeCredits = json.getLong("before", 0);
			afterCredits = json.getLong("after", 0);
		}
	}

	public class RewardSymbol
	{
		public int reel;
		public int pos;
		public string symbol;

		public RewardSymbol(JSON json)
		{
			reel = json.getInt("reel", -1);
			pos = json.getInt("pos", -1);
			symbol = json.getString("symbol", "");
		}
	}
}

