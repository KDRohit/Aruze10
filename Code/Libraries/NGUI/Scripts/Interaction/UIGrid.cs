//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// All children added to the game object with this script will be repositioned to be on a grid of specified dimensions.
/// If you want the cells to automatically set their scale based on the dimensions of their content, take a look at UITable.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Interaction/Grid")]
public class UIGrid : TICoroutineMonoBehaviour
{
	public enum Arrangement
	{
		Horizontal,
		Vertical,
	}

	public Arrangement arrangement = Arrangement.Horizontal;
	public int maxPerLine = 0;
	public float cellWidth = 200f;
	public float cellHeight = 200f;
	public bool repositionNow = false;
	public bool sorted = false;
	public bool hideInactive = true;
	public float tweenTime = 0.2f;
	
	[SerializeField] protected UISprite gridBackground;
	[SerializeField] private Vector2 baseBackgroundSize = Vector3.one;
	[SerializeField] private Vector2 maximumBackgroundSize = Vector2.zero;
	[SerializeField] private Vector2 relativeSize = Vector2.one;
	[SerializeField] private Vector2 pixelOffset = Vector2.zero;
	[SerializeField] private UIAnchor[] dependentAchors;

	
	protected bool mStarted = false;

	void Start ()
	{
		mStarted = true;
		Reposition();
	}

	void Update ()
	{
		if (repositionNow)
		{
			repositionNow = false;
			Reposition();
		}
	}

	//Paul S - Adding this for real-time updating grid positions in editor when adjusting values in the inspector without having to press the Reposition Now button; makes aligning things and setting up background panels much quicker
	private void OnValidate()
	{
		Reposition();
	}

	static public int SortByName (Transform a, Transform b) { return string.Compare(a.name, b.name); }


	// MCC -- Adding this to tween the object to their desired position.
	public void RepositionTweened()
	{
		if (!gameObject.activeSelf)
		{
			// If it isnt active then we can't run coroutines (and also cant see the objects)
			// so just doing a normal reposition instead.
			Reposition();
			return;
		}
		
		if (!mStarted)
		{
			return;
		}

		Transform myTrans = transform;

		int x = 0;
		int y = 0;		

		for (int i = 0; i < myTrans.childCount; ++i)
		{
			Transform t = myTrans.GetChild(i);

			if (!NGUITools.GetActive(t.gameObject) && hideInactive) continue;

			float depth = t.localPosition.z;
			
		    Vector3 newPosition = (arrangement == Arrangement.Horizontal) ?
				new Vector3(cellWidth * x, -cellHeight * y, depth) :
				new Vector3(cellWidth * y, -cellHeight * x, depth);

		    //iTween.MoveTo(t.gameObject, newPosition, tweenTime);
			iTween.MoveTo(t.gameObject, iTween.Hash(
			    "x", newPosition.x,
				"y", newPosition.y,
				"z", newPosition.z,
				"time", tweenTime,
				"islocal", true,
				"easetype", iTween.EaseType.linear));
			
			if (++x >= maxPerLine && maxPerLine > 0)
			{
				x = 0;
				++y;
			}
		}
		UIDraggablePanel drag = NGUITools.FindInParents<UIDraggablePanel>(gameObject);
		if (drag != null) drag.UpdateScrollbars(true);			
	}
	
	/// <summary>
	/// Recalculate the position of all elements within the grid, sorting them alphabetically if necessary.
	/// </summary>

	public virtual void Reposition ()
	{
		if (!mStarted)
		{
			repositionNow = true;
			return;
		}

		Transform myTrans = transform;

		int x = 0;
		int y = 0;

		if (sorted)
		{
			List<Transform> list = new List<Transform>();

			for (int i = 0; i < myTrans.childCount; ++i)
			{
				Transform t = myTrans.GetChild(i);
				if (t && (!hideInactive || NGUITools.GetActive(t.gameObject))) list.Add(t);
			}
			list.Sort(SortByName);

			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				Transform t = list[i];

				if (!NGUITools.GetActive(t.gameObject) && hideInactive) continue;

				float depth = t.localPosition.z;
				t.localPosition = (arrangement == Arrangement.Horizontal) ?
					new Vector3(cellWidth * x, -cellHeight * y, depth) :
					new Vector3(cellWidth * y, -cellHeight * x, depth);

				if (++x >= maxPerLine && maxPerLine > 0)
				{
					x = 0;
					++y;
				}
			}
		}
		else
		{
			for (int i = 0; i < myTrans.childCount; ++i)
			{
				Transform t = myTrans.GetChild(i);

				if (!NGUITools.GetActive(t.gameObject) && hideInactive) continue;

				float depth = t.localPosition.z;
				t.localPosition = (arrangement == Arrangement.Horizontal) ?
					new Vector3(cellWidth * x, -cellHeight * y, depth) :
					new Vector3(cellWidth * y, -cellHeight * x, depth);

				if (++x >= maxPerLine && maxPerLine > 0)
				{
					x = 0;
					++y;
				}
			}
		}

		UIDraggablePanel drag = NGUITools.FindInParents<UIDraggablePanel>(gameObject);
		if (drag != null) drag.UpdateScrollbars(true);

		if (gridBackground != null)
		{
			resizeBackground(x,y);
		}
	}

	protected void resizeBackground(int x, int y)
	{
		int columns = y > 0 ? maxPerLine : Mathf.Max(1,x);
		float newXScale = baseBackgroundSize.x;
		float newYScale = baseBackgroundSize.y;

		//y only increments when we have a full row/column but we still need to stretch when a row/column is partially full
		int rows = Mathf.Max(1,x < maxPerLine && x > 0 ? y+1 : y);

		int xScalar = arrangement == Arrangement.Horizontal ? columns : rows;
		int yScalar = arrangement == Arrangement.Horizontal ? rows : columns;

		Vector3 localScale = transform.localScale;

		float xCalc = pixelOffset.x + baseBackgroundSize.x + Mathf.Abs(cellWidth) * (xScalar - 1) * relativeSize.x * localScale.x;
		float yCalc = pixelOffset.y + baseBackgroundSize.y + Mathf.Abs(cellHeight) * (yScalar - 1) * relativeSize.y * localScale.y;

		if (maximumBackgroundSize.x > 0) //limits the background X scale dimension if one is desired
		{
			newXScale = Mathf.Clamp(xCalc, baseBackgroundSize.x, maximumBackgroundSize.x);
		}
		else
		{
			newXScale = xCalc;
		}

		if (maximumBackgroundSize.y > 0) //limits the background Y scale dimension if one is desired
		{
			newYScale = Mathf.Clamp(yCalc, baseBackgroundSize.y, maximumBackgroundSize.y);
		}
		else
		{
			newYScale = yCalc;
		}

		Vector2 resizeVector = new Vector2(newXScale, newYScale);
		gridBackground.transform.localScale = resizeVector;
		if (dependentAchors != null)
		{
			foreach (UIAnchor anchor in dependentAchors)
			{
				anchor.reposition();
			}
		}
	}
}
