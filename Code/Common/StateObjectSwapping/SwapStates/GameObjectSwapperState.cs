using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[Serializable]
[System.Obsolete("Animation list should be used instead of object swapper")]
public sealed class GameObjectSwapperState : SwapperState
{
	public List<GameObject> objectsToControl;

	public void enableAll()
	{
		for (int i = 0; i < objectsToControl.Count; ++i)
		{
			SafeSet.gameObjectActive(objectsToControl[i], true);
		}
	}

	public void disableAll()
	{
		for (int i = 0; i < objectsToControl.Count; ++i)
		{
			SafeSet.gameObjectActive(objectsToControl[i], false);
		}
	}
}