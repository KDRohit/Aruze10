using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;

/*
Game Network dev panel.
*/

public class DevGUIMenuNetworkFriends : DevGUIMenu
{	
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Add Test Friend"))
		{
			string jsonString = "{\"api_success\":\"true\",\"zid\":\"70664634367\"}";
			JSON json = new JSON(jsonString);
			NetworkFriends.instance.fakeAddFriend(json);
		}

		if (GUILayout.Button("Add Test Friend #2"))
		{
			string jsonString = "{\"api_success\":\"true\",\"zid\":\"73473747920\"}";
			JSON json = new JSON(jsonString);
			NetworkFriends.instance.fakeAddFriend(json);
		}

		if (GUILayout.Button("Add Fake Friend"))
		{
			long zidBase = 70664630000L;
			string newZid = (zidBase + Random.Range(0, 9999)).ToString();
			string jsonString = "{\"api_success\":\"true\",\"zid\":\"" + newZid + "\"}";
			JSON json = new JSON(jsonString);
			NetworkFriends.instance.fakeAddFriend(json);
		}

		if (GUILayout.Button("Invited By Fake Friend"))
		{
			// Generate a zid
			long zidBase = 70664630000L;
			string newZid = (zidBase + Random.Range(0, 9999)).ToString();
			string jsonString = "{\"api_success\":\"true\",\"zid\":\"" + newZid + "\"}";
			JSON json = new JSON(jsonString);
			NetworkFriends.instance.fakeInvitedByFriend(json);
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Clear Seen Friends List"))
		{
			NetworkFriends.instance.clearSeenFriends();
		}
		if (GUILayout.Button("Clear Seen Requests List"))
		{
			NetworkFriends.instance.clearSeenRequests();

		}
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
