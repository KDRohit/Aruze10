using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
	If the GPU is an adreno and the CPU has NEON in its name, this game object should kill itself.
*/
public class BadGraphicalProcessingUnitSeppuku : MonoBehaviour
{
	public void Awake()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (SystemInfo.graphicsDeviceName.ToLower().Contains("adreno") && SystemInfo.processorType.ToLower().Contains("neon"))
		{
			Destroy (gameObject);
		}
#endif
	}
}