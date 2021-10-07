using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RichPassRewardIconElite : RichPassRewardIcon
{
	[SerializeField] private LabelWrapperComponent amountLabel;
	
	public override void init(PassReward rewardToAward, RichPassCampaign.RewardTrack tier, SlideController parentSlideController)
	{
		base.init(rewardToAward, tier, parentSlideController);
		amountLabel.text = CommonText.formatNumber(rewardToAward.amount);
	}
}
