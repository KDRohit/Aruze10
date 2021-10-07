using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurvedPathObjectMover : MonoBehaviour
{
	[SerializeField] private GameObject objectToMove;
	[SerializeField] public Transform[] pathForiTween;
	[SerializeField] private iTween.EaseType easeType = iTween.EaseType.easeInQuad;
	[SerializeField] private float duration = 1.0f;

	private bool tweenInProgress = false;

	public void startTween()
	{
		if (!tweenInProgress)
		{
			iTween.ValueTo(this.gameObject, iTween.Hash(
				"from", 0.0f, 
				"to", 1.0f,
				"time", duration, 
				"onupdate", "tweenUpdate",
				"easetype", easeType,
				"oncomplete", "tweenFinish"
			));
		}

	}

	private void tweenUpdate(float percent)
	{
		tweenInProgress = true;
		iTween.PutOnPath(objectToMove.gameObject, pathForiTween, percent);
	}

	private void tweenFinish()
	{
		tweenInProgress = false;
	}
	
	private void OnDrawGizmos()
	{
		if (pathForiTween != null)
		{
			iTween.DrawPath(pathForiTween);
		}
	}
}
