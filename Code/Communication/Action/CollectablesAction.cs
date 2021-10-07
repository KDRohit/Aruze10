using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
for handling Collectables server actions.
*/

public class CollectablesAction : ServerAction
{
	//Server Action Types are public so we can reg for them without passing in a callback

	// Gets info for a specific rush via event ID?
	public const string SET_SEEN = "collectible_set_seen";
	public const string GET_COLLECTABLE_SEASON_INFO = "get_collectable_season_info";
	public const string GET_FREE_FIRST_PACK = "collectible_pack_first_drop";
	public const string GET_FREE_FIRST_POWERUP_PACK = "collectible_pack_first_powerup_drop";
	public const string PACK_DROP_SEEN = "collectible_pack_drop_seen";
	public const string SEASON_END_SEEN = "collectible_season_end_seen";
	public const string REWARD_UPGRADED_SEEN = "collectible_reward_upgraded_seen";

	private string albumKey = "";
	private string setKey = "";
	private string eventId = "";
	private string gameKey = "";

	//property names
	private const string ALBUM_KEY = "album";
	private const string SET_KEY = "set";
	private const string EVENT_ID = "event";
	private const string GAME_KEY = "game_key";

	//prevent seen pack form sending multiple times
	private static HashSet<string> _seenPacks = new HashSet<string>();

	private CollectablesAction(ActionPriority priority, string type) : base(priority, type)
	{
		
	}

	public static void upgradedRewardSeen()
	{
		CollectablesAction action = new CollectablesAction(ActionPriority.HIGH, REWARD_UPGRADED_SEEN);
	}

	public static void markSetSeen(string albumKeyName, string setKeyName)
	{
		CollectablesAction action = new CollectablesAction(ActionPriority.HIGH, SET_SEEN);
		action.albumKey = albumKeyName;
		action.setKey = setKeyName;
	}

	public static void getFreeFirstPack(string albumKeyName)
	{
		CollectablesAction action = new CollectablesAction(ActionPriority.IMMEDIATE, GET_FREE_FIRST_PACK);
		action.albumKey = albumKeyName;
	}

	public static void getFreeFirstPowerupPack(string albumKeyName)
	{
		CollectablesAction action = new CollectablesAction(ActionPriority.IMMEDIATE, GET_FREE_FIRST_POWERUP_PACK);
		action.albumKey = albumKeyName;
	}

	public static void cardPackSeen(string eventId, string gameKey = "")
	{
		if (!string.IsNullOrEmpty(eventId) && !_seenPacks.Contains(eventId))
		{
			if (gameKey == "" && RoyalRushEvent.instance.rushInfoList != null)
			{
				//In case this pack came during login from a missed pack, check for any paused RR games to un pause
				for (int i = 0; i < RoyalRushEvent.instance.rushInfoList.Count; i++)
				{
					RoyalRushInfo info = RoyalRushEvent.instance.rushInfoList[i];
					if (info.currentState == RoyalRushInfo.STATE.PAUSED)
					{
						gameKey = info.gameKey;
						break;
					}
				}
			}

			CollectablesAction action = new CollectablesAction(ActionPriority.IMMEDIATE, PACK_DROP_SEEN);
			action.eventId = eventId;
			action.gameKey = gameKey;

			_seenPacks.Add(eventId);
		}
	}

	public static void seasonEndSeen()
	{
		CollectablesAction action = new CollectablesAction(ActionPriority.LOW, SEASON_END_SEEN);
	}

	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(SET_SEEN, new string[] { ALBUM_KEY, SET_KEY });
				_propertiesLookup.Add(GET_COLLECTABLE_SEASON_INFO, new string[] { ALBUM_KEY });
				_propertiesLookup.Add(GET_FREE_FIRST_PACK, new string[] { ALBUM_KEY });
				_propertiesLookup.Add(GET_FREE_FIRST_POWERUP_PACK, new string[] { ALBUM_KEY });
				_propertiesLookup.Add(PACK_DROP_SEEN, new string[] { EVENT_ID, GAME_KEY });
				_propertiesLookup.Add(REWARD_UPGRADED_SEEN, new string[] {});
				_propertiesLookup.Add(SEASON_END_SEEN, new string[] {});
			}
			return _propertiesLookup;
		}
	}
	private static Dictionary<string, string[]> _propertiesLookup = null;

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!propertiesLookup.ContainsKey(type))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}

		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
			case ALBUM_KEY:
				appendPropertyJSON(builder, property, albumKey);
				break;
			case SET_KEY:
				appendPropertyJSON(builder, property, setKey);
				break;
			case EVENT_ID:
				appendPropertyJSON(builder, property, eventId);
				break;
			case GAME_KEY:
				appendPropertyJSON(builder, property, gameKey);
				break;
			default:
				Debug.LogWarning("Unknown property for action: " + type + ", " + property);
				break;
			}
		}
	}


	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		_propertiesLookup = null;
		_seenPacks.Clear();

	}
}
