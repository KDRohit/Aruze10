using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This is a dictionary with enum typed keys, and generic object type values.
This is for use where ever generic hashtable types might be used to store and pass around
values.

As you need keys for different uses, please add them to the D enum below.

The purpose of this class is for us to avoid using string literal constants in code excessively.
It is okay that this be a multi-purpose class, however it is recommended that a strict class
with exactly named members where known parameters are passed around. Only use this for
passing around "generic"-ish data.
*/
public class Dict : Dictionary<D, object>
{
	private Dict() : base(new EnumComparer())
	{
		// This should only be called internally from the factory method.
	}

	// Creates and returns a new Dict, automatically merging in the given args.
	// Returns null if there is any error processing args.
	// Everything should use this factory method to create a Dict object.
	public static Dict create(params object[] args)
	{
		Dict dict = new Dict();
		if (dict.merge(args))
		{
			return dict;
		}
		return null;
	}

	// avoids boxing and allocations when dictionary is used with enum key
	// http://stackoverflow.com/questions/26280788/dictionary-enum-key-performance
	public struct EnumComparer : IEqualityComparer<D>
	{
		public bool Equals(D x, D y)
		{
			return x == y;
		}

		public int GetHashCode(D enumVal)
		{
			return (int)enumVal;
		}
	}

	public bool isEqualTo(Dict args)
	{
		if (this.Count != args.Count)
		{
			return false;
		}

		object value;
		foreach (KeyValuePair<D, object> pair in args)
		{
			TryGetValue(pair.Key, out value);
			if (pair.Value != value || value == null)
			{
				return false;
			}
		}

		return true;
	}
	
	// Universal interface to help in the creation of Dicts out of both key/value pairs
	// and other Dicts. The args are consumed left to right.
	//
	// When a D is encountered, that D is used as a key and the next arg is used as the value.
	// When a Dict is encountered, all pairs from that Dict are merged into this one.
	// When a null is encountered, it is skipped (unless of course it is a value in a pair).
	//
	// If something unexpected is encountered, or if there is a missing value for a D, then
	// a LogError occurs and the function returns. It does not attempt to keep processing.
	// Returns false if any errors were encountered, and returns true otherwise.
	//
	// Example inputs could be:
	// 	dict.merge(D.TITLE, "My Title", D.MESSAGE, "Hello World");
	// 	dict.merge(someOtherDict);
	// 	dict.merge(D.MESSAGE, "Hello World", someOtherDict, D.CALLBACK, new GenericDelegate(myCallbackFunc));
	public bool merge(params object[] args)
	{
		int i = 0;
		while (i < args.Length)
		{
			if (args[i] == null)
			{
				// Skip nulls
				i++;
			}
			else if ((i + 1 < args.Length) && (args[i] is D))
			{
				// Merge in a key/value pair
				this[(D)args[i]] = args[i + 1];
				i += 2;
			}
			else if (args[i] is Dict)
			{
				// Merge in a full other Dict
				foreach (KeyValuePair<D, object> p in (Dict)args[i])
				{
					if (this.ContainsKey(p.Key))
					{
						Debug.LogWarningFormat("Dict.cs -- merge -- merging in another Dict and found a conflict, taking new value for {0}", p.Key);
					}
					this[p.Key] = p.Value;
				}
				i++;
			}
			else
			{
				// Safety check for args that break the formatting
				Debug.LogError("Dict.merge() encountered an error processing the given args."); 
				return false;
			}
		}
		return true;
	}
	
	// Attempts to get a value, using the default if the key isn't found.
	public object getWithDefault(D key, object defaultValue)
	{
		object returnVal;
		if (this.TryGetValue(key, out returnVal))
		{
			return returnVal;
		}
		return defaultValue;
	}

	/// Determine if the dicitonary contains the passed key
	public bool containsKey(D key)
	{
		return ContainsKey(key);
	}
}

// This is an enum for compile-checking of Dict entries.
// This list might get REALLY long, so please add entries
// in alphabetical order.
public enum D
{
	ACHIEVEMENT,
	ACTIVE,
	AMOUNT,
	ANSWER,
	APPLIES_TO,
	AUDIO_KEY,
	AUTOPOP,	
	BASE_CREDITS,
	BET_CREDITS,
	BONUS_CREDITS,
	BONUS_PERCENT,
	BONUS_GAME,
	BUNDLE_NAME,
	CALLBACK,
	CALLING_OBJECT,
	CAMPAIGN_NAME,
	CHARM,
	CLOSE,
	COLLECTABLE_CARDS,
	CUSTOM_INPUT,
	DAILY_BONUS_DATA,
	DATA,
	DELAYED_DIALOG_ID,
	DIALOG_TYPE,
	DOWNLOADED_TEXTURES,
	EMAIL,
	END_TIME,
	EVENT_ID,
	FACEBOOK_ID,
	FEATURE_TYPE,
	GAME_KEY,
	HEIGHT,
	ICON,
	INDEX,
	IMAGE_PATH,
	IMAGE_TRANSFORM,
	IS_LOBBY_ONLY_DIALOG,
	IS_PASSIVE,	
	IS_PERSISTENT_TEXTURE,
	IS_PINNED,
	IS_WAITING,
	IS_JACKPOT_ELIGIBLE,
	KEY,
	KEYS_NEED,
	LIST_ITEM,
	MESSAGE,
	MODE,
	MOTD_KEY,
	NEW_LEVEL,
	OG_ARGS,
	OBJECT,
	OPTION,
	OPTION1,
	OPTION2,
	PACKAGE_KEY,
	PACKAGE,
	PAYOUT_CREDITS,
	PAYTABLE_KEY1,
	PAYTABLE_KEY2,
	PLAYER,
	PLAYER1,
	PRIORITY,
	PAYLOAD,
	PTR_NODE_INFO,
	RANK,
	REASON,
	REMAINING_SECS,
	SALE,
	SALE_BONUS_PERCENT,
	SECONDARY_CALLBACK,
	SCORE,
	SCORE2,
	SHOW_CLOSE_BUTTON,
	SHOW_DIALOG,
	SHOW_PET,
	SHOULD_HIDE_LOADING,
	SHOULD_HIDE_BROKEN,
	SHROUD,
	SKIP_OOC_TITLE,
	SKU_KEY,
	STATE,
	IS_TOP_OF_LIST,
	STACK,
	START_TIME,
	TEXTURE,
	THEME,
	TIME,
	TIME_LEFT,
	TITLE,
	TOTAL_CREDITS,
	TRANSFORM,
	TYPE,
	URL,
	UNLOCK_LEVEL,
	VALUE,
	VALUES,
	VIP_BONUS_PERCENT,
	VIP_LEVEL_NAME,
	VIP_POINTS,
	WIDGET,
	WIDTH
}
