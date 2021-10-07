using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

/*
Editor tool to reduce repeating pixels and create stretchy sprites for NGUI atlases.
*/

public class AudioDataMaker : ScriptableWizard
{
	private string wavFolder = "";
	private string outputFolder = "";
	private string gameKey = "";
	
	[MenuItem ("Zynga/Wizards/Audio Data Maker")] static void CreateWizard()
	{
		AudioDataMaker window = ScriptableWizard.DisplayWizard<AudioDataMaker>("Audio Data Maker", "Close");

		window.getPrefs();
		
		int windowWidth = Screen.currentResolution.width - 200;
		int windowHeight = 300;
		window.position = new Rect((Screen.currentResolution.width - windowWidth) / 2, (Screen.currentResolution.height - windowHeight) / 2, windowWidth, windowHeight);
	}
	
	public void getPrefs()
	{
		wavFolder = PlayerPrefsCache.GetString(DebugPrefs.AUDIO_MAKER_WAV_FOLDER, "");
		outputFolder = PlayerPrefsCache.GetString(DebugPrefs.AUDIO_MAKER_OUTPUT_FOLDER, "");
		gameKey = PlayerPrefsCache.GetString(DebugPrefs.AUDIO_MAKER_GAME_KEY, "");	
	}
	
	void OnGUI()
	{
		GUILayout.BeginHorizontal();
		
		if (GUILayout.Button("Choose wav base folder", GUILayout.Height(30), GUILayout.Width(200)))
		{
			wavFolder = EditorUtility.OpenFolderPanel("Choose wav base folder", wavFolder, "");
			PlayerPrefsCache.SetString(DebugPrefs.AUDIO_MAKER_WAV_FOLDER, wavFolder);
		}
		
		GUILayout.Label("wav Folder: " + wavFolder);
		
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Choose output folder", GUILayout.Height(30), GUILayout.Width(200)))
		{
			outputFolder = EditorUtility.OpenFolderPanel("Choose output folder", outputFolder, "");
			PlayerPrefsCache.SetString(DebugPrefs.AUDIO_MAKER_OUTPUT_FOLDER, outputFolder);
		}
		
		GUILayout.Label("Output Folder: " + outputFolder);
		
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		GUILayout.Label("Game Key:");
		
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		
		gameKey = GUILayout.TextField(gameKey, GUILayout.Height(20), GUILayout.Width(200));
		PlayerPrefsCache.SetString(DebugPrefs.AUDIO_MAKER_GAME_KEY, gameKey);
		PlayerPrefsCache.Save();

		GUILayout.EndHorizontal();
		GUILayout.Space(20);
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Make It Happen!", GUILayout.Height(30), GUILayout.Width(200)))
		{
			System.Diagnostics.ProcessStartInfo proc = new System.Diagnostics.ProcessStartInfo();
			proc.FileName = "python";
			proc.WorkingDirectory = Application.dataPath + "/../../tools/";
			proc.Arguments = "audiobuilder.py \"" + wavFolder + "\" " + "\"" + outputFolder + "\" " + gameKey;
			System.Diagnostics.Process.Start(proc);
			
			EditorUtility.DisplayDialog("Finished making csv file at", outputFolder, "Kick Ass!");
		}

		GUILayout.EndHorizontal();
	}
}
