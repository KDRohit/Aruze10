using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainLobbyBottomGrid : UICenteredGrid
{
	public override void reposition()
	{
		//ignore if this object is already marked for destruction
		if (this.gameObject == null || this.transform == null)
		{
			return;
		}
		
		Transform myTrans = transform;
		int x = 0;
		int y = 0;
		int numChildren = 0;
		for (int i = 0; i < myTrans.childCount; ++i)
		{
			if (myTrans.GetChild(i) == null ||
			    myTrans.GetChild(i).gameObject == null ||
			    (!myTrans.GetChild(i).gameObject.activeSelf && hideInactive))
			{
				continue;
			}
			numChildren++;
		}
		
		//This bottom grid is always horizontal
		float centeredOffsetX = (cellWidth * 0.5f * (Mathf.Min(numChildren, maxPerLine) -1 ));
		float centeredOffsetY = 0;

		//https://jira.corp.zynga.com/browse/HIR-84018
		//Desired order for these buttons should be VIP, Rich Pass, Collections, Daily Bonus, Friends, Max Voltage.
		List<BottomOverlayButton> sortedList = BottomOverlayButton.globalList;
		sortedList.Sort(sortByIndex);

		if (DailyBonusButton.instance != null && sortedList.Contains(DailyBonusButton.instance))
		{
			sortedList.Remove(DailyBonusButton.instance);
			sortedList.Insert((int)Mathf.Ceil(sortedList.Count / 2f), DailyBonusButton.instance);
		}

		for (int i = 0; i < sortedList.Count; ++i)
		{
			if (sortedList[i] == null || sortedList[i].gameObject == null ||
			    (!sortedList[i].gameObject.activeSelf && hideInactive))
			{
				continue;
			}
			
			Transform t = sortedList[i].transform;

			float depth = t.localPosition.z;

			Vector3 newPosition = Vector3.zero;
			newPosition = new Vector3((cellWidth * x) - centeredOffsetX, (-cellHeight * y) + centeredOffsetY, depth);
			t.localPosition =  newPosition;
			x++;
		}
		
		UIDraggablePanel drag = NGUITools.FindInParents<UIDraggablePanel>(gameObject);
		if (drag != null) drag.UpdateScrollbars(true);
	}

	private static int sortByIndex(BottomOverlayButton a, BottomOverlayButton b)
	{
		return a.sortIndex - b.sortIndex;
	}
}