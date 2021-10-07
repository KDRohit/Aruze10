using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Used for caching GameObjects
*/
public class GameObjectCacher : GenericObjectCacher<GameObject>
{
	public GameObject parent;
	public GameObject gameObjPrefab;									// The game object prefab to generate instances from
	private bool generateUnderParent = false;

	public GameObjectCacher(GameObject parent, GameObject prefab, bool generateUnderParent = false)
	{
		if (parent != null)
		{
			this.parent = parent;

			// Check if the parent already has a GameObjectCacherTrackingComponent if so we will add to is
			// otherwise we will create it and then add to it.
			GameObjectCacherTrackingComponent tracker = parent.GetComponent<GameObjectCacherTrackingComponent>();
			if (tracker == null)
			{
				tracker = parent.AddComponent<GameObjectCacherTrackingComponent>();
			}
			tracker.addGameObjectCacherToTrackedList(this);

			MemoryWarningHandler.instance.addOnMemoryWarningDelegate(onMemoryWarning);
		}
		else
		{
			Debug.LogError("GameObjectCacher.GameObjectCacher() - parent was null!  This shouldn't happen as it will prevent the cacher from registering to clear if a memory warning occurs.");
		}
		this.generateUnderParent = generateUnderParent;
		gameObjPrefab = prefab;
	}

	/// Handle creating a new instance
	protected override GameObject generateNewObject()
	{
		
		if (gameObjPrefab != null)
		{
			if (generateUnderParent)
			{
				return CommonGameObject.instantiate(gameObjPrefab, parent.transform) as GameObject;
			}
			else
			{
				return CommonGameObject.instantiate(gameObjPrefab) as GameObject;
			}

		}
		else
		{
			Debug.LogError("gameObjPrefab is null!");
			return null;
		}
	}

	/// Handle anything that needs to happen before the object is released back into the cache
	protected override void onReleaseObject(GameObject releasingObj)
	{
		if (releasingObj != null)
		{
			// unparent the object, and parent it to the GameObjectCacher,
			// so that if what it is attached to (like a symbol) is cleaned up, 
			// the effect isn't destroyed
			if (parent != null)
			{
				releasingObj.transform.parent = parent.transform;
			}
			else
			{
				releasingObj.transform.parent = null;
			}

			// hide objects before putting them into the cache
			releasingObj.SetActive(false);
		}
	}

	// Will be called by MemoryWarningHandler
	private void onMemoryWarning()
	{
		for (int i = 0; i < freeEffectList.Count; i++)
		{
			Object.Destroy(freeEffectList[i]);
			freeEffectList[i] = null;
		}

		// cleanup the list now that we've destroyed all of the GameObject effects
		clearList();
	}

	// Needs to be called in parent Component classes OnDestroy method
	public void onParentClassDestroyed()
	{
		// unregister from the MemoryWarningHandler delegate
		if (MemoryWarningHandler.instance != null)
		{
			MemoryWarningHandler.instance.removeOnMemoryWarningDelegate(onMemoryWarning);
		}
	}
}
