using UnityEngine;
using System.Collections;

/**
 * GameObject tween controller for position
 */
public class ObjectTweenOffset : TICoroutineMonoBehaviour 
{
	public Vector3 tweenOffset = Vector3.zero;						///< Inspector populated offset position
	public float duration = 0.2f;									///< Time to get to tween
	public UITweener.Method method = UITweener.Method.EaseInOut;	///< blend method

	protected Vector3 mPos;

	void Start () 
	{ 
		mPos = transform.localPosition;
	}
	
	/// slides to and from tweenOffset and original gameobject position 
	public void slide(bool slideOff)
	{
		Vector3 slidePosition = mPos;
		if (slideOff)
		{
			slidePosition += tweenOffset;
		}
		TweenPosition.Begin(gameObject, duration,slidePosition).method = method;
	}
}
