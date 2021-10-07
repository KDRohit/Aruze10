using System.Collections.Generic;
using TMPro;
using UnityEngine;

/*
Data structure for holding info about progressive jackpots.
*/

public class ProgressiveJackpot : IResetGame
{
	public enum Type
	{
		NONE,
		STANDARD,
		MULTI,
		GIANT,
		REEVALUATOR,
		VIP,
		BUY
	}

	public const float ROLLUP_BUFFER_TIME = 2.0f; // Amount of extra time to roll up after getting a pool amount update from the server.

	public string keyName = "";
	public Type type = Type.NONE;
	public string name = "";
	public string description = "";
	public LobbyGame game = null;
	public long minWagerMultiplier = 0L;  // I don't know why, but all multiplier variables seem to be long, so I do that here to prevent typecasting.
	public long maxJackpot = 0L;
	public bool shouldGrantGameUnlock = false;
	public string[] possibleGameKeys = null;	// LOLA uses this to connect a progressive-enabled game to its progressive. Non-LOLA just just treats all games in this as progressive-enabled.

	private readonly long resetValue = 0L;

	private float lastPoolUpdateTime = 0.0f;
	private float currentRollupValue = 0.0f;
	private float rollupStart = 0.0f;
	private float rollupDifference = 0.0f;

	// A list of labels to update the new pool value whenever it changes.
	// These must be registered using registerLabel() to make it convenient
	// to have a progressive pool automatically updated on any given label.
	private readonly List<LabelWrapper> labels = new List<LabelWrapper>();
	
	// A list of LabelWrapperComponent to update the new pool value whenever it changes.
	// These must be registered using registerLabel() to make it convenient
	// to have a progressive pool automatically updated on any given label.
	private readonly List<LabelWrapperComponent> labelWrapperComponents = new List<LabelWrapperComponent>();

	// Stores the event datas to delay the celebration until the current spin's reels stop.
	// Use a list in the unlikely event that additional win events are received while
	// waiting for the reels to stop spinning.
	public static List<JSON> winEvents = new List<JSON>();

	private static GameTimer updateTimer = null;
	private static float lastRollupUpdateTime = 0.0f; // Don't update rollups each frame, for performance on slower devices.
	private static float rollupDelay = 0.0f;
	private const float NUM_ROLLUP_UPDATES_PER_SECOND = 4;

	public static Dictionary<string, ProgressiveJackpot> all = new Dictionary<string, ProgressiveJackpot>();
	// A subset of "all" that only contains jackpots with at least one registered label.
	private static readonly List<ProgressiveJackpot> jackpotsWithRegisteredLabels = new List<ProgressiveJackpot>();
	private static readonly List<ProgressiveJackpot> jackpotsWithRegisteredLabelWrapperComponents = new List<ProgressiveJackpot>();

	// A simple list of all the progressive games that appear in the MAIN lobby.
	public static List<LobbyGame> allGames = new List<LobbyGame>();

	// A convenient variable to work with the VIP jackpot, which is defined by ZRT.
	public static ProgressiveJackpot vipJackpot = null;

	// Convenient variables to work with the VIP revamp jackpots
	public static ProgressiveJackpot vipRevampMini = null;
	public static ProgressiveJackpot vipRevampMajor = null;
	public static ProgressiveJackpot vipRevampGrand = null;

	// max voltage jackpot
	public static ProgressiveJackpot maxVoltageJackpot = null;

	// A convenient variable to work with the Giant jackpot, which is determined by a hard coded game key.
	public static ProgressiveJackpot giantJackpot = null;

	// A convenient variable to work with the buy credits jackpot, which is determined by a hard coded jackpot key.
	public static ProgressiveJackpot buyCreditsJackpot = null;

	// Whether or not progressive jackpot that can unlock a game exists.
	public static bool doesGameUnlockProgressiveExist = false;

	public static string[] blacklist = null;

