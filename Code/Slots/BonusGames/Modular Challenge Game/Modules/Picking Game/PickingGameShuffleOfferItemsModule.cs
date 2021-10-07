using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingGameShuffleOfferItemsModule : PickingGameModule
{
	[SerializeField] protected string shuffleAnimation;
	[SerializeField] protected float waitTimeBetweenGroups;
	[SerializeField] protected float waitTimeBetweenItems;
	[SerializeField] protected int itemsToGroup;
	[SerializeField] protected int itemGroupsToShuffle;
	[SerializeField] protected AudioListController.AudioInformationList shuffleSounds;

	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		List<PickingGameBasePickItem> picks = new List<PickingGameBasePickItem>(this.pickingVariantParent.pickmeItemList);

		for (int i = 0; i < itemGroupsToShuffle; i++)
		{
			CommonDataStructures.shuffleList(picks);

			for (int j = 0; j < itemsToGroup; j++)
			{
				picks[j].pickAnimator.Play(shuffleAnimation);
				if (waitTimeBetweenItems > 0)
				{
					yield return new TIWaitForSeconds(waitTimeBetweenItems);
				}
			}

			yield return StartCoroutine(AudioListController.playListOfAudioInformation(shuffleSounds));
			yield return new TIWaitForSeconds(waitTimeBetweenGroups);
		}

	}
}
