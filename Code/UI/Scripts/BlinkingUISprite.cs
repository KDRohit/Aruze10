using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Turns a object's renderer on and off based on parameters.
*/

public class BlinkingUISprite : TICoroutineMonoBehaviour
{
	public float onDuration = 1f;
	public float offDuration = 1f;
	public bool defaultOn = true;

	private UIWidget _sprite;
	private float _switchTime = 0;
	
	void Awake()
	{
		_sprite = GetComponent<UIWidget>();
		
		if (_sprite == null)
		{
			Debug.LogError("No UISprite found for BlinkingUISprite.", gameObject);
		}
	}
	
	void Start()
	{
		_switchTime = Time.realtimeSinceStartup;
	}
	
	void Update()
	{
		float now = Time.realtimeSinceStartup;
		
		if (_sprite.enabled)
		{
			if (now - _switchTime > onDuration)
			{
				toggle(now);
			}
		}
		else
		{
			if (now - _switchTime > offDuration)
			{
				toggle(now);
			}
		}
	}
	
	private void toggle(float now)
	{
		_sprite.enabled = !_sprite.enabled;
		_switchTime = now;
	}
}
