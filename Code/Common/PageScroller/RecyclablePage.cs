using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecyclablePage : MonoBehaviour, IRecycle
{
	private VIPCarouselButtonV3[] children;

	public void refresh()
	{
		children = this.GetComponentsInChildren<VIPCarouselButtonV3>();
	}

	public bool isEmpty()
	{
		return children == null;
	}

	public void init(Dict args)
	{
		//nothing
		if (children == null)
		{
			return;
		}

		for (int i = 0; i < children.Length; ++i)
		{
			if (children[i] == null)
			{
				continue;
			}
			children[i].init(args);
		}
	}

	public void reset()
	{
		if (children == null)
		{
			return;
		}

		for (int i = 0; i < children.Length; ++i)
		{
			if (children[i] == null)
			{
				continue;
			}
			children[i].reset();
		}
	}
}
