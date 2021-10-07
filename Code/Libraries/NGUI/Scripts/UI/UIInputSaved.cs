using Zynga.Core.Util;
//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Editable text input field that automatically saves its data to PlayerPrefsCache.
/// </summary>

[AddComponentMenu("NGUI/UI/Input (Saved)")]
public class UIInputSaved : UIInput
{
	public string playerPrefsField;
	
	private PreferencesBase preferences = null;
	public override string text
	{
		get
		{
			return base.text;
		}
		set
		{
			base.text = value;
			SaveToPlayerPrefs(value);
		}
	}

	void Awake ()
	{
		onSubmit = SaveToPlayerPrefs;
		preferences = SlotsPlayer.getPreferences();

		if (!string.IsNullOrEmpty(playerPrefsField) && preferences.HasKey(playerPrefsField))
		{
			text = preferences.GetString(playerPrefsField);
		}
	}

	private void SaveToPlayerPrefs (string val)
	{
		if (!string.IsNullOrEmpty(playerPrefsField))
		{
			preferences.SetString(playerPrefsField, val);
		}
	}

	void OnApplicationQuit ()
	{
		SaveToPlayerPrefs(text);
		preferences.Save();
	}
}
