
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/*
Represents a single objective in a mission for a challenge.
*/

public class Objective
{
	public enum ConstraintType
	{
		SPINS
	}
	
	// A restriction on the challenge
	public class Constraint
	{
		public ConstraintType type;
		public long amount;
		public long limit;

		public Constraint(ConstraintType constraintType, long currentAmount, long objectiveLimit)
		{
			type = constraintType;
			amount = currentAmount;
			limit = objectiveLimit;
		}
	}
	// =============================
	// PUBLIC GET, PROTECTED SET
	// =============================

	// translated description from the loc key
	protected string _description = "";

	public virtual string description
	{
		get
		{
			return _description;
		}
		protected set
		{
			_description = value;
		}
		
	}

	// game tied to this objective
	public int id { get; private set; }
	public string game { get; protected set; }

	// quantity required to complete the objective
	public long amountNeeded { get; protected set; }

	public virtual long progressBarMax
	{
		get
		{
			return amountNeeded;
		}
	}
	
	public long rank { get; protected set; }

	public virtual List<Constraint> constraints

	{
		get { return null;}
		protected set { }
	}
	
	protected int constraintDisplayIndex = 0;
	
	public List<ChallengeReward> rewards { get; protected set; }

	// min bet required to increase progress through this objective
	public int minWager { get; protected set; }

	// type of objective, eg. "spin", "big win", etc
	public string type { get; protected set; }

	public bool requiresCollectAction { get; protected set; }

	// =============================
	// PUBLIC
	// =============================
	// current progress into the objective
	public long currentAmount;

	// If we tried to show a symbol before we had it
	public bool shouldRetryGettingSymbol = false; 

	// =============================
	// PROTECTED
	// =============================

	// Symbol stays as cached as the actual symbol until we get game data, displaySymbol is what we put up in the meantime
	protected string symbol;
	protected string displaySymbol;

	// =============================
	// CONST
	// =============================
	private const string ID = "id";
	private const string GAMES = "games";
	private const string REWARDS = "rewards";
	private const string COUNT = "count";
	private const string MWAGER = "min_wager";
	private const string COLLECT = "reward_collect";
	private const string TARGET_COUNT = "target_count";
	private const string CURRENT_COUNT = "current_count";
	private const string RANK = "rank";

	// types
	public const string SPIN = "spin";
	public const string BONUS_GAME = "bonus_game";
	public const string LEVEL = "level";
	public const string CREDITS = "credits";
	public const string CREDITS_WON = "credits_won";
	public const string CREDITS_BET = "credits_bet";
	public const string BIG_WIN = "big_win";
	public const string PACKS_COLLECTED = "card_packs";
	public const string CARDS_COLLECTED = "cards";

	public const string LEVEL_X_TIMES = "level_x_times";
	public const string DAILY_BONUS = "daily_bonus";
	public const string JP_QUALIFY_WAGER = "jp_qual_wager";
	public const string WELCOME_JOURNEY = "welcome_journey";
	public const string GIFTED_SPINS = "gifted_spins";
	public const string WIN_X_TIMES = "win_x_times";
	public const string SLOTVENTURE_WIN = "slotventure_win";
	public const string POWERUPS = "powerups";
	public const string FINISH_PPU = "finish_ppu";
	public const string MAX_VOLTAGE_MINI_GAME = "max_voltage_mini_game";
	public const string VIP_TOKENS_COLLECT = "vip_tokens_collect";
	public const string MAX_VOLTAGE_TOKENS_COLLECT = "max_voltage_tokens_collect";
	public const string DAILY_RIVAL_WIN = "daily_rival_win";
	public const string QFC_WIN = "qfc_win";
	public const string PINATA_PURCHASE = "pinata_purchase";
	public const string ELITE_POINTS = "elite_points";
	public const string COLLECTIBLE_SETS = "collectible_sets";
	public const string QFC_KEY_COLLECT = "qfc_key_collect";
	public const string PURCHASE_COINS = "purchase_coins";
	public const string VIP_MINI_GAME = "vip_mini_game";
	public const string TICKET_TUMBLER_TOKENS_COLLECT = "ticket_tumbler_tokens_collect";
	public const string FINISH_TOP_ROYAL_RUSH = "finish_top_3_royal_rush";
	public const string FINISH_TOP_X_WEEKLY_RACE = "finish_top_x_weekly_race";
	public const string LEVEL_UP_X_WITH_Y_COINS = "level_up_x_with_y_coins";
	public const string INVITE_NEW_FRIENDS = "invite_new_friends";

