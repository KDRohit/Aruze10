using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class S3ValidatorControlPanel : EditorWindow 
{
	private static S3Validator curValidator;

	[MenuItem ("Zynga/S3 Validator")]
	static void Init()
	{
		// Get existing open window or if none, make a new one:
		curValidator = S3Validator.instance;
		S3ValidatorControlPanel window = (S3ValidatorControlPanel)EditorWindow.GetWindow(typeof(S3ValidatorControlPanel));
		window.Show();
	}

	void OnGUI()
	{
		drawControlPanel();
	}

	public static void drawControlPanel()
	{	
		if(curValidator == null)
		{
			curValidator = S3Validator.instance;
		}

		drawGamesPopup();

		drawButtons();
	}
	private static void drawGamesPopup()
	{
		int curPick = curValidator.pickedGameIndex;
		curValidator.pickedGameIndex = EditorGUILayout.Popup(curValidator.pickedGameIndex, curValidator.gameKeyList);
	}

	private static void drawButtons()
	{
		drawSelectedGameButtons();
		drawAllGameButtons();	
	}

	private static void drawSelectedGameButtons()
	{
		GUILayout.Label("Current Selected Game");
		if (GUILayout.Button("Check Lobby Option Image"))
		{
			curValidator.validateLobbyOptionImages(curValidator.curGameKey);
		}

		if (GUILayout.Button("Import Missing Lobby Option Images"))
		{
			curValidator.importMissingLobbyOptionImages(curValidator.curGameKey);
		}

		if (GUILayout.Button("Update Lobby Option Texture Settings"))
		{
			curValidator.updateLobbyOptionImageSettings(curValidator.curGameKey);
		}

		if (GUILayout.Button("Check Lobby Icon Image"))
		{
			curValidator.validateLobbyIconImages(curValidator.curGameKey);
		}

		if (GUILayout.Button("Import Missing Lobby Icon Images"))
		{
			curValidator.importMissingLobbyIconImages(curValidator.curGameKey);
		}

		if (GUILayout.Button("Update Lobby Icon Texture Settings"))
		{
			curValidator.updateLobbyIconImageSettings(curValidator.curGameKey);
		}

		if (GUILayout.Button("Force Update Lobby Option Images"))
		{
			curValidator.forceUpdateLobbyOptionImages(curValidator.curGameKey);
		}

		if (GUILayout.Button("Force Update Lobby Icon Images"))
		{
			curValidator.forceUpdateLobbyIconImages(curValidator.curGameKey);
		}
	}

	private static void drawAllGameButtons()
	{
		GUILayout.Label("All Games");
		if (GUILayout.Button("Check All Lobby Option Images"))
		{
			curValidator.validateAllLobbyOptions();
		}

		if (GUILayout.Button("Import All Missing Lobby Option Images"))
		{
			curValidator.importAllMissingLobbyOptionImages();
		}

		if (GUILayout.Button("Update All Lobby Option Texture Settings"))
		{
			curValidator.updateAllLobbyOptionImageSettings();
		}

		if (GUILayout.Button("Check All Lobby Icon Images"))
		{
			curValidator.validateAllLobbyIcons();
		}

		if (GUILayout.Button("Import All Missing Lobby Icon Images"))
		{
			curValidator.importAllMissingLobbyIconImages();
		}

		if (GUILayout.Button("Update All Lobby Icons Texture Settings"))
		{
			curValidator.updateAllLobbyIconImageSettings();
		}

		if (GUILayout.Button("Force Update All Lobby Option Images"))
		{
			curValidator.forceUpdateAllLobbyOptionImages();
		}

		if (GUILayout.Button("Force Update All Lobby Icon Images"))
		{
			curValidator.forceUpdateAllLobbyIconImages();
		}
	}
}
