using UnityEngine;
using System.Collections;

/*
Simple script that continuously throbs simple visual things.
This uses CommonEffects.pulsateBetween() instead of Throbamatic
because Throbamatic's ease type isn't smooth for looping
since it's intended to have pauses between throbs instead of smooth looping.
*/
public class ThrobObject : TICoroutineMonoBehaviour
{
	public Vector3 axisFactor;
	public float scale = 1.0f;
	public float speed = 1.0f;
	
	private Vector3 originalScale;
	private Vector3 throbbedScale;
	private Vector3 scaleBuffer;
	
	void Awake()
	{
		originalScale = transform.localScale;
		scaleBuffer = transform.localScale;
		throbbedScale = new Vector3(
			originalScale.x * axisFactor.x * scale,
			originalScale.y * axisFactor.y * scale,
			originalScale.z * axisFactor.z * scale
		);
	}
	
	void Update()
	{
		scaleBuffer.x = CommonEffects.pulsateBetween(originalScale.x, throbbedScale.x, speed);
		scaleBuffer.y = CommonEffects.pulsateBetween(originalScale.y, throbbedScale.y, speed);
		scaleBuffer.z = CommonEffects.pulsateBetween(originalScale.z, throbbedScale.z, speed);
		
		transform.localScale = scaleBuffer;
	}
}
