using UnityEngine;
using System.Collections;

//This is to provide the artists with a new way to attach VFX Prefabs to Model Prefabs.
public class VfxPrefabInstancer : MonoBehaviour
{
	public GameObject prefab;

	public Transform parent;
	private GameObject _instance;

	// Instantiate and parent the prefabs
	void OnEnable()
	{
		if (_instance == null) 
		{
			_instance = CommonGameObject.instantiate(prefab) as GameObject;
			Transform tform = _instance.transform;
			Transform localParent;
			if (null == parent) 
			{
				localParent = gameObject.transform;
			} 
			else
			{
				localParent = parent;
			}
			tform.parent = localParent;
			tform.localPosition = Vector3.zero;
			tform.localRotation = Quaternion.identity;
			tform.localScale = Vector3.one;
		} 
		else 
		{
			_instance.SetActive(false);
			_instance.SetActive(true);
		}
	}

}