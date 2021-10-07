using UnityEngine;
using System.Collections;
using TMPro;

/*
 * Parent Class for all Robust Challenges Rewards
 * Override the init function for each reward types custom functionality/setup
*/
public class RobustChallengesRewardIcon : MonoBehaviour 
{
	[SerializeField] protected TextMeshPro rewardLabel;

	public virtual void init(MissionReward reward)
	{
		//Override this in child classes
	}

	public virtual float onCollect()
	{
		return 0.0f;
	}
}
