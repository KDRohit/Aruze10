using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuPlayer : DevGUIMenu
{
	private string addCredits = "100";
	private string addXP = "100";
	private string jumpToLevel = "";
	private string addVIPPoints = "100";
	private string setFakeLevel = "1";
	private string unlockGame = "";

	private float vipButtonCoolDown;
	private string subEndDateDays = "0" ;

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		addCredits = intInputField("Credits", addCredits, 100).ToString();
		if (GUILayout.Button("Add", GUILayout.Width(100)))
		{
			try
			{
				int amount = int.Parse(addCredits);
				PlayerAction.addCredits(amount);
				SlotsPlayer.addCredits(amount, "dev panel");
			}
			catch
			{
				Debug.LogError("Non-numeric credits amount specified: " + addCredits);
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		addXP = intInputField("XP", addXP, 100).ToString();
		if (GUILayout.Button("Add", GUILayout.Width(100)))
		{
			int amount = int.Parse(addXP);
			PlayerAction.addXP(amount);

			// The server is going to multiply the amount, so the client should, too.
			SlotsPlayer.instance.xp.add(XPMultiplierEvent.instance.xpMultiplier * amount, "dev panel");
		}
		GUILayout.EndHorizontal();

		if (SlotsPlayer.instance.socialMember.experienceLevel < ExperienceLevelData.maxLevel)
		{
			GUILayout.BeginHorizontal();
			if (jumpToLevel == "")
			{
				jumpToLevel = (SlotsPlayer.instance.socialMember.experienceLevel + 1).ToString();
			}
			int newLevel = intInputField
			(
				"Jump to Level (will suppress unlock dialogs)",
				jumpToLevel,
				1,
				SlotsPlayer.instance.socialMember.experienceLevel + 1,
				ExperienceLevelData.maxLevel
			);
			jumpToLevel = newLevel.ToString();
			if (GUILayout.Button("Jump", GUILayout.Width(100)))
			{
				LevelUpDialog.isDevLevelUp = true;
				PlayerAction.addLevels(newLevel - SlotsPlayer.instance.socialMember.experienceLevel);
			}
			GUILayout.EndHorizontal();
		}

		GUILayout.BeginHorizontal();
		ExperienceLevelData nextLevel = ExperienceLevelData.find(SlotsPlayer.instance.socialMember.experienceLevel + 1);

		long requiredXp = 0;
		if (nextLevel != null)
		{
			requiredXp = nextLevel.requiredXp;
		}

		GUILayout.Label("Current XP: " + CommonText.formatNumber(SlotsPlayer.instance.xp.amount) + " / Next Level: " + CommonText.formatNumber(requiredXp));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		addVIPPoints = intInputField("VIP Points", addVIPPoints, 100).ToString();
		// allow button to be pressed every 2 seconds so we don't flood the server with player actions which can cause issues
		// such as missing vip level up events
		if (vipButtonCoolDown > 0)
		{
			vipButtonCoolDown -= Time.deltaTime;
			Color resetColor = GUI.color;
			GUI.color = Color.red;
			GUILayout.Button("Adding....", GUILayout.Width(100));
			GUI.color = resetColor;
		}
		else if (GUILayout.Button("Add", GUILayout.Width(100)))
		{
			try
			{
				int amount = int.Parse(addVIPPoints);
				PlayerAction.addVIPPoints(amount);
				SlotsPlayer.instance.addVIPPoints(amount);
				vipButtonCoolDown = 2.0f;						
			}
			catch
			{
				Debug.LogError("Non-numeric VIP points amount specified: " + addVIPPoints);
			}
		}
		GUILayout.EndHorizontal();

		if (SelectGameUnlockDialog.gamesToDisplay != null)
		{
			GUILayout.Label("Games available for unlocking : " + SelectGameUnlockDialog.gamesToDisplay.Count);
		}
		GUILayout.Label("Is waiting for Level Up event : " + SelectGameUnlockDialog.isWaitingForLevelUpEvent);
		GUILayout.BeginHorizontal();
		GUILayout.Label("Unlock Game");
		unlockGame = GUILayout.TextField(unlockGame, GUILayout.Width(isHiRes ? 120 : 60));
		if (GUILayout.Button("Unlock", GUILayout.Width(100)))
		{
			PlayerAction.devGameUnlock(unlockGame);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Unlock All Games"))
		{
			if (SlotResourceMap.map != null)
			{
				foreach (string gameKey in SlotResourceMap.map.Keys)
				{
					PlayerAction.devGameUnlock(gameKey);
				}
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		setFakeLevel = intInputField("Set VIP Level buff level", setFakeLevel, 100).ToString();
		if (GUILayout.Button("Set", GUILayout.Width(100)))
		{
			int amount = int.Parse(setFakeLevel);
			ExperimentWrapper.VIPLevelUpEvent.boostAmount = amount;
			VIPStatusBoostEvent.setup();
		}
		GUILayout.EndHorizontal();


		GUILayout.BeginHorizontal();

		string prevCountryCode = PlayerPrefs.GetString(DebugPrefs.DEVGUI_COUNTRY_CODE, "");	
		GUILayout.Label("Country Code Override on startup. Leave blank for default behavior: ");

		string countryCodeOverride = GUILayout.TextField(prevCountryCode);

		if (prevCountryCode != countryCodeOverride)
		{
			PlayerPrefs.SetString(DebugPrefs.DEVGUI_COUNTRY_CODE, countryCodeOverride);
		}


		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Device has touch support: " + Input.touchSupported);
		GUILayout.EndHorizontal();

	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
