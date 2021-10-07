using UnityEngine;
using System.Collections;

/**
Simple script that rotates an object back and forth between two values.
Summer of Love currently uses this for some floating icons.
*/
public class RotateBetween : TICoroutineMonoBehaviour
{
	/// Pronounced "Oiler"
	public Vector3 eulerAnglesOne;
	public Vector3 eulerAnglesTwo;
	public float speed = 1.0f;
	
	private bool isBackwards = false;
	private GameTimer rotationTimer;
	private Vector3 destination;
	/// Update is called once per frame
	void Update()
	{
		if (rotationTimer == null)
		{
			destination = eulerAnglesOne;
			rotationTimer = new GameTimer(speed);
		}
		else if (rotationTimer.isExpired)
		{
			isBackwards = !isBackwards;
			destination = isBackwards ? eulerAnglesTwo : eulerAnglesOne;
			rotationTimer = new GameTimer(speed);
		}
		transform.Rotate(destination * (Time.deltaTime * speed));
	}
}
