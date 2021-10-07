using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls the swinging motion of the microphone on the Man Behind The Curtain bonus game.
*/

public class MicPendulum : TICoroutineMonoBehaviour
{
	private Vector3 rot = Vector3.zero;
	private Vector3 scale = Vector3.one;
	
	void Update()
	{
		rot.z = CommonEffects.pulsateBetween(-2, 2, 1);
		scale.x = CommonEffects.pulsateBetween(.9f, 1, 1.5f);
		
		transform.localEulerAngles = rot;
		transform.localScale = scale;
	}
}
