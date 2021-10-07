using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Used for caching a list of different GameObjects
Handles grabbing random objects from the caches and releasing them accordingly
*/
public class RandomGameObjectCacher : TICoroutineMonoBehaviour
{
	
	private List<GameObjectCacher> cacheList = new List<GameObjectCacher>(); // The game object prefab to generate instances from
	private List<string> gameObjectTypes = new List<string>(); //List of the names of our gameObject caches so we know which ones we have caches for

	public void createCache(List<GameObject> listToCache)
	{
		if (cacheList.Count <= 0)
		{
			foreach (GameObject objectToCreateCacheFor in listToCache)
			{
				if (!gameObjectTypes.Contains(objectToCreateCacheFor.name))
				{
					cacheList.Add(new GameObjectCacher(this.gameObject, objectToCreateCacheFor));
					gameObjectTypes.Add(objectToCreateCacheFor.name);
				}
				else
				{
					Debug.LogError("A cache already exists for this object: " + objectToCreateCacheFor.name);
				}
			}
		}
		else
		{
			Debug.LogError("This cache has already been initialized");
		}

	}

	public void releaseInstance(GameObject objInstance)
	{
		//releases the object from its corresponding cache
		int cacheIndex = gameObjectTypes.IndexOf(objInstance.name); //Find the index of this object in our cache list
		if (cacheIndex >= 0)
		{
			cacheList [cacheIndex].releaseInstance(objInstance);
		}
		else
		{
			Debug.LogError("No cache exists for this object: " + objInstance.name);
		}
	}

	public GameObject getInstance()
	{
		GameObject instance = cacheList[Random.Range(0, cacheList.Count - 1)].getInstance();
		instance.name = instance.name.Replace("(Clone)", ""); //Trim off "(Clone)" so the name will match up with what we are labeling our caches
		return instance;
	}
}
