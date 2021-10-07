using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System;
using System.Reflection;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class NGUIButtonClickAction : Action
	{
		public string zapButtonId;

		private Collider collider;
		private Camera buttonCamera;


		public NGUIButtonClickAction(string buttonId)
		{
			this.zapButtonId = buttonId;
		}

		public override void initAction()
		{
			getButtonCollider();
			getButtonCamera();
		}

		private void getButtonCollider()
		{
			if (!string.IsNullOrEmpty(zapButtonId))
			{
				getColliderFromZapButtonId();
			}
			else
			{
				Debug.LogError("ZAPLOG -- Zap Error: not sure how to get the collider for this button");
			}
		}

		private void getColliderFromZapButtonId()
		{
			ZapButton[] zapButtons = FindObjectsOfType<ZapButton>();
			foreach (ZapButton zapButton in zapButtons)
			{
				if (zapButton.buttonId == zapButtonId)
				{
					collider = zapButton.gameObject.GetComponent<Collider>();

					if (collider == null)
					{
						Debug.LogError("ZAPLOG -- Zap Error: no collider found using the for buttonId " + zapButtonId);
					}

					return;
				}
			}

			Debug.LogError("ZAPLOG -- Zap Button not found : " + zapButtonId);
		}

		private void getButtonCamera()
		{
			if (collider != null)
			{
				buttonCamera = NGUIExt.getObjectCamera(collider.gameObject);

				if (buttonCamera == null)
				{
					Debug.LogError("ZAPLOG -- Zap Error: no NGUI camera found for " + collider.gameObject.name);
				}
			}
		}

		public override IEnumerator doAction(params GameObject[] args)
		{
			Debug.LogFormat("ZAPLOG -- trying to do ngui click action: {0}", zapButtonId);
			initAction();
			if (collider != null && buttonCamera != null)
			{
				yield return RoutineRunner.instance.StartCoroutine(Input.simulateMouseClickOn(collider, 0, buttonCamera));
			}
		}

		public override bool isValid()
		{
			initAction();
			return collider != null && buttonCamera != null;
		}
	}
#endif
}