	protected const string DEFAULT_LOCALIZATION_PREFIX = "robust_challenges_desc_V2_";

	public Objective(JSON data)
	{
		if (data != null)
		{
			parse(data);				
		}
	}

	public ConstraintType getConstraintTypeFromString(string text)
	{
		ConstraintType type = ConstraintType.SPINS;
		switch (text)
		{
			case "spin":
			case "spins":
				type = ConstraintType.SPINS;
				break;
				
			default:
				Debug.LogError("Invalid constraint type, using spins as a default: " + text);
				type = ConstraintType.SPINS;
				break;
		}

		return type;
	}

	public virtual void resetProgress(float replayGoalRatio)
	{
		currentAmount = 0;
		amountNeeded = amountNeeded + System.Convert.ToInt64((amountNeeded * replayGoalRatio));

		if (rewards != null)
		{
			for (int i = 0; i < rewards.Count; i++)
			{
				if (rewards[i] == null)
				{
					Debug.LogWarning("Invalid reward");
					continue;
				}
				rewards[i].reset();
			}
		}
	}

	// This is a constructore specifically for PPU, since it gets its data in an odd way.
	public Objective(PartnerPowerupCampaign campaign)
	{
		// Partner powerup isnt setup like a standard campaign so we'll init in a weird way.
		amountNeeded = campaign.challengeGoal;
		currentAmount = campaign.userProgress + CampaignDirector.partner.buddyProgress;
		minWager = 0;
		type = COUNT; // I guess we need a specific count of something so this seems appropriate 
	}

	private void parse(JSON data)
	{
		type = data.getString("definition", string.Empty);
		if (type.IsNullOrWhiteSpace())
		{
			type = data.getString("type", string.Empty);
		}
		init(data);
		buildLocString();
	}

	public virtual void init(JSON data)
	{
		foreach (string key in data.getKeyList())
		{
			parseKey(data, key);
		}
	}

	protected virtual void parseKey(JSON data, string key)
	{
		switch (key)
		{
			case ID:
				id = data.getInt(key, 0);
				break;
			
			case GAMES:
				game = data.getString(key, null);
				break;

			case COUNT:
			case TARGET_COUNT:
				amountNeeded = data.getLong(key, 0);
				break;
			
			case CURRENT_COUNT:
				currentAmount = data.getLong(key, 0);
				break;
			
			case MWAGER:
				minWager = data.getInt(key, 0);
				break;

			case COLLECT:
				requiresCollectAction = data.getInt(key, 0) > 0;
				break;
			
			case REWARDS:
				rewards = parseRewards(data.getBool("is_unlocked", false), data.getJsonArray(key));
				break;
			
			case RANK:
				rank = data.getInt(key, 0);
				break;
				
		}
	}

	public void collectAllRewards(string source)
	{
		if (rewards == null)
		{
			return;
		}

		for (int i = 0; i < rewards.Count; i++)
		{
			rewards[i].collect(source + "_obj_" + rewards[i].type);
		}
	}

	private List<ChallengeReward> parseRewards(bool isUnlocked, JSON[] data)
	{
		List<ChallengeReward> rewardItems = new List<ChallengeReward>();
		for (int i = 0; i < data.Length; i++)
		{
			//for now the only objective that have rewards are rich pass rewards / change this when we have different kinds of rewards
			rewardItems.Add(new PassReward(isUnlocked, data[i]));
		}
		return rewardItems;
	}

