using UnityEngine;

/**
InitAssetLayers is used by downloaded assets to set the entire object tree to the same layer as the parent object
  Make sure to put this in the root object, as that is the only one guaranteed to be set to the parent's layer
*/
public class InitAssetLayers : TICoroutineMonoBehaviour
{
	// Use this for initialization
	void Start ()
	{
		CommonGameObject.setLayerRecursively(gameObject, gameObject.layer);
		Destroy(this);
	}
}

