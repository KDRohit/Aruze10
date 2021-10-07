using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Attaches to and appears over QFCMapDialog when player reached node with a story localization associated with it
**/
namespace QuestForTheChest
{
	public class QFCStoryNodeOverlay : QFCMapDialogOverlay
	{
		[SerializeField] private TextMeshPro creditsRewardLabel;
		[SerializeField] private ButtonHandler collectButton;
		[SerializeField] private ButtonHandler closeButton;

		private long creditsReward = 0;

		public void init(int nodeIndex, List<QFCReward> rewards)
		{
			setUpDynamicNodeAssets(nodeIndex);
			collectButton.registerEventDelegate(collectClicked);
			closeButton.registerEventDelegate(closeClicked); //Might not need a seperate event if functionality is the same as Collect button

			initRewards(rewards);
		}

		private void initRewards(List<QFCReward> rewards)
		{
			for (int i = 0; i < rewards.Count; i++)
			{
				switch (rewards[i].type)
				{
					case "coin":
						creditsReward = rewards[i].value;
						creditsRewardLabel.text = CreditsEconomy.convertCredits(creditsReward);
						break;
					default:
						Debug.LogWarningFormat("Reward type {0} is unhandled", rewards[i].type);
						break;
				}
			}
		}

		private void closeClicked(Dict data = null)
		{
			gameObject.SetActive(false);
		}

		private void collectClicked(Dict data = null)
		{
			gameObject.SetActive(false);
		}
	}
}
