using UnityEngine;
using System.Collections;

/**
This creates an instance of a prefab as a child to itself, and then self-destructs.
*/
public class AddPrefabChild : TICoroutineMonoBehaviour
{
	public GameObject prefabTemplate;
	
	void Start()
	{
		GameObject newObject = CommonGameObject.instantiate(prefabTemplate) as GameObject;
		newObject.name = prefabTemplate.name;
		newObject.transform.parent = transform;
		newObject.transform.localPosition = Vector3.zero;
		newObject.transform.localRotation = Quaternion.identity;
		newObject.transform.localScale = Vector3.one;
		Destroy(this);
	}
}
