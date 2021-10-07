using Zynga.Core.Util;

//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Attach this script to a popup list, the parent of a group of checkboxes, or to a checkbox itself to save its state.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Saved Option")]
public class UISavedOption : TICoroutineMonoBehaviour
{
	/// <summary>
	///PlayerPrefsCache -stored key for this option.
	/// </summary>

	public string keyName;

	private PreferencesBase preferences = null;

	string key { get { return (string.IsNullOrEmpty(keyName)) ? "NGUI State: " + name : keyName; } }

	UIPopupList mList;
	UICheckbox mCheck;

	/// <summary>
	/// Cache the components and register a listener callback.
	/// </summary>

	void Awake ()
	{
		mList = GetComponent<UIPopupList>();
		mCheck = GetComponent<UICheckbox>();
		if (mList != null) mList.onSelectionChange += SaveSelection;
		if (mCheck != null) mCheck.onStateChange += SaveState;

		preferences = SlotsPlayer.getPreferences();
	}

	/// <summary>
	/// Remove the callback.
	/// </summary>

	void OnDestroy ()
	{
		if (mCheck != null) mCheck.onStateChange -= SaveState;
		if (mList != null) mList.onSelectionChange -= SaveSelection;
	}

	/// <summary>
	/// Load and set the state of the checkboxes.
	/// </summary>

	protected override void OnEnable ()
	{
		base.OnEnable();

		if (preferences == null)
		{
			preferences = SlotsPlayer.getPreferences();
		}
		
		if (mList != null)
		{
			string s = preferences.GetString(key);
			if (!string.IsNullOrEmpty(s)) mList.selection = s;
			return;
		}

		if (mCheck != null)
		{
			mCheck.isChecked = (preferences.GetInt(key, 1) != 0);
		}
		else
		{
			string s = preferences.GetString(key);
			UICheckbox[] checkboxes = GetComponentsInChildren<UICheckbox>(true);

			for (int i = 0, imax = checkboxes.Length; i < imax; ++i)
			{
				UICheckbox ch = checkboxes[i];
				ch.isChecked = (ch.name == s);
			}
		}
	}

	/// <summary>
	/// Save the state on destroy.
	/// </summary>

	protected override void OnDisable ()
	{
		base.OnDisable();

		if (preferences == null)
		{
			preferences = SlotsPlayer.getPreferences();
		}
		
		if (mCheck == null && mList == null)
		{
			UICheckbox[] checkboxes = GetComponentsInChildren<UICheckbox>(true);

			for (int i = 0, imax = checkboxes.Length; i < imax; ++i)
			{
				UICheckbox ch = checkboxes[i];

				if (ch.isChecked)
				{
					SaveSelection(ch.name);
					break;
				}
			}

			preferences.Save();
		}
	}

	/// <summary>
	/// Save the selection.
	/// </summary>

	void SaveSelection (string selection) 
	{ 
		if (preferences != null)
		{
			preferences.SetString(key, selection); 
		}
		
	}

	/// <summary>
	/// Save the state.
	/// </summary>

	void SaveState (bool state) 
	{ 
		if (preferences != null)
		{
			preferences.SetInt(key, state ? 1 : 0); 
		}
	}
}
