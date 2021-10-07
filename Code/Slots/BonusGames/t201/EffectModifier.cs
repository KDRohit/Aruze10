using UnityEngine;
using System.Collections;

public class EffectModifier : MonoBehaviour {

	// this code sets an animation object active/inactive after a set amount of time

	// Use this for initialization
	public GameObject animationGameObject;
	public float timeToWait = 10.0f;
	public float animationWaitTime = 1.0f;

	private float _timeCounter;
	private bool _stopCounter = false;
	void Start () 
	{
		_timeCounter = this.timeToWait;
		animationGameObject.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (_timeCounter <= 0)
		{
			StartCoroutine (activateAnimation());
		}
		else
		{
			if (!_stopCounter)
			{
				_timeCounter -= Time.deltaTime;
			}
		}
	}

	private IEnumerator activateAnimation()
	{
		_stopCounter = true;
		_timeCounter = timeToWait;
		animationGameObject.SetActive(true);
		yield return new WaitForSeconds(animationWaitTime);
		animationGameObject.SetActive(false);
		_stopCounter = false;
	}
}
