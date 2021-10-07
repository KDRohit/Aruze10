using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SlotventuresProgressPanel : MonoBehaviour
{
	// Try to center this depending on how many nodes we have
	public GameObject nodeParent;
	public GameObject firstNode; // We're going to be making copies of this
	public GameObject backgroundSprite;
	public GameObject dynamicElements;
	public List<SlotventuresProgressPip> pips = new List<SlotventuresProgressPip>();
	private const int PIP_SPACING = 80;
	private const int PIP_ROTATION = 15;
	private const int PIP_VERTICAL_POSITION = -18;

	[SerializeField] private rewardBar jackpotBar;

	public void init(ChallengeLobbyCampaign slotventuresCampaign)
	{
		if (slotventuresCampaign != null && slotventuresCampaign.lastMission != null &&
		    slotventuresCampaign.lastMission.rewards != null && slotventuresCampaign.lastMission.rewards.Count > 0)
		{
			// Display the jack pot. It could be a loot box, or could be coins
			if (jackpotBar != null)
			{
				jackpotBar.showJackPot(slotventuresCampaign.lastMission.rewards[0]);
			}
		}

		int numberCreated = 0;
		bool isLowerPip = false;

		CommonTransform.setWidth(backgroundSprite.transform, backgroundSprite.transform.localScale.x + (slotventuresCampaign.missions.Count-2) * PIP_SPACING);
		CommonTransform.setX(dynamicElements.transform, 0 + slotventuresCampaign.missions.Count * (PIP_SPACING/2));
		Vector3 rotationVector = Vector3.zero;

		// If it's odd, we'll end on a lower platform. If it's even we'll end on a higher platform.
		if ((slotventuresCampaign.missions.Count-1) % 2 != 0)
		{
			isLowerPip = true;
			rotationVector = firstNode.transform.localEulerAngles;
			rotationVector.z = PIP_ROTATION;
			firstNode.transform.localEulerAngles = rotationVector;
			CommonTransform.setY(firstNode.transform, PIP_VERTICAL_POSITION);
		}

		// The nodes are made from right to left so do this in reverse. Also the -2 is because the jackpot node does not count as progress.
		for (int i = slotventuresCampaign.missions.Count - 2; i >= 0; i--)
		{
			GameObject createdObject;
			// The first pip is always there. Technically the coin will be a mission eventually, so this will become -2
			if (i < slotventuresCampaign.missions.Count - 2)
			{
				createdObject = NGUITools.AddChild(nodeParent, firstNode);
				CommonTransform.setX(createdObject.transform, firstNode.transform.localPosition.x - (PIP_SPACING * numberCreated));
				if (isLowerPip)
				{
					CommonTransform.setY(createdObject.transform, PIP_VERTICAL_POSITION);
					rotationVector = createdObject.transform.localEulerAngles;
					rotationVector.z = PIP_ROTATION;
					createdObject.transform.localEulerAngles = rotationVector;
				}
				else
				{
					CommonTransform.setY(createdObject.transform, 0);
					rotationVector = createdObject.transform.localEulerAngles;
					rotationVector.z = -PIP_ROTATION;
					createdObject.transform.localEulerAngles = rotationVector;
				}
			}
			else
			{
				createdObject = firstNode;
			}

			pips.Add(createdObject.GetComponent<SlotventuresProgressPip>());

			if (slotventuresCampaign.missions[i].isComplete)
			{
				pips[numberCreated].playFull();
			}
			else if (slotventuresCampaign.currentEventIndex == i)
			{
				pips[numberCreated].playActive();
			}
			else
			{
				pips[numberCreated].playEmpty();
			}

			isLowerPip = !isLowerPip;
			numberCreated++;
		} 	
	}

	public void clear()
	{
		if (nodeParent != null)
		{
			int childs = nodeParent.transform.childCount;
			for (int i = childs - 1; i > 0; i--)
			{
				GameObject.Destroy(transform.GetChild(i).gameObject);
			}
		}
	}

	public void fillPipsToIndex(int index)
	{
		// Play the fill animation on the node array at the current mission index.
		// Or play the animation, wait, then play it for the next node if we somehow finished more than one mission
	}
}
