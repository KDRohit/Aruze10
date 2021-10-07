using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class allows you to create a UIGRid with a cell width or height being calculated
/// at runtime depending on the number of children and the max size of the widget.
/// </summary>
public class DynamicCellSizeUIGrid : UIGrid
{
	[SerializeField] private bool isLastFixed = false;
	public override void Reposition()
	{
		Transform myTrans = transform;
		List<Transform> activeChildren = new List<Transform>();
		for (int i = 0; i < myTrans.childCount; i++)
		{
			Transform child = myTrans.GetChild(i);
			if (!hideInactive || child.gameObject.activeSelf)
			{
				activeChildren.Add(child);
			}
		}

		if (activeChildren.Count <= 0)
		{
			return;
		}
		
		Transform first = activeChildren[0];
		Transform last = activeChildren[activeChildren.Count-1];
		
		float totalLegth = 0;
		int lastIndex = isLastFixed ? activeChildren.Count - 1 : activeChildren.Count;
		lastIndex = Mathf.Max(lastIndex, 1);
		
		if (arrangement == Arrangement.Horizontal)
		{
			totalLegth = Mathf.Abs(last.localPosition.x - first.localPosition.x);				
			cellWidth = totalLegth / lastIndex;
		}
		else
		{
			totalLegth = Mathf.Abs(last.localPosition.y - first.localPosition.y);
			cellHeight = totalLegth / lastIndex;
		}

		int x = 0;
		int y = 0;

		if (sorted)
		{
			activeChildren.Sort(SortByName);

			for (int i = 0, imax = activeChildren.Count; i < imax; ++i)
			{
				Transform t = activeChildren[i];

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
			for (int i = 0; i < activeChildren.Count; ++i)
			{
				Transform t = activeChildren[i];
				
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
}