	public void buildLocString(string prefix = DEFAULT_LOCALIZATION_PREFIX, bool includeCredits = true)
	{
		_description = getLocString(prefix, includeCredits);
	}

	protected virtual string getLocString(string prefix, bool includeCredits, bool inProgress = false)
	{
		List<object> locItems = new List<object>();
		StringBuilder sb = new StringBuilder();
		sb.Append(prefix + type + "_count_{0}");

		if (minWager > 0)
		{
			sb.Append(Localize.DELIMITER + "min_wager_{1}");
		}

		long remainingAmount = amountNeeded;
		if (inProgress)
		{
			remainingAmount = amountNeeded - currentAmount;
		}

		if (remainingAmount > 1)
		{
			sb.Append(Localize.DELIMITER + "plural");
		}

		if ((type.Contains("bet") || type.Contains("credits")) && includeCredits)
		{

			locItems.Add(CreditsEconomy.convertCredits(remainingAmount));
			locItems.Add(CreditsEconomy.convertCredits(minWager));
		}
		else
		{
			locItems.Add(remainingAmount);
			locItems.Add(CreditsEconomy.convertCredits(minWager));
		}

		locItems.Add(game);

		return Localize.text(sb.ToString(), locItems.ToArray());
	}

	// In case you just need to display simple challenge info
	public string localizedChallengeType()
	{
		switch (type)
		{
			case SPIN:
				return Localize.text("spin");

			case BONUS_GAME:
				return Localize.text("bonus");

			case CREDITS_BET:	
				return Localize.text("bet");
			
			case JP_QUALIFY_WAGER:
				return Localize.text("make");

			case XDoneYTimesObjective.WIN_X_COINS_Y_TIMES:
			case CREDITS_WON:
				return Localize.text("win_2");

			case BIG_WIN:
				return Localize.text("big_wins");

			case LEVEL:
				return Localize.text("levels");

			case CollectObjective.OF_A_KIND:
				return Localize.text("matches");

			case CollectObjective.SYMBOL_COLLECT:
				return Localize.text("symbols_collected");
			
			case XinYObjective.X_COINS_IN_Y:
				return Localize.text("coins");

			default:
				return Localize.text("goal");
		}
	}

	protected virtual bool usePercentageForProgress()
	{
		switch (type)
		{
			case CREDITS_BET:
			case CREDITS_WON:
				return true;

			default:
				return amountNeeded > 99;  //more than 2 characters
		}
	}
	
	public virtual string getProgressText()
	{
		long totalAmount = amountNeeded;
		long amountToDisplay = currentAmount >= totalAmount ? totalAmount : currentAmount;
		if (usePercentageForProgress())
		{
			return string.Format("{0}%", Mathf.Floor(((float)amountToDisplay / (float)totalAmount) * 100));
		}
		return string.Format("{0}/{1}", amountToDisplay, totalAmount);
	}

	public virtual string getCompletedProgressText()
	{
		long totalAmount = amountNeeded;
		if (usePercentageForProgress())
		{
			return Localize.text("{0}%", 100);
		}
		
		return string.Format("{0}/{1}", totalAmount, totalAmount);

	}

