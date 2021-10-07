using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostPurchaseChallengeProgressMeter : MonoBehaviour
{
	[SerializeField] private UIMeterNGUI progressMeter;
	

	public void updateProgress(PostPurchaseChallengeCampaign campaign)
	{
		if (campaign != null)
		{
			progressMeter.setState(campaign.getCurrentAmount(), campaign.getTargetAmount());
		}
	}
	
	public void updateProgress(int percentAmount)
	{
		progressMeter.setState(percentAmount, 100);
	}
}