	public ProgressiveJackpot(JSON data)
	{
		keyName = data.getString("key_name", "");
		if (all.ContainsKey(keyName))
		{
			Debug.LogError("Ignoring duplicate ProgressiveJackpot definition for: " + keyName);
			return;
		}

		name = data.getString("name", "");
		description = data.getString("description", "");
		shouldGrantGameUnlock = data.getBool("should_grant_game_unlock", false);

		string typeString = data.getString("type_key", "none").ToUpper();

		if (System.Enum.IsDefined(typeof(Type), typeString))
		{
			type = (Type)System.Enum.Parse(typeof(Type), typeString);
		}
		else
		{
			type = Type.NONE;
		}

		switch (type)
		{
			case Type.BUY:
				if (ExperimentWrapper.BuyPageProgressive.isInExperiment)
				{
					if (buyCreditsJackpot == null)
					{
						buyCreditsJackpot = this;
					}
					else
					{
						Debug.LogError("Found more than one Buy Page ProgressiveJackpot. Only one is allowed! " + keyName);
						return;
					}
				}
				break;

			case Type.VIP:
				if (vipJackpot == null)
				{
					vipJackpot = this;
					// We will call setGame() on each VIP game when we get to setting up the VIPLevel data,
					// since we don't yet know which games are VIP games at this point.
					// It might seem silly to call setGame() multiple times on a single ProgressiveJackpot,
					// but it also creates the link from the game to the ProgressiveJackpot,
					// which is what we really need.
				}
				else
				{
					Debug.LogError("Found more than one VIP ProgressiveJackpot. Only one is allowed! " + keyName);
					return;
				}
				break;

			default:	// When using LoLa, the type value should always be NONE.
				if (type == Type.GIANT)
				{
					// Giant jackpot also uses the "games" array, so put this in the default case.
					giantJackpot = this;
				}

				possibleGameKeys = data.getStringArray("games");
				break;
		}

		// VIP Revamp jackpots
		if (keyName.Contains("vip_revamp"))
		{
			switch(keyName)
			{
				case "hir_vip_revamp_grand":
					vipRevampGrand = this;
					break;

				case "hir_vip_revamp_major":
					vipRevampMajor = this;
					break;

				default:
					vipRevampMini = this;
					break;
			}
		}

		if (keyName.Contains("max_voltage"))
		{
			maxVoltageJackpot = this;
		}

		resetValue = data.getLong("reset_value", 0L);
		maxJackpot = data.getLong("max_jackpot", 0L);
		minWagerMultiplier = data.getLong("minimum_wager_multiplier", 0L);

		all.Add(keyName, this);
	}

	public static void populateAll(JSON[] dataArray)
	{
		foreach (JSON data in dataArray)
		{
			string keyName = data.getString("key_name", "");

			bool isBlacklisted = false;
			if (blacklist != null)
			{
				isBlacklisted = (System.Array.IndexOf(blacklist, keyName) > -1);
			}

			if (!isBlacklisted)
			{
				// Game could be null for web-only games, so only create the object for games that exist here.
				// Ignore game null-checking if this progressive is the VIP one, which has no particular game assigned.
				new ProgressiveJackpot(data);
			}
		}
		// Don't sort yet. That is done after all other data has been loaded.

		// Global data is needed before we can process any events.
		// Now that it's been populated, we can register for events which require it in order to be handled.  
		registerEventDelegates();
	}

	// Sets the given game as the game for this progressive jackpot.
	public void setGame(LobbyGame game, bool isVIPGame)
	{
		if (isVIPGame && game.progressiveJackpots != null)
		{
			// This is a VIP game that already has an individually defined progressive pool.
			// This shouldn't happen but could happen if data is badly configured,
			// so we need to handle it. The individual VIP pool overrides the VIP pool here.
			return;
		}
			
		this.game = game;
		if (game.progressiveJackpots == null)
		{
			game.progressiveJackpots = new List<ProgressiveJackpot>();
		}

		if (!game.progressiveJackpots.Contains(this))
		{
			game.progressiveJackpots.Add(this);
			game.progressiveJackpots.Sort(sortByMaxJackpot);
		}

		if (!allGames.Contains(game))
		{
			allGames.Add(game);
		}
	}

