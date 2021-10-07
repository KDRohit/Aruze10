using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
	Class: AtlasViewer
	Class to collect all the Atlases uses for the selected SKU in one place so you don't have to search through the project for them.
*/


public class LiveDataViewer : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/LiveData Viewer")]
	public static void openLiveDataViewer()
	{
		LiveDataViewer playerViewer = (LiveDataViewer)EditorWindow.GetWindow(typeof(LiveDataViewer));
		playerViewer.Show();
	}

	private LiveDataViewerObject playerViewerObject;
	
	public void OnGUI()
	{
		if (playerViewerObject == null)
		{
			playerViewerObject = new LiveDataViewerObject();
		}
		playerViewerObject.drawGUI(position);
	}
}

public class LiveDataViewerObject : EditorWindowObject
{
	protected override string getButtonLabel()
	{
		return "LiveData Viewer";
	}

	protected override string getDescriptionLabel()
	{
		return "(RUNTIME ONLY) This will let you see your information at runtime and copy to the clipboard";
	}
	
	private string searchString;
	[SerializeField] private Vector2 scrollPosition = Vector2.zero;
	
	public override void drawGuts(Rect position)
	{
		GUILayout.BeginVertical();

		if (Application.isPlaying)
		{
			searchString = GUILayout.TextField(searchString);
			// Lets only display stuff if we are searching for things to speed it up a bit.
			if (Data.liveData != null && !string.IsNullOrEmpty(searchString))
			{
				scrollPosition = GUILayout.BeginScrollView(scrollPosition);
				for (int i = 0; i < Data.liveData.keys.Count; i++)
				{
					string key = Data.liveData.keys[i];
					if (string.IsNullOrEmpty(searchString) || key.ToLower().Contains(searchString))
					{
						if (GUILayout.Button(key))
						{
							Debug.Log(key + "::" + Data.liveData.getString(key, "none"));
							EditorGUIUtility.systemCopyBuffer = Data.liveData.getString(key, "none");
						}
					}
				}
				GUILayout.EndScrollView();
			}
		}
		else
		{
			GUILayout.Label("Need to be playing the game for this to run");
		}
		GUILayout.EndVertical();
	}
}
