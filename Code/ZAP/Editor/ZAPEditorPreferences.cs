using UnityEngine;
using UnityEditor;

namespace Zap.Automation.Editor
{
	public class ZAPEditorPreferences
	{
		//Have we loaded the prefs yet
		private static bool prefsLoaded = false;

		//All ZAP EditorPrefs
		private static string _defaultLocation = "";
		private static string _resultsLocation = "";

		[PreferenceItem("ZAP")]
		public static void PreferencesGUI()
		{
			//Load the preferences
			if (!prefsLoaded)
			{
				_defaultLocation = ZAPFileHandler.getZapSaveFileLocation();
				_resultsLocation = ZAPFileHandler.getZapResultsFileLocation();

				prefsLoaded = true;
			}

			//Preferences GUI
			GUILayout.BeginHorizontal();
			_defaultLocation = EditorGUILayout.TextField("Zap Test Plans Location", _defaultLocation);
			if (GUILayout.Button("..."))
			{
				string selectedPath = EditorUtility.OpenFolderPanel("Choose Test Plan Location", _defaultLocation, "");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					_defaultLocation = selectedPath;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			_resultsLocation = EditorGUILayout.TextField("Zap Results Location", _resultsLocation);
			if (GUILayout.Button("..."))
			{
				string selectedPath = EditorUtility.OpenFolderPanel("Choose Results Location", _resultsLocation, "");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					_resultsLocation = selectedPath;
				}
			}
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Results Folder: " + SlotsPlayer.getPreferences().GetString(ZAPPrefs.TEST_RESULTS_FOLDER_KEY));
			GUILayout.EndHorizontal();

			//Save the preferences
			if (GUI.changed)
			{
				SlotsPlayer.getPreferences().SetString(ZAPPrefs.ZAP_SAVE_LOCATION, _defaultLocation);
				SlotsPlayer.getPreferences().SetString(ZAPPrefs.ZAP_RESULTS_LOCATION, _resultsLocation);
			}
		}
	}
}
