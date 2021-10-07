using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;
using TMPro;
using Zynga.Core.Util;


public class SmoothPageScroller : BasePageScroller
{
	public int poolSize = 5;
	private int pageOffset = 0;
	private Queue<GameObject> objectPool;


	/// Must be called as soon as the panel count is known, but after attaching it to the NGUI anchor.
	public override void init(int panelCount, PanelDelegate onCreatePanel, PanelGroupDelegate onCreateGroupPanel = null, PanelDelegate onCreateGroupHeaderPanel = null, PanelDelegate onDestroyPanel = null)
	{
		objectPool = new Queue<GameObject>();
		GameObject thePrefab = effectivePanelPrefab;
		for (int i = 0; i < poolSize; ++i)
		{
			GameObject panel = thePrefab == null ? new GameObject() : CommonGameObject.instantiate(thePrefab) as GameObject;
			if (panel != null)
			{
				panel.name = "panel" + i + "(pooled object)";
				panel.transform.SetParent(this.transform);
				objectPool.Enqueue(panel);
			}
		}

		base.init(panelCount, onCreatePanel, onCreateGroupPanel, onCreateGroupHeaderPanel, onDestroyPanel);
	}

	private GameObject getPoolObject()
	{
		if (objectPool == null || objectPool.Count == 0)
		{
			Debug.LogError("No objects availalbe in pool");
			return null;
		}

		return objectPool.Dequeue();

	}

	protected override void createPanel(int index)
	{
		if (panels == null)
		{
			Debug.LogError("Total panels not set yet, can't create panel");
			return;
		}
		else if (panels.Count <= index)
		{
			Debug.LogError("Invalid index");
			return;
		}


		GameObject panel = null;
		if (panels[index] == null)
		{
			panel = getPoolObject();
			panels[index] = panel;

			float x = 0;
			float y = 0;
			int indexOnPage = index % panelsPerPage;
			int panelPage = Mathf.FloorToInt(index / panelsPerPage);
			int row = Mathf.FloorToInt((float)indexOnPage / actualPanelsPerRow);
			int column = indexOnPage % actualPanelsPerRow;

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

			Vector3 pos = new Vector3(x, y, 0);

			panel.transform.parent = transform;
			panel.transform.localScale = new Vector3(scalePage, scalePage, 1f); // Vector3.one;
			panel.transform.localPosition = pos;
			CommonGameObject.setLayerRecursively(panel, gameObject.layer);
		}
		else
		{
			panel = panels[index];
		}

		if (panel != null)
		{
			// Only has normal panels.
			int relativeIndex = (index + pageOffset) % _totalPanels;
			onCreatePanel(panel, relativeIndex);

			RecyclablePage recScript = panel.GetComponent<RecyclablePage>();
			if (recScript != null)
			{
				recScript.refresh();
			}
		}


	}

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
				//cleanupShownPanels(true);

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

	public override void forEachShownPanel(PanelDelegate doFunction)
	{
		if (panels == null)
		{
			return;
		}

		for (int i = 0; i < panels.Count; ++i)
		{
			if (panels[i] == null)
			{
				continue;
			}
			if (isPanelInView(i))
			{
				doFunction(panels[i], i);
			}
		}
	}

	public override void cleanupShownPanels(bool includeInView = false)
	{

		if (panels == null)
		{
			Debug.LogWarning("Invalid panel list");
			return;
		}

		for (int i = 0; i < panels.Count; ++i)
		{
			if ((i >= 0 && i < panels.Count) &&
			    (includeInView || !isPanelInView(i)))
			{
				cleanupPanel(i);
			}
		}
	}

	/// Cleans up a single panel.
	protected override void cleanupPanel(int index)
	{
		if (panels == null || panels.Count <= index || index < 0)
		{
			return;
		}

		GameObject panel = panels[index];
		if (panel == null)
		{
			return;
		}

		if (onDestroyPanel != null)
		{
			int relativeIndex = (index + pageOffset) % _totalPanels;
			onDestroyPanel(panel, relativeIndex);
		}

		IRecycle recScript = panel.GetComponent<IRecycle>();
		if (recScript != null)
		{
			recScript.reset();
		}

		objectPool.Enqueue(panel);
		panels[index] = null;
	}

	/// Jumps to the first page immediately.
	public override void resetToFirstPage()
	{
		//rebuild panels
		CommonTransform.setX(transform, scrollBasePos);

		//offset so we're back one page
		pageOffset+= _totalPanels -1;
		pageOffset = pageOffset % _totalPanels;

		//set to first page
		_scrollPos = (!allowSingleScrolling && isLooping ? 1 : 0);

		//re-init panels
		createVisiblePanels();

		//hide panels that aren't visible
		cleanupShownPanels();

		//now tell it to scroll to the next page
		scrollPosQuietly++;

	}

	protected override void resetScrollPos()
	{
		Debug.LogWarning("Reset Scroll Position Not implemented yet");
	}
}
