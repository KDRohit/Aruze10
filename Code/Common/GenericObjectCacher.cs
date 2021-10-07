using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Generic class that implments basic object caching
*/
public class GenericObjectCacher<T> where T : new()
{
	protected List<T> freeEffectList = new List<T>(); 					// The list of cached objects

	/// Handle creating a new instance
	protected virtual T generateNewObject()
	{
		return new T();
	}

	/// Get an effect instance that is either cached or newly created and will be cached in the future by calling releaseEffectInstance
	public T getInstance()
	{
		// note that default(T) may be null
		T objectInstance = default(T);

		if (freeEffectList.Count == 0)
		{
			// no cached effects avaliable so create a new one
			objectInstance = generateNewObject();
		}
		else
		{
			// already have effects cached, so hand out one of those
			objectInstance = freeEffectList[freeEffectList.Count - 1];
			freeEffectList.RemoveAt(freeEffectList.Count - 1);
		}

		return objectInstance;
	}

	/// Handle anything that needs to happen before the object is released back into the cache
	protected virtual void onReleaseObject(T releasingObj)
	{
		// handle in override if needed
	}

	/// Release an effect instance back into the cached pool
	public void releaseInstance(T objInstance)
	{
		if (objInstance == null)
		{
			Debug.LogError("Trying to release null!");
		}

		if (!freeEffectList.Contains(objInstance))
		{
			onReleaseObject(objInstance);
			freeEffectList.Add(objInstance);
		}
		else
		{
			Debug.LogWarning("Object is already in the cached list!");
		}
	}

	// Clear the list, used for memory warning stuff
	protected void clearList()
	{
		freeEffectList.Clear();
	}
}