	public void formatSymbol()
	{
		if (string.IsNullOrEmpty(symbol) )
		{
			return;
		}

		// These don't have their symbol names sent so set them here
		if (symbol == "SC")
		{
			displaySymbol = Localize.text("scatter");
			return;
		}

		else if (symbol == "BN")
		{
			displaySymbol = Localize.text("bonus");
			return;
		}

		else if (symbol == "WD")
		{
			displaySymbol = Localize.text("wild");
			return;
		}

		if (string.IsNullOrEmpty(game))
		{
			shouldRetryGettingSymbol = true;
			displaySymbol = Localize.text("special");
			return;
		}
		
		SlotGameData gameData = SlotGameData.find(game);
		if (gameData != null && gameData.symbolDisplayInfoList.Count > 0)
		{
			// SC doesn't always appear in the paytable, so we are hard coding the translation

			foreach (SymbolDisplayInfo symbolInfo in gameData.symbolDisplayInfoList)
			{
				if (symbolInfo.keyName == symbol && !string.IsNullOrEmpty(symbolInfo.name))
				{
					shouldRetryGettingSymbol = false;
					symbol = symbolInfo.name;
					displaySymbol = symbol;
					return;
				}
			}
		}
		else
		{
			shouldRetryGettingSymbol = true;
			displaySymbol = Localize.text("special");
		}
	}


	public string getShortChallengeTypeActionHeader()
	{
		switch (type)
		{
			case BIG_WIN:
			case CARDS_COLLECTED:
			case DAILY_BONUS:
				return Localize.text("get");

			default:
				return getChallengeTypeActionHeader();
				
		}
	}

	public bool usesTwoPartLocalization()
	{
		switch (type)
		{
			case SPIN:
			case PURCHASE_COINS:
			case PACKS_COLLECTED:
			case COLLECTIBLE_SETS:
			case PINATA_PURCHASE:
			case ELITE_POINTS:
			case DAILY_RIVAL_WIN:
			case VIP_TOKENS_COLLECT:
			case MAX_VOLTAGE_TOKENS_COLLECT:
			case POWERUPS:
			case VIP_MINI_GAME:
			case MAX_VOLTAGE_MINI_GAME:
			case FINISH_TOP_ROYAL_RUSH:
			case WIN_X_TIMES:
			case FINISH_TOP_X_WEEKLY_RACE:
			case GIFTED_SPINS:
			case TICKET_TUMBLER_TOKENS_COLLECT:
			case FINISH_PPU:
			case INVITE_NEW_FRIENDS:
			case QFC_WIN:
			case SLOTVENTURE_WIN:
			case QFC_KEY_COLLECT:
			case JP_QUALIFY_WAGER:
			case LEVEL_X_TIMES:
			case XDoneYTimesObjective.WIN_X_COINS_Y_TIMES:
				return false;
			
			default:
				return true;
		}
	}
	//Returns the string localization of the play action needed for the Objective
	public string getChallengeTypeActionHeader()
	{
		switch (type)
		{
			case CollectObjective.OF_A_KIND:
			case CREDITS_WON:
			case BONUS_GAME:
			case XinYObjective.X_COINS_IN_Y:
				return Localize.text("win");

			case CREDITS_BET:
				return Localize.text("bet");
			
			case LEVEL_UP_X_WITH_Y_COINS:
				return Localize.text("level up");

			case BIG_WIN:
			case CollectObjective.SYMBOL_COLLECT:
			case CARDS_COLLECTED:
			case DAILY_BONUS:
				return Localize.text("collect");

			case LEVEL:
				return Localize.text("vip_earning_levelup_heading");

			case WELCOME_JOURNEY:
				return Localize.text("claim");

			default:
				return Localize.text("goal");
		}
	}

	public string getDynamicChallengeDescription(bool abbreviateNumber = false)
	{
		return getChallengeTypeActionHeaderDynamic("robust_challenges_header_", abbreviateNumber);
	}

	public string getTinyDynamicChallengeDescription(bool abbreviateNumber = false)
	{
		return getChallengeTypeActionHeaderDynamic("robust_challenges_header_tiny_", abbreviateNumber);
	}