	// Called after populating the SlotResourceMap, remove any games that aren't defined in SlotResourceMap.
	// Also remove games that aren't enabled by LoLa, or if it's a VIP game.
	public static void removeUnknownGames()
	{
		for (int i = 0; i < allGames.Count; i++)
		{
			LobbyGame game = allGames[i];

			if (SlotResourceMap.getData(game.keyName) == null ||
				!game.isEnabledForLobby ||
				game.vipLevel != null)
			{
				// Only remove it from the list of progressive games for the main lobby.
				// Don't remove the ProgressiveJackpot data from the games,
				// just in case the game needs to appear in the VIP lobby - it still needs its data.
				allGames.RemoveAt(i);
				i--;
			}
		}

		// Check if there are any progressive games left that we know of, and then update the select game unlock list.
		if (allGames.Count == 1)
		{
			doesGameUnlockProgressiveExist = (allGames[0] != LobbyGame.vipEarlyAccessGame);
		}
		else
		{
			doesGameUnlockProgressiveExist = (allGames.Count > 1);
		}
	}

	// Used by the Sort() method to sort the data by maxJackpot.
	public static int sortByMaxJackpot(ProgressiveJackpot a, ProgressiveJackpot b)
	{
		int maxJackpotCompare = a.maxJackpot.CompareTo(b.maxJackpot);
		if (maxJackpotCompare != 0)
		{
			return maxJackpotCompare;
		}
		else
		{
			// As a tie breaker for ProgessiveJackpots that have the 
			// same maxJackpot (might occur in built in progressive games
			// with tiers), compare using the reset_value
			return a.resetValue.CompareTo(b.resetValue);
		}
	}

	// Standard find method.
	public static ProgressiveJackpot find(string keyName)
	{
		return all.ContainsKey(keyName) ? all[keyName] : null;
	}

	public void reset()
	{
		pool = (long)(resetValue * SlotsPlayer.instance.currentPjpAmountInflationFactor);
	}

	// Client should only log this once per app run.
	// Otherwise we might log the error very 20 seconds (PROGRESSIVE_JACKPOT_UPDATE = 20).  This will quickly
	// eat up all the bugsnag bandwith we have
	private static bool poolInvalidSetValueLogged = false;
	public long pool
	{
		get { return _pool; }

		// The value comes from events, not global data.
		set
		{
			lastPoolUpdateTime = GameTimer.SSSS;

			if (value < resetValue)
			{
				if (!poolInvalidSetValueLogged)
				{
					poolInvalidSetValueLogged = true;
					Debug.LogError($"ProgressiveJackpot.pool set value={value} is invalid for pool keyName={keyName}. It can not be less than resetValue={resetValue} ");
				}

				value = resetValue;
			}
			
			if (value < _pool || _pool == 0)
			{
				// If the pool was reset, reset the current rollup value to the new amount.
				// ...or...
				// If the previous value was 0, then this is the first time this is being set during a session.
				currentRollupValue = (long)(0.95f * value);
			}

			rollupStart = currentRollupValue;
			rollupDifference = (float)value - currentRollupValue;
			_pool = value;
		}
	}

	private long _pool = 0L;

	// Updates all the rollups for all jackpots.
	// This is called once per frame from the Overlay object.
	public static void update()
	{
		bool shouldGetInfo = false;

		if (updateTimer == null)
		{
			// First update, create the timer.
			updateTimer = new GameTimer(Glb.PROGRESSIVE_JACKPOT_UPDATE);
			shouldGetInfo = true; // Force getting the info now even though the new timer isn't yet expired.

			rollupDelay = 1 / NUM_ROLLUP_UPDATES_PER_SECOND;
		}

		if (shouldGetInfo || updateTimer.isExpired)
		{
			getUpdatedPoolInfo();
			updateTimer.startTimer(Glb.PROGRESSIVE_JACKPOT_UPDATE);
		}

		if (Time.realtimeSinceStartup - lastRollupUpdateTime > rollupDelay)
		{
			for (int i = 0; i < jackpotsWithRegisteredLabels.Count; i++)
			{
				jackpotsWithRegisteredLabels[i].updateRollup();
			}

			for (int i = 0; i < jackpotsWithRegisteredLabelWrapperComponents.Count; i++)
			{
				jackpotsWithRegisteredLabelWrapperComponents[i].updateRollup();
			}
			
			lastRollupUpdateTime = Time.realtimeSinceStartup;
		}
	}

	private static bool isLabelWrapperNull(LabelWrapper label) 
	{
		return (label == null || !label.hasLabelReference);
	}

	private static bool isLabelWrapperComponentNull(LabelWrapperComponent label)
	{
		return (label == null || !label.hasLabelReference());
	}

