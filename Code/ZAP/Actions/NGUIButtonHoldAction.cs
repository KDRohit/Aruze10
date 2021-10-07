using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class NGUIButtonHoldAction : Action
	{
		public string id = "";
		public string path = "";
		public int time = 0;

		private Collider collider;
		private Camera buttonCamera;
		
		public override void initAction()
		{
			collider = GameObject.Find(path).GetComponent<Collider>();
			buttonCamera = NGUIExt.getObjectCamera(collider.gameObject); ;
		}
				
		public override IEnumerator doAction(params GameObject[] args)
		{
			initAction();
			yield return RoutineRunner.instance.StartCoroutine(Input.simulateMouseClickOn(collider, time, buttonCamera));
		}
	}
#endif
}