using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

/*
Editor tool to make copying audio files from p4 directory to assets easier
*/

public class AudioImportWizard : ScriptableWizard
{
	private string srcFolder = "";
	private string destFolder = "";
	private string maxDepth = "";
	private string prevMaxDepth = "";
	private const string defaultMaxDepth = "3";

	[MenuItem("Zynga/Wizards/Audio Import Wizard")]
	static void CreateWizard()
	{
		AudioImportWizard window = ScriptableWizard.DisplayWizard<AudioImportWizard>("Audio Import Wizard", "Close");

		window.getPrefs();

		int windowWidth = Screen.currentResolution.width / 2;
		int windowHeight = 400;
		window.position = new Rect((Screen.currentResolution.width - windowWidth) / 2, (Screen.currentResolution.height - windowHeight) / 2, windowWidth, windowHeight);
	}

	public void getPrefs()
	{
		srcFolder = PlayerPrefsCache.GetString(DebugPrefs.AUDIO_IMPORT_SRC_FOLDER, "");
		destFolder = PlayerPrefsCache.GetString(DebugPrefs.AUDIO_IMPORT_DEST_FOLDER, "");
		maxDepth = PlayerPrefs.GetString(DebugPrefs.AUDIO_IMPORT_MAX_DEPTH, defaultMaxDepth);
		prevMaxDepth = maxDepth;
	}

	void OnGUI()
	{
		addLabelLine("P4 Folder: " + srcFolder);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Choose P4 folder", GUILayout.Height(30), GUILayout.Width(200)))
		{
			srcFolder = EditorUtility.OpenFolderPanel("Choose wav base folder", srcFolder, "");
			PlayerPrefsCache.SetString(DebugPrefs.AUDIO_IMPORT_SRC_FOLDER, srcFolder);
			PlayerPrefsCache.Save();
		}
		GUILayout.EndHorizontal();

		addLabelLine("");
		addLabelLine("Game Audio Folder: " + destFolder);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Choose output folder", GUILayout.Height(30), GUILayout.Width(200)))
		{
			destFolder = EditorUtility.OpenFolderPanel("Choose output folder", destFolder, "");
			PlayerPrefsCache.SetString(DebugPrefs.AUDIO_IMPORT_DEST_FOLDER, destFolder);
			PlayerPrefsCache.Save();
		}
		GUILayout.EndHorizontal();

		addLabelLine("");
		addLabelLine("Max Folder Recursion Depth: " + maxDepth);
		GUILayout.BeginHorizontal();
		string prevMaxDepth = maxDepth;
		maxDepth = GUILayout.TextField(maxDepth, 2);
		if (maxDepth != prevMaxDepth)
		{
			int i;

			if (!string.IsNullOrEmpty(maxDepth) && !int.TryParse(maxDepth, out i))
			{
				maxDepth = prevMaxDepth;
			}
			else
			{
				PlayerPrefsCache.SetString(DebugPrefs.AUDIO_IMPORT_MAX_DEPTH, maxDepth);
				PlayerPrefsCache.Save();
			}
		}
		GUILayout.EndHorizontal();

		addLabelLine("");

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Import Audio", GUILayout.Height(30), GUILayout.Width(200)))
		{
			verifyMaxDepth();
			System.Diagnostics.ProcessStartInfo proc = new System.Diagnostics.ProcessStartInfo();
			proc.FileName = "python";
			proc.WorkingDirectory = Application.dataPath + "/../../tools/";
			proc.Arguments = "addP4GameAudio.py \"" + srcFolder + "\" " + "\"" + destFolder + "\" " + "--maxDepth " + maxDepth;
			System.Diagnostics.Process.Start(proc);
			EditorUtility.DisplayDialog("Audio Import Complete", "Import Complete", "Okay");
		}
		GUILayout.EndHorizontal();

		addLabelLine("");
		addLabelLine("Command Line (use this to run the command manually)");
		GUILayout.BeginHorizontal();
		GUILayout.TextArea(Application.dataPath + "/../../tools/" + "addP4GameAudio.py \"" + srcFolder + "\" " + "\"" + destFolder + "\" " + "--maxDepth " + maxDepth, 600);
		GUILayout.EndHorizontal();
	}

	private void verifyMaxDepth()
	{
		if (string.IsNullOrEmpty(maxDepth))
		{
			maxDepth = prevMaxDepth = defaultMaxDepth;
			PlayerPrefsCache.SetString(DebugPrefs.AUDIO_IMPORT_MAX_DEPTH, maxDepth);
			PlayerPrefsCache.Save();
		}
	}

	private void addLabelLine(string label)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label);
		GUILayout.EndHorizontal();
	}
}
