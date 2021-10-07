using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Manages the list of disposable objects that were created in a scene,
so they can be destroyed before changing scenes.
*/

public class DisposableObject : IResetGame
{
	private static Dictionary<GameObject, bool> registered = new Dictionary<GameObject, bool>();
	
	/// Registers a GameObject for scene-change disposal
	public static void register(GameObject go)
	{
		if (registered.ContainsKey(go))
		{
			return;
		}

		registered.Add(go, true);
	}
	
	/// Performs scene cleanup of registered objects, called from Glb for scene changes
	public static void sceneCleanup()
	{
		foreach (KeyValuePair<GameObject, bool> p in registered)
		{
			GameObject go = p.Key;
			if (go != null)
			{
				GameObject.Destroy(go);
			}
		}

		registered.Clear();
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{	
		sceneCleanup();
	}	
}
