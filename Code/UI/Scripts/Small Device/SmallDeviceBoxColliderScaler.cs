using UnityEngine;
using System.Collections;

/**
 Use this component to apply a multiplying factor for box colliders on small devices.  It works very similar to the sprite scaler.
 */
[RequireComponent(typeof(BoxCollider))]
public class SmallDeviceBoxColliderScaler : MonoBehaviour
{
	public float xFactor = 0.0f;
	public float yFactor = 0.0f;
	///When we start up the prefab, scale up the collider box by the factor specified
	void Awake()
	{
		if (MobileUIUtil.isSmallMobile)
		{
			BoxCollider box = gameObject.GetComponent<BoxCollider>();
			Vector3 boxSize = box.size;
			Vector3 boxCenter = box.center;
			if (xFactor > 0) 
			{
				boxSize.x = box.size.x * xFactor;
				boxCenter.x = box.center.x * xFactor;
			}
			if (yFactor > 0) 
			{
				boxSize.y = box.size.y * yFactor;
				boxCenter.y = box.center.y * yFactor;
			}
			box.size = boxSize;
			box.center = boxCenter;
		}
		Destroy (this);
	}
}

