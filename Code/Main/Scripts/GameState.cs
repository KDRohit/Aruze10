using UnityEngine;
using System.Collections.Generic;

/**
This class controls the stack that tells where the player is in the game,
and the trail taken to get there.

Use the public methods to push to the stack:
	GameState.pushGame(gameInfo:Object);
	
Use the public getters to get current state info.
	bool isMain = GameState.isMainLobby;
	LobbyGame gameInfo = GameState.game;	// This will return null if the state isn't a game.
	
When going back down the stack, call the pop method:
	GameState.pop();
*/

public class GameState : TICoroutineMonoBehaviour
{
	// =============================
	// PRIVATE
	// =============================
	private List<StackLevel> gameStack = new List<StackLevel>();
	private StackLevel stackTop = null;	///< Always contains the top level of the gameStack, for convenience and performance.

	// =============================
	// PUBLIC
	// =============================
	public static GameState instance = null;
	public static LobbyGame lastGamePlayed { get; private set; }
	
	void Awake()
	{
		instance = this;
	}
	
	/// Returns whether the top level of the stack is the main lobby.
	public static bool isMainLobby
	{
		get { return (instance != null && instance.stackTop == null); }
	}
	
	/// Used to safely get a string describing the game state. Used for logging/debugging.
	public static string currentStateName
	{
		get
		{
			return getStateName();
		}
	}
	
	public static string currentStateOrKeyName
	{
		get
		{
			return getStateName(true);
		}
	}

	private static string getStateName(bool useGameKey = false)
	{
		if (instance != null)
		{
			if (isMainLobby)
			{
				// We're in the lobby
				return "main lobby";
			}
		
			if (instance.stackTop != null)
			{
				if (instance.stackTop.bonusToLoad != null)
				{
					if (instance.stackTop.bonusToLoad.inboxItem != null)
					{
						return "gifting";
					}
				}
				if (instance.stackTop.game != null)
				{
					// We're in a game
					if (useGameKey)
					{
						return instance.stackTop.game.keyName;
					}
					else
					{
						return instance.stackTop.game.name;
					}
						
				}
				// Any other stack object types in the future might get enumerated here...
			}
		}
		
		// We don't know the game state?!
		return "unknown";
	}

	public static bool isDeprecatedMultiSlotBaseGame()
	{
		return (game.keyName.Contains("hi03") || game.keyName.Contains("gen27") || game.keyName.Contains("oz07"));
	}
	
	/// Returns an object of game info for the top level of the stack, if available.
	public static LobbyGame game
	{
		get
		{
			if (instance == null || instance.stackTop == null)
			{
				return null;
			}
		
			return instance.stackTop.game;
		}
	}

	public static BonusGameNameData bonusGameNameData
	{
		get
		{
			if (instance == null || instance.stackTop == null)
			{
				return null;
			}
		
			return instance.stackTop.bonusGameNameData;
		}
	}

	public static long bonusGameMultiplierForLockedWagers
	{
		get
		{
			if (instance == null)
			{
				// this is most likely a gifted game, assume that the multiplier is 1
				return 1;
			}

			if (instance.stackTop == null || instance.stackTop.game == null)
			{
				return 1;
			}
			
			// used to make bonus games show scaled up values
			return SlotsWagerSets.getMinMultiplierForGame(instance.stackTop.game.keyName);
		}
	}

	public static long baseWagerMultiplier
	{
		get
		{
			if (game == null)
			{
				return 1;
			}

			if (instance.stackTop.gameBaseWagerMultiplier == 0)
			{
				// just return 1 for this in the new system
				instance.stackTop.gameBaseWagerMultiplier = 1;
			}

			return instance.stackTop.gameBaseWagerMultiplier;
		}
	}

	/// Returns an object of GameLoadInfo info for the top level of the stack, if available.
	public static GameLoadInfo giftedBonus
	{
		get
		{
			if (instance.stackTop != null)
			{
				return instance.stackTop.bonusToLoad;
			}
		
			return null;
		}
	}

	public static bool hasEventId
	{
		get
		{	
			if (instance.stackTop != null && instance.stackTop.bonusToLoad != null)
			{
				return instance.stackTop.bonusToLoad.eventId != null;
			}

			return false;
		}
	}		
	
