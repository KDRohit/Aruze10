using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Component created to track GameObjectCacher objects that are part of other compoenents
this will allow universal handling of these for things like MemoryWarningHandler

Original Author: Scott Lepthien
Creation Date: June 5, 2017
*/
public class GameObjectCacherTrackingComponent : MonoBehaviour 
{
	private List<GameObjectCacher> trackedCacherList = new List<GameObjectCacher>();

	private void OnDestroy()
	{
		for (int i = 0; i < trackedCacherList.Count; i++)
		{
			trackedCacherList[i].onParentClassDestroyed();
		}

		trackedCacherList.Clear();
	}

	public void addGameObjectCacherToTrackedList(GameObjectCacher cacher)
	{
		trackedCacherList.Add(cacher);
	}
}
