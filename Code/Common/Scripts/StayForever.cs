using UnityEngine;
using System.Collections;

/**
This object should stay around forever, completely outside of other logic.
This script was created to address a specific Android device problem when we
stop rendering on all other cameras.
This is only attached to a dummy "Blank Camera" in the scene.
*/
public class StayForever : MonoBehaviour
{
	void Awake()
	{
		GameObject.DontDestroyOnLoad(gameObject);
	}
	
#if UNITY_EDITOR
	void OnDestroy()
	{
		if (Application.isPlaying && !Glb.isQuitting)
		{
			Debug.LogError("NOOOOOOO! WHAT HAVE YOU DONE?! OH GOD(S), WHAT HAVE YOU DONE????");
		}
	}
#endif
}