	/// Push a game to the stack.
	// this game is considered to be coming from the lobby and will be treated as such
	// during loading, such as setting the initial wager amount etc..
	public static void pushGame(LobbyGame game)
	{
		instance.gameStack.Add(new StackLevel(game));
		instance.updateTop();
		lastGamePlayed = game;
		Debug.Log("pushed game " + game.name);
	}

	/// Push a gifted bonus game to the stack.
	// used to add a game that is known to exist in the lobby
	public static void pushGiftedBonus(InboxItem inboxItem)
	{
		instance.gameStack.Add(new StackLevel(inboxItem));
		instance.updateTop();
	}

	/// Push a game by game key only
	// used for games that don't exist in the lobby, such as VIP mini progressive
	public static void pushBonusGame(string gameKey, string gameType, string bonusType, string eventId, JSON slotOutcome = null)
	{
		instance.gameStack.Add(new StackLevel(gameKey, gameType, bonusType, eventId, slotOutcome));
		instance.updateTop();
	}	
	
	/// Removes the top level of the game stack.
	/// Does not return the new top level,
	/// because we don't want to expose the gameStack data directly.
	/// Use the class getters instead.
	public static void pop()
	{
		if (instance.gameStack.Count == 0)
		{
			Debug.LogError("GameState.pop() called when the gameStack had no objects in it to pop.");
			return;
		}
		instance.gameStack.RemoveAt(instance.gameStack.Count - 1);
		instance.updateTop();
		Debug.Log("popped gameStack");
	}

	public static void clearGameStack()
	{
		while(instance.gameStack.Count > 0)
		{
			pop();
		}
		Bugsnag.Context = currentStateName;
	}

	public static bool hasGameStack
	{
		get
		{
			return (instance.stackTop != null && instance.stackTop.game != null) || (instance.gameStack != null && instance.gameStack.Count > 0);
		}
	}
	
	/// Keeps the top updated whenever the stack changes.
	private void updateTop()
	{
		if (gameStack.Count == 0)
		{
			stackTop = null;
		}
		else
		{
			stackTop = gameStack[gameStack.Count - 1];
		}
		Bugsnag.Context = currentStateName;
	}

	public static bool tryGetUniqueMultiplier(out float multiplier)
	{
		multiplier = 1f;
		if (game != null)
		{
			switch (game.keyName)
			{
				case "max01":
					multiplier = SlotsPlayer.instance.currentMaxVoltageInflationFactor;
					return true;

				default:
					return false;
			}
		}

		return false;
	}
	
	private class StackLevel
	{
		public LobbyGame game = null;
		public GameLoadInfo bonusToLoad = null; // the base slot game will be unloaded and the bonus game will load with a progress bar if this is not null
		public long gameBaseWagerMultiplier = 0;
		public BonusGameNameData bonusGameNameData = new BonusGameNameData();
		
		// used to stack bonus free spin games on top of existing lobby game
		// bonusToLoad remains null which means the current slot game does not unload, it is only disabled and becomes enabled again after bonusToLoad gets unloaded
		public StackLevel(LobbyGame lobbyGame)
		{
			game = lobbyGame;
		}

		public StackLevel(InboxItem inboxItem)
		{
			initGameLoadInfo(inboxItem.gameKey, "gift_bonus", "gifting", inboxItem.eventId);

			bonusToLoad.inboxItem = inboxItem;
		}

		// used to stack any game
		// the currently loaded slot game will be unloaded when this loads
		public StackLevel(string slotsGameKey, string gameType = "", string bonusType = null, string eventId = "", JSON slotOutcome = null)
		{
			initGameLoadInfo(slotsGameKey, gameType, bonusType, eventId, slotOutcome);
		}

		private void initGameLoadInfo(string slotsGameKey, string gameType, string bonusType = null, string eventId = null, JSON slotOutcome = null)
		{
			bonusToLoad = new GameLoadInfo(slotsGameKey, gameType, bonusType, eventId);
			if (slotOutcome != null)
			{
				bonusToLoad.outcomeJSON = slotOutcome;
			}
			
			if (!slotsGameKey.IsNullOrWhiteSpace())
			{
				game = LobbyGame.find(slotsGameKey);
			}

			if (game == null)
			{
				Debug.LogError("No game could be found to load in StackLevel " + slotsGameKey);
			}
		}
	}

