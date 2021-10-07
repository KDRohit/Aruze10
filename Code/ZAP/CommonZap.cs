using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	public static class CommonZap
	{
		public static IEnumerator loadGameFromLobby(string key)
		{
			// If we aren't in the lobby, go back there.
			if (!GameState.isMainLobby)
			{
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("Back"));
			}

			// Wait for loading to finish.
			while (!GameState.isMainLobby || Loading.isLoading)
			{
				yield return null;
			}

			LobbyGame gameInfo = LobbyGame.find(key);
			if (gameInfo == null)
			{
				throw new System.Exception("CommonAutomation.cs -- loadGameFromLobby() -- Failed to find game: " + key);
			}

			SlotResourceData resourceData = SlotResourceMap.getData(key);
			// Load the game.
			Overlay.instance.top.showLobbyButton();
			GameState.pushGame(gameInfo);
			Loading.show(Loading.LoadingTransactionTarget.GAME);
			Glb.loadGame();
		}

		public static string getRandomUnlockedGame()
		{
			List<LobbyGame> games = new List<LobbyGame>(LobbyGame.getAll());
			System.Random r = new System.Random();
			LobbyGame game = null;
			while (games.Count > 0)
			{
				game = games[r.Next(games.Count)];
				if (game.isUnlocked)
				{
					return game.keyName;
				}

				games.Remove(game);
			}

			return "";
		}
	}
#endif
}
