using UnityEngine;
using System.Collections;

public class TransformWatcher : MonoBehaviour
{

	private Vector3 lastScale;
	private Vector3 lastPosition;
	
    void Update()
	{
		if (lastScale != transform.localScale)
		{
			Debug.LogErrorFormat("TransformWatcher.cs -- Update -- scale changed on object {0}", gameObject.name);
		}
		lastScale = transform.localScale;

		if (lastPosition != transform.localPosition)
		{
			Debug.LogErrorFormat("TransformWatcher.cs -- Update -- position changed on object {0}", gameObject.name);
		}
		lastPosition = transform.localPosition;
	}
}