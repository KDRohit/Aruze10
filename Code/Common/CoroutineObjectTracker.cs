using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Class made to track the callers of coroutines in order to allow for an entire group of corotuines
 * to be canceled at once.  An example would be for instance AnimationListController where a number
 * of child sub-coroutines are created and need to be canceled if the parent is canceled.
 *
 * Creation Date: 6/3/2019
 * Original Author: Scott Lepthien
 */
public class CoroutineObjectTracker
{
	private Dictionary<TICoroutineMonoBehaviour, List<TICoroutine>> trackedCoroutines = new Dictionary<TICoroutineMonoBehaviour, List<TICoroutine>>();
	private List<TICoroutine> tempCoroutineList; // Declaring this here to avoid some extra GC on declaring lots of different lists when some functions in here are called frequently, like isEveryCoroutineFinished()

	public void addTrackedCoroutineList(TICoroutineMonoBehaviour coroutineCaller, List<TICoroutine> coroutineList)
	{
		if (!trackedCoroutines.ContainsKey(coroutineCaller))
		{
			trackedCoroutines.Add(coroutineCaller, new List<TICoroutine>());
		}
		
		trackedCoroutines[coroutineCaller].AddRange(coroutineList);
	}
	
	public void addTrackedCoroutine(TICoroutineMonoBehaviour coroutineCaller, TICoroutine coroutine)
	{
		if (!trackedCoroutines.ContainsKey(coroutineCaller))
		{
			trackedCoroutines.Add(coroutineCaller, new List<TICoroutine>());
		}
		
		trackedCoroutines[coroutineCaller].Add(coroutine);
	}

	public void removeTrackedCoroutine(TICoroutineMonoBehaviour coroutineCaller, TICoroutine coroutine)
	{
		if (trackedCoroutines.ContainsKey(coroutineCaller))
		{
			trackedCoroutines[coroutineCaller].Remove(coroutine);
		}
	}

	public bool isEveryCoroutineFinished()
	{
		foreach (KeyValuePair<TICoroutineMonoBehaviour, List<TICoroutine>> kvp in trackedCoroutines)
		{
			tempCoroutineList = kvp.Value;

			TICoroutine currentCoroutine = null;
			for (int i = 0; i < tempCoroutineList.Count; i++)
			{
				currentCoroutine = tempCoroutineList[i];
				if (currentCoroutine != null && !currentCoroutine.finished)
				{
					return false;
				}
			}
		}

		return true;
	}

	public void stopAndClearAllTrackedCoroutines()
	{
		foreach (KeyValuePair<TICoroutineMonoBehaviour, List<TICoroutine>> kvp in trackedCoroutines)
		{
			TICoroutineMonoBehaviour coroutineCaller = kvp.Key;
			tempCoroutineList = kvp.Value;

			TICoroutine currentCoroutine = null;
			for (int i = 0; i < tempCoroutineList.Count; i++)
			{
				currentCoroutine = tempCoroutineList[i];
				if (currentCoroutine != null && !currentCoroutine.finished)
				{
					coroutineCaller.StopCoroutine(tempCoroutineList[i]);
				}
			}
		}
		
		trackedCoroutines.Clear();
	}
}
