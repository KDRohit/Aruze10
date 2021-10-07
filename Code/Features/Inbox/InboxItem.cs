using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Data storage class for inbox items for the user
/// </summary>
public class InboxItem
{
	// =============================
	// PUBLIC
	// =============================
	/// <summary>
	/// Raw data associated with this inbox item
	/// </summary>
	public JSON itemData { get; protected set; }

	/// <summary>
	/// User's zid that sent the gift
	/// </summary>
	public string senderZid { get; protected set; }

	/// <summary>
	/// Associated SocialMember instance if a senderZid was applied
	/// </summary>
	public SocialMember senderSocialMember
	{
		get
		{
			if (!string.IsNullOrEmpty(senderZid))
			{
				return CommonSocial.findOrCreate(zid:senderZid, fbid:"");
			}

			return null;
		}
	}

	/// <summary>
	/// Cooldown before this item can be displayed again once it's been viewed
	/// </summary>
	public int cooldown { get; protected set; }

	/// <summary>
	/// Max number of times the user can claim the item
	/// </summary>
	public int maxClaims { get; protected set; }

	/// <summary>
	/// Which inbox tab the item belongs in
	/// </summary>
	public string inboxTab { get; protected set; }

	/// <summary>
	/// Associated event id for server handling
	/// </summary>
	public string eventId { get; protected set; }

	/// <summary>
	/// Call to action button text
	/// </summary>
	public string ctaText { get; protected set; }

	/// <summary>
	/// Date the item expires
	/// </summary>
	public int expiration { get; protected set; }

	/// <summary>
	/// Date/time the user last viewed this item
	/// </summary>
	public long lastViewedTime { get; protected set; }

	/// <summary>
	/// Number of times the user has claimed the item
	/// </summary>
	public int claims { get; protected set; }

	/// <summary>
	/// Possible game key for things like gifted free spins
	/// </summary>
	public string gameKey { get; protected set; }

	/// <summary>
	/// Bonus game id
	/// </summary>
	public string bonusGameId { get; protected set; }

	/// <summary>
	/// String message that appears in the inbox item
	/// </summary>
	public string message { get; protected set; }

	/// <summary>
	/// String message key that's assigned to this type of inbox item (typically only for system messages)
	/// </summary>
	public string messageKey { get; protected set; }

	/// <summary>
	/// Command to use when closing or dismissing the inbox item
	/// </summary>
	public InboxCommand closeCommand { get; protected set; }

	/// <summary>
	/// Command to use for the primary CTA with this inbox item
	/// </summary>
	public InboxCommand primaryCommand { get; protected set; }

	/// <summary>
	/// Command to use for the primary CTA with this inbox item
	/// </summary>
	public Dictionary<string, InboxCommand> primaryCommands { get; protected set; } = new Dictionary<string, InboxCommand>();

	/// <summary>
	/// Set to true once the action for this inbox item has been performed
	/// </summary>
	public bool hasAcceptedItem { get; protected set; }

	/// <summary>
	/// User has requested to remove the item
	/// </summary>
	public bool hasDeclinedItem { get; protected set; }

	/// <summary>
	/// Set to the background url if it should be loaded
	/// </summary>
	public string background { get; protected set; }

	/// <summary>
	/// Item sort order
	/// </summary>
	public int sortOrder { get; protected set; }
	
	/// <summary>
	/// Name of feature that spawned this message
	/// </summary>
	public string feature { get; protected set; }

	/// <summary>
	/// Timer for the inbox item's expiration
	/// </summary>
	public GameTimerRange expirationTimer { get; protected set; }

	/// <summary>
	/// Cooldown timer before item can be displayed again
	/// </summary>
	public GameTimerRange cooldownTimer { get; protected set; }

	/// <summary>
	/// Boolean to show/hide the timer
	/// </summary>
	public bool showTimer { get; protected set; }

	public enum InboxType
	{
		NONE,
		MESSAGE,
		FREE_SPINS,
		FREE_CREDITS,
		SEND_CREDITS
	}

	public const string COINS = "coins";
	public const string SPINS = "spins";
	public const string MESSAGES = "messages";

	// string parameter translations
	public const string CREDITS = "credits";
	private const string TIMER = "timer";
	private const string USER_FIRST_NAME = "user_first_name";
	private const string SENDER_FIRST_NAME = "sender_first_name";

