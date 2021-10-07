using UnityEngine;
using System.Collections;

/**
 * Simple class for disabling colliders in NGUI
 */
public class DisableChildCollision : TICoroutineMonoBehaviour 
{
	public bool enableFlag = false;
	
	void OnClick()
	{
		disableColliders();
	}
	
	public void disableColliders()
	{
		foreach (Collider coll in gameObject.GetComponentsInChildren<Collider>()) 
		{
			// only do the children of this
			if (coll != this.GetComponent<Collider>()) 
			{
				coll.enabled = enableFlag;
			}
		}
		enableFlag = !enableFlag;
	}
}

