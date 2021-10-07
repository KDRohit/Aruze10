using UnityEngine;
using System.Collections.Generic;
using Com.Scheduler;

/*
A dev panel.
*/

public class DevGUIMenuLobbyOnly : DevGUIMenu
{
	private bool showCommonNames = false;
	private bool showSymbolOptimizedGames = false;
	private bool showUncheckedPortedGames = false;
	private bool lobbyOptionsUpdated = false;
	private bool showGoTopage = false;
	private bool goToPinnedOnly = false;
	private string filter = "";
	
	private static List<LobbyGame> games = null;

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Go to Max Voltage Lobby"))
		{
			DoSomething.now("max_voltage_lobby");
		}

		if (GUILayout.Button("Go to VIP Lobby"))
		{
			DoSomething.now("vip_lobby");
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Key Name Filter:");		
		filter = GUILayout.TextField(filter).Trim();
		GUILayout.EndHorizontal();
		
		GUILayout.BeginVertical();
		showCommonNames = GUILayout.Toggle(showCommonNames, "Show Common Names");
		showSymbolOptimizedGames = GUILayout.Toggle(showSymbolOptimizedGames, "Show Symbol Optimized Games (OPT)");
		showUncheckedPortedGames = GUILayout.Toggle(showUncheckedPortedGames, "Show Unchecked Ported Games (PORT)");
		showGoTopage = GUILayout.Toggle(showGoTopage, "Show Go To Page");
		goToPinnedOnly = GUILayout.Toggle(goToPinnedOnly, "Go to 1x2 only");
		GUILayout.EndVertical();

		// Show a button for each game defined in the slot resource map.
		if (games == null)
		{
			// Only populate this once per session.
			games = new List<LobbyGame>();
			
			List<string> gameKeys = new List<string>(SlotResourceMap.map.Keys);
			gameKeys.Sort();

			foreach (string gameKey in gameKeys)
			{
				LobbyGame game = LobbyGame.find(gameKey);
				if (game != null)	// This nullcheck might not be necessary since we may have already validated data in SlotResourceMap, but whatevs.
				{
					games.Add(game);
				}
			}
		}
		
		// Show a button for each game, with up to four buttons per row for showing keys, or 2 per row when showing common names.
		int buttonsPerRow = (showCommonNames ? 2 : 4);
		int buttonWidth = (int)(DevGUI.windowRect.width - 60) / buttonsPerRow;
		if (showGoTopage)
		{
			buttonWidth /= 2;
		}
		int i = 0;
		string textToSearch = "";
		
		GUILayout.BeginHorizontal();
		foreach (LobbyGame game in games)
		{
			textToSearch = (game.keyName + " " + game.name).ToLower();
			if (filter == "" || textToSearch.Contains(filter))
			{
				if (i % buttonsPerRow == 0 &&
					i > 0 &&
					i < games.Count
					)
				{
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
				}
				SlotResourceData resourceData = SlotResourceMap.getData(game.keyName);
				if (resourceData != null && 
					(resourceData.gameStatus == SlotResourceData.GameStatus.PORT || resourceData.gameStatus == SlotResourceData.GameStatus.PORT_NEEDS_ART)
					)
				{
					if (!showUncheckedPortedGames)
					{
						continue;
					}
				}
				gameButton(game, buttonWidth);
				i++;
			}
		}
		
		GUILayout.EndHorizontal();
	}
	
	// Draws a button for launching a given game.
	private void gameButton(LobbyGame game, int buttonWidth)
	{
		Color resetColor = GUI.color;
		
		string displayName = game.keyName;
		if (showCommonNames)
		{
			displayName += ": " + game.name;
		}
		
		SlotResourceData resourceData = SlotResourceMap.getData(game.keyName);
		if (resourceData != null)
		{
			GUI.color = CommonColor.getColorForGame(game.keyName);
			string gameStatus = CommonColor.getStatusForGame(game.keyName);

			if (gameStatus != null && gameStatus != "")
			{
				displayName = gameStatus + " " + displayName;
			}

			if (showSymbolOptimizedGames)
			{
				if (SlotResourceMap.isGameUsingOptimizedFlattenedSymbols(game.keyName))
				{
					displayName += " (OPT)";
				}
			}

			if (GUILayout.Button(displayName, GUILayout.Width(buttonWidth)))
			{
				DevGUI.isActive = false;

				// Disable all game buttons to make sure a second click isn't registered.
				NGUIExt.disableAllMouseInput();

				// check if wager data isn't setup yet for this game
				if (!SlotsWagerSets.doesGameHaveWagerSet(game.keyName))
				{
					string errorMsg = "Missing Wager Set for: " + game.keyName + ". Disable \"Use new wager system\" from DevGUI Main tab to load this game.";
					Debug.LogError(errorMsg);
					GenericDialog.showDialog(
							Dict.create(
								D.TITLE, Localize.text("error"),
								D.MESSAGE, errorMsg,
								D.REASON, "dev-gui-game-missing-wager-set"
							),
							SchedulerPriority.PriorityType.IMMEDIATE
						);
					return;
				}
				else
				{
					Overlay.instance.top.showLobbyButton();
					GameState.pushGame(game);
					Loading.show(Loading.LoadingTransactionTarget.GAME);
					Glb.loadGame();
				}
			}

			if (showGoTopage)
			{
				if (GUILayout.Button("Go to Page", GUILayout.Width(buttonWidth)))
				{
					DevGUI.isActive = false;

					LobbyInfo lobbyInfo = LobbyInfo.find(LobbyInfo.Type.MAIN);

					if (lobbyInfo != null)
					{
						if (!lobbyOptionsUpdated)
						{
							MainLobby.hirV3.organizeOptionsForAllPages();
							lobbyOptionsUpdated = true;
						}

						for (int i = 0; i < lobbyInfo.allLobbyOptions.Count; i++)
						{
							LobbyOption lobbyOption = lobbyInfo.allLobbyOptions[i];
							if (lobbyOption != null && lobbyOption.game != null &&
							    lobbyOption.game.keyName == game.keyName)
							{
								if (lobbyOption.isPinned && goToPinnedOnly)
								{
									MainLobby.hirV3.pageController.goToPage(lobbyOption.pinned.page);
									break;
								}
								
								MainLobby.hirV3.pageController.goToPage(lobbyOption.page);
							}
						}
					}
				}
			}
		}
		GUI.color = resetColor;
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		games = null;
	}
}
