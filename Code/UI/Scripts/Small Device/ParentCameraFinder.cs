using UnityEngine;
using System.Collections;

/// Attach this script to an object with a UIAnchor to have the anchor automatically find the parent when void instead of the nearest one.
[RequireComponent(typeof(UIAnchor))]
public class ParentCameraFinder : MonoBehaviour
{
	// public void Start()
	// {
	// 	findAndAttachParentCamera();
	// }
	// 
	// private void findAndAttachParentCamera()
	// {
	// 	GameObject objectBeingScanned = this.gameObject;
	// 	Camera cameraObjectToSet = objectBeingScanned.GetComponent<Camera>();
	// 	while (cameraObjectToSet == null && objectBeingScanned.transform.parent != null)
	// 	{
	// 		objectBeingScanned = objectBeingScanned.transform.parent.gameObject;
	// 		cameraObjectToSet = objectBeingScanned.GetComponent<Camera>();
	// 	}
	// 	if (cameraObjectToSet != null)
	// 	{
	// 		//Modify the anchor that is required to be altered
	// 		UIAnchor anchorToModify = this.gameObject.GetComponent<UIAnchor>();
	// 		anchorToModify.uiCamera = cameraObjectToSet;
	// 		//Adjust the stretch on this component as well, if it exists
	// 		UIStretch stretchToModify = this.gameObject.GetComponent<UIStretch>();
	// 		if(stretchToModify != null)
	// 		{
	// 			stretchToModify.uiCamera = cameraObjectToSet;
	// 		}
	// 
	// 	}
	// }
} 