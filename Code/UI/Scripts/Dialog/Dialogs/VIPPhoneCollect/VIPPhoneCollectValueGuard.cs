using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

public class VIPPhoneCollectValueGuard : MonoBehaviour {

	public BoxCollider[] inputfieldColliders;

	private void OnSelect(bool isSelect)
	{
		if (!isSelect)
		{
			// Deselected
			StartCoroutine(setActiveInputfield());
			return;
		}

		// Selected
		foreach (BoxCollider collider in inputfieldColliders)
		{
			if (collider.enabled) {
				collider.enabled = false;
			}
		}
	}

	IEnumerator setActiveInputfield()
	{
		VIPPhoneCollectDialog.instance.OnSubmit("");
		// Delay for prevent the Virtual keyboard confused focus on IOS
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			yield return new WaitForSeconds(0.3f);
		}
		foreach (BoxCollider collider in inputfieldColliders)
		{
			collider.enabled = true;
		}

	}
}
