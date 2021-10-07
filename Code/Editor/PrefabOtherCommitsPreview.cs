/*
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;


//This is a custom inspector that provides notice when a prefab has been worked on by someone else.

//This is helpful to avoid being surprised by prefab merges when syncing up with upstream changes.

//Based on work by Warfleet team.

//Original Author: Willy Lee
//Creation Date: 4/19/2017

[CustomPreview (typeof(GameObject))]
public class PrefabOtherCommitsEditor : ObjectPreview
{
	private string lastStatus = string.Empty;
	private string status = string.Empty;
	private static string workingDirectory = string.Empty;

	private void setWorkingDirectory()
	{
		if (string.IsNullOrEmpty(workingDirectory))
		{
			workingDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
		}
	}

	private string lastRepoPath;
	private string repoPath;

	private void changeStatus(string newStatus)
	{
		lastStatus = status;
		status = newStatus;
		EditorApplication.update += Update;
	}

	private void checkForOtherCommits()
	{
		var process = new System.Diagnostics.Process();
		var startInfo = new System.Diagnostics.ProcessStartInfo("git");
		startInfo.RedirectStandardOutput = true;
		startInfo.UseShellExecute = false;
		startInfo.CreateNoWindow = true;
		startInfo.WorkingDirectory = workingDirectory;
		process.StartInfo = startInfo;

		changeStatus("Fetching status...");

		// Get last commit date on current branch for this file.
		System.DateTime lastCommitDate;
		string author;
		startInfo.Arguments = string.Format("log -1 --pretty=format:\"%ai%n%an\" -- \"{0}\"", repoPath);
		process.Start();
		using (System.IO.StreamReader reader = process.StandardOutput)
		{
			lastCommitDate = System.DateTime.Parse(reader.ReadLine().Trim());
			author = reader.ReadLine().Trim();
			changeStatus(string.Format("On this branch: {0} by {1}", lastCommitDate, author));
		}

		// Get commits on other branches after last commit on current branch.
		System.DateTime otherCommitDate;
		startInfo.Arguments = string.Format("log -10 --all --branches=* --remotes=* --since \"{1}\" --pretty=format:\"%ai%n%an\" -- \"{0}\"", repoPath, lastCommitDate);
		process.Start();
		using (System.IO.StreamReader reader = process.StandardOutput)
		{
			var newStatusLine = new System.Text.StringBuilder();
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				line = line.Trim();
				otherCommitDate = System.DateTime.Parse(line);
				author = reader.ReadLine().Trim();
				if (otherCommitDate > lastCommitDate)
				{
					newStatusLine.Append(otherCommitDate);
					newStatusLine.Append(" by ");
					newStatusLine.Append(author);
					newStatusLine.AppendLine();
				}
			}
			if (newStatusLine.Length > 0)
			{
				newStatusLine.Insert(0, "More recent commits:\n");
				newStatusLine.Insert(0, status + "\n");
				changeStatus(newStatusLine.ToString());
			}
		}
	}

	private Object getPrefabObject(Object obj)
	{
		string path = AssetDatabase.GetAssetPath(obj);

        // user is looking directly at a prefab in the project browser
        if (!string.IsNullOrEmpty(path))
		{
			return obj;
		}

        // user is looking at a prefab in a scene?
		UnityEngine.Object prefabObject = null;
        if (obj is GameObject)
		{
			GameObject prefab = PrefabUtility.FindPrefabRoot((GameObject) obj);
			if (prefab != null)
			{
				prefabObject = PrefabUtility.GetPrefabParent(prefab);
			}
		}

        return prefabObject;
	}

	public override bool HasPreviewGUI()
	{
		return true;
	}

	public override void OnPreviewGUI(Rect r, GUIStyle background)
	{
		base.OnPreviewGUI(r, background);
		setWorkingDirectory();

		if (Event.current.type == EventType.Repaint
			|| Event.current.type == EventType.Layout)
		{
			Object prefab = getPrefabObject(target);
			if (prefab != null)
			{
				string assetPath = AssetDatabase.GetAssetPath(prefab);
				// Debug.Log(string.Format("OnPreviewGUI: Looking at {0}: {1}", target.name,
				// 						assetPath));

				if (!string.IsNullOrEmpty(assetPath))
				{
					repoPath = Path.Combine("Unity", assetPath);
				}

				if (repoPath != lastRepoPath)
				{
					lastRepoPath = repoPath;
					System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(checkForOtherCommits));
					thread.Start();
				}

				GUI.skin.label.wordWrap = true;
				GUI.Label(new Rect(r.x, r.y, r.width, r.height / 2), assetPath);
				GUI.Label(new Rect(r.x, r.y + r.height / 2, r.width, r.height / 2), status);
			}
		}
	}

	public override GUIContent GetPreviewTitle()
	{
		if (getPrefabObject(target) != null)
		{
			return new GUIContent(string.Format("Git remote commits: {0}", target.name));
		}
		else
		{
			return new GUIContent(target.name);
		}
	}

	private void Update()
	{
		if (target != null && status != lastStatus)
		{
			EditorUtility.SetDirty(target);
			lastStatus = status;
			EditorApplication.update -= Update;
		}
	}
}
*/
