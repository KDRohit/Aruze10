using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RobustChallengesRewardIconCards : RobustChallengesRewardIcon 
{
	[SerializeField] private GameObject cardPackPrefab;
	[SerializeField] private GameObject cardPackParent;
	[SerializeField] private GameObject[] starObjects;
	[SerializeField] private UICenteredGrid starGrid;
	[SerializeField] private GameObject doublePackParent;
	[SerializeField] private TextMeshPro multiplierLabel;
	
	private CollectablePack cardPack;

	private const string MIN_NUM_CARDS_LOC_KEY = "minimum_{0}_card";

	public override void init (MissionReward reward)
	{
		//Set up label and stars and card pack
		string packDataKey = reward.cardPackKeyName.IsNullOrWhiteSpace() ? Collectables.getChallengePack(reward.packIndex, reward.packType) : reward.cardPackKeyName;
		CollectablePackData packData = Collectables.Instance.findPack(packDataKey);
		GameObject instancedCardPack = NGUITools.AddChild(cardPackParent, cardPackPrefab);
		cardPack = instancedCardPack.GetComponent<CollectablePack>();
		if (cardPack != null)
		{
			cardPack.init(packDataKey, true);
		}
		
		long rewardAmount = reward.amount;
		
		doublePackParent.SetActive(rewardAmount > 1);
		multiplierLabel.text = string.Format("{0}X", rewardAmount);
		if (packData != null)
		{
			string locString = packData.constraints[0].guaranteedPicks > 1 ? MIN_NUM_CARDS_LOC_KEY + "_plural" : MIN_NUM_CARDS_LOC_KEY;
			rewardLabel.text = Localize.text(locString, packData.constraints[0].guaranteedPicks);

			for (int i = 0; i < packData.constraints[0].minRarity; i++)
			{
				if (i < starObjects.Length)
				{
					starObjects[i].SetActive(true);
				}
				else
				{
					break;
				}
			}
			starGrid.reposition();
		}
	}
}