	//"dynamic" just meaning not hard coded. 
	protected virtual string getChallengeTypeActionHeaderDynamic(string prefix, bool abbreviateNumber)
	{
		if (type == CREDITS_WON || type == CREDITS_BET)
		{
			return abbreviateNumber ? CreditsEconomy.multiplyAndFormatNumberAbbreviated(amountNeeded) : CreditsEconomy.convertCredits(amountNeeded);
		}
		else
		{
			string shortLocString = prefix + type + "_count_{0}";
			if (amountNeeded > 1)
			{
				shortLocString += Localize.DELIMITER + "plural";
			}

			switch (type)
			{
				case XDoneYTimesObjective.WIN_X_COINS_Y_TIMES: //Cases where we need localization and amount needed abbreviation at the same time:
					return Localize.text(shortLocString, CreditsEconomy.multiplyAndFormatNumberAbbreviated(amountNeeded));
				
				case FINISH_TOP_X_WEEKLY_RACE:
					return Localize.text(shortLocString, rank);
				
				default: 
					return Localize.text(shortLocString, amountNeeded);
			}
		}
	}


	public virtual void updateConstraintAmounts(List<long> constraintValues)
	{
		
	}

	//Returns the string localization of the play action needed for the Objective
	public virtual string getShortDescriptionLocalization(string prefix = "robust_challenges_desc_short_", bool abbreviateNumber = false)
	{
		if (type == CREDITS_WON || type == CREDITS_BET)
		{
			return abbreviateNumber ? CreditsEconomy.multiplyAndFormatNumberAbbreviated(amountNeeded) : CreditsEconomy.convertCredits(amountNeeded);
		}
		else
		{
			string shortLocString = prefix + type + "_count_{0}";

			if (amountNeeded > 1)
			{
				shortLocString += Localize.DELIMITER + "plural";
			}

			return Localize.text(shortLocString, amountNeeded);
		}
	}

	public virtual bool isComplete
	{
		get
		{
			return currentAmount >= amountNeeded && currentAmount > 0;
		}
	}

	public virtual float progressPercent
	{
		get
		{
			return isComplete ? 1.0f : currentAmount / (float)amountNeeded;
			
		}
	}

	public virtual string getInProgressText(string prefix = DEFAULT_LOCALIZATION_PREFIX, bool includeCredits = true)
	{
		return getLocString(prefix, includeCredits, true);
	}

	public long getRewardAmount(ChallengeReward.RewardType rewardType, bool logWarning = true)
	{
		if (rewards == null)
		{
			return 0;
		}
		for (int i = 0; i < rewards.Count; i++)
		{
			if (rewards[i] == null)
			{
				continue;
			}
			if (rewards[i].type == rewardType)
			{
				return rewards[i].amount;
			}
		}

		if (logWarning)
		{
			Debug.LogWarningFormat("Reward Type {0} not found", rewardType);
		}

		return 0;
	}

	const string WIN_X_TIMES_IMAGE_PATH = "robust_challenges/Generic HIR Win Challenge Card 1X1";
	const string GIFTED_SPINS_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Collect Free Spin Gifts";
	const string POWERUPS_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Collect Powerups";
	const string QFC_KEY_COLLECT_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Collect Quest Keys";
	const string ELITE_POINTS_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Elite Member";
	const string SLOTVENTURE_WIN_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Complete Slotventure";
	const string DAILY_RIVAL_WIN_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Beat Daily Rival";
	const string COLLECTIBLE_SETS_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Collections";
	const string QFC_WIN_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Win Quest for the Chest";
	const string FINISH_PPU_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Finish Partner Powerup Goal";
	const string MAX_VOLTAGE_MINI_GAME_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Max Voltage Jackpot";
	const string PURCHASE_COINS_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Buy Coin Package";
	const string FINISH_TOP_X_WEEKLY_RACE_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Weekly Race Top";
	const string FINISH_TOP_ROYAL_RUSH_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Place Top Three Royal Rush";
	const string PINATA_PURCHASE_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Buy Pinata Coin Package";
	const string VIP_TOKENS_COLLECT_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Collect VIP Room Tokens";
	const string TICKET_TUMBLER_TOKENS_COLLECT_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Win Ticket Tumbler Tickets";
	const string INVITE_NEW_FRIENDS_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Invite Friend";
	const string MAX_VOLTAGE_TOKENS_COLLECT_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Collect Max Voltage Tokens";
	const string LEVEL_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Level Up X Times";
	const string LEVEL_UP_X_WITH_Y_COINS_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - Level Up X Times";
	const string VIP_MINI_GAME_IMAGE_PATH = "robust_challenges/Rich Pass Challenge Image - VIP Jackpot Games";


