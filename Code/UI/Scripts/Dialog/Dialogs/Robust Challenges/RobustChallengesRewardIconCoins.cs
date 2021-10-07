using UnityEngine;
using System.Collections;
using TMPro;

public class RobustChallengesRewardIconCoins : RobustChallengesRewardIcon 
{
	[SerializeField] private Animator coinParticleAnimator;

	public override void init(MissionReward reward)
	{
		//Override this in child classes
		rewardLabel.text = CreditsEconomy.convertCredits(reward.amount);
	}

	public override float onCollect()
	{
		coinParticleAnimator.Play("on");
		return 1.5f;
	}
}
