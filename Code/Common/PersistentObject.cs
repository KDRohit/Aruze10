using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Manages the list of persistent objects that were created in the Startup scene,
so they can be destroyed before re-loading the Startup scene during a game reset.
*/

public class PersistentObject : IResetGame
{
	private static Dictionary<GameObject, bool> registered = new Dictionary<GameObject, bool>();
	
	/// Registers a GameObject for game-reset disposal
	public static void register(GameObject go)
	{
		if (registered.ContainsKey(go))
		{
			Debug.LogWarning("Tried to register a persistent object that is already registered.", go);
			return;
		}
		GameObject.DontDestroyOnLoad(go);
		registered.Add(go, true);
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
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
}

