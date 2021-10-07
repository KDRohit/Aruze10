using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
	Class: AtlasViewer
	Class to collect all the Atlases uses for the selected SKU in one place so you don't have to search through the project for them.
*/


public class PlayerViewer : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Player Viewer")]
	public static void openPlayerViewer()
	{
		PlayerViewer playerViewer = (PlayerViewer)EditorWindow.GetWindow(typeof(PlayerViewer));
		playerViewer.Show();
	}

	private PlayerViewerObject playerViewerObject;
	
	public void OnGUI()
	{
		if (playerViewerObject == null)
		{
			playerViewerObject = new PlayerViewerObject();
		}
		playerViewerObject.drawGUI(position);
	}
}

public class PlayerViewerObject : EditorWindowObject
{
	protected override string getButtonLabel()
	{
		return "Player Viewer";
	}

	protected override string getDescriptionLabel()
	{
		return "(RUNTIME ONLY) This will let you put in a zid and view the data we have stored for a player.";
	}

	private string filter = "";

	private SocialMember toggledMember = null;
	private bool isProfileOpen = false;
	private Vector2 scrollPos = Vector2.zero;

	private bool showAllUsers = true;
	private bool showAllFriends = false;
	private bool showRequests = false;
	private bool showRequested = false;

	private List<JSON> populateAllDatas;
	

	private Dictionary<SocialMember, bool> expandedList = new Dictionary<SocialMember, bool>();


	private void populateExpanded()
	{
		if (expandedList == null)
		{
			expandedList = new Dictionary<SocialMember, bool>();
		}
		if (SocialMember.allIndexedByZId !=  null && SocialMember.allIndexedByZId.Count != expandedList.Count)
		{
			expandedList.Clear();
			foreach(KeyValuePair<string, SocialMember> pair in SocialMember.allIndexedByZId)
			{
				expandedList[pair.Value] = false;
			}
		}
	}


	public bool isInFilter(string filter, SocialMember member)
	{
		return string.IsNullOrEmpty(filter) ||
	    	member.zId.Contains(filter) ||
	    	member.fullName.Contains(filter) ||
	    	member.firstName.Contains(filter) ||
	    	member.lastName.Contains(filter) ||
			member.id.Contains(filter);
	}

	private void showMember(SocialMember member)
	{
		expandedList[member] = EditorGUILayout.Foldout(expandedList[member], member.zId);
		if (expandedList[member])
		{
			GUILayout.TextArea(member.ToString());
			if (GUILayout.Button("Get Profile"))
			{
				NetworkProfileAction.getProfile(member, copyToClipboardAndUpdate);
				NetworkProfileFeature.instance.getPlayerProfile(member);
			}
		}
	}

	private void copyToClipboardAndUpdate(JSON data)
	{
		EditorGUIUtility.systemCopyBuffer = data.ToString();
		Debug.LogFormat("PlayerViewer.cs -- copyToClipboardAndUpdate -- copied to clipboard!");		
		NetworkProfileFeature.instance.parsePlayerProfile(data);
	}

	
	public override void drawGuts(Rect position)
	{
		scrollPos = GUILayout.BeginScrollView(scrollPos);
		GUILayout.BeginVertical();
		if (Application.isPlaying)
		{
			if (SocialMember.hasInitialized)
			{
				for (int i = 0; i < SocialMember.populatedJson.Count; i++)
				{
					JSON data = SocialMember.populatedJson[i];
					if (GUILayout.Button("Copy Data Blob: " + i.ToString()))
					{
						EditorGUIUtility.systemCopyBuffer = data.ToString();
					}
				}
				populateExpanded();
				
				filter = GUILayout.TextField(filter);
				showAllUsers = EditorGUILayout.Foldout(showAllUsers, "All Users: " + SocialMember.allIndexedByZId.Values.Count.ToString());
				if (showAllUsers)
				{
					EditorGUI.indentLevel++;					
					foreach (SocialMember member in SocialMember.allIndexedByZId.Values)
					{
						if (isInFilter(filter, member))
						{
							// Draw the social member
							showMember(member);
						}
					}
					EditorGUI.indentLevel--;					
				}

				showAllFriends = EditorGUILayout.Foldout(showAllFriends, "All Friends: " + SocialMember.allFriends.Count.ToString());
				if (showAllFriends)
				{
					EditorGUI.indentLevel++;					
					foreach (SocialMember member in SocialMember.allFriends)
					{
						if (isInFilter(filter, member))
						{
							// Draw the social member
							showMember(member);
						}
					}
					EditorGUI.indentLevel--;					
				}
				

				showRequests = EditorGUILayout.Foldout(showRequests, "Invited Players: " + SocialMember.invitedPlayers.Count.ToString());
				if (showRequests)
				{
					EditorGUI.indentLevel++;					
					foreach (SocialMember member in SocialMember.invitedPlayers)
					{
						if (isInFilter(filter, member))
						{
							// Draw the social member
							showMember(member);
						}
					}
					EditorGUI.indentLevel--;					
				}
				showRequested = EditorGUILayout.Foldout(showRequested, "Invited By Players: " + SocialMember.invitedByPlayers.Count.ToString());
				if (showRequested)
				{
					EditorGUI.indentLevel++;
					foreach (SocialMember member in SocialMember.invitedByPlayers)
					{
						if (isInFilter(filter, member))
						{
							// Draw the social member
							showMember(member);
						}
					}
					EditorGUI.indentLevel--;
				}	
			}
			else
			{
				GUILayout.Label("SocialMember has not initialized yet.");
			}
		}
		else
		{
			GUILayout.Label("Need to be playing the game for this to run");
		}
		GUILayout.EndVertical();
		GUILayout.EndScrollView();		
	}
}
