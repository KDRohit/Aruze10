using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerupInGameUIGrid : UIGrid
{
	private int sortByPowerupTimer(Transform a, Transform b)
	{
		PowerupTimer aTimer = a.gameObject.GetComponent<PowerupTimer>();
		PowerupTimer bTimer = b.gameObject.GetComponent<PowerupTimer>();

		if (aTimer != null && bTimer != null && aTimer.powerup != null && bTimer.powerup != null)
		{
			// compare running timer lengths, reutrn the shortest duration
			if (aTimer.powerup.runningTimer != null && bTimer.powerup.runningTimer != null)
			{
				// if the time is the same, return a rarity comparison
				if (aTimer.powerup.runningTimer.timeRemaining == bTimer.powerup.runningTimer.timeRemaining)
				{
					return (int)bTimer.powerup.rarity - (int)aTimer.powerup.rarity;
				}
				return aTimer.powerup.runningTimer.timeRemaining.CompareTo(bTimer.powerup.runningTimer.timeRemaining);
			}

			// sort non null timers
			if (aTimer.powerup.runningTimer == null && bTimer.powerup.runningTimer != null)
			{
				return 1;
			}

			// sort non null timers
			if (aTimer.powerup.runningTimer != null && bTimer.powerup.runningTimer == null)
			{
				return -1;
			}

			// move pending powerups forward
			if (aTimer.powerup.isPending)
			{
				return -1;
			}

			// move pending powerups forward
			if (bTimer.powerup.isPending)
			{
				return 1;
			}

			// nothing is happening, default to rarity sort
			return (int)aTimer.powerup.rarity - (int)bTimer.powerup.rarity;
		}

		return 0;
	}

	public override void Reposition()
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
				if (t != null)
				{
					list.Add(t);
				}
			}
			list.Sort(sortByPowerupTimer);

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
}