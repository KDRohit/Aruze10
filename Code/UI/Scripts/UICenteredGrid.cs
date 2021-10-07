using UnityEngine;
using System.Collections.Generic;

/*
  Class: UICenteredGrid
  Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
  Description: A version of the NGUI UIGrid that keeps the elements centered around it.
  This is useful when you want to disable/enable child objects, and then just hit position rather than
  calculating the new position of the grid everytime.
*/

[ExecuteInEditMode]
public class UICenteredGrid : TICoroutineMonoBehaviour
{
	public enum Arrangement
	{
		Horizontal,
		Vertical,
	}

	public delegate void RepositionDelegate();
	public event RepositionDelegate onRepositionFinished;
	
	public Arrangement arrangement = Arrangement.Horizontal;
	public int maxPerLine = 0;
	public float cellWidth = 200f;
	public float cellHeight = 200f;
	public bool repositionNow = false;
	public bool sorted = false;
	public bool hideInactive = true;
	public float tweenTime = 0.2f;

	private int numTweens = 0;
	private int numChildren = 0;

	[SerializeField] protected UISprite gridBackground;
	[SerializeField] private Vector2 baseBackgroundSize = Vector3.one;
	[SerializeField] private Vector2 maximumBackgroundSize = Vector2.zero;
	[SerializeField] private Vector2 relativeSize = Vector2.one;
	[SerializeField] private Vector2 pixelOffset = Vector2.zero;
	[SerializeField] private UIAnchor[] dependentAchors;
	[SerializeField] protected UIStretch strectchedBackground;

	protected bool mStarted = false; //assigned but never used



	void Start()
	{
		mStarted = true;
		reposition();
	}

	void Update()
	{
		if (repositionNow)
		{
			repositionNow = false;
			reposition();
		}
	}

	//Paul S - Adding this for real-time updating grid positions in editor when adjusting values in the inspector without having to press the Reposition Now button; makes aligning things and setting up background panels much quicker
	private void OnValidate()
	{
		reposition();
	}

	static public int SortByName (Transform a, Transform b) { return string.Compare(a.name, b.name); }

	// MCC -- Adding this to tween the object to their desired position.
	public virtual void RepositionTweened()
	{
		Transform myTrans = transform;
		int x = 0;
		int y = 0;
		numChildren = 0;
		for (int i = 0; i < myTrans.childCount; ++i)
		{
			if (!myTrans.GetChild(i).gameObject.activeSelf && hideInactive)
			{
				continue;
			}
			numChildren++;
		}
		
		float centeredOffsetX = arrangement == Arrangement.Horizontal ? (cellWidth * 0.5f * (Mathf.Min(numChildren, maxPerLine) -1 )) : 0;
		float centeredOffsetY = arrangement == Arrangement.Vertical ? (cellHeight * 0.5f * (numChildren - 1)) : 0;
		numTweens = 0; // reset the number of tweens
		for (int i = 0; i < myTrans.childCount; ++i)
		{
			Transform t = myTrans.GetChild(i);

			if (!t.gameObject.activeSelf && hideInactive)
			{
				continue;
			}
			
			float depth = t.localPosition.z;

			Vector3 newPosition = Vector3.zero;
			if (arrangement == Arrangement.Horizontal)
			{
				newPosition = new Vector3((cellWidth * x) - centeredOffsetX, (-cellHeight * y) + centeredOffsetY, depth);
			}
			else
			{
				newPosition = new Vector3((cellWidth * y) - centeredOffsetX, (-cellHeight * x) + centeredOffsetY, depth);
			}
			numTweens++;
			iTween.MoveTo(t.gameObject, iTween.Hash(
			    "x", newPosition.x,
				"y", newPosition.y,
				"z", newPosition.z,
				"time", tweenTime,
				"islocal", true,
				"easetype", iTween.EaseType.linear,
				"oncompletetarget", gameObject,
				"oncomplete", "onComplete"));

			if (++x >= maxPerLine && maxPerLine > 0)
			{
				x = 0;
				++y;
			}
		}
		///////////////////////////////
		UIDraggablePanel drag = NGUITools.FindInParents<UIDraggablePanel>(gameObject);
		if (drag != null) drag.UpdateScrollbars(true);			
	}

    private void onComplete()
	{
		if (onRepositionFinished != null)
		{
			numTweens--;
			if (numTweens <= 0)
			{
				onRepositionFinished();
			}
		}
	}
	
	public virtual void reposition()
	{
		Transform myTrans = transform;
		int x = 0;
		int y = 0;
		numChildren = 0;
		for (int i = 0; i < myTrans.childCount; ++i)
		{
			if (!myTrans.GetChild(i).gameObject.activeSelf && hideInactive)
			{
				continue;
			}
			numChildren++;
		}
		
		float centeredOffsetX = arrangement == Arrangement.Horizontal ? (cellWidth * 0.5f * (Mathf.Min(numChildren, maxPerLine) -1 )) : 0;
		float centeredOffsetY = arrangement == Arrangement.Vertical ? (cellHeight * 0.5f * (numChildren - 1)) : 0;
		
		for (int i = 0; i < myTrans.childCount; ++i)
		{
			Transform t = myTrans.GetChild(i);

			if (!t.gameObject.activeSelf && hideInactive)
			{
				continue;
			}
			
			float depth = t.localPosition.z;

			Vector3 newPosition = Vector3.zero;
			if (arrangement == Arrangement.Horizontal)
			{
				newPosition = new Vector3((cellWidth * x) - centeredOffsetX, (-cellHeight * y) + centeredOffsetY, depth);
			}
			else
			{
				newPosition = new Vector3((cellWidth * y) - centeredOffsetX, (-cellHeight * x) + centeredOffsetY, depth);
			}
			t.localPosition =  newPosition;
			if (++x >= maxPerLine && maxPerLine > 0)
			{
				x = 0;
				++y;
			}
		}
		UIDraggablePanel drag = NGUITools.FindInParents<UIDraggablePanel>(gameObject);
		if (drag != null) drag.UpdateScrollbars(true);

		if (gridBackground != null)
		{
			resizeBackground(x, y);
		}
	}

	protected void resizeBackground(int x, int y)
	{
		int columns = y > 0 ? maxPerLine : Mathf.Max(1, x);
		float newXScale = baseBackgroundSize.x;
		float newYScale = baseBackgroundSize.y;

		//y only increments when we have a full row/column but we still need to stretch when a row/column is partially full
		int rows = Mathf.Max(1, x < maxPerLine && x > 0 ? y + 1 : y);

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
		reAnchor();
	}

	public void reAnchor()
	{
		if (dependentAchors != null)
		{
			foreach (UIAnchor anchor in dependentAchors)
			{
				anchor.reposition();
			}
		}
		
		if (strectchedBackground != null)
		{
			strectchedBackground.enabled = true;
		}
	}

	public float getBottomBound()
	{
		if (!mStarted) //make sure we've at least positioned once so we have a valid child count
		{
			reposition();
		}

		if (numChildren == 0)
		{
			return 0;
		}
		
		return transform.GetChild(numChildren-1).position.y;
	}

	public void adjustBackgroundPosition(Vector3 offset)
	{
		if (gridBackground != null)
		{
			gridBackground.transform.position += offset;
		}
	}
}
