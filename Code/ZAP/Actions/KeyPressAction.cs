using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class KeyPressAction : Action
	{
		public string key = "";

		public KeyPressAction(string key)
		{
			this.key = key;
		}		

		public override IEnumerator doAction(params GameObject[] args)
		{
			Debug.Log("ZAPLOG -- KeyPressAction: ZAP is pressing " + key);
			yield return RoutineRunner.instance.StartCoroutine(Input.simulateKeyPress(key));
		}
	}
#endif
}
