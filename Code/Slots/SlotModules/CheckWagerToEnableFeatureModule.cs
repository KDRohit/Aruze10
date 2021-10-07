using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Checks the wager value from the checkpoint wager data to enable/disable a feature
public class CheckWagerToEnableFeatureModule : SlotModule
{
	[SerializeField] private AnimationListController.AnimationInformationList featureOn;
	[SerializeField] private AnimationListController.AnimationInformationList featureOff;
	[SerializeField] private string checkpointWagerKey;
	private long checkpointWager;
	private long oldWager;

	protected override void OnEnable()
	{
		base.OnEnable();
		oldWager = 0;
		checkWagerToEnableFeature(false);
	}

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		oldWager = 0;
		string reelSetKey = reelSetDataJson.getString("base_reel_set", "");
		if (reelGame != null && reelGame.slotGameData != null && reelSetKey != null)
		{
			ReelSetData reelSetData = reelGame.slotGameData.findReelSet(reelSetKey);
			if (reelSetData != null && reelSetData.checkpointWagerData != null)
			{
				float minWager = reelSetData.checkpointWagerData.getLong(checkpointWagerKey, 0) * SlotsPlayer.instance.currentBuyPageInflationFactor;
				long[] allWagers = SlotsWagerSets.getWagerSetValuesForGame(GameState.game.keyName);

				//find the first wager that is greater than or equal to the scaled value
				checkpointWager = 0;
				for (int i = 0; i < allWagers.Length; ++i)
				{
					if (allWagers[i] >= minWager)
					{
						checkpointWager = allWagers[i];
						break;
					}
				}
			}
		}

		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		checkWagerToEnableFeature(false);
	}

	public override bool needsToExecuteOnBigWinEnd()
	{
		return true;
	}

	public override void executeOnBigWinEnd()
	{
		oldWager = 0;
		checkWagerToEnableFeature(false);
	}

	public override bool needsToExecuteOnWagerChange(long currentWager)
	{
		return true;
	}

	public override void executeOnWagerChange(long currentWager)
	{
		checkWagerToEnableFeature();
	}

	private void checkWagerToEnableFeature(bool includeAudio = true)
	{
		if (reelGame.currentWager < checkpointWager && oldWager >= checkpointWager)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(featureOff, includeAudio: includeAudio));
		}
		else if (reelGame.currentWager >= checkpointWager && oldWager < checkpointWager)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(featureOn, includeAudio: includeAudio));
		}
		oldWager = reelGame.currentWager;
	}
}
