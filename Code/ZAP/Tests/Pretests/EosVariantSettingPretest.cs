using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

/*
Class Name: EosVariantSettingPretest.cs
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
Description: This class is used for whitelisting the current player's zid into a specific variant of an EOS experiment.
Feature-flow: Add this to an AutomatableTestSetup to make sure that you are in the desired test variant.
*/
namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class EosVariantSettingPretest : Pretest
	{
		[SerializeField] private string eosExperimentName = "";
		[SerializeField] private string eosVariant = "";
		[SerializeField] private float timeoutDuration = 10f;

		private bool isWaitingOnEosChange = false;

		// need a default constructor to create objects
		public EosVariantSettingPretest() {}

		// we need this because it is a requirement for ISerializable to deserialize the data
		public EosVariantSettingPretest(SerializationInfo info, StreamingContext context)
		{
			DeserializeBaseData(info, context);
		}

		public override void DeserializeBaseData(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
			info.TryGetValue<string>("eosExperimentName", out eosExperimentName);
			info.TryGetValue<string>("eosVariant", out eosVariant);
			info.TryGetValue<float>("timeoutDuration", out timeoutDuration);
		}

		// serialize the data here
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("eosExperimentName", eosExperimentName);
			info.AddValue("eosVariant", eosVariant);
			info.AddValue("timeoutDuration", timeoutDuration);
		}

		public override void init()
		{
			base.init();
		}

		public override IEnumerator doTest()
		{
			Debug.LogFormat("ZAPLOG -- Starting EOSVariant test");
			// Wait for us to finish if we are loading something.
			while (Loading.isLoading)
			{
				yield return new WaitForSeconds(0.5f);
			}

			TestingSetupManager.onEosChanged += eosChangeCallback;
			isWaitingOnEosChange = true;
			// Send up the request to put ourselves into the desired variant.
			TestingSetupManager.instance.setEosWhitelist(
				eosExperimentName,
				eosVariant);
			yield return RoutineRunner.instance.StartCoroutine(waitForCallbacks());
			testIsFinished();
		}

		private IEnumerator waitForCallbacks()
		{
			float waitDuration = 0f;
			while (isWaitingOnEosChange)
			{
				yield return new WaitForSeconds(0.5f);
				waitDuration += 0.5f;

				if (waitDuration > timeoutDuration)
				{
					throw new System.Exception("EosVariantSettingPretest.cs -- Time out waiting for eos change to come back. Attempted change EOS: " + eosExperimentName + " variant: " + eosVariant);
				}
			}
			yield break;
		}

		private void eosChangeCallback(string experiment, string variant, bool success, JSON data)
		{
			if (experiment == eosExperimentName)
			{
				if (!success)
				{
					throw new System.Exception(string.Format("Failed to setup the experiment {0} to be in the desired variant: {1}. Variant is {2}", experiment, eosVariant, variant));
				}
				isWaitingOnEosChange = false;
				TestingSetupManager.onEosChanged -= eosChangeCallback;
			}
			else
			{
				Debug.LogErrorFormat("ZAPLOG -- Got a callback but for the wrong eos experiment: {0}, expecting: {1}", experiment, eosExperimentName);
			}
		}
	}
#endif
}
