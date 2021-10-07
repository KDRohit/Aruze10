using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class DoSomethingReward : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		RewardAction.validateReward(parameter);
	}
}