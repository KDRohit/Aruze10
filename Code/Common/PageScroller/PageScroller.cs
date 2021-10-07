using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;
using TMPro;

/**
Standard class for controlling a UI that requires pages of items that scroll from page to page when clicking arrow buttons (or swiping for mobile).
*/

public class PageScroller : BasePageScroller
{

	/// Show the appropriate number of dots for the number of pages of panels, and center them.
	[HideInInspector] public Dictionary<GameObject, int> shownPanels = new Dictionary<GameObject, int>();		/// Holds index numbers of shown panels, keyed on the panels, for reverse lookup.


	public override void forEachShownPanel(PanelDelegate doFunction)
	{
		foreach (KeyValuePair<GameObject, int> kvp in shownPanels)
		{
			int i = kvp.Value;
			doFunction(panels[i], i);
		}
	}

	/// Sets or gets the current scroll position.
	public override int scrollPos
	{
		get { return _scrollPos; }

		set
		{
			if (transform == null)
			{
				Debug.LogError("PageScroller null transform.");
				return;
			}
			
			int pos = value;

			if (allowSingleScrolling)
			{
				// If allowing single scrolling, don't allow empty slots to be shown at the end of the list.
				pos = Mathf.Clamp(pos, 0, Mathf.Max(0, _totalPanels - panelsPerPage));
			}
			else
			{
				// If using page scrolling, allow empty slots shown on the last page.
				pos = Mathf.Clamp(pos, 0, Mathf.Max(0, (maxPage - 1) * panelsPerPage));
			}

			bool doScroll = true;
			int diff = pos - _scrollPos;

			if (diff != 0 && pos > 0 && !allowSingleScrolling && Mathf.Abs(diff) % panelsPerPage > 0 && panelsPerPage > 1)
			{
				Debug.LogWarning("Tried to scroll by an amount (" + diff.ToString() + ") not evenly divisible by the number of panels on a page (" + panelsPerPage + "), which isn't allowed for this PageScroller (except scrollPos 0).");
				return;
			}

			// Play the swipe sound, but make sure we're not playing it on anything automatic.
			if (diff < 0)
			{
				if (!isMultiScrolling && shouldPlaySound)
				{
					Audio.play(SOUND_MOVE_PREVIOUS);
				}
			}
			else if (diff > 0)
			{
				if (!isMultiScrolling && shouldPlaySound)
				{
					Audio.play(SOUND_MOVE_NEXT);
				}
			}
			else
			{
				// If scrollPosition is set to itself, it is a refresh of the current page.
				// This is legit if the list of data has changed, and it needs to be refreshed
				// but we don't want to change what page we're displaying.
				// First we need to cleanup all existing panels before making new ones.
				cleanupShownPanels(true);

				doScroll = false;	// We don't need to scroll for this.

				// Define how many panels to re-create.
				diff = Mathf.Min(_totalPanels, panelsPerPage);
			}

			_scrollPos = pos;

			if (doScroll && onBeforeScroll != null)
			{
				// If a function is specified to call before scrolling,
				// call it also before creating the new panels.
				onBeforeScroll();
			}

			int absDiff = Mathf.Abs(diff);
			
			if (pageNumberLabel != null)
			{
				// Update the page number label.
				if (maxPage <= 1)
				{
					pageNumberLabel.text = "";
				}
				else
				{
					pageNumberLabel.text = (page + 1).ToString() + "/" + maxPage.ToString();
				}
			}

			float goal = scrollBasePos - effectiveSpacingX * (_scrollPos / actualRowsPerPage);
			
			if (doScroll)
			{
				// Tween the friends panel to scroll it.

				// Don't disable scroll buttons while scrolling  for singleScrollers - HIR-493
				if (!allowSingleScrolling)
				{
					disableScrollButtons();
				}

				if (transform.localPosition.x == goal)
				{
					// Already at the goal.
					finishScrolling();
				}
				else if (absDiff > panelsPerPage || baseScrollTime == 0.0f)
				{
					// If scrolling more than one page's worth of options, or there is no scroll time delay
					// just jump to the new page instead of visually scrolling.
					CommonTransform.setX(transform, goal);
					finishScrolling();
				}
				else
				{
					isScrolling = true;

					float scrollTime = getScrollTime(absDiff);

					// Make sure a previous tween isn't still happening.
					iTween.Stop(gameObject);

					float thisScrollTime = scrollTime;
					iTween.EaseType thisEaseType = easeType;

					if (isMultiScrolling)
					{
						// If scrolling through multiple pages for a reset kind of thing,
						// then go linear so the scrolling doesn't have a noticeable pause between pages.
						thisEaseType = iTween.EaseType.linear;
						// Also scroll faster.
						thisScrollTime = scrollTime * .5f;
					}

					// Start the tween.
					iTween.MoveTo(transform.gameObject, iTween.Hash("x", goal, "time", thisScrollTime, "oncomplete", "finishScrolling", "islocal", true, "easetype", thisEaseType));
				}
			}
			else
			{
				// If not scrolling, call this to make sure panels are refreshed properly.
				CommonTransform.setX(transform, goal);
				createVisiblePanels();
			}
		}
	}

	protected override void createVisiblePanels()
	{
		if (panels == null)
		{
			panels = new List<GameObject>();
		}

		base.createVisiblePanels();

		if (!allowSingleScrolling && isLooping)
		{
			// If looping, and at the start or end, also create the matching panels at the opposite end for seamless looping.
			if (_scrollPos == 1)
			{
				createPanel(maxPage - 2);
			}
			if (_scrollPos == maxPage - 2)
			{
				createPanel(1);
			}
		}
	}

