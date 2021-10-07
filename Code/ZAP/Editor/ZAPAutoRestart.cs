#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/**
 * Class made in order to detect when ZAP has encountered an exception and should auto restart the editor
 *
 * Creation Date: 1/7/2020
 * Original Author: Scott Lepthien
 */
[InitializeOnLoad]
public class ZAPAutoRestart
{
	static ZAPAutoRestart() 
	{
		EditorApplication.playModeStateChanged += onUnityPlayModeChanged;
	}
	
	private static void onUnityPlayModeChanged(PlayModeStateChange state) 
	{
		if (state == PlayModeStateChange.EnteredEditMode)
		{
			// Check if the should resume flag was set and the game isn't playing, which will basically only
			// happen right now when an exception is thrown, other times where this flag gets set it usually just
			// resets the game and then continues from there.
			// NOTE: Currently this flag is cleared when the game is resumed, if that were to ever change then we could
			// end up with this code keeping the game in play mode, which would be bad.
			int shouldZapResume = SlotsPlayer.getPreferences().GetInt(Zap.Automation.ZAPPrefs.SHOULD_RESUME, 0);
			if (shouldZapResume == 1)
			{
				UnityEditor.EditorApplication.isPlaying = true;
			}
		}
	}
}
#endif
