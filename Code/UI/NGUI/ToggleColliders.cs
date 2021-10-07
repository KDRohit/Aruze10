using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Toggles all linked colliders. Called as an NGUI click event.
*/

public class ToggleColliders : TICoroutineMonoBehaviour
{
	public List<GameObject> colliders = null;
	public bool enableFlag = false;
	
	public void OnClick()
	{
		toggleCollider();
	}
	
	public void toggleCollider()
	{
		foreach (GameObject g in colliders)
		{
			Collider col = g.GetComponent<Collider>();
			if (col != null)
			{
				col.enabled = enableFlag;
			}
		}
		enableFlag = !enableFlag;
	}
}

