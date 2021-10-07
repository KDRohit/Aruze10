using UnityEngine;
using System.Collections;

/**
 Use this component to apply a multiplying factor for capsule colliders on small devices.
 */
[RequireComponent(typeof(CapsuleCollider))]
public class SmallDeviceCapsuleColliderScaler : MonoBehaviour
{
	public float radiusFactor = 1.0f;
	public float heightFactor = 1.0f;
	
	///When we start up the prefab, scale the collider by the factor specified
	void Awake()
	{
		if (MobileUIUtil.isSmallMobile)
		{
			CapsuleCollider cap = gameObject.GetComponent<CapsuleCollider>();
			Vector3 center = cap.center;

			cap.radius *= radiusFactor;
			cap.height *= heightFactor;

			// Also adjust the centering based on which direction the capsule is oriented.
			if (cap.direction == 0)
			{
				// Along the x axis.
				center.x *= heightFactor;
				center.y *= radiusFactor;
			}
			else if (cap.direction == 1)
			{
				// Along the y axis.
				center.x *= radiusFactor;
				center.y *= heightFactor;
			}
			
			cap.center = center;
		}
		Destroy (this);
	}
}

