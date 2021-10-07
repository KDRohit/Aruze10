using UnityEngine;
using System.Collections;

public class DestroyNonParentedObjectOnDestroy : TICoroutineMonoBehaviour 
{
	public GameObject someGameObject;
	
	void OnDestroy()
	{
		if (someGameObject != null && someGameObject != gameObject)
		{
			Destroy(someGameObject);
		}
	}
}
