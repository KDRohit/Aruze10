using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Handles general GUI stuff that isn't handled by a script intended for specific behavior.
*/

public class GUIScript : TICoroutineMonoBehaviour
{
	public GUISkin devSkinHi;		///< Hi resolution dev panel skin.
	public GUISkin devSkinLow;		///< Low resolution dev panel skin.
	
	public static GUIScript instance = null;
	
	void Awake()
	{
		instance = this;
	}
}