	// Updates the rollup of a particular ProgressiveJackpot.
	// Rolls up from the current value to the latest known server value.
	private void updateRollup()
	{
		// do not update game labels if the game is not in the viewport
		if (game != null)
		{
			if (MainLobby.hirV3 != null && !MainLobby.hirV3.isGameInView(game) || MainLobby.hirV3 == null && GameState.game != game)
			{
				return;
			}
		}

		float elapsed = GameTimer.SSSS - lastPoolUpdateTime;
		float totalRollupTime = Glb.PROGRESSIVE_JACKPOT_UPDATE + ROLLUP_BUFFER_TIME;

		currentRollupValue = rollupStart + rollupDifference * Mathf.Clamp01(elapsed / totalRollupTime);
		// display value needs to be converted using the interpolation as well in order to hide
		// the economy multiplier, if you don't do this the display value will not look like
		// all digits are rolling up (very obvious when the economy multiplier is 100x because
		// the last two digits of the display value would always be two zeros)
		long displayValue = (long)(CreditsEconomy.multipliedCredits((long)rollupStart) + CreditsEconomy.multipliedCredits((long)rollupDifference) * Mathf.Clamp01(elapsed / totalRollupTime));

		/////////////////////////////////////////////////////////////
		// Update all the registered labels;

		bool doRemoveNullLabels = false;
		bool doRemoveNullLabelWrapperComponents = false;

		// Update all the registered labels.
		for (int i = 0; i < labels.Count; i++)
		{
			LabelWrapper label = labels[i];
			if (label == null || !label.hasLabelReference)
			{
				// Label was destroyed, so remove it from the registered list and do nothing else.
				doRemoveNullLabels = true;
			}
			else
			{
				// Note, as seen above, this value already includes the CreditsEconomy multiplier
				// so we don't need to apply it here
				label.text = CommonText.formatNumber(displayValue);
			}
		}
		
		// Update all the registered label wrapper components.
		for (int i = 0; i < labelWrapperComponents.Count; i++)
		{
			LabelWrapperComponent labelWrapperComponent = labelWrapperComponents[i];
			if (labelWrapperComponent == null || !labelWrapperComponent.hasLabelReference())
			{
				// Label was destroyed, so remove it from the registered list and do nothing else.
				doRemoveNullLabelWrapperComponents = true;
			}
			else
			{
				// Note, as seen above, this value already includes the CreditsEconomy multiplier
				// so we don't need to apply it here
				labelWrapperComponent.text = CommonText.formatNumber(displayValue);
			}
		}

		// Remove the registered labels that no longer exist.
		if (doRemoveNullLabelWrapperComponents)
		{
			labelWrapperComponents.RemoveAll(isLabelWrapperComponentNull);

			if (labelWrapperComponents.Count == 0 )
			{
				jackpotsWithRegisteredLabelWrapperComponents.Remove(this);
			}
		}
		
		// Remove the registered labels that no longer exist.
		if (doRemoveNullLabels)
		{
			labels.RemoveAll(isLabelWrapperNull);

			if (labels.Count == 0)
			{
				jackpotsWithRegisteredLabels.Remove(this);
			}
		}
	}

	public void registerLabel(LabelWrapperComponent labelWrapperComponent)
	{
		// When registering a label, make sure it's not still registered with another progressive jackpot.
		ProgressiveJackpot jp = null;
		for (int i = jackpotsWithRegisteredLabelWrapperComponents.Count - 1; i > -1; i--)
		{
			jp = jackpotsWithRegisteredLabelWrapperComponents[i];

			// Make sure we remove the label from other progressives if the wrapper is around the same label.
			for (int j = jp.labelWrapperComponents.Count - 1; j > -1; j--)
			{
				if (labelWrapperComponent.labelWrapper.matchesLabel(jp.labelWrapperComponents[j].labelWrapper))
				{
					jp.unregisterLabel(jp.labelWrapperComponents[j]);
				}
			}

			// Always remove the wrapper if it is found.
			jp.unregisterLabel(labelWrapperComponent);
		}

		labelWrapperComponents.Add(labelWrapperComponent);

		if (!jackpotsWithRegisteredLabelWrapperComponents.Contains(this))
		{
			jackpotsWithRegisteredLabelWrapperComponents.Add(this);
		}

		pool = pool;  // Force the label to be updated immediately.
	}

