using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class GameObjectCycler : TICoroutineMonoBehaviour
{
	[SerializeField] private List<GameObject> objectsToCycle;
	[SerializeField] private float cycleDelay;
	[SerializeField] private float fadeTime;
	[SerializeField] private bool disableFadedOutObjects;
	[SerializeField] private bool includeInActiveObjects;

	private int currentIndex;
	private SmartTimer cycleTimer;

	private List<TICoroutine> runningCoroutines = new List<TICoroutine>();

	private void Awake()
	{
		cycleTimer = new SmartTimer(cycleDelay, true, cycleObjects);
	}

	private void OnDestroy()
	{
		if (cycleTimer != null)
		{
			cycleTimer.destroy();
		}
		
	}

	public void startCycling(bool enableFirstObject)
	{
		if (canCycle)
		{
			if (enableFirstObject)
			{
				if (objectsToCycle[0] != null)
				{
					objectsToCycle[0].SetActive(true);
					CommonGameObject.alphaGameObject(objectsToCycle[0], 1.0f, includeUIObjects:true);
				}
			}
			
			for (int i = 1; i < objectsToCycle.Count; ++i)
			{
				if (objectsToCycle[i] != null)
				{
					objectsToCycle[i].SetActive(false);
				}
			}

			cycleTimer.start();
		}
	}

	public void stopCycling()
	{
		cycleTimer.stop();
		
		for (int i = 0; i < runningCoroutines.Count; i++)
		{
			StopCoroutine(runningCoroutines[i]);
		}
		
		runningCoroutines.Clear();
		
		if (objectsToCycle != null && objectsToCycle.Count > 0)
		{
			if (objectsToCycle[0] != null)
			{
				runningCoroutines.Add(StartCoroutine(CommonGameObject.fadeGameObjectTo(objectsToCycle[0], 0f, 1f, 0.2f, includeInActiveObjects)));
			}

			for (int i = 1; i < objectsToCycle.Count; i++)
			{
				if (objectsToCycle[i] != null)
				{
					runningCoroutines.Add(StartCoroutine(CommonGameObject.fadeGameObjectTo(objectsToCycle[i], 1f, 0f, 0.2f, includeInActiveObjects)));
				}
			}
			
			currentIndex = 0;
		}
	}

	public void stopCyclingImmediate()
	{
		cycleTimer.stop();
		
		for (int i = 0; i < runningCoroutines.Count; i++)
		{
			StopCoroutine(runningCoroutines[i]);
		}
		runningCoroutines.Clear();
		
		if (objectsToCycle != null && objectsToCycle.Count > 0)
		{
			if (objectsToCycle[0] != null)
			{
				CommonGameObject.alphaGameObject(objectsToCycle[0], 1.0f, includeUIObjects:true);
				objectsToCycle[0].SetActive(false);
			}

			for (int i = 1; i < objectsToCycle.Count; i++)
			{
				if (objectsToCycle[i] != null)
				{
					CommonGameObject.alphaGameObject(objectsToCycle[i], 0.0f, includeUIObjects:true);
					objectsToCycle[i].SetActive(!disableFadedOutObjects);
				}
			}
			currentIndex = 0;
		}
	}

	private void cycleObjects()
	{
		//if this object is not active coroutines will fail to start and spam errors
		if (!canCycle || !isRunning || this == null || this.gameObject == null || !this.gameObject.activeSelf)
		{
			return;
		}

		if (objectsToCycle[currentIndex] != null)
		{
			objectsToCycle[currentIndex].SetActive(true);
			runningCoroutines.Add(StartCoroutine(fadeOutCurrentObject(currentIndex)));
		}

		currentIndex = ++currentIndex % objectsToCycle.Count;

		if (objectsToCycle[currentIndex] != null)
		{
			objectsToCycle[currentIndex].SetActive(true);
			runningCoroutines.Add(StartCoroutine(CommonGameObject.fadeGameObjectTo(objectsToCycle[currentIndex], 0f, 1f, fadeTime, includeInActiveObjects)));
		}
	}

	private IEnumerator fadeOutCurrentObject(int index)
	{
		if (objectsToCycle[index] != null)
		{
			TICoroutine fadeOutCoroutine = StartCoroutine(CommonGameObject.fadeGameObjectTo(objectsToCycle[index], 1f, 0f, fadeTime, includeInActiveObjects));
			runningCoroutines.Add(fadeOutCoroutine);
			yield return fadeOutCoroutine;
			objectsToCycle[index].SetActive(!disableFadedOutObjects);
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	public bool canCycle
	{
		get { return objectsToCycle != null && objectsToCycle.Count > 1; }
	}

	public bool isRunning
	{
		get { return cycleTimer != null && cycleTimer.isRunning; }
	}

	public void addObjectToCycle(GameObject objToCycle)
	{
		objToCycle.SetActive(false);
		objectsToCycle.Add(objToCycle);
	}
}