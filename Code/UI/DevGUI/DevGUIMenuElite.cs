using UnityEngine;
using System.Collections;

public class DevGUIMenuElite : DevGUIMenu
{
	public override void drawGuts()
	{
		
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Elite Mode On"))
		{
			if (Overlay.instance != null)
			{
				Overlay.instance.enableElite();
			}

			if (SpinPanel.hir != null)
			{
				SpinPanel.hir.enableElite();
			}

			if (MainLobbyBottomOverlayV4.instance != null)
			{
				MainLobbyBottomOverlayV4.instance.enableElite();
			}
		}

		if (GUILayout.Button("Elite Mode Off"))
		{
			if (Overlay.instance != null)
			{
				Overlay.instance.disableElite();
			}

			if (SpinPanel.hir != null)
			{
				SpinPanel.hir.disableElite();
			}

			if (MainLobbyBottomOverlayV4.instance != null)
			{
				MainLobbyBottomOverlayV4.instance.disableElite();
			}
		}

		GUILayout.EndHorizontal();

		if (GUILayout.Button("Open Elite Dialog"))
		{
			EliteDialog.showDialog();
			DevGUI.isActive = false;
		}
		
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("First Access"))
		{
			EliteAccessDialog.showDialog(Dict.create(D.STATE, EliteAccessState.FIRST_ACCESS));
			DevGUI.isActive = false;
		}
		if (GUILayout.Button("Second Access"))
		{
			EliteAccessDialog.showDialog(Dict.create(D.STATE, EliteAccessState.SECOND_ACCESS));
			DevGUI.isActive = false;
		}
		if (GUILayout.Button("Expired"))
		{
			EliteAccessDialog.showDialog(Dict.create(D.STATE, EliteAccessState.EXPIRED));
			DevGUI.isActive = false;
		}
		if (GUILayout.Button("Rejoin"))
		{
			EliteAccessDialog.showDialog(Dict.create(D.STATE, EliteAccessState.REJOIN));
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Dialog With Lobby Transition"))
		{
			VIPStatusBoostMOTD.showDialog();
			if (MainLobbyV3.instance != null && MainLobbyV3.hirV3 != null)
			{
				EliteManager.forceLobbyTransition(true);
				MainLobbyV3.hirV3.handleEliteTransition();
				
			}
			
		}

		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}
}