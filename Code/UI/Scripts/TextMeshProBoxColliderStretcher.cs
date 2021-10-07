using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Stretches a box collider that's on a TextMeshPro component so that it matches the size of the text.
*/

[ExecuteInEditMode]

public class TextMeshProBoxColliderStretcher : MonoBehaviour
{
	public TextMeshPro label;
	new public BoxCollider collider;
	
	private float lastWidth = 0.0f;
	private float lastHeight = 0.0f;
	private Vector2 lastPivot = Vector2.zero;
		
	void Update()
	{
		if (label == null || collider == null)
		{
			return;
		}
		
		// TODO:UNITY2018:obsoleteTextContainer:confirm
		if (lastWidth != label.bounds.size.x || lastHeight != label.bounds.size.y || lastPivot != label.rectTransform.pivot)
		{
			collider.size = new Vector3(label.bounds.size.x, label.bounds.size.y, 0.0f);
			
			float halfWidth = collider.size.x * 0.5f;
			float halfHeight = collider.size.y * 0.5f;
		
			Vector2 centerOffset = label.rectTransform.pivot;
			centerOffset.x -= 0.5f;
			centerOffset.y -= 0.5f;
			centerOffset *= -1;
			collider.center = new Vector3(centerOffset.x * halfWidth, centerOffset.y * halfHeight, 0.0f);
			
			// Keep the center updated too, based on the label's pivot.
			// switch (textContainer.anchorPosition)
			// {
			// 	case TextContainerAnchors.TopLeft:
			// 		collider.center = new Vector3(halfWidth, -halfHeight, 0.0f);
			// 		break;
			// 	case TextContainerAnchors.Top:
			// 		collider.center = new Vector3(0.0f, -halfHeight, 0.0f);
			// 		break;
			// 	case TextContainerAnchors.TopRight:
			// 		collider.center = new Vector3(-halfWidth, -halfHeight, 0.0f);
			// 		break;
			// 	case TextContainerAnchors.Left:
			// 		collider.center = new Vector3(halfWidth, 0.0f, 0.0f);
			// 		break;
			// 	case TextContainerAnchors.Middle:
			// 		collider.center = Vector3.zero;
			// 		break;
			// 	case TextContainerAnchors.Right:
			// 		collider.center = new Vector3(-halfWidth, 0.0f, 0.0f);
			// 		break;
			// 	case TextContainerAnchors.BottomLeft:
			// 		collider.center = new Vector3(halfWidth, halfHeight, 0.0f);
			// 		break;
			// 	case TextContainerAnchors.Bottom:
			// 		collider.center = new Vector3(0.0f, halfHeight, 0.0f);
			// 		break;
			// 	case TextContainerAnchors.BottomRight:
			// 		collider.center = new Vector3(-halfWidth, halfHeight, 0.0f);
			// 		break;
			// }
			
			lastWidth = label.bounds.size.x;
			lastHeight = label.bounds.size.y;
			lastPivot = label.rectTransform.pivot;
		}
	}
}
