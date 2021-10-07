using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

/*
Class Name: LiveDataOverridePretest.cs
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
Description: Test to setup specific overrides for the environment/platform you are on to make sure that a feature is active.
Feature-flow: Put this in an AutomatableTestSetup to setup the test environment before the actual test runs.
*/
namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class LiveDataOverridePretest : Pretest
	{
		[SerializeField] private string liveDataKey = "";
		[SerializeField] private ValueType liveDataValueType;
		[SerializeField] private string liveDataValueString;
		[SerializeField] private float timeoutDuration = 10f;

		private enum ValueType
		{
			STRING = 0,
			BOOL = 1,
			JSON = 2,
			FLOAT = 3,
			LONG = 4
		}

		private bool isWaitingOnLiveDataChange = false;

		// need a default constructor to create objects
		public LiveDataOverridePretest() {}

		// we need this because it is a requirement for ISerializable to deserialize the data
		public LiveDataOverridePretest(SerializationInfo info, StreamingContext context)
		{
			DeserializeBaseData(info, context);
		}

		public override void DeserializeBaseData(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
			info.TryGetValue<string>("liveDataKey", out liveDataKey);
			info.TryGetValue<ValueType>("liveDataValueType", out liveDataValueType);
			info.TryGetValue<string>("liveDataValueString", out liveDataValueString);
			info.TryGetValue<float>("timeoutDuration", out timeoutDuration);
		}

		public override void init()
		{
			base.init();
		}

		public override IEnumerator doTest()
		{
			Debug.LogFormat("ZAPLOG -- starting live data override test.");
			// Wait for us to finish if we are loading something.
			while (Loading.isLoading)
			{
				yield return new WaitForSeconds(0.5f);
			}

			isWaitingOnLiveDataChange = true;

			TestingSetupManager.onLiveDataChanged += liveDataChangeCallback;
			// Send up the request to put ourselves into the desired variant.
			switch (liveDataValueType)
			{
				case ValueType.STRING:
					TestingSetupManager.instance.setLiveDataOverride(
						liveDataKey,
						liveDataValueString);
					break;
				case ValueType.BOOL:
					bool boolValue = liveDataValueString == "true" ? true : false;
					TestingSetupManager.instance.setLiveDataOverride(
						liveDataKey,
						boolValue);
					break;
				case ValueType.JSON:
					JSON jsonValue = new JSON(liveDataValueString);
					TestingSetupManager.instance.setLiveDataOverride(
						liveDataKey,
						jsonValue);
					break;
				case ValueType.FLOAT:
					float floatValue = float.Parse(liveDataValueString);
					TestingSetupManager.instance.setLiveDataOverride(
						liveDataKey,
						floatValue);
					break;
				case ValueType.LONG:
					long longValue = long.Parse(liveDataValueString);
					TestingSetupManager.instance.setLiveDataOverride(
						liveDataKey,
						longValue);
					break;
			}


			yield return RoutineRunner.instance.StartCoroutine(waitForCallbacks());
			testIsFinished();
		}

		private IEnumerator waitForCallbacks()
		{
			float waitDuration = 0f;
			while (isWaitingOnLiveDataChange)
			{
				yield return new WaitForSeconds(0.5f);
				waitDuration += 0.5f;

				if (waitDuration > timeoutDuration)
				{
					throw new System.Exception("ZAPLOG -- Time out waiting for livedata to come back. Attempted change LiveData key: " + liveDataKey + " to value: " + liveDataValueString);
				}
			}
			yield break;
		}

		private void liveDataChangeCallback(string liveData, string overriddenValue, bool success, JSON data)
		{
			if (liveData == liveDataKey)
			{
				if (!success)
				{
					throw new System.Exception(string.Format("ZAPLOG -- Failed to setup the live data key {0} to have the desired value: {1}.", liveDataKey, liveDataValueString ));
				}
				TestingSetupManager.onLiveDataChanged -= liveDataChangeCallback;
				isWaitingOnLiveDataChange = false;
			}
		}

		// serialize the data here
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("liveDataKey", liveDataKey);
			info.AddValue("liveDataValueType", liveDataValueType);
			info.AddValue("liveDataValueString", liveDataValueString);
			info.AddValue("timeoutDuration", timeoutDuration);
		}
	}
#endif
}
