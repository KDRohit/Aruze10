using UnityEngine;
using System.Collections;
using System;

public class TabManager : MonoBehaviour
{
	public bool isEnabled = true;
	public TabSelector currentTab = null;
	
	public TabSelector[] tabs;
	public UICenteredGrid uiGrid;

	public delegate void onSelectDelegate(TabSelector tab);
	public event onSelectDelegate onSelect;

	public virtual void selectTab(int index)
	{
		if (index < tabs.Length)
		{
			selectTab(tabs[index]);
		}
	}
	
	public virtual void selectTab(TabSelector tab)
	{
		if (!isEnabled)
		{
			Debug.LogFormat("TabManager.cs -- selectTab -- isEnabled is false so not doing anything.");
			return;
		}
		if (tab.index >= tabs.Length)
		{
			Debug.LogErrorFormat("TabManager.cs -- selectTab -- OutOfBounds trying to select a tab index that is too large: {0}", tab.index);
			// Bail
			return;
		}
		
		currentTab = tab; // Set the current tab.
		
		if (onSelect != null)
		{
			// Do this first so that any setup gets called before we turn on the object.
			onSelect(tab);
		}
		
		for (int i = 0; i < tabs.Length; i++)
		{
			if (i == tab.index)
			{
				// select it
				tabs[i].selected = true;
			}
			else
			{
				// unselect it
				tabs[i].selected = false;
			}
		}
	}

	public virtual void init(Type t, int defaultTab = 0, onSelectDelegate onSelectDelegate = null)
	{
		if (t.BaseType == typeof(Enum))
		{
			if (Enum.GetValues(t).Length != tabs.Length)
			{
				// We want to make sure that the manager and the dialog using it are operating with the same number of tabs.
				Debug.LogFormat("TabManager.cs -- init -- initializing with a different number of indicies than linked tabs, please fix this.");
			}

			// Setup the callback
			onSelect += onSelectDelegate;

			if (onSelectDelegate == null)
			{
				Debug.LogWarningFormat("TabManager.cs -- init -- initializing a TabManager twice, two onSelect delegates will now be called.");
			}

			for (int i = 0; i < tabs.Length; i++)
			{
				if (tabs[i] == null)
				{
					Debug.LogErrorFormat("TabManager.cs -- init -- trying to init {0} tabs but found {1} is null, you should check your prefab array.", tabs.Length, i);
				}
				else
				{
					tabs[i].init(i, this);
				}

			}

			// Now select the default tab.
			selectTab(tabs[defaultTab]);
		}
		else
		{
			Debug.LogErrorFormat("TabManager.cs -- init -- Trying to initialize with a non-Enum type.");
		}
	}

	public virtual void hideTab(int tabIndex)
	{
		if (tabs.Length > tabIndex)
		{
			tabs[tabIndex].gameObject.SetActive(false);
		}
		
		if (uiGrid != null)
		{
			// If we are using a UIGrid to arrange the tabs, call reposition.
		    uiGrid.reposition();
		}
	}

	public virtual void showTab(int tabIndex)
	{
		if (tabs.Length > tabIndex)
		{
			tabs[tabIndex].gameObject.SetActive(true);
		}
		if (uiGrid != null)
		{
			// If we are using a UIGrid to arrange the tabs, call reposition.
		    uiGrid.reposition();
		}
	}

	public virtual void disableAllTabs()
	{
		if (tabs != null)
		{
			for (int i = 0; i < tabs.Length; i++)
			{
				if (tabs[i] == null)
				{
					continue;
				}
				tabs[i].selected = false;
				tabs[i].clickHandler.boxCollider.enabled = false;
			}
		}
		this.enabled = false;
	}
}
