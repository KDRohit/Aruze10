using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public abstract class Action : UnityEngine.ScriptableObject
	{
		public string action = "";

		public virtual IEnumerator doAction(params GameObject[] args)
		{
			throw new System.NotImplementedException();
		}

		public virtual void initAction()
		{
			throw new System.NotImplementedException();
		}

		public virtual bool isValid()
		{
			// Assume all actions are valid unless specified otherwise since only
			// the NGUI buttons right now have invalid states
			return true;
		}
	}
#endif
}
