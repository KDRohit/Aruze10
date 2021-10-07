using UnityEngine;
using System.Collections;
using System;

public class DynamicTabManager : TabManager
{
	public float maxTabWidth = -1; // -1 is used as no upper limit.
	public float minTabWidth = -1; // -1 is used as no lower limit
	public float totalWidth = 0; // This MUST be set.

	private float originalCellWidth = 0;
	public override void init(Type t, int defaultTab = 0, onSelectDelegate onSelectDelegate = null)
	{
		// Use this time to grab the initial UIGrid cellWidth so we can use it down the line.
		originalCellWidth = uiGrid.cellWidth;

		// Now init as normal.
		base.init(t, defaultTab, onSelectDelegate);
	}

	public override void hideTab(int tabIndex)
	{
		if (tabs.Length > tabIndex)
		{
			tabs[tabIndex].gameObject.SetActive(false);
		}

		// Find the new width that we want now that a tab is disabled.
		int numActiveTabs = getNumActiveTabs();
		float desiredWidth = getDesiredTabWidth(numActiveTabs);
		for (int i = 0; i < tabs.Length; i++)
		{
			// Then set each tab to this new width.
			setTabWidth(i, desiredWidth);
		}

		// Update the grid cell widths.
		uiGrid.cellWidth = getDesiredCellWidth(numActiveTabs);
		if (uiGrid != null)
		{
			// If we are using a UIGrid to arrange the tabs, call reposition.
		    uiGrid.reposition();
		}
	}

	private void setTabWidth(int tabIndex, float newWidth)
	{

		if (tabs.Length > tabIndex && tabs[tabIndex] != null)
		{
			TabSelector tab = tabs[tabIndex];
			// Set the collider width, as well as the on/off sprite widths.
			tab.clickHandler.boxCollider.size = new Vector3(newWidth,
				tab.clickHandler.boxCollider.size.y,
				tab.clickHandler.boxCollider.size.z);
			SafeSet.spriteWidth(tab.targetSprite, newWidth);
			SafeSet.spriteWidth(tab.targetOffSprite, newWidth);
		}
		else
		{
			Debug.LogErrorFormat("DynamicTabManager.cs --  -- invalid request, no tab at index: {0}", tabIndex);
		}
	}

	private float getDesiredTabWidth(int numActiveTabs)
	{
		return totalWidth / numActiveTabs;
	}

	private float getDesiredCellWidth(int numActiveTabs)
	{
		return (originalCellWidth / tabs.Length) * numActiveTabs;
	}

	private int getNumActiveTabs()
	{
		int numActiveTabs = 0;
		for (int i = 0; i < tabs.Length; i++)
		{
			if (tabs[i] != null && tabs[i].gameObject.activeSelf)
			{
				numActiveTabs++;
			}
		}

		return numActiveTabs;
	}
}