	public static bool addGameOptionImage(Objective objective,List<string> gameOptionImages)
	{
		bool pathAdded = true;
		switch (objective.type)
		{
			case Objective.WIN_X_TIMES: gameOptionImages.Add(WIN_X_TIMES_IMAGE_PATH); break;
			case Objective.GIFTED_SPINS: gameOptionImages.Add(GIFTED_SPINS_IMAGE_PATH); break;
			case Objective.POWERUPS: gameOptionImages.Add(POWERUPS_IMAGE_PATH); break;
			case Objective.QFC_KEY_COLLECT: gameOptionImages.Add(QFC_KEY_COLLECT_IMAGE_PATH); break;
			case Objective.ELITE_POINTS: gameOptionImages.Add(ELITE_POINTS_IMAGE_PATH); break;
			case Objective.SLOTVENTURE_WIN: gameOptionImages.Add(SLOTVENTURE_WIN_IMAGE_PATH); break;
			case Objective.DAILY_RIVAL_WIN: gameOptionImages.Add(DAILY_RIVAL_WIN_IMAGE_PATH); break;
			case Objective.COLLECTIBLE_SETS: gameOptionImages.Add(COLLECTIBLE_SETS_IMAGE_PATH); break;
			case Objective.QFC_WIN: gameOptionImages.Add(QFC_WIN_IMAGE_PATH); break;
			case Objective.FINISH_PPU: gameOptionImages.Add(FINISH_PPU_IMAGE_PATH); break;
			case Objective.MAX_VOLTAGE_MINI_GAME: gameOptionImages.Add(MAX_VOLTAGE_MINI_GAME_IMAGE_PATH); break;
			case Objective.PURCHASE_COINS: gameOptionImages.Add(PURCHASE_COINS_IMAGE_PATH); break;
			case Objective.FINISH_TOP_X_WEEKLY_RACE: gameOptionImages.Add(FINISH_TOP_X_WEEKLY_RACE_IMAGE_PATH); break;
			case Objective.FINISH_TOP_ROYAL_RUSH: gameOptionImages.Add(FINISH_TOP_ROYAL_RUSH_IMAGE_PATH); break;
			case Objective.PINATA_PURCHASE: gameOptionImages.Add(PINATA_PURCHASE_IMAGE_PATH); break;
			case Objective.VIP_TOKENS_COLLECT: gameOptionImages.Add(VIP_TOKENS_COLLECT_IMAGE_PATH); break;
			case Objective.TICKET_TUMBLER_TOKENS_COLLECT: gameOptionImages.Add(TICKET_TUMBLER_TOKENS_COLLECT_IMAGE_PATH); break;
			case Objective.INVITE_NEW_FRIENDS: gameOptionImages.Add(INVITE_NEW_FRIENDS_IMAGE_PATH); break;
			case Objective.MAX_VOLTAGE_TOKENS_COLLECT: gameOptionImages.Add(MAX_VOLTAGE_TOKENS_COLLECT_IMAGE_PATH); break;
			case Objective.LEVEL: gameOptionImages.Add(LEVEL_IMAGE_PATH); break;
			case Objective.LEVEL_UP_X_WITH_Y_COINS: gameOptionImages.Add(LEVEL_UP_X_WITH_Y_COINS_IMAGE_PATH); break;
			case Objective.VIP_MINI_GAME: gameOptionImages.Add(VIP_MINI_GAME_IMAGE_PATH); break;
			default: pathAdded = false; break;
		}
		return pathAdded;
	}
}
