using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
	Class: AtlasViewer
	Class to collect all the Atlases uses for the selected SKU in one place so you don't have to search through the project for them.
*/


public class InformationViewer : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Player Viewer")]
	public static void openInformationViewer()
	{
		InformationViewer playerViewer = (InformationViewer)EditorWindow.GetWindow(typeof(InformationViewer));
		playerViewer.Show();
	}

	private InformationViewerObject playerViewerObject;
	
	public void OnGUI()
	{
		if (playerViewerObject == null)
		{
			playerViewerObject = new InformationViewerObject();
		}
		playerViewerObject.drawGUI(position);
	}
}

public class InformationViewerObject : EditorWindowObject
{
	protected override string getButtonLabel()
	{
		return "Information Viewer";
	}

	protected override string getDescriptionLabel()
	{
		return "(RUNTIME ONLY) This will let you see your information at runtime and copy to the clipboard";
	}

	private string zid = "";
	private string fbid = "";
	private string nid = "";
	private string friendCode = "";
	
	public override void drawGuts(Rect position)
	{
		GUILayout.BeginVertical();
		if (Application.isPlaying)
		{
			if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
			{
				GUILayout.BeginHorizontal();
				zid = SlotsPlayer.instance.socialMember.zId;				
				GUILayout.Label("ZID: " + zid);
				if (GUILayout.Button("Copy"))
				{
				    EditorGUIUtility.systemCopyBuffer = zid;
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
			    fbid = SlotsPlayer.instance.socialMember.id;
				GUILayout.Label("FBID: " + fbid);
				if (GUILayout.Button("Copy"))
				{
				    EditorGUIUtility.systemCopyBuffer = fbid;
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
			    nid = SlotsPlayer.instance.socialMember.networkID;
				GUILayout.Label("NID: " + nid);
				if (GUILayout.Button("Copy"))
				{
				    EditorGUIUtility.systemCopyBuffer = nid;
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Copy Login Data: ");
				if (GUILayout.Button("Copy"))
				{
				    EditorGUIUtility.systemCopyBuffer = Data.login.ToString();
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Copy Live Data: ");
				if (GUILayout.Button("Copy"))
				{
					EditorGUIUtility.systemCopyBuffer = Data.liveData.getAsJSON().ToString();
				}
				GUILayout.EndHorizontal();

				if (SlotsPlayer.instance.socialMember.networkProfile != null)
				{
					GUILayout.BeginHorizontal();
					friendCode = SlotsPlayer.instance.socialMember.networkProfile.friendCode;
					GUILayout.Label("Friend Code: " + friendCode);
					if (GUILayout.Button("Copy"))
					{
						EditorGUIUtility.systemCopyBuffer = friendCode;
					}
					GUILayout.EndHorizontal();
				}
				else
				{
					GUILayout.Label("No Network Profile so no friend code.");
				}
			}
			else
			{
				GUILayout.Label("Waiting on initialization of the slots player/social member");
			}
		}
		else
		{
			GUILayout.Label("Need to be playing the game for this to run");
		}
		GUILayout.EndVertical();
	}
}
