using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Controls curtains that are used as a transition between lobby rooms.
*/

public class LobbyCurtainsTransition : MonoBehaviour
{
	private float TWEEN_TIME = 0.75f;

	public GameObject left;		// The left side of the curtains.
	public GameObject right;	// The right side of the curtains.
	public MeshRenderer rendererLeft;
	public MeshRenderer rendererRight;
	
	// This one is bundled.
	public const string CURTAIN_IMAGE_PATH = "vip_curtain/Curtain.png";

	public void Awake()
	{
		// Put the curtains in the Dialog camera area, but behind where dialogs would be,
		// so they will appear on top of the friends bar when closing, but still behind
		// any dialogs they may appear during the transition (which should almost never happen).
		NGUIExt.attachToAnchor(gameObject, NGUIExt.SceneAnchor.DIALOG, new Vector3(0, 0, 2000));
		
		// Download the curtain texture and use it. They should be preloaded by now.
		DisplayAsset.loadTextureToRenderer(rendererLeft, CURTAIN_IMAGE_PATH);
		DisplayAsset.loadTextureToRenderer(rendererRight, CURTAIN_IMAGE_PATH);
	}
	
	// Close the red curtains.
	public IEnumerator closeCurtains()
	{
		// Wait one frame before starting, to make sure the anchors are set first.
		yield return null;
		yield return null;
		
		float leftCurtainToPosition = -left.transform.parent.localPosition.x;
		float rightCurtainToPosition = -right.transform.parent.localPosition.x;
		
		CommonTransform.setWidth(left.transform, Mathf.Abs(leftCurtainToPosition));
		CommonTransform.setWidth(right.transform, Mathf.Abs(rightCurtainToPosition));

		iTween.MoveTo(left, iTween.Hash("x", leftCurtainToPosition, "islocal", true, "time", TWEEN_TIME, "easetype", iTween.EaseType.easeInOutQuad));
		iTween.MoveTo(right, iTween.Hash("x", rightCurtainToPosition, "islocal", true, "time", TWEEN_TIME, "easetype", iTween.EaseType.easeInOutQuad));

		// Wait for the curtains to finish moving.
		yield return new WaitForSeconds(TWEEN_TIME);
		
		// Make sure the tween is 100% done to make sure they're fully closed before continuing.
		yield return null;
		yield return null;
	}

	// Open the red curtains. Assumes that the transition is done once the curtains are open, so it destroys itself.
	public IEnumerator openCurtains()
	{
		iTween.MoveTo(left, iTween.Hash("x", 0, "islocal", true, "time", TWEEN_TIME, "easetype", iTween.EaseType.easeInOutQuad));
		iTween.MoveTo(right, iTween.Hash("x", 0, "islocal", true, "time", TWEEN_TIME, "easetype", iTween.EaseType.easeInOutQuad));
	
		// Wait for the curtains to finish moving.
		yield return new WaitForSeconds(TWEEN_TIME);

		// Make sure the tween is 100% done before hiding the curtains.
		yield return null;
		yield return null;

		Destroy(gameObject);
	}
}
