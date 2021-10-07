using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Com.Scheduler;

public class CollectablesRewardsIncreasedDialog : DialogBase 
{
	[SerializeField] private ButtonHandler closeButton;
	[SerializeField] private ButtonHandler okayButton;
	[SerializeField] private TextMeshPro totalRewardsLabel;

	public override void init()
	{
		CollectablesAction.upgradedRewardSeen();
		closeButton.registerEventDelegate(closeClicked);
		okayButton.registerEventDelegate(okayClicked);

		long totalRewards = 0L;
		CollectableAlbum album = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);
		if (album != null)
		{
			totalRewards += album.rewardAmount;
			List<CollectableSetData> currentSets = Collectables.Instance.getSetsFromAlbum(album.keyName);
			for (int i = 0; i < currentSets.Count; i++)
			{
				totalRewards += currentSets[i].rewardAmount;
			}
		}

		totalRewardsLabel.text = CreditsEconomy.convertCredits(totalRewards);

		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "hir_collection",
			phylum: "rewards_increased",
			klass: SlotsPlayer.instance.vipNewLevel.ToString(),
			genus: "view"
		);
	}

	public void closeClicked(Dict args = null)
	{
		Dialog.close();
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "hir_collection",
			phylum: "rewards_increased",
			klass: SlotsPlayer.instance.vipNewLevel.ToString(),
			family: "close",
			genus: "click"
		);
	}

	public void okayClicked(Dict args = null)
	{
		CollectableAlbumDialog.showDialog(Collectables.currentAlbum, "rewards_increased");
		Dialog.close();
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "hir_collection",
			phylum: "rewards_increased",
			klass: SlotsPlayer.instance.vipNewLevel.ToString(),
			family: "collections",
			genus: "click"
		);
	}

	public override void close()
	{

	}

	public static bool showDialog()
	{
		Scheduler.addDialog("collectables_rewards_increased", Dict.create(D.MOTD_KEY, "collectables_rewards_increased"));
		return true;
	}
}
