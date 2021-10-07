using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class Pretest : Test
	{
		// need a default constructor to create objects
		public Pretest() {}

		// we need this because it is a requirement for ISerializable to deserialize the data
		public Pretest(SerializationInfo info, StreamingContext context)
		{
			DeserializeBaseData(info, context);
		}

		public override void DeserializeBaseData(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
		}

		// serialize the data here
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}

		public override void init()
		{
			base.init();
		}

		public override IEnumerator doTest()
		{
			throw new System.NotImplementedException();
		}

		public override List<string> compatibleAutomatables(List<string> potentialAutomatables)
		{
			// By default a Pretest is only valid for AutomatableTestSetup.
			return new List<string>(){"AutomatableTestSetup"};
		}		
	}
#endif
}
