using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class ScaleGroup
{
	private List<Transform> objectsToScale = new List<Transform>();


	public void add(Transform t)
	{
		if (!objectsToScale.Contains(t))
		{
			objectsToScale.Add(t);
		}
	}

	public void remove(Transform t)
	{
		if (objectsToScale.Contains(t))
		{
			objectsToScale.Remove(t);
		}
	}

	public void setScale(Vector3 size)
	{
		for (int i = 0; i < objectsToScale.Count; ++i)
		{
			objectsToScale[i].localScale = size;
		}
	}

	public void adjustWidthBy(float amount)
	{
		for (int i = 0; i < objectsToScale.Count; ++i)
		{
			Vector3 localScale = objectsToScale[i].localScale;

			if (localScale.x + amount > 0)
			{
				objectsToScale[i].localScale = new Vector3(localScale.x + amount, localScale.y, localScale.z);
			}
		}

		refresh();
	}

	public void refresh()
	{
		for (int i = 0; i < objectsToScale.Count; ++i)
		{
			UIAnchor anchor = objectsToScale[i].transform.gameObject.GetComponent<UIAnchor>();

			if (anchor != null)
			{
				anchor.enabled = true;
			}
		}
	}

	public void adjustHeightBy(float amount)
	{
		for (int i = 0; i < objectsToScale.Count; ++i)
		{
			Vector3 localScale = objectsToScale[i].localScale;

			if (localScale.y + amount > 0)
			{
				objectsToScale[i].localScale = new Vector3(localScale.x + amount, localScale.y, localScale.z);
			}

			objectsToScale[i].localScale = new Vector3(localScale.x, localScale.y + amount, localScale.z);
		}
	}

	public Vector3 currentSize()
	{
		if (objectsToScale.Count > 0)
		{
			return objectsToScale[0].localScale;
		}

		return Vector3.one;
	}

	public void clear()
	{
		objectsToScale.Clear();
	}
}