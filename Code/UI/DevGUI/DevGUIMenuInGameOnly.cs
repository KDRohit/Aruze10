using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuInGameOnly : DevGUIMenu
{
	private string[] mysteryGiftForcedOutcomeKeyStrings = new string[]
	{
		";",
		"'",
		",",
		"."
	};
	
	private KeyCode[] mysteryGiftForcedOutcomeKeyCodes = new KeyCode[]
	{
		KeyCode.Semicolon,
		KeyCode.Quote,
		KeyCode.Comma,
		KeyCode.Period
	};
	
	public static string slotOutcomeString = "";
	public string slotOutcomeName = "";
	private bool toggleOverlay = true;

	private static readonly KeyCode[] keyCodes = new[] {KeyCode.W, KeyCode.Y,KeyCode.U,KeyCode.I,KeyCode.G,KeyCode.C,KeyCode.B,KeyCode.N,KeyCode.M,KeyCode.Alpha1,
														KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5,KeyCode.Alpha6,KeyCode.Alpha7, KeyCode.Alpha8,
														KeyCode.Alpha9,KeyCode.Alpha0,KeyCode.Q,KeyCode.R,KeyCode.E,KeyCode.T,KeyCode.S};
	
	public override void drawGuts()
	{
		const int buttonsPerRow = 3;
		int buttonWidth = (int)((DevGUI.windowRect.width / 3) * 2.65f) / buttonsPerRow;
		
		GUILayout.BeginHorizontal();

		GUI.skin.button.padding.right = 12;

		int drawnButtonCount = 0;
		foreach (var keyCode in keyCodes)
		{
			if (drawnButtonCount % buttonsPerRow == 0 && drawnButtonCount > 0 && drawnButtonCount < keyCodes.Length)
			{
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
			}

			string keyCharacter = keyCode.ToString();
			
			//number keys remove "Alpha" from display
			keyCharacter = keyCharacter.Replace("Alpha", "");
			drawnButtonCount = gameKeyButton(keyCharacter, keyCode, buttonWidth) ? drawnButtonCount + 1 : drawnButtonCount;
		}
		GUILayout.EndHorizontal();

		GUI.skin.button.padding.right = 0;
		
		GUILayout.Space(10);

		if (ReelGame.activeGame.reelGameBackground != null)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Toggle Overlay"))
			{
				ReelGame.activeGame.reelGameBackground.toggleForceShowSpecialWinOverlay(toggleOverlay);
				toggleOverlay = !toggleOverlay;
				DevGUI.isActive = false;
			}
			
			GUILayout.EndHorizontal();
		}

		if (ReelGame.activeGame != null)
		{
			if (GUILayout.Button("Symbol Names: " + (ReelGame.activeGame.testGUI ? "On": "Off")))
			{
				ReelGame.activeGame.testGUI = !ReelGame.activeGame.testGUI;
			}
		}

		// Cheat buttons for mystery gifts is more dynamic since it relies on
		// an id that is specific to the game.
		if (SlotBaseGame.instance.slotGameData.mysteryGiftForcedOutcomeData != null && SlotBaseGame.instance.slotGameData.mysteryGiftForcedOutcomeData.Count > 0)
		{
			GUILayout.BeginHorizontal();
			const int mysteryGiftButtonsPerRowMax = 2;

			drawnButtonCount = 0;
			for (int i = 0; i < SlotBaseGame.instance.slotGameData.mysteryGiftForcedOutcomeData.Count && i < mysteryGiftForcedOutcomeKeyStrings.Length; i++)
			{
				if (drawnButtonCount > 0 && drawnButtonCount % mysteryGiftButtonsPerRowMax == 0)
				{
					// Limit to two buttons per row, due to the length of the pay_table_set_key_name.
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
				}
				
				// Show the keyboard key and the paytable id on the button.
				string buttonText = mysteryGiftForcedOutcomeKeyStrings[i] + " (" +
					SlotBaseGame.instance.slotGameData.mysteryGiftForcedOutcomeData[i].getString("pay_table_set_key_name", "") +
					")";
				
				int id = SlotBaseGame.instance.slotGameData.mysteryGiftForcedOutcomeData[i].getInt("id", 0);
				if (id > 0)
				{
					drawnButtonCount = gameKeyButton(buttonText, mysteryGiftForcedOutcomeKeyCodes[i], buttonWidth) ? drawnButtonCount + 1 : drawnButtonCount;
				}
			}
			
			GUILayout.EndHorizontal();
		}
		
		GUILayout.Space(15);
		
		GUILayout.BeginHorizontal();
		float spinSpeed = SlotBaseGame.instance.slotGameData.spinMovementNormal * SlotBaseGame.instance.slotGameData.symbolHeight;
		GUILayout.Label("Spin Speed " + (int)spinSpeed, GUILayout.Width(isHiRes ? 260 : 130));
		spinSpeed = GUILayout.HorizontalSlider(spinSpeed, 1, 10000, GUILayout.Width(isHiRes ? 300 : 150));
		SlotBaseGame.instance.slotGameData.spinMovementNormal = spinSpeed / SlotBaseGame.instance.slotGameData.symbolHeight;
		GUILayout.EndHorizontal();
		
		GUILayout.Space(15);
		
		TextAnchor oldAlignment = GUI.skin.label.alignment;
		GUI.skin.label.alignment = TextAnchor.MiddleCenter;
		GUILayout.Label("OUTCOME STRING");
		GUI.skin.label.alignment = oldAlignment;
		
		GUILayout.BeginHorizontal();
		
#if UNITY_EDITOR
		if (GUILayout.Button("Copy", GUILayout.Width(100)))
		{
			UnityEditor.EditorGUIUtility.systemCopyBuffer = slotOutcomeString;
		}

		slotOutcomeName = GUILayout.TextField(slotOutcomeName);
	
		if (GUILayout.Button("Save (eg 'lis01-pickem-2x')", GUILayout.Width(400)))
		{
			if (!string.IsNullOrEmpty(slotOutcomeName) && SlotBaseGame.instance != null) 
			{
				string dir =
					"Assets/-Temporary Storage-/Fake Server Message Collection/" +
					SlotBaseGame.instance.slotGameData.keyName + "/";
				
				if (!System.IO.Directory.Exists(dir))
				{
					System.IO.Directory.CreateDirectory(dir);
				}
				
				System.IO.File.WriteAllText(
					dir + slotOutcomeName + ".txt",
					slotOutcomeString);
				
				// Make sure you can see the new file in the project.
				UnityEditor.AssetDatabase.Refresh();
			}
		}
#endif
		
		GUILayout.EndHorizontal();

		drawEmailButtonGuts("Email Outcome", handleEmailClick);

		GUILayout.TextArea(slotOutcomeString);
		
		if (GameState.game != null)
		{
			GUILayout.Space(15);
			GUILayout.BeginHorizontal();

			oldAlignment = GUI.skin.label.alignment;
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			GUILayout.Label("WAGERS");
			GUI.skin.label.alignment = oldAlignment;
			
			GUILayout.EndHorizontal();
			
			LobbyGame game = LobbyGame.find(GameState.game.keyName);
			string wagerSet = SlotsWagerSets.getWagerSetForGame(GameState.game.keyName);
			SlotsWagerSets.WagerSetData wagerSetData = SlotsWagerSets.getWagerSet(wagerSet);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Wager Set: " + wagerSet);

			if (game.isProgressive || game.isMultiProgressive)
			{
				long minQualifyingBet = 0L;

				minQualifyingBet = game.isMultiProgressive ? wagerSetData.multiProgressiveMinBet : wagerSetData.progressiveJackpotMinBet;

				GUILayout.Label("Min Bet (Uninflated): " + CommonText.formatNumber(minQualifyingBet));
				GUILayout.Label("Current PJP Inflation: " + SlotsPlayer.instance.currentPjpWagerInflationFactor);
				GUILayout.Label("Min Bet (Inflated): " + CommonText.formatNumber(minQualifyingBet * SlotsPlayer.instance.currentPjpWagerInflationFactor));
			}
			else if (game.isMaxVoltageGame && MaxVoltageTokenCollectionModule.bronzeTokenInfo != null &&
					MaxVoltageTokenCollectionModule.silverTokenInfo != null && MaxVoltageTokenCollectionModule.goldTokenInfo != null)
			{
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Min Bet Bronze (Uninflated): " + CommonText.formatNumber(MaxVoltageTokenCollectionModule.bronzeTokenInfo.startingMinWager));
				GUILayout.Label("Min Bet Silver (Uninflated): " + CommonText.formatNumber(MaxVoltageTokenCollectionModule.silverTokenInfo.startingMinWager));
				GUILayout.Label("Min Bet Gold (Uninflated): " + CommonText.formatNumber(MaxVoltageTokenCollectionModule.goldTokenInfo.startingMinWager));
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Current MVZ Inflation: " + SlotsPlayer.instance.currentMaxVoltageInflationFactor);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Min Bet Bronze (Inflated+Nearest): " + CommonText.formatNumber(MaxVoltageTokenCollectionModule.bronzeTokenInfo.minimumWager));
				GUILayout.Label("Min Bet Silver (Inflated+Nearest): " + CommonText.formatNumber(MaxVoltageTokenCollectionModule.silverTokenInfo.minimumWager));
				GUILayout.Label("Min Bet Gold (Inflated+Nearest): " + CommonText.formatNumber(MaxVoltageTokenCollectionModule.goldTokenInfo.minimumWager));
			}

			GUILayout.EndHorizontal();

			long[] wagers = SlotsWagerSets.getWagerSetValuesForGame(SlotBaseGame.instance.slotGameData.keyName);

			if (wagers != null)
			{
				foreach (long wager in wagers)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label(wager.ToString());
					GUILayout.EndHorizontal();
				}
			}
		}
	}

	private void handleEmailClick()
	{
		string subject = "Outcome for";

		if (GameState.game != null)
		{
			subject += " " + GameState.game.keyName;
		}

		sendDebugEmail(subject, slotOutcomeString);
	}
	
	// Draws a button for effectively pressing a keyboard key while a game is running.
	private bool gameKeyButton(string keyCharacter, KeyCode keyCode, int buttonWidth)
	{
		Color prevColor = GUI.color;
		GUI.skin.button.alignment = TextAnchor.MiddleLeft;
		GUI.skin.button.contentOffset = new Vector2(10, 0);
		bool hasValidCheat = false;

		if (SlotBaseGame.instance != null && SlotBaseGame.instance.checkForcedOutcomes(keyCharacter))
		{
			hasValidCheat = true;
			if (SlotBaseGame.instance.hasFakeServerMessageInForcedOutcomes(keyCharacter))
			{
				GUI.color = new Color(0.0f, 0.5f, 1.0f);
			}
			else if (SlotBaseGame.instance.isUsingServerCheatForForcedOutcome(keyCharacter))
			{
				GUI.color = new Color(0.0f, 0.65f, 0.65f);
			}
			else
			{				
				GUI.color = new Color(0.0f, 1.0f, 0.0f);
			}
		}
		else
		{
			GUI.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		}

		if (hasValidCheat)
		{
			string cheatName = "";
			SlotBaseGame.ForcedOutcome forcedOutcome = SlotBaseGame.instance.getForcedOutcome(keyCharacter);
			if (forcedOutcome != null && !string.IsNullOrEmpty(forcedOutcome.serverCheatName))
			{
				cheatName = forcedOutcome.serverCheatName + ": ";
			}

			if (GUILayout.Button( cheatName + "<b>" + keyCharacter + "</b>", GUILayout.Width(buttonWidth)) && SlotBaseGame.instance)
			{
				SlotBaseGame.instance.touchKey(keyCode);
			}
		}

		GUI.color = prevColor;
		GUI.skin.button.alignment = TextAnchor.MiddleCenter;
		GUI.skin.button.contentOffset = Vector2.zero;

		return hasValidCheat;
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{


	}
}