	// Resets the position of the panels to match the current scrollPos.
	// This is done to snap a drag back to normal if no change in scrollPos is needed.
	protected override void resetScrollPos()
	{
		float distance = Mathf.Abs(downScrollX - transform.localPosition.x);
		float scrollTime = getScrollTime(panelsPerPage) * (distance / pageSpacing);

		iTween.MoveTo(transform.gameObject,
			iTween.Hash(
				"x", downScrollX,
				"time", scrollTime,
				"islocal", true,
				"easetype", easeType,
				"oncomplete", "finishScrolling"
			)
		);
	}
		
	/// Jumps to the first page immediately.
	public override void resetToFirstPage()
	{
		//rebuild panels
		CommonTransform.setX(transform, scrollBasePos);
		scrollPosQuietly = (!allowSingleScrolling && isLooping ? 1 : 0);
	}

	protected override void finishScrolling()
	{
		base.finishScrolling();

		if (!allowSingleScrolling && isLooping)
		{
			// When scrolling is finished, check to see if we're on the first or last page,
			// and jump to the matching page at the other end of the list for a seamless loop.
			if (scrollPos == 0)
			{
				scrollPosQuietly = maxPage - 2;
			}
			else if (scrollPos == maxPage - 1)
			{
				scrollPosQuietly = 1;	// The second page, since it's 0-based.
			}
		}
	}
	
	/// Cleans up shown panels, optionally only cleaning up ones out of view or all of them.
	public override void cleanupShownPanels(bool includeInView = false)
	{
		List<int> toDelete = null;
		var shownPanelsEnum = shownPanels.GetEnumerator();
		while(shownPanelsEnum.MoveNext())
		{
			int index = shownPanelsEnum.Current.Value;

			if (includeInView || !isPanelInView(index))
			{
				if (toDelete == null)
				{
					toDelete = new List<int>();
				}
				toDelete.Add(index);
			}
		}

		if (toDelete != null)
		{
			// Delete the panels after everything else is done.
			foreach (int del in toDelete)
			{
				cleanupPanel(del);
			}
		}
	}

	/// Cleans up a single panel.
	protected override void cleanupPanel(int index)
	{
		GameObject panel = panels[index];

		if (onDestroyPanel != null)
		{
			onDestroyPanel(panel, index);
		}

		Destroy(panel);
		panels[index] = null;
		shownPanels.Remove(panel);
	}

	protected override void cleanupPageIndicators()
	{
		while (pageIndicators.Count > 0)
		{
			Destroy(pageIndicators[0].gameObject);
			pageIndicators.RemoveAt(0);
		}
	}

	/// Creates a panel for the slot at the given index in the array.
	protected override void createPanel(int index)
	{
		if (panels[index] != null)
		{
			// There is already a panel for this index.
			return;
		}

		GameObject thePrefab = effectivePanelPrefab;
		float x = 0;
		float y = 0;
		bool isHeader = false;
		int indexOnPage = index % panelsPerPage;
		int panelPage = Mathf.FloorToInt(index / panelsPerPage);
		int row = Mathf.FloorToInt((float)indexOnPage / actualPanelsPerRow);
		int column = indexOnPage % actualPanelsPerRow;

		if (panelInfo.Count == 0)
		{
			// This list only has one kind of panel.
			if (allowSingleScrolling)
			{
				x = index * effectiveSpacingX;
			}
			else
			{
				x = panelPage * pageSpacing + column * effectiveSpacingX;
				y = row * -effectiveSpacingY;
			}
		}
		else
		{
			// This list has multiple groups of panels, so calculating x and y is more complicated.
			// Assumes one panel per row, or shit's gonna get wacky.
			int headersBefore = 0;
			int itemsBefore = 0;

			for (int i = 0; i < index; i++)
			{
				if (Mathf.FloorToInt((float)i / panelsPerPage) == panelPage)
				{
					if (panelInfo[i].headerGroup > -1)
					{
						headersBefore++;
					}
					else if (panelInfo[i].itemGroup > -1)
					{
						itemsBefore++;
					}
				}
			}

			x = panelPage * Mathf.Max(effectiveSpacingX, headerSpacingX);
			y = -(headersBefore * headerSpacingY + itemsBefore * effectiveSpacingY);

			isHeader = (panelInfo[index].headerGroup > -1);
			if (isHeader)
			{
				thePrefab = panelHeaderPrefab;
			}
		}

		
		GameObject panel = thePrefab == null ? new GameObject() : CommonGameObject.instantiate(thePrefab) as GameObject;
		if (thePrefab == null )
		{
			panel.name = "Panel " + index.ToString();
		}
		panels[index] = panel;
		shownPanels[panel] = index;

		Vector3 pos = new Vector3(x, y, 0);

		panel.transform.parent = transform;
		panel.transform.localScale = new Vector3(scalePage, scalePage, 1f); // Vector3.one;
		panel.transform.localPosition = pos;
		CommonGameObject.setLayerRecursively(panel, gameObject.layer);

		if (panelInfo.Count == 0)
		{
			// Only has normal panels.
			onCreatePanel(panel, index);
		}
		else
		{
			// Has normal panels and header panels, so figure out which callback to make.

			if (isHeader && onCreateGroupHeaderPanel != null)
			{
				onCreateGroupHeaderPanel(panel, panelInfo[index].headerGroup);
			}
			else if (!isHeader && onCreateGroupPanel != null)
			{
				onCreateGroupPanel(panel, panelInfo[index].itemGroup, panelInfo[index].itemIndex);
			}
		}
	}
}