	public InboxItem(JSON data)
	{
		itemData = data;
		senderZid = data.getString("sender_zid", "");
		cooldown = data.getInt("cooldown", 0);
		inboxTab = data.getString("inbox_tab", "messages");
		eventId = data.getString("event_id", "");
		message = data.getString("message", "");
		messageKey = data.getString("message_key", "");
		ctaText = data.getString("cta_text", "");
		expiration = data.getInt("expiration", 0);
		claims = data.getInt("claims", 0);
		maxClaims = data.getInt("max_claims", 0);
		lastViewedTime = data.getLong("last_viewed_time", 0);
		gameKey = data.getString("slots_game_key", "");
		bonusGameId = data.getString("bonus_game_id", "");
		background = data.getString("background", "");
		sortOrder = data.getInt("sort_order", 0);
		showTimer = data.getBool("timer", true);
		feature = data.getString("feature", "");

		DateTime endTime = Common.convertFromUnixTimestampSeconds(expiration);
		DateTime startTime = Common.convertFromUnixTimestampSeconds(GameTimer.currentTime);

		if (expiration > 0)
		{
			if (Application.isPlaying)
			{
				// expiration timer is slightly in front of server to give a small buffer for latency
				expirationTimer = GameTimerRange.createWithTimeRemaining((int)(endTime - startTime).TotalSeconds - Data.liveData.getInt("CLIENT_TIMER_EXPIRATION_BUFFER", 0));
			}
		}

		parseActions(data);

		if (Application.isPlaying)
		{
			translateMessage(data.getStringArray("message_params"));
		}
	}

	/// <summary>
	/// Parses actions to create associated commands for closing, and CTA
	/// </summary>
	/// <param name="data"></param>
	protected void parseActions(JSON data)
	{
		JSON actions = data.getJSON("actions");
		if (actions != null)
		{
			JSON closeAction = actions.getJSON("close");
			JSON primaryAction = actions.getJSON("primary");

			closeCommand = InboxCommandGenerator.generateCommand(closeAction);
			primaryCommand = InboxCommandGenerator.generateCommand(primaryAction);

			// Go through all possible primary options, e.g. primary_0, primary_1, primary_2, etc. in case we have multiple
			// buttons.  inbox rating will be a good exampe that we have "love", "like" and "dislike" buttons
			foreach(string actionKey in actions.getKeyList())
			{
				if (actionKey.Contains("primary_"))
				{
					primaryAction = actions.getJSON(actionKey);
					InboxCommand command = InboxCommandGenerator.generateCommand(primaryAction);
					primaryCommands.Add(actionKey, command);

					// If no primary command yet, make the first one in primary command array the default primary command
					if (primaryCommand == null)
					{
						primaryCommand = command;
					}
				}
			}
		}
	}

	/// <summary>
	/// For local testing purposes only, allows dev panel control to force expiration times
	/// </summary>
	/// <param name="secondsRemaining"></param>
	public void overrideTimerExpiration(int secondsRemaining)
	{
		if (Data.debugMode)
		{
			if (expirationTimer != null)
			{
				expirationTimer.startTimers(1, GameTimer.currentTime + secondsRemaining);
			}
			else
			{
				expirationTimer = GameTimerRange.createWithTimeRemaining(secondsRemaining);
			}
		}
	}

	/// <summary>
	/// Runs the primary inbox command attached to this inbox item.
	///
	/// This function is used for the regular inbox item with just a single primary action
	/// </summary>
	public void action()
	{
		claims++;
		
		hasAcceptedItem = true;

		if (primaryCommand != null)
		{
			primaryCommand.execute(this);
		}

		if (cooldown > 0)
		{
			cooldownTimer = GameTimerRange.createWithTimeRemaining(cooldown);
		}
	}

	/// <summary>
	/// Runs the primary inbox command attached to this inbox item
	///
	/// This function is used for the inbox item with more than one primary actions. e.g. inbox slot machine rating has
	/// primary options "love", "like" and "dislike".
	/// so the caller needs to specify the actionKey to get the correct action involved.
	/// </summary>
	public void action(string actionKey)
	{
		claims++;
		
		hasAcceptedItem = true;
		InboxCommand command = null;

		if (primaryCommands != null && primaryCommands.TryGetValue(actionKey, out command))
		{
			command.execute(this);
		}

		if (cooldown > 0)
		{
			cooldownTimer = GameTimerRange.createWithTimeRemaining(cooldown);
		}
	}
	