	// everything you need to stack a bonus game for loading
	public class GameLoadInfo
	{
		private string _gameKey = null;
		private string _bonusType = null;
		private string _gameType = null;
		private string _eventId = null;
		public JSON _outcomeJSON = null;	// gets set by server action after game assets are loaded
		public InboxItem inboxItem = null;	// if not null the src of this game is from the inbox

		public GameLoadInfo(string gameKey, string gameType, string bonusType, string eventId)
		{
			_gameKey = gameKey;
			_gameType = gameType;
			_bonusType = bonusType;
			_eventId = eventId;
		}

		public string slotsGameKey
		{
			get
			{
				return _gameKey;
			}		
		}

		public string bonusGameType
		{
			get
			{
				return _bonusType;
			}
		}

		public string type
		{
			get
			{
				//Debug.Log("GAME TYPE: " + type);
				return _gameType;
			}		
		}		

		public string eventId
		{
			get
			{			
				return _eventId;
			}
		}

		public JSON outcomeJSON
		{
			set
			{
				_outcomeJSON = value;
			}

			get
			{
				return _outcomeJSON;			
			}		
		}					
	}	

	// Tracks what bonus games are possible for a game currently on the game stack, these names are used in SlotOutcome.cs
	public class BonusGameNameData
	{
		public List<string> giftingBonusGameNames = new List<string>();
		public List<string> portalBonusGameNames = new List<string>();
		public List<string> scatterPickGameBonusGameNames = new List<string>();
		public List<string> challengeBonusGameNames = new List<string>();
		public List<string> creditBonusGameNames = new List<string>();

		public void populateBonusGameNames(string gameKey)
		{
			SlotGameData gameData = SlotGameData.find(gameKey);
			//SlotGameData gameData = SlotGameData.find("som01");
			if (gameData == null)
			{
				Debug.LogError("GameState.BonusGameNameData() - Couldn't locate SlotGameData for gameKey = " + gameKey);
				return;
			}

			List<string> possibleBonusGameDataKeys = gameData.bonusGameDataKeys;

			for (int i = 0; i < possibleBonusGameDataKeys.Count; i++)
			{
				string name = possibleBonusGameDataKeys[i];
				BonusGame bonusData = BonusGame.find(name);

				if (bonusData != null && bonusData.payTableType == BonusGame.PaytableTypeEnum.FREE_SPIN)
				{
					// Freespin game
					giftingBonusGameNames.Add(name);
					//Debug.Log("gameKey: " + gameKey + " - Adding " + name + " as free_spin game.");
				}
				else if (bonusData != null && bonusData.payTableType == BonusGame.PaytableTypeEnum.WHEEL && name.Contains("_portal") && !name.Contains("_credit"))
				{
					// Portal game
					portalBonusGameNames.Add(name);
					//Debug.Log("gameKey: " + gameKey + " - Adding " + name + " as portal game.");
				}
				else if (bonusData != null && bonusData.payTableType == BonusGame.PaytableTypeEnum.WHEEL && name.Contains("_scatter"))
				{
					// Scatter picking game
					scatterPickGameBonusGameNames.Add(name);
					//Debug.Log("gameKey: " + gameKey + " - Adding " + name + " as scatter picking game.");
				}
				else if (bonusData != null && (bonusData.payTableType == BonusGame.PaytableTypeEnum.BASE_BONUS || bonusData.payTableType == BonusGame.PaytableTypeEnum.PICKEM || bonusData.payTableType == BonusGame.PaytableTypeEnum.CROSSWORD || bonusData.payTableType == BonusGame.PaytableTypeEnum.THRESHOLD_LADDER) 
					|| (bonusData.payTableType == BonusGame.PaytableTypeEnum.WHEEL && !name.Contains("_portal") && !name.Contains("_credit")))
				{
					// Challenge game (Wheel or pickem)
					challengeBonusGameNames.Add(name);
					//Debug.Log("gameKey: " + gameKey + " - Adding " + name + " as challenge game.");
				}
				else if (bonusData != null && bonusData.payTableType == BonusGame.PaytableTypeEnum.WHEEL && !name.Contains("_portal") && name.Contains("_credit"))
				{
					// Credits game
					creditBonusGameNames.Add(name);
					//Debug.Log("gameKey: " + gameKey + " - Adding " + name + " as credits game.");
				}
			}
		}
	}
}
