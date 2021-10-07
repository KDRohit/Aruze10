using UnityEngine;
using System.Collections;

/**
Simple script that rotates simple visual things.
Don Quixote currently uses this for the windmills.
*/
public class RotateObject : TICoroutineMonoBehaviour
{
	/// Pronounced "Oiler"
	public Vector3 eulerAngles;
	public float speed = 1.0f;
	
	/// Update is called once per frame
	void Update()
	{
		transform.Rotate(eulerAngles * (Time.deltaTime * speed));
	}
}