	// Register a LabelWrapper for automatic updates whenever the pool value changes.
	public void registerLabel(LabelWrapper label)
	{
		// When registering a label, make sure it's not still registered with another progressive jackpot.
		ProgressiveJackpot jp = null;
		for (int i = jackpotsWithRegisteredLabels.Count - 1; i > -1; i--)
		{
			jp = jackpotsWithRegisteredLabels[i];

			// Make sure we remove the label from other progressives if the wrapper is around the same label.
			for (int j = jp.labels.Count - 1; j > -1; j--)
			{
				if (label.matchesLabel(jp.labels[j]))
				{
					jp.unregisterLabel(jp.labels[j]);
				}
			}

			// Always remove the wrapper if it is found.
			jp.unregisterLabel(label);
		}

		labels.Add(label);

		if (!jackpotsWithRegisteredLabels.Contains(this))
		{
			jackpotsWithRegisteredLabels.Add(this);
		}

		pool = pool;  // Force the label to be updated immediately.
	}

	// Register a UILabel for automatic updates whenever the pool value changes.
	public void registerLabel(UILabel label)
	{
		registerLabel(new LabelWrapper(label));
	}

	// Register a TextMeshPro component for automatic updates whenever the pool value changes.
	public void registerLabel(TextMeshPro label)
	{
		registerLabel(new LabelWrapper(label));
	}
	
	public void unregisterLabel(LabelWrapperComponent label)
	{
		if (labelWrapperComponents.Count == 0)
		{
			// Don't do anything if this jackpot has no labels to remove anyway.
			return;
		}

		labelWrapperComponents.Remove(label);

		if (labelWrapperComponents.Count == 0)
		{
			jackpotsWithRegisteredLabelWrapperComponents.Remove(this);
		}
	}

	public void unregisterLabel(LabelWrapper label)
	{
		if (labels.Count == 0)
		{
			// Don't do anything if this jackpot has no labels to remove anyway.
			return;
		}

		labels.Remove(label);

		if (labels.Count == 0)
		{
			jackpotsWithRegisteredLabels.Remove(this);
		}
	}

	// Asks the server for updated pool info.
	public static void getUpdatedPoolInfo()
	{
		Server.registerEventDelegate("progressive_jackpot_info", processJackpotInfo);
		ProgressiveJackpotAction.getInfo();

		// string test = "{\"pool_data\": [" +
		// 	"{" +
		// 	"\"key\": \"pjp_pool_1\"," +
		// 	"\"total\": " + Random.Range(100000, 2000000) +
		// 	"}," +
		// 	"{" +
		// 	"\"key\": \"pjp_pool_2\"," +
		// 	"\"total\": " + Random.Range(100000, 2000000) +
		// 	"}," +
		// 	"{" +
		// 	"\"key\": \"pjp_pool_3\"," +
		// 	"\"total\": " + Random.Range(100000, 2000000) +
		// 	"}," +
		// 	"{" +
		// 	"\"key\": \"pjp_pool_4\"," +
		// 	"\"total\": " + Random.Range(100000, 2000000) +
		// 	"}" +
		// 	"]}";
		// processJackpotInfo(new JSON(test));
	}

	// Register some persistent event delegates, since they could come in at any time.
	public static void registerEventDelegates()
	{
		Server.registerEventDelegate("progressive_jackpot_taken", processJackpotTaken, true);
		Server.registerEventDelegate("progressive_jackpot_won", processJackpotWon, true);
	}

	// Process server event.
	private static void processJackpotInfo(JSON data)
	{
		foreach (JSON json in data.getJsonArray("pool_data"))
		{
			string key = json.getString("key", "");
			ProgressiveJackpot pj = find(key);
			if (pj != null)
			{
				pj.pool = json.getLong("total", 0L);
			}
			// The object may be null if not found due to being inactive.
			// We don't bother creating the object if the pool is inactive,
			// but the server still sends info updates for inactive pools too.
		}
	}

