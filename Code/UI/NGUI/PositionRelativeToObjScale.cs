using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UIAnchor))]
public class PositionRelativeToObjScale : MonoBehaviour
{

	private const float EPSILON = 0.001f;

	[SerializeField] private Transform watch;
	private UIAnchor anchor;
	private Transform localTransform;
	private float scaleX;

	private void Awake()
	{
		localTransform = gameObject.transform;
		anchor = gameObject.GetComponent<UIAnchor>();
	}


	private void Update()
	{
		if (anchor == null)
		{
			enabled = false;
			return;
		}

		float newScaleX = watch.localScale.x;
		float diffX = Mathf.Abs(scaleX - newScaleX);
		if (diffX > EPSILON)
		{
			scaleX = newScaleX;
			Vector3 currentPosition = localTransform.localPosition;
			float newX = 0;
			switch (anchor.side)
			{
				case UIAnchor.Side.Left:
					newX = -(scaleX / 2.0f) + anchor.pixelOffset.x;
					break;
			}
			localTransform.localPosition = new Vector3(newX, currentPosition.y, currentPosition.z);
		}
	}

}
