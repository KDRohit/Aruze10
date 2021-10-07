using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.HitItRich.Feature.VirtualPets;

public class DevGUIMenuVirtualPetsNotifs : DevGUIMenu
{
	public override void drawGuts()
	{
		GUILayout.BeginVertical();

		GUILayout.Label("Local Notifs");
		if (GUILayout.Button("Low Energy Notif"))
		{
			NotificationManager.scheduleTestLocalNotifications( VirtualPetsFeature.LOW_ENERGY_NOTIF_KEY, 10);
		}

		if (GUILayout.Button("Mid Energy Notif"))
		{
			NotificationManager.scheduleTestLocalNotifications( VirtualPetsFeature.MID_ENERGY_NOTIF_KEY, 10);
		}
		
		if (GUILayout.Button("Daily Bonus Fetch Notif"))
		{
			NotificationManager.scheduleTestLocalNotifications( VirtualPetsFeature.DB_FETCH_NOTIF_KEY, 10);
		}

		GUILayout.EndVertical();
	}
}
