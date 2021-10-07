using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Attach to a NGUI button to give it "click and hold" functionality,
so that something happens after touching and holding a button for specified amount of time.
*/

public class ClickAndHold : TICoroutineMonoBehaviour
{
	public float holdTime = 1f;
	public GameObject target;
	public string functionName;
	public bool includeChildren = false;
	
	private float startClickTime = 0;
	
	void OnPress (bool isPressed)
	{
		if (isPressed)
		{
			startClickTime = Time.realtimeSinceStartup;
		}
		else
		{
			startClickTime = 0;
		}
	}
	
	void Update()
	{
		if (startClickTime > 0 && Time.realtimeSinceStartup - startClickTime >= holdTime)
		{
			startClickTime = 0;
			Send();
		}
	}

	/// This Send() function stolen from UIButtonMessage and modified for our standards.
	private void Send()
	{
		if (string.IsNullOrEmpty(functionName))
		{
			return;
		}
		if (target == null)
		{
			target = gameObject;
		}

		if (includeChildren)
		{
			Transform[] transforms = target.GetComponentsInChildren<Transform>();

			for (int i = 0, imax = transforms.Length; i < imax; ++i)
			{
				Transform t = transforms[i];
				t.gameObject.SendMessage(functionName, gameObject, SendMessageOptions.DontRequireReceiver);
			}
		}
		else
		{
			target.SendMessage(functionName, gameObject, SendMessageOptions.DontRequireReceiver);
		}
	}
	
}
