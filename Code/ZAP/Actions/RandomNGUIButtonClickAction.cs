using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using System.Reflection;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class RandomNGUIButtonClickAction : Action
	{
		public string zapButtonId;

		private List<ColliderCameraPair> pairs;

		public RandomNGUIButtonClickAction(string buttonId)
		{
			this.zapButtonId = buttonId;
		}

		public override void initAction()
		{
			findAllPairs();
		}

		private void findAllPairs()
		{
			pairs = new List<ColliderCameraPair>();
			List<Collider> colliders = new List<Collider>();
			Collider collider = null;
			ZapButton[] zapButtons = FindObjectsOfType<ZapButton>();
			for (int i = 0; i < zapButtons.Length; i++)
			{
				if (zapButtons[i].buttonId == zapButtonId)
				{
					collider = zapButtons[i].gameObject.GetComponent<Collider>();

					if (collider == null)
					{
						Debug.LogError("ZAPLOG -- Zap Error: no collider found using the for buttonId " + zapButtonId);
					}
					else
					{
						colliders.Add(collider);
					}
				}
			}

			for (int i = 0; i < colliders.Count; i++)
			{
				Camera cam = getButtonCameraFromCollider(colliders[i]);
				if (cam != null)
				{
					pairs.Add(new ColliderCameraPair(colliders[i], cam));
				}
				else
				{
					Debug.LogErrorFormat(
						"ZAPLOG -- RandomNGUIButtonClickAction.findAllPairs() -- null camera for collider on object: {0}",
						colliders[i].gameObject.name);
				}
			}
		}

		private Camera getButtonCameraFromCollider(Collider collider)
		{
			Camera buttonCamera = null;
			if (collider != null)
			{
				buttonCamera = NGUIExt.getObjectCamera(collider.gameObject);

				if (buttonCamera == null)
				{
					Debug.LogError("ZAPLOG -- Zap Error: no NGUI camera found for " + collider.gameObject.name);
				}
			}

			return buttonCamera;
		}

		public override IEnumerator doAction(params GameObject[] args)
		{
			Debug.LogFormat("ZAPLOG -- trying to do random click action: {0}", zapButtonId);
			initAction();
			if (pairs == null || pairs.Count == 0)
			{
				Debug.LogError("Zap Button not found : " + zapButtonId);
			}

			// Pick a random valid collider
			ColliderCameraPair chosenOne = null;
			System.Random random = new System.Random();

			while (chosenOne == null)
			{
				if (pairs == null || pairs.Count == 0)
				{
					Debug.LogErrorFormat(
						"ZAPLOG -- RandomNGUIButtonClickAction.doAction() -- Couldn't find a valid button to click on for buttonId: {0}",
						zapButtonId);
					yield break;
				}

				int index = random.Next(0, pairs.Count);
				chosenOne = pairs[index];
				if (chosenOne.collider == null || chosenOne.buttonCamera == null)
				{
					// Remove it from the list and null it out so that we grab another;
					pairs.Remove(chosenOne);
					chosenOne = null;
				}
			}

			yield return RoutineRunner.instance.StartCoroutine(Input.simulateMouseClickOn(chosenOne.collider, 0,
				chosenOne.buttonCamera));
		}

		public override bool isValid()
		{
			initAction();
			if (pairs == null)
			{
				return false;
			}

			for (int i = 0; i < pairs.Count; i++)
			{
				if (pairs[i].collider != null && pairs[i].buttonCamera != null)
				{
					return true;
				}
			}

			return false;
		}

		class ColliderCameraPair
		{
			public Collider collider;
			public Camera buttonCamera;

			public ColliderCameraPair(Collider collider, Camera buttonCamera)
			{
				this.collider = collider;
				this.buttonCamera = buttonCamera;
			}
		}
	}
#endif
}