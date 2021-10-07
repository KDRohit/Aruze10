using UnityEngine;
using System.Collections;

public class SATC01FreeSpinDiamondTrail : TICoroutineMonoBehaviour {

	public Vector3 initialCoords;
	public Vector3 finalCoordsHorizontal;
	public Vector3 finalCoordsVertical;
	public float duration;
	[HideInInspector] public FreeSpinDelegate callback;
	[HideInInspector] public int callbackParam;
	[HideInInspector] public GameObject endAnimationPrefab;
	
	// Use this for initialization
	protected override void OnEnable () 
	{
		base.OnEnable();
		
		finalCoordsHorizontal = new Vector3(finalCoordsHorizontal.x, this.transform.localPosition.y, this.transform.localPosition.z);
		iTween.MoveTo(this.gameObject, iTween.Hash("position", finalCoordsHorizontal,
												"time", duration/2,
												"easetype", iTween.EaseType.linear,
												"oncomplete", "startNextPhase",
												"oncompletetarget", gameObject,
												"islocal", true));
	}
	
	private void startNextPhase()
	{
		finalCoordsVertical = new Vector3(this.transform.position.x, finalCoordsVertical.y, this.transform.position.z);
		iTween.MoveTo(this.gameObject, iTween.Hash("position", finalCoordsVertical,
												"time", duration/2,
												"easetype", iTween.EaseType.linear,
												"oncomplete", "endAnimation",
												"oncompletetarget", gameObject));
												
	}
	
	private IEnumerator endAnimation()
	{
		GameObject gb = (GameObject)CommonGameObject.instantiate(endAnimationPrefab, this.transform.localPosition, this.transform.localRotation);
		gb.SetActive(true);
		gb.transform.parent = this.transform.parent;
		gb.transform.position = this.transform.position;
		callback(callbackParam);
		
		yield return new WaitForSeconds(1.0f);
		Destroy(gb);
	}
	
	

	public delegate void FreeSpinDelegate(int a);
}