	private static SocialMember getMemberFromJSON(JSON data)
	{
		string fbId = data.getString("fbid", "");
		string zid = data.getString("zid", "");
		string firstName = data.getString("first_name", "");
		string lastName = data.getString("last_name", "");
		long achievementScore = data.getLong("achievement_score", -1);

		if (!string.IsNullOrEmpty(fbId))
		{
			SocialMember member = CommonSocial.findOrCreate(
				fbid: fbId,
				zid: zid,
				firstName: firstName,
				lastName: lastName,
				achievementScore: achievementScore);

			if (member == null)
			{
				Debug.LogErrorFormat("ProgressiveJackpot.cs -- getMemberFromJSON -- failed to create a member from fbid: {0}. Returning null.", fbId);
				return null;
			}
			return member;
		}
		else
		{
			return null;
		}
	}

	// Process server event.
	public static void processJackpotTaken(JSON jackpotData)
	{
		if (jackpotData == null)
		{
			Debug.LogError("ProgressiveJackpot.cs -- processJackpotTaken -- jackpot data was null!");
			return;
		}

		string jackpotKey =  jackpotData.getString("jackpot_key", "");
		if (string.IsNullOrEmpty(jackpotKey))
		{
			Debug.LogError("ProgressiveJackpot.cs -- Invalid jackpot key");
			return;
		}

		ProgressiveJackpot jp = find(jackpotKey);
		SocialMember member = getMemberFromJSON(jackpotData);

		if (jp == null)
		{
			Debug.LogError("ProgressiveJackpot.cs -- processJackpotTaken -- jackpot was null! jackpot_key=" + jackpotKey);
			return;
		}

		if (jp == buyCreditsJackpot)
		{
			Dict args = Dict.create(D.CUSTOM_INPUT, jackpotData, D.TOTAL_CREDITS, jackpotData.getLong("credits", 0L));

			if (ToasterManager.isPlayerAlertsOn)
			{
				if (ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive) 
				{
					//If this timer is active then we're using the new Jackpot Days version of the feature
					ToasterManager.getPlayerDataAndAddToaster(member, ToasterType.JACKPOT_DAYS, args);
				}
				else
				{
					ToasterManager.getPlayerDataAndAddToaster(member, ToasterType.BUY_COIN_PROGRESSIVE, args);
				}
			}
		}
		else if ((jp.game != null && LobbyOption.activeGameOption(jp.game.keyName) != null) ||
			(jp.keyName != null && jp.keyName.Contains("hir_vip_revamp")) ||
			(jp.keyName != null && jp.keyName.Contains("hir_max_voltage")))
		{
			// Only show a toaster if the game is found on mobile.
			if (jp != vipRevampMajor && 
			    jp != vipRevampMini &&
			    (jp == vipRevampGrand || 
					jp == maxVoltageJackpot || 
					(jp.game != null && 
						(!jp.game.isMultiProgressive || 
						(jp.game.progressiveJackpots != null && jp.game.progressiveJackpots.Count >= 3)))))
			{
				// If the jackpot is from a multiprogressive, then only show
				// the grand notification (the highest) as a toaster.
				// If not multiprogressive, then always show the toaster.
				Dict args = Dict.create(
					D.CUSTOM_INPUT, jackpotData,
					D.CALLBACK, new ProtoToaster.ValidationDelegate(isValidToShow));

				if (jackpotData.hasKey("unlock_level"))
				{
					args.Add(D.UNLOCK_LEVEL, jackpotData.getInt("unlock_level", 10));
				}

				if (isValidToShow(args))
				{
					ToasterType type = ToasterType.PROGRESSIVE_NOTIF;

					// vip revamp grand progressive
					if (jp == vipRevampGrand)
					{
						type = ToasterType.VIP_REVAMP_PROGRESSIVE;
					}
					else if (jp.game != null && jp.game.isMultiProgressive)
					{
						type = ToasterType.MEGA_JACKPOT;
					}
					// max voltage linked jackpot
					else if (jp == maxVoltageJackpot)
					{
						type = ToasterType.MAX_VOLTAGE;
						MaxVoltageLobbyHIR.recentWinnerData = jackpotData;

						if (MaxVoltageLobbyHIR.instance != null)
						{
							MaxVoltageLobbyHIR.instance.setRecentWinner();
						}
					}
					// giant jackpot or normal progressive
					else if (jp.game != null && jp.game.isGiantProgressive)
					{
						type = ToasterType.GIANT_PROGRESSIVE;
					}

					if (member == null)
					{
						// If this is a null member, then its anonymous and we dont need to get any data.
						ToasterManager.addToaster(type, args);	
					}
					else
					{
						ToasterManager.getPlayerDataAndAddToaster(member, type, args);
					}
				}
			}
			jp.reset();
		}
	}

