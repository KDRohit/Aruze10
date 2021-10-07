using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class OpenCloseDialogTest : Test
	{
		[SerializeField] public string openAction;
		[SerializeField] public float openActionTime;
		[SerializeField] public string closeAction;
		[SerializeField] public float closeActionTime;

		// need a default constructor to create objects
		public OpenCloseDialogTest()
		{
		}

		// we need this because it is a requirement for ISerializable to deserialize the data
		public OpenCloseDialogTest(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
			info.TryGetValue<string>("openAction", out openAction);
			info.TryGetValue<float>("openActionTime", out openActionTime);
			info.TryGetValue<string>("closeAction", out closeAction);
			info.TryGetValue<float>("closeActionTime", out closeActionTime);
		}

		public override void init()
		{
			base.init();
		}

		public override IEnumerator doTest()
		{
			for (int i = 0; i < iterations; i++)
			{
				ZyngaAutomatedPlayer.instance.shouldClearDialogs = true;
				while (CommonAutomation.IsDialogActive())
				{
					yield return null;
				}
				ZyngaAutomatedPlayer.instance.shouldClearDialogs = false;

				TICoroutine actionCoroutine = RoutineRunner.instance.StartCoroutine(ActionController.doAction(openAction));
				yield return actionCoroutine;
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Dialog Opening"));
				yield return new WaitForSeconds(openActionTime);

				if (!Dialog.instance.isShowing || Dialog.instance.isOpening)
				{
					Debug.LogErrorFormat("{0} failed : Dialog.instance.isShowing {1} Dialog.instance.isOpening {2}", openAction, Dialog.instance.isShowing, Dialog.instance.isOpening);
					result.additionalInfo.Add(new KeyValuePair<string, string>("ActionFailed", string.Format("-=-=-= {0} failed : Dialog.instance.isShowing {1} Dialog.instance.isOpening {2}", openAction, Dialog.instance.isShowing, Dialog.instance.isOpening)));
					testIsFinished();
					yield break;
				}

				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(closeAction));
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Dialog Closing"));
				if (!Dialog.instance.isClosing)
				{
					Debug.LogErrorFormat("{0} failed to start closing: Dialog.instance.isShowing {1} Dialog.instance.isClosing {2}", closeAction, Dialog.instance.isShowing, Dialog.instance.isClosing);
					testIsFinished();
					yield break;
				}
				yield return new WaitForSeconds(closeActionTime);
			}

			ZyngaAutomatedPlayer.instance.shouldClearDialogs = true;
			testIsFinished();
		}

		// serialize the data here
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("openAction", openAction);
			info.AddValue("openActionTime", openActionTime);
			info.AddValue("closeAction", closeAction);
			info.AddValue("closeActionTime", closeActionTime);
		}

		public override List<string> compatibleAutomatables(List<string> potentialAutomatables)
		{
			// By default a test is valid for every type passed in.
			return new List<string>(){"AutomatableDialog"};
		}		
	}
#endif
}