	/// <summary>
	/// Runs the close command attached to this inbox item
	/// </summary>
	public void dismiss()
	{
		claims++;

		hasDeclinedItem = true;

		if (closeCommand != null)
		{
			closeCommand.execute(this);
		}

		if (cooldown > 0)
		{
			cooldownTimer = GameTimerRange.createWithTimeRemaining(cooldown);
		}
	}

	/// <summary>
	/// Returns the type of item
	/// </summary>
	public InboxType itemType
	{
		get
		{
			// check for coins/ask for coins
			if (inboxTab == COINS)
			{
				if (primaryCommand is InboxSendCreditsCommand)
				{
					return InboxType.SEND_CREDITS;
				}
				return InboxType.FREE_CREDITS;
			}

			// check for free spins
			if (inboxTab == SPINS)
			{
				return InboxType.FREE_SPINS;
			}

			// check for messages
			if (inboxTab == MESSAGES)
			{
				return InboxType.MESSAGE;
			}

			// default to system messages
			return InboxType.NONE;
		}
	}

	public bool isExpired
	{
		get { return expirationTimer != null && expirationTimer.isExpired; }
	}

	public bool hasMaxClaims
	{
		get { return claims == maxClaims && maxClaims > 0; }
	}

	public bool canBeClaimed
	{
		get { return !isExpired && !hasMaxClaims && (cooldownTimer == null || cooldownTimer.isExpired) && !hasAcceptedItem; }
	}

	/// <summary>
	/// Returns true if there's a primary action associated with the inbox item
	/// </summary>
	public bool hasPrimaryCommand
	{
		get
		{
			return primaryCommand != null && !string.IsNullOrEmpty(primaryCommand.action);
		}
	}

	/// <summary>
	/// Returns true if there's a close action associated with the inbox item
	/// </summary>
	public bool hasCloseCommand
	{
		get
		{
			return closeCommand != null && !string.IsNullOrEmpty(closeCommand.action);
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	/// <summary>
	/// Any inbox items that will be creating credits for the user will be need the conversion with the users VIP bonus
	/// </summary>
	/// <param name="credits"></param>
	/// <returns></returns>
	public static long creditsWithVIPBonus(long credits)
	{
		long finalCredits = credits;

		VIPLevel vipLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel, "coins_from_gifts");
		finalCredits += finalCredits * vipLevel.receiveGiftBonusPct / 100;
		return finalCredits;
	}

	/// <summary>
	/// Translates some parameters send along with the system messages. This will overwrite the current message to the translated version
	/// </summary>
	/// <param name="messageParams"></param>
	private void translateMessage(string[] messageParams)
	{
		List<string> translatedStrings = new List<string>();

		if (messageParams.Length > 0)
		{
			for (int i = 0; i < messageParams.Length; ++i)
			{
				switch (messageParams[i])
				{
					case CREDITS:
						if (primaryCommand is InboxCollectCreditsCommand || primaryCommand is InboxEliteCommand)
						{
							long creditValue = 0L;
							long.TryParse(primaryCommand.args, out creditValue);
							translatedStrings.Add(CreditsEconomy.convertCredits(creditValue));
						}
						else if (primaryCommand != null)
						{
							translatedStrings.Add(primaryCommand.getActionValue(CREDITS));
						}
						break;
					
					case USER_FIRST_NAME:
						translatedStrings.Add(SlotsPlayer.instance.socialMember.firstName);
						break;
					
					case SENDER_FIRST_NAME:
						if (SocialMember.findByZId(senderZid) != null)
						{
							translatedStrings.Add(SocialMember.findByZId(senderZid).firstName);
						}
						break;
					
					case TIMER:
						if (primaryCommand != null)
						{
							GameTimer timer = primaryCommand.timer;
							if (timer != null)
							{
								translatedStrings.Add(timer.timeRemainingFormatted);
							}
							else
							{
								translatedStrings.Add("");
							}
						}
						else
						{
							translatedStrings.Add("");
						}
						break;

				}
			}

			try
			{
				message = string.Format(message, translatedStrings.ToArray());
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to format inbox message: " + System.Environment.NewLine + message + System.Environment.NewLine + e.Message);
			}
		}
	}
}