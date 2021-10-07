using PrizePop;
using UnityEngine;
using Zynga.Core.Util;

public class DevGUIMenuPrizePopTheme : DevGUIMenu
{
	private string[] allThemes = null;

	public void populateAllThemes()
	{
		allThemes = Data.liveData.getArray("PRIZE_POP_AVAILABLE_THEMES", new string[] {});
	}
	
	public override void drawGuts()
	{
		PreferencesBase prefs = SlotsPlayer.getPreferences();

		//print current override
		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("Active Theme Override: {0}", prefs.GetString(DebugPrefs.PRIZE_POP_THEME_OVERRIDE, "")));
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Clear Override"))
		{
			//don't reset the game if we don't have to
			if (!string.IsNullOrEmpty(prefs.GetString(DebugPrefs.PRIZE_POP_THEME_OVERRIDE, "")))
			{
				prefs.DeleteKey(DebugPrefs.PRIZE_POP_THEME_OVERRIDE);
				prefs.Save();
				Glb.resetGame("User reloaded game to change prize pop theme");
			}

		}
		GUILayout.EndHorizontal();

		if (allThemes == null)
		{
			populateAllThemes();
		}
		
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		for (int i = 0; i < allThemes.Length; i++)
		{
			if (i == allThemes.Length / 2)
			{
				GUILayout.EndVertical();
				GUILayout.BeginVertical();
			}
			if (GUILayout.Button(allThemes[i]))
			{
				prefs.SetString(DebugPrefs.PRIZE_POP_THEME_OVERRIDE, allThemes[i]);
				prefs.Save();

				Glb.resetGame("User reloaded game to change prize pop theme");
			}
		}
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
	}
}
