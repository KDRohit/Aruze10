using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class RandomClickAction : Action
	{
		public RandomClickAction()
		{
		}

		public override IEnumerator doAction(params GameObject[] args)
		{
			yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.clickRandomColliderIn(args[0]));
		}
	}
#endif
}