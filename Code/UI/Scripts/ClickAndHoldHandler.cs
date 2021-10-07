using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class ClickAndHoldHandler : ClickHandler
{
	public float holdTime = 0.0f;
	private bool isHeld = false;
	private float timeHeld = 0.0f;

	void Start()
	{
		if (registeredEvent != MouseEvent.OnHold)
		{
			Debug.LogWarningFormat("ClickAndHoldHandler requires the OnHold MouseEvent but its not set for this object: {0}", gameObject.name);
		}
	}

	public override void OnPress (bool isPressed)
	{
		if (isPressed)
		{
			timeHeld = 0.0f;
			isHeld = true;
		}
		else
		{
			isHeld = false;
		}
	}

	void Update()
	{
		if (isHeld)
		{
			if (timeHeld >= holdTime)
			{
				isHeld = false;
				handleMouseEvent(MouseEvent.OnHold);
			}
			else
			{
				timeHeld += Time.deltaTime;
			}
		}
	}
}
