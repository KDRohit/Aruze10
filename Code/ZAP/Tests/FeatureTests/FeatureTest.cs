using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class FeatureTest : Test
	{
		// With variable numbers of dialogs that can autopop on their own, we want to delay the test start
		// to make sure that whatever dialog we try to open wont get potentially autoclosed.
		[SerializeField] protected float testStartDelayTime = 2.0f;
		[SerializeField] protected float timeoutThreshold = 30f;

#region Test Implementation
		// Empty constructor needed for serialization.
		public FeatureTest(){}

		// we need this because it is a requirement for ISerializable to deserialize the data
		public FeatureTest(SerializationInfo info, StreamingContext context)
		{
			DeserializeBaseData(info, context);
		}

		public override void DeserializeBaseData(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
			info.TryGetValue<int>("iterations", out iterations);
			info.TryGetValue<float>("timeoutThreshold", out timeoutThreshold);
			info.TryGetValue<float>("testStartDelayTime", out testStartDelayTime);
		}

		public override IEnumerator doTest()
		{
			throw new System.NotImplementedException();
		}

		// serialize the data here
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("timeoutThreshold", timeoutThreshold);
			info.AddValue("testStartDelayTime", testStartDelayTime);
		}

		public override void init()
		{
			base.init();
		}

		public override List<string> compatibleAutomatables(List<string> potentialAutomatables)
		{
			// By default a feature test is valid for only AutomatableDialog.
			return new List<string>{"AutomatableDialog"};
		}
#endregion
#region Test Helper Methods/Variables/Properties

		// Shared timeout counter, reset before using.
		protected float timeoutCounter = 0f;
		// Shared boolean for whether the last dialog opened was the one we wanted.
		protected bool _wasCorrectDialog = true;
		protected bool wasCorrectDialog
		{
			get
			{
				return _wasCorrectDialog;
			}
		}

		// Waits for the loading screen and MOTDs to close, and a neutral lobby to be there.
		protected IEnumerator waitForNeutralLobby()
		{
			addTestStepLog("Waiting", "Waiting for a neutral lobby.");
			// Wait for loading screen to end.
			while (Loading.isLoading)
			{
				yield return new WaitForSeconds(0.5f);
			}
			// Way for a couple seconds (variable) to make sure everything has queued that will.
			yield return new WaitForSeconds(testStartDelayTime);
			
			// Clear any open dialogs that are queued.
			yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());

			// Wait for a second to make sure everything has closed.
			yield return new WaitForSeconds(1f);
		}

		protected IEnumerator waitForGameToSpin()
		{
			while(ZyngaAutomatedPlayer.instance.wait())
			{
				yield return null;
			}
		}

		protected IEnumerator waitForDialogToOpen(string dialogKey = "")
		{
			addTestStepLog("OPENING DIALOG",
				string.Format("Waiting for Dialog to open: {0}", dialogKey));
			_wasCorrectDialog = true;
			// Wait for the dialog to open.
			timeoutCounter = 0.0f;
			while (Dialog.instance.currentDialog == null)
			{
				// Wait for the dialog to open.
				timeoutCounter += 0.5f;
				if (timeoutCounter >= timeoutThreshold)
				{
					addTestStepLog("Timeout", "timeout trying to open the dialog. Breaking out.");
					break;
				}
				yield return new WaitForSeconds(0.5f);
			}

			if (!string.IsNullOrEmpty(dialogKey))
			{
				if (Dialog.instance != null && Dialog.instance.currentDialog != null)
				{
					string openDialogKey = Dialog.instance.currentDialog.type.keyName;
					if (openDialogKey != dialogKey)
					{
						// If we specified a dialog, check to make sure the right one loaded.
						addTestStepLog(
							"OPENING DIALOG",
							string.Format("Expected dialog {0}, open dialog is: {1}",
								dialogKey,
								Dialog.instance.currentDialog.type.keyName
							));
						// Set this to false.
						_wasCorrectDialog = false;
					}
				}
			}
		}
#endregion
	}
#endif
}
