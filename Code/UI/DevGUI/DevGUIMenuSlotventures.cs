using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.Util;

public class DevGUIMenuSlotventures : DevGUIMenu
{

	private string[] AllThemes = null;

	public void populateAllThemes()
	{
		AllThemes = Data.liveData.getArray("SLOTVENTURES_AVAILABLE_THEMES", new string[] {});
	}

	public override void drawGuts()
	{
		if (AllThemes == null)
		{
			populateAllThemes();
		}
		
		// Define style for horizontal line separator.
		GUIStyle hlStyle = new GUIStyle(GUI.skin.box);
		hlStyle.stretchWidth = true;
		hlStyle.fixedHeight = 2;

		//saved data
		PreferencesBase prefs = SlotsPlayer.getPreferences();


		//print current override
		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("Active Theme Override: {0}", prefs.GetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE, "")));
		GUILayout.EndHorizontal();

		// Horizontal line separator.
		GUILayout.Box("", hlStyle);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Clear Override"))
		{
			//don't reset the game if we don't have to
			if (!string.IsNullOrEmpty(prefs.GetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE, "")))
			{
				prefs.DeleteKey(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE);
				prefs.Save();
				Glb.resetGame("User reloaded game to change slotventure theme");
			}

		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Reset EUE Status"))
		{
			//don't reset the game if we don't have to
			CustomPlayerData.setValue(CustomPlayerData.SLOTVENTURES_HAS_SEEN_EUE, false);
			CustomPlayerData.setValue(CustomPlayerData.SLOTVENTURES_CURRENT_LOBBY_LOAD, 0);

		}
		GUILayout.EndHorizontal();

		// Horizontal line separator.
		GUILayout.Box("", hlStyle);

		GUILayout.BeginHorizontal();

		int halfWay = AllThemes.Length / 2;
		if (AllThemes.Length % 2 == 1)
		{
			++halfWay;
		}


		GUILayout.BeginVertical();
		for (int i = 0; i < halfWay; i++)
		{
			if (GUILayout.Button(AllThemes[i]))
			{
				prefs.SetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE, AllThemes[i]);
				prefs.Save();

				Glb.resetGame("User reloaded game to change slotventure theme");
			}
		}
		GUILayout.EndVertical();

		GUILayout.BeginVertical();
		for (int i = halfWay; i < AllThemes.Length; ++i)
		{
			if (GUILayout.Button(AllThemes[i]))
			{
				prefs.SetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE, AllThemes[i]);
				prefs.Save();

				Glb.resetGame("User reloaded game to change slotventure theme");
			}
		}
		GUILayout.EndVertical();

		GUILayout.EndHorizontal();
	}
}