	// Process server event.
	private static void processJackpotWon(JSON jackpotData)
	{
		ProgressiveJackpot jp = ProgressiveJackpot.find(jackpotData.getString("jackpot_key", ""));

		if (jp == null)
		{
			return;
		}
		
		if (jp == buyCreditsJackpot)
		{
			if (ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive)
			{
				JackpotDaysWinDialog.showDialog(jackpotData.getLong("credits", 0L));
			}
			else
			{
				BuyCreditsProgressiveWinDialog.showDialog(jackpotData.getLong("credits", 0L));
			}
		}
		else if (jp == vipRevampMini)
		{
			//Don't pop a dalog here since we're going to play the mini game which handles show the player if they won or not.
			VIPTokenCollectionModule.miniJpValue = jackpotData.getLong("credits", 0L);
		}
		else if (jp == vipRevampMajor)
		{
			//Don't pop a dalog here since we're going to play the mini game which handles show the player if they won or not.
			VIPTokenCollectionModule.majorJpValue = jackpotData.getLong("credits", 0L);
		}
		else if (jp == vipRevampGrand)
		{
			//Don't pop a dalog here since we're going to play the mini game which handles show the player if they won or not.
			VIPTokenCollectionModule.grandJpValue = jackpotData.getLong("credits", 0L);
		}
		else if (jp == maxVoltageJackpot)
		{
			MaxVoltageTokenCollectionModule.jackpotValue = jackpotData.getLong("credits", 0L);
		}
		else if (SlotBaseGame.instance != null && SlotBaseGame.instance.engine != null && !SlotBaseGame.instance.engine.isStopped)
		{
			// Check if this game is using a built in progressive, and if so
			// we need to ignore the win event since the game will award that itself
			if (jp.type != Type.REEVALUATOR)
			{
				// The reels are spinning, so queue up the jackpot to be shown when they stop.
				winEvents.Add(jackpotData);
			}
		}
		else
		{
			// Show it immediately if not in a game or spinning.
			showWin(jackpotData);
		}
	}

	// Show the winning animation and stuff.
	public static void showWin(JSON jackpotData)
	{
		winEvents.Remove(jackpotData);

		ProgressiveJackpotDialog.showDialog(jackpotData);
	}

	// Is a particular jackpot toaster valid to be shown?
	private static bool isValidToShow(Dict args)
	{
		// Check the toaster type against user preference settings
		if (args == null)
		{
			Debug.LogError("ProgressiveJackpot.isValidToShow() expects args but null was passed in.");
			return false;
		}

		JSON data = args.getWithDefault(D.CUSTOM_INPUT, null) as JSON;

		if (data == null)
		{
			Debug.LogError("ProgressiveJackpot.isValidToShow() args expects D.CUSTOM_INPUT but none were found.");
			return false;
		}

		if (!ToasterManager.isPlayerAlertsOn && GameState.game != null)
		{
			// If the alerts option is off, then we only show progressive toasters
			// if the player is in the game that the toaster is for.
			string jpKey = data.getString("jackpot_key", "");
			ProgressiveJackpot jackpot = ProgressiveJackpot.find(jpKey);

			if (jackpot == null)
			{
				// This should never happen.
				return false;
			}

			if (jackpot == vipJackpot)
			{
				// Slightly different logic for the VIP jackpot since there are multiple games for it.
				// Check the current game's jackpot to see if it matches the current jackpot.
				// We can check only index 0 because we know the VIP jackpot isn't a multiprogressive.
				return (GameState.game.isProgressive && GameState.game.progressiveJackpots[0] == jackpot);
			}

			if (jackpot.game == null || jackpot.game != GameState.game)
			{
				// We're not in the game for the jackpot, so don't show it.
				return false;
			}

			return true;
		}

		return ToasterManager.isPlayerAlertsOn;
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		updateTimer = null;
		all = new Dictionary<string, ProgressiveJackpot>();
		allGames = new List<LobbyGame>();
		winEvents = new List<JSON>();
		vipJackpot = null;
		giantJackpot = null;
		buyCreditsJackpot = null;
		blacklist = null;
	}
}
