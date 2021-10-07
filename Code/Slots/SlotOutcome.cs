using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Google.Apis.Json;

/// SlotOutcome - encapsulates the json object that the server returns when playing a game.  For cases where a specific game
/// needs specialized processing, feel free to extend this class and do your own version of getSubOutcomes that feeds each JSON
/// instance in "outcomes" to a customized class.  For example, a spin game may have fields "round_<N>_stop_id" which you could
/// do a JSON query for directly using getJsonObject, or create a BonusSpinOutcome class that inherits from SlotOutcome and
/// provides a getter for such fields.
public class SlotOutcome
{
	public const string FIELD_TYPE = "type";
	private const string FIELD_OUTCOME_TYPE = "outcome_type";
	private const string FIELD_OUTCOMES = "outcomes";
	private const string FIELD_BONUS_FLAG = "bonus";
	private const string FIELD_BONUS_GAME_CHOICE = "bonus_game_choice";
	private const string FIELD_REEL_STOPS = "reel_stops";
	private const string FIELD_REEL_SET = "reel_set";
	private const string FIELD_FOREGROUND_REEL_SET = "foreground_reel_set";
	private const string FIELD_REEL_STRIPS = "reel_strips";
	private const string FIELD_FOREGROUND_REEL_STRIPS = "foreground_reel_strips";
	private const string FIELD_ANTICIPATION_INFO = "anticipation_info";
	private const string FIELD_ANTICIPATION_INFO_SYMBOLS = "symbols";
	private const string FIELD_REELS_LANDED = "reels_landed";
	private const string FIELD_TRIGGERS = "triggers";
	private const string FIELD_WIN_ID = "win_id";
	private const string FIELD_SYMBOL = "symbol";
	private const string FIELD_PAY_LINE = "pay_line";
	private const string FIELD_BONUS_GAME = "bonus_game";
	private const string FIELD_BONUS_GAME_PAY_TABLE = "bonus_game_pay_table";
	private const string FIELD_BONUS_GAME_PAY_TABLE_SET_ID = "pay_table_set_id";
	private const string FIELD_MULTIPLIER = "multiplier";
	private const string FIELD_BONUS_MULTIPLIER = "bonus_multiplier";
	private const string FIELD_WAGER = "wager_amount";
	private const string FIELD_FREESPINS = "free_spins";
	private const string FIELD_CREDITS = "credits";
	private const string FIELD_OVERRIDE_CREDITS = "override_credits";
	private const string FIELD_JACKPOT_KEY = "jackpot_key";
	private const string FIELD_PICKS = "picks";
	private const string FIELD_MODIFIERS = "modifiers";
	private const string FIELD_EVENT_ID = "eventID";
	private const string FIELD_BONUS_POOLS = "bonus_pools";
	private const string FIELD_TUMBLE_OUTCOMES = "tumble_outcomes";
	private const string FIELD_REEVALUATIONS = "reevaluations";
	private const string FIELD_MUTATIONS = "mutations";
	private const string FIELD_USES_WILD = "uses_wild";
	private const string FIELD_FROM_RIGHT = "from_right";
	private const string FIELD_NEW_PAY_TABLE = "new_pay_table";
	private const string FIELD_PAY_TABLE = "pay_table";
	private const string FIELD_ROUND_1_STOP_ID = "round_1_stop_id";
	private const string FIELD_ROUND_2_STOP_ID = "round_2_stop_id";
	private const string FIELD_PARAMETER = "parameter";
	private const string FIELD_NUMBER_OF_FREESPINS_OVERRIDE = "number_of_freespins_override";
	private const string FIELD_LANDED_REELS = "landed_reels";
	private const string FIELD_LINKED_REELS = "linked_reels";
	private const string FIELD_PAYLINE_SET = "pay_line_set";
	private const string FIELD_STICKY_SYMBOLS = "new_stickies";
	private const string FIELD_SC_STICKY_SYMBOLS = "scatter_stickies";
	private const string PROPERTY_STICKY_SYMBOLS_COLUMN = "reel";
	private const string PROPERTY_STICKY_SYMBOLS_ROW = "position";
	private const string PROPERTY_STICK_SYMBOLS_NAME = "to_symbol";
	public const string FIELD_REPLACEMENT_SYMBOLS = "replace_symbols";
	private const string FIELD_REEVALUATED_MATRIX = "reevaluated_matrix";
	private const string FIELD_REEVALUATED_REEL_STOPS = "reevaluated_stops";
	private const string FIELD_FOREGROUND_REEL_STOPS = "foreground_reel_stops";
	private const string FIELD_REEVALUATED_FOREGROUND_REEL_STOPS = "reevaluated_foreground_reel_stops";
	private const string FIELD_FREESPIN_INITIAL_REELSET = "freespin_initial_reel_sets";
	private const string FIELD_STATIC_REELS = "static_reels";
	private const string FIELD_DEBUG_SYMBOL_MATRIX = "symbol_matrix";
	private const string FIELD_DEBUG_MUTATED_SYMBOL_MATRIX = "mutated_symbol_matrix";
	public const string FIELD_REVEALS = "reveals";
	private const string FIELD_REEL_INFO = "reel_info";			// The reel info passed down in the freespins gifting for layered games.
	private const string FIELD_MYSTERY_GIFTS = "mystery_gifts";
	private const string FIELD_FEATURE_SYMBOL = "feature_symbol";	// Introduced for the extra reel in pb01 Princess Bride
	private const string FIELD_MEGA_REELS = "mega_reels";	// Introduced for the overlay reel for hot01.
	private const string FIELD_GAMES = "games";
	private const string FIELD_BOARD = "board";	// Used for crossword style games (e.g. zynga04)
	private const string FIELD_WORDS = "words";	// Used for crossword style games (e.g. zynga04)
	private const string FIELD_WORD = "word";	// Used for crossword style games (e.g. zynga04)
	private const string FIELD_MULTI_GAME_ANTICIPATION = "multi_game_anticipation_info";
	public const string FIELD_BET_MULTIPLIER = "bet_multiplier";	// Used to override all multipliers in bonus games, see CumulativeBonusModule.cs and zynga04 for use examples
	private const string FIELD_ACTIVE_REEL_START_INDEX = "active_reel_start_idx";	// Used for games where a subset of the reels are evaluated (e.g. sinatra01)
	private const string FIELD_AWARD_MULTIPLIER = "award_multiplier"; // Used by munsters01 to store a multiplier that is applied as part of a triggered feature
	private const string FIELD_EXTRAS = "extras"; // Used to store non-standard outcome information, such as progressive jackpot win info for a built in feature like elvis03
	private const string FIELD_PROGRESSIVE_JACKPOT_WON = "progressive_jackpot_won"; // Used to store info about a won progressive jackpot from a built in slot game feature like is present in elvis03
	private const string FIELD_PERSONAL_JACKPOT_OUTCOME = "personal_jackpot_outcome"; // Used to get personal jackpot object from an outcome
	private const string FIELD_PERSONAL_JACKPOT_OUTCOME_LIST = "personal_jackpot_outcome_list"; // Used to get personal jackpot object from an outcome
	private const string FIELD_PROGRESSIVE_JACKPOT_OUTCOME = "progressive_jackpot_outcome"; // Used to get progressive jackpot object from an outcome
	private const string FIELD_RUNNING_TOTAL = "running_total"; // Name of the calculated credits in a progressive_jackpot_outcome
	private const string FIELD_REEL_STRIP_REPLACEMENTS = "reel_strip_replacements"; // Used to represent any kind of reel strip replacement for any game type (can be used to replace "reel_strips" and "foreground_reel_strips"
	private const string FIELD_OVERRIDE_SYMBOLS = "override_symbols"; // Used to represent individual symbols which need to be overridden on a specific reel.  See SlotReel.symbolOverrides for how these are used.

	// These entries can be used to test against FIELD_OUTCOME_TYPE's value for client behavior selection.
	public const string OUTCOME_TYPE_REEL_SET = "reel_set";
	public const string OUTCOME_TYPE_WHEEL = "wheel";
	public const string OUTCOME_TYPE_LINE_WIN = "line";
	public const string OUTCOME_TYPE_SCATTER_WIN = "scatter";
	public const string OUTCOME_TYPE_CLUSTER_WIN = "cluster";
	public const string OUTCOME_TYPE_SYMBOL_COUNT = "symbol_count";
	public const string OUTCOME_TYPE_BONUS_SYMBOL = "bonus_symbol";
	public const string OUTCOME_TYPE_BONUS_GAME = "bonus_game";
	public const string OUTCOME_TYPE_PICKEM = "pickem";
	public const string OUTCOME_TYPE_THRESHOLD_LADDER = "threshold_ladder";
	public const string OUTCOME_TYPE_SYMBOL_CREDITS = "symbol_credits";

	public const string REEVALUATION_TYPE_SYMBOL_SHUFFLE = "symbol_shuffle";
	public const string REEVALUATION_TYPE_SPOTLIGHT = "spotlight_reel_effect";
	public const string REEVALUATION_TYPE_BONUS_SYMBOL_ACCUMULATION_MULTI = "bonus_symbol_accumulation_multi";
	public const string FIELD_CHECKPOINT_REEL_SET = "checkpoint_reel_set";
	public const string FIELD_RESPIN_REEL_SET = "respin_reel_set";

	public enum OutcomeTypeEnum
	{
		UNDEFINED = -1,
		LINE_WIN = 0,
		CLUSTER_WIN,
		SCATTER_WIN,
		BONUS_GAME,
		BONUS_SYMBOL,
		SYMBOL_COUNT,
		WHEEL,
		PICKEM,
		REEL_SET,
		THRESHOLD_LADDER,
		SYMBOL_CREDITS
	};

	protected JSON outcomeJson;
	protected SlotOutcome parentOutcome = null;	// Parent outcome JSON, stored if needed

	public bool isGifting;
	public bool isChallenge;
	public bool isCredit;
	public bool isPortal;
	public bool isScatter;		
	public int layer = -1; // which layer is this outcome on (for multi-slot games like gwtw01)

	public long winAmount = 0; // Only used if we pull out a win amount from a portal, and don't enter a game.

	public bool hasAwardedAdditionalSpins = false; // Track if this outcome has awarded additional spins, and if so it will not add anymore, prevents looping outcomes from awarding over and over

	private Queue<SlotOutcome> multipleBonusGameQueue = new Queue<SlotOutcome>(); // tracks when an outcome has multiple bonus games to trigger, these will be handled when processAdditionalBonuses() is called
	private OutcomeTypeEnum outcomeType = OutcomeTypeEnum.UNDEFINED; // Cache this out once it is grabbed using getOutcomeType(), does away with needless string compares for the string type names
	private List<SlotOutcome> cachedSubOutcomes = null; // Cache out the suboutcomes they shouldn't be able to change after being grabbed so we don't need to rebuild this list over and over
	private ReadOnlyCollection<SlotOutcome> cachedSubOutcomesReadOnly; // Cached out version of cachedSubOutcomes that can't be modified and is safe to return to others outside this class
	private ReadOnlyCollection<ReelStripReplacementData> cachedReelStripReplacementDataReadOnly; // Cached out version of the reel strip replacement data that can't be modified, since this should only be used as read only and not modified
	
	private SlotOutcome _portalChildBonusOutcome; // If this outcome contained a portal bonus as the first bonus found, this will be the bonus under the portal.  Used by some Legacy games in PortalScript.cs
	public SlotOutcome portalChildBonusOutcome
	{
		get { return _portalChildBonusOutcome; }
	}
	

	// Determines if the multipleBonusGameQueue contains a queued bonus still
	public bool hasQueuedBonuses
	{
		get { return multipleBonusGameQueue.Count > 0; }
	}

	// Returns the count of queued bonuses for this outcome, useful if you need to do something special say for the last bonus to trigger
	public int queuedBonusesCount
	{
		get { return multipleBonusGameQueue.Count; }
	}

	// Peek at the next queued bonus game if one exists, used when automatically checking for bonus games
	public SlotOutcome peekAtNextQueuedBonusGame()
	{
		if (hasQueuedBonuses)
		{
			return multipleBonusGameQueue.Peek();
		}
		else
		{
			return null;
		}
	}

	public SlotOutcome(JSON passedJson)
	{
		outcomeJson = passedJson;
		outcomeType = OutcomeTypeEnum.UNDEFINED;
	}

	// Allows the setting of a parent JSON whose values will be checked 
	// if the values can't be obtained in the outcomeJson
	public void setParentOutcome(SlotOutcome parent)
	{
		parentOutcome = parent;
	}

	// Returns whether the outcome has any kind of bonus game (or other thing that requires special action).
	public bool isBonus
	{
		get { return isGifting || isChallenge || isCredit || isScatter || isPortal; }
	}

	// Reset the isBonus status by reseting all flags, do this after a bonus game returns to ensure that the game doesn't think it needs to launch another bonus


	// Returns whether this outcome contains a freespins game in it, including ones that are queued
	public bool hasFreespinsBonus()
	{
		if (isGifting)
		{
			return true;
		}
		else
		{
			foreach (SlotOutcome queuedBonus in multipleBonusGameQueue)
			{
				if (queuedBonus.isGifting)
				{
					return true;
				}
			}

			return false;
		}
	}

	private void setBonusGameOutcomeFromCache(string gameKey, JSON outcomeJson)
	{
		GameState.BonusGameNameData bonusGameNameData = GameState.bonusGameNameData;
		SlotOutcome currentOutcomeToCheck = this;

		while (currentOutcomeToCheck != null)
		{
			// Test for a portal type game
			// If there's no portal defined then lets just pass this up.
			if (SlotResourceMap.hasPortalPrefabPath(gameKey))
			{
				for (int i = 0; i < bonusGameNameData.portalBonusGameNames.Count; i++)
				{
					string name = bonusGameNameData.portalBonusGameNames[i];
					SlotOutcome outcome = getBonusGameOutcome(currentOutcomeToCheck, name, isRecursiveCheck:false);
					if (outcome != null)
					{
						isPortal = true;
						
						// Store out the nested bonus inside the portal (if there is one). Legacy portal scripts
						// make use of this in PortalScript.
						_portalChildBonusOutcome = outcome.getBonusGameInOutcomeDepthFirst();
						if (_portalChildBonusOutcome != null)
						{
							_portalChildBonusOutcome.processBonus();
						}

						// We found the correct portal bonus, bail out
						return;
					}
				}
			}

			// Test for scatter picking game
			// Only check for these types of scatter games if we have info defined in the resource map for it
			if (SlotResourceMap.hasScatterBonusPrefabPath(gameKey))
			{
				// Check to see if we actually have any scatter bonuses
				for (int i = 0; i < bonusGameNameData.scatterPickGameBonusGameNames.Count; i++)
				{
					string name = bonusGameNameData.scatterPickGameBonusGameNames[i];
					SlotOutcome outcome = getBonusGameOutcome(currentOutcomeToCheck, name, isRecursiveCheck:false);
					if (outcome != null)
					{
						// need to determine how many objects will be part of the scatter so we can fill extra data
						// try looking at the number of possible wins in the paytable for this version of the scatter game
						int extraInfo = 0;
						JSON scatterPaytable = BonusGamePaytable.findPaytable(name);
						if (scatterPaytable != null)
						{
							JSON[] roundsArray = scatterPaytable.getJsonArray("rounds");
							if (roundsArray.Length > 0)
							{
								JSON[] winsArray = roundsArray[0].getJsonArray("wins");
								extraInfo = winsArray.Length;
							}
						}

						isScatter = true;
						BonusGameManager.instance.outcomes[BonusGameType.SCATTER] = new WheelOutcome(outcome);
						(BonusGameManager.instance.outcomes[BonusGameType.SCATTER] as WheelOutcome).extraInfo = extraInfo;

						// We found the correct scatter bonus, bail out
						//Debug.Log("SlotOutcome.setBonusGameOutcomeFromCache() - gameKey = " + gameKey + "; name = " + name + "; found Scatter bonus with extraInfo = " + extraInfo);
						return;
					}
				}
			}

            // Test for a gifting/freespins game
            for (int i = 0; i < bonusGameNameData.giftingBonusGameNames.Count; i++)
            {
                string name = bonusGameNameData.giftingBonusGameNames[i];
                if (isGiftingBonus(currentOutcomeToCheck, name, isRecursiveCheck: false))
                {
                    // We found the correct gifting bonus, bail out
                    return;
                }
            }

            // Test for a credits game
            for (int i = 0; i < bonusGameNameData.creditBonusGameNames.Count; i++)
			{
				string name = bonusGameNameData.creditBonusGameNames[i];
				SlotOutcome outcome = getBonusGameOutcome(currentOutcomeToCheck, name, isRecursiveCheck:false);
				if (outcome != null)
				{
					isCredit = true;
					WheelOutcome creditOutcome = new WheelOutcome(outcome);
					winAmount = creditOutcome.getNextEntry().credits;
					// We found the correct credit bonus, bail out
					return;
				}
			}

			// Test for a challenge type game
			if (SlotResourceMap.hasChallengeBonusPrefabPath(gameKey) || SlotResourceMap.hasCreditBonusPrefabPath(gameKey))
			{
				for (int i = 0; i < bonusGameNameData.challengeBonusGameNames.Count; i++)
				{
					string name = bonusGameNameData.challengeBonusGameNames[i];
					SlotOutcome outcome = getBonusGameOutcome(currentOutcomeToCheck, name, isRecursiveCheck: false);
					if (outcome != null)
					{
						string creditBonusOutcomeKey = SlotResourceMap.getCreditBonusOutcomeKey(GameState.game.keyName);
						 if (name == creditBonusOutcomeKey)
						{
							// This bonus should be treated as a a credit bonus
							// since this game has two different bonus games which
							// are challenges
							isCredit = true;
						}
						else
						{
							isChallenge = true;
						}

						BonusGame bonusData = BonusGame.find(name);
						switch (bonusData.payTableType)
						{
							case BonusGame.PaytableTypeEnum.WHEEL:
								// lls games has a special case where it uses a different outcome type, so we need to check for it here
								// ted01 also uses that same outcome type for one of its bonuses
								if (name == "lls_challenge" || name == "ted01_challenge")
								{
									BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new MegaWheelOutcome(outcome);
								}
								else
								{
									BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new WheelOutcome(outcome);
								}
								break;
							case BonusGame.PaytableTypeEnum.PICKEM:
								BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new PickemOutcome(outcome);
								break;
							case BonusGame.PaytableTypeEnum.BASE_BONUS:
								BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new NewBaseBonusGameOutcome(outcome);
								break;
							case BonusGame.PaytableTypeEnum.CROSSWORD:
								BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new CrosswordOutcome(outcome);
								break;
							case BonusGame.PaytableTypeEnum.THRESHOLD_LADDER:
								BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new ThresholdLadderOutcome(outcome);
								break;
							default:
								Debug.LogError("Couldn't determine paytable type for challenge outcome!");
#if !ZYNGA_TRAMP
								Debug.Break();
#endif
								break;
						}

						// We found the correct challenge bonus, bail out
						return;
					}
				}
			}

			// Test for bonus game choices
			if (getBonusGameChoicesOutcome(currentOutcomeToCheck, isRecursiveCheck:false) != null)
			{
				isPortal = true;
				return;
			}
			
			currentOutcomeToCheck = currentOutcomeToCheck.getBonusGameInOutcomeDepthFirst();
		}
	}

	// Reset the bonus game flags, used when loading in queued bonus games to reset what was previously set
	public void resetBonusGameFlags()
	{
		isGifting = false;
		isChallenge = false; 
		isCredit = false;
		isScatter = false;
		isPortal = false;

		outcomeType = OutcomeTypeEnum.UNDEFINED;
	}

	// Remove the bonus that was completed from the list, assuming there was a queued list, this will happen after a bonus game completes
	public void removeBonusFromQueue()
	{
		if (hasQueuedBonuses)
		{
			// every time this is called we will assume the front of the queue was processed, and that we need to move onto the next bonus
			multipleBonusGameQueue.Dequeue();
		}
	}

	// Some games will support more than one bonus occuring in a row, aruze02 Extreme Dragon is one of the first games to do such a thing
	// to accomplish this, we will progress through a pre-compiled list of bonuses that was grabbed when processBonus was called for the first time
	public void processNextBonusInQueue()
	{
		if (hasQueuedBonuses)
		{
			// clear the flags from the previous bonus as they should be replaced by whatever bonus is triggering next
			resetBonusGameFlags();

			// we have another bonus to trigger
			processBonus(isTriggeringAdditionalBonuses:true);
		}
	}

	/**
	Process the outcome data to look for bonus games, including gifting and challenges.
	Since each game can have a unique outcome set, each came is processed in its own way,
	but stores the outcomes in standard BonusGameOutcome objects (sub-classed per outcome type).
	Those outcome objects are stored in the BonusGameManager.instance.outcomes Dictionary,
	keyed on the a BonusGameType, since there should be a maximum of one BonusGameType outcome per bonus game.
	Then when the actual bonus game code starts up, it pulls the relevant outcomes from
	BonusGameManager.instance.outcomes using the same game names it used when creating them,
	and easily iterates through the formatted outcomes in the objects.
	Example of iterating for most BonusGameOutcome objects:
		PickemOutcome outcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		while (outcome.pickCount > 0)
		{
			PickemPick pick = outcome.getNextEntry();
			// Do stuff with the pick.
		}
	*/
	public void processBonus(bool isTriggeringAdditionalBonuses = false)
	{
		if (!isTriggeringAdditionalBonuses)
		{
			winAmount = 0;

			// build the multipleBonusGameQueue
			// for now just using the reevaluations section called "bonus_games"
			JSON[] reevals = getArrayReevaluations();

			for (int i = 0; i < reevals.Length; i++)
			{
				JSON[] bonusGamesJSON = reevals[i].getJsonArray("bonus_games");

				for (int k = 0; k < bonusGamesJSON.Length; k++)
				{
					SlotOutcome bonusGame = new SlotOutcome(bonusGamesJSON[k]);
					bonusGame.processBonus();
					multipleBonusGameQueue.Enqueue(bonusGame);
				}
			}
		}

		// Clear out the storage area for bonus outcomes
		BonusGameManager.instance.outcomes.Clear();

		string gameKey = GameState.game.keyName;
		string[] possibleBonusGames = SlotGameData.find(gameKey).bonusGames;

		BonusGameManager.currentBonusGameOutcome = this;
		
		// Check for mystery gifts, which can happen on any game, so do it outside of the big switch statement below.
		processMysteryGifts();

		if (!isGifting)
		{
			// All new games should be able to go through setBonusGameOutcomeFromCache(), if that doesn't work we should
			// look into why, only legacy games should require entries here.
			switch (gameKey)
			{
				case "oz01":
					if (!isYBRBonus(outcomeJson, "oz_ybr"))
					{
						// Our "Free Spins Gifting" bonus is really a challenge bonus, but use the FreeSpinsOutcome class for storing it.
						// This is why we can't simply use the isGiftingBonus() function like usual.
						SlotOutcome challenge = getBonusGameOutcome(this, "oz01_challenge");
						if (challenge != null)
						{
							isChallenge = true;
							BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(challenge);
						}
						else
						{
							// Gifting in oz01 is actually a wheel game...oddly enough
							SlotOutcome rubyWheel = getBonusGameOutcome(this, "oz01_gifting");
							if (rubyWheel != null)
							{
								// Gifting found
								isGifting = true;
								BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new WheelOutcome(rubyWheel);
							}
						}
					}
					break;

				case "vip01":
					{
						// vip01 needs an entry since it isn't a standard game, it is the VIP Revamp bonus
						if (isGiftingBonusInsideReevaluation("vip_revamp_freespin"))
						{
							// vip01 bonus freespins game
						}
						break;
					}

				case "wow04":
					SlotOutcome wow04Portal = getBonusGameOutcome(this, "wow04_wheel");
					if (wow04Portal != null)
					{
						isPortal = true;
						BonusGameManager.instance.outcomes[BonusGameType.PORTAL] = new WheelOutcome(wow04Portal, true, 2);
					}

					if (isGiftingBonus(this, "wow_freespin"))
					{
						isGifting = true;
					}

					/// This could happen even without a portal when playing a gifted challenge,
					/// so don't wrap this check inside one of the above checks.
					SlotOutcome atg = getBonusGameOutcome(this, "wow_atg");
					if (atg != null)
					{
						isChallenge = true;
						BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new WheelOutcome(atg);
					}
					break;

				case "zynga01":
					if (!isGiftingBonus(this, "zynga01_gifting"))
					{
						SlotOutcome zynga01_challenge = getBonusGameOutcome(this, "zynga01_animal_pickem_5");
						if (zynga01_challenge == null)
						{
							zynga01_challenge = getBonusGameOutcome(this, "zynga01_animal_pickem_7");
							if (zynga01_challenge == null)
							{
								zynga01_challenge = getBonusGameOutcome(this, "zynga01_animal_pickem_9");
							}
						}

						if (zynga01_challenge != null)
						{
							isChallenge = true;
							BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new PickemOutcome(zynga01_challenge);
						}

						SlotOutcome creditBonus = getBonusGameOutcome(this, "zynga01_credit_bonus");
						if (creditBonus != null)
						{
							isCredit = true;
							WheelOutcome creditWheel = new WheelOutcome(creditBonus);
							winAmount = creditWheel.getNextEntry().credits;
							//Debug.Log("Win amount we got from the portal:" + winAmount);
						}
					}
					break;
                case "aruze05":
                    if(multipleBonusGameQueue.Count > 0)
                    {
                        BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new WheelOutcome(multipleBonusGameQueue.Peek());
                        isChallenge = true;
                    }
                    else
                    {
                        setBonusGameOutcomeFromCache(gameKey, outcomeJson);
                    }
                    break;

                default:
					setBonusGameOutcomeFromCache(gameKey, outcomeJson);
				break;
			}
		}
	}

	/// Check for gifting bonus (free spins).
	private bool isGiftingBonus(SlotOutcome outcomeToCheck, string gameName, bool isRecursiveCheck = true)
	{
		SlotOutcome gifting = getBonusGameOutcome(outcomeToCheck, gameName, isRecursiveCheck:isRecursiveCheck);
		if (gifting != null)
		{
			isGifting = true;
			BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(gifting);
			return true;
		}
		return false;
	}

	//Checking for a gifting game outcome when it's nested inside of the reevaluations
	private bool isGiftingBonusInsideReevaluation(string gameName)
	{
		SlotOutcome gifting = getBonusGameOutcome(this, gameName);
		if (gifting != null)
		{
			isGifting = true;
			SlotOutcome bonusOutcome = this;

			JSON[] bonusBaseOutcome = getJsonObject().getJsonArray("outcomes");
			if (bonusBaseOutcome.Length > 0) 
			{
				JSON[] baseReevaluations = bonusBaseOutcome[0].getJsonArray("reevaluations");
				if (baseReevaluations.Length > 0)
				{
					bonusOutcome = new SlotOutcome(baseReevaluations[0]);
				}
			}

			FreeSpinsOutcome freespinsOutcome = new FreeSpinsOutcome(bonusOutcome);
			freespinsOutcome.paytable = BonusGamePaytable.findPaytable("free_spin", getBonusGamePayTableName()); //Paytable name is still inside the baseoutcome and not the nested bonus outcomes
			BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = freespinsOutcome;
			return true;
		}
		return false;
	}
	
	// Processes any mystery gift outcomes in preparation for showing them before normal outcomes.
	public void processMysteryGifts()
	{
		JSON[] gifts = getMysteryGiftJsonList();
		
		if (gifts != null)
		{
			// There may be more than one gift in the outcome, but at first we only have a single one.
			foreach (JSON gift in gifts)
			{
				MysteryGift.outcomes.Add(gift);
			}
		}
	}

	/// Grab the list of mystery gifts
	private JSON[] getMysteryGiftJsonList()
	{
		return getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_MYSTERY_GIFTS);
	}

	/// Check if the outcome contains mystery gifts
	public bool isMysteryGiftPresent()
	{
		if (outcomeJson.hasKey(FIELD_MYSTERY_GIFTS))
		{
			return true;
		}
		else if (parentOutcome != null && parentOutcome.outcomeJson.hasKey(FIELD_MYSTERY_GIFTS))
		{
			return true;
		}
		return false;
	}

	/// Common function for checking for YBR bonus game, since multiple games may use it.
	private bool isYBRBonus(JSON baseOutcome, string gameName)
	{
		SlotOutcome challenge = getBonusGameOutcome(this, gameName);
		if (challenge != null)
		{
			isCredit = true;
			BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new ThresholdLadderOutcome(challenge);
			return true;
		}
		return false;
	}
	
	// Variant of getBonusGameOutcome, where we care only care about the game name that's first returned instead.
	// Should only really get used when first entering the bonus game json processing, and not anytime after.
	public static string getFirstBonusGameOutcomeName(JSON outcome)
	{
		if (outcome.getString(FIELD_BONUS_GAME, "") != "")
		{
			return outcome.getString(FIELD_BONUS_GAME, "");
		}
		
		if (outcome.hasKey(FIELD_OUTCOMES))
		{
			foreach (JSON sub in outcome.getJsonArray(FIELD_OUTCOMES))
			{
				// Recurse if we didn't find it yet.
				string recurse = getFirstBonusGameOutcomeName(sub);
				if (recurse != null)
				{
					return recurse;
				}
			}
		}
		
		return null;
	}

	private bool isBonusGameChoice()
	{
		return getOutcomeJsonValue(JSON.getBoolStatic, FIELD_BONUS_GAME_CHOICE, false, false);
	}

	// Variant of getBonusGameOutcome, where we care only care if we have a choice of a bonus game or not.
	public static SlotOutcome getBonusGameChoicesOutcome(SlotOutcome outcome, bool isRecursiveCheck = true)
	{
		if (outcome.isBonusGameChoice())
		{
			//Debug.Log("Found bonus game choice");
			BonusGameManager.instance.challengeProgressEventId = outcome.getBonusGameCreditChoiceEventID();
			if (SlotBaseGame.instance == null)
			{
				return null;
			}
			PayTable currentGamePaytable = PayTable.find(SlotBaseGame.instance.engine.gameData.basePayTable);
			if (currentGamePaytable == null)
			{
				return null;
			}
			
			BonusGameManager.instance.possibleBonusGameChoices = currentGamePaytable.scatterWins[outcome.getWinId()].bonusGameChoices;
			return outcome;
		}
		
		if (outcome.hasSubOutcomes() && isRecursiveCheck)
		{
			foreach (SlotOutcome sub in outcome.getSubOutcomesReadOnly())
			{
				// Recurse if we didn't find it yet.
				// Recurse if we didn't find it yet.
				SlotOutcome bonusGameChoiceOutcome = getBonusGameChoicesOutcome(sub);
				if (bonusGameChoiceOutcome != null)
				{
					return bonusGameChoiceOutcome;
				}
			}
		}

		return null;
	}

	/// Instance version of this function
	public SlotOutcome getBonusGameOutcome(string bonusGame, bool updateSummaryScreenName = true, bool givePriorityToMysteryGifts = false)
	{
		return SlotOutcome.getBonusGameOutcome(this, bonusGame, updateSummaryScreenName, givePriorityToMysteryGifts);
	}

	// Recurses over the outcome collecting the bonus game name found in order, used to loop for matches via ModularChallengeGame
	public static List<string> getBonusGameNameList(SlotOutcome outcome)
	{
		List<string> bonusGameNameList = new List<string>();
		string bonusGameName = outcome.getBonusGame();
		if (bonusGameName != "")
		{
			bonusGameNameList.Add(bonusGameName);
		}

		// check for queued bonuses
		foreach (SlotOutcome queuedOutcome in outcome.multipleBonusGameQueue)
		{
			bonusGameName = queuedOutcome.getBonusGame();
			if (bonusGameName != "")
			{
				bonusGameNameList.Add(bonusGameName);
			}
		}

		// check for suboutcome bonuses
		if (outcome.hasSubOutcomes())
		{
			ReadOnlyCollection<SlotOutcome> subOutcomeList = outcome.getSubOutcomesReadOnly();
			for (int i = 0; i < subOutcomeList.Count; i++)
			{
				SlotOutcome sub = subOutcomeList[i];
				List<string> recurseGameNameList = getBonusGameNameList(sub);
				if (recurseGameNameList.Count > 0)
				{
					bonusGameNameList.AddRange(recurseGameNameList);
				}
			}
		}

		return bonusGameNameList;
	}

	/// Recurses into the outcome until it finds the first sub outcome for the given bonus game name,
	/// then returns it as a SlotOutcome object so it can be used to create the appropriate BonusGameOutcome object.
	public static SlotOutcome getBonusGameOutcome(SlotOutcome outcome, string bonusGame, bool updateSummaryScreenName = true, bool givePriorityToMysteryGifts = false, bool isRecursiveCheck = true)
	{
		if (string.IsNullOrEmpty(bonusGame))
		{
			// This isn't a valid lookup, ignore it
			Debug.LogWarning("SlotOutcome.getBonusGameOutcome() - bonusGame passed was null or empty!");
			return null;
		}

		string bonusGameName = outcome.getBonusGame();
		if (bonusGameName == bonusGame)
		{
			// Grab the instance of the bonus game to check and see if it's name is defined
			BonusGame thisBonusGame = BonusGame.find(bonusGame);
			if (thisBonusGame != null && !thisBonusGame.name.Contains(bonusGame) && updateSummaryScreenName)
			{
				// Set the summary screen to be the last bonus game outcome that was found with a name defined.
				BonusGameManager.instance.summaryScreenGameName = bonusGame;
			}

			// Set whether or not this bonus game can be gifted
			if (thisBonusGame != null)
			{
				BonusGameManager.instance.isGiftable = thisBonusGame.gift;
			}

			return new SlotOutcome(outcome.outcomeJson);
		}

		if (givePriorityToMysteryGifts && outcome.isMysteryGiftPresent())
		{
			foreach (JSON sub in outcome.getMysteryGiftJsonList())
			{
				// Recurse if we didn't find it yet.
				SlotOutcome recurse = getBonusGameOutcome(new SlotOutcome(sub), bonusGame, updateSummaryScreenName);
				if (recurse != null)
				{
					return recurse;
				}
			}
		}
		else if (outcome.hasSubOutcomes() && isRecursiveCheck)
		{
			foreach (SlotOutcome sub in outcome.getSubOutcomesReadOnly())
			{
				// Recurse if we didn't find it yet.
				SlotOutcome recurse = getBonusGameOutcome(sub, bonusGame, updateSummaryScreenName, givePriorityToMysteryGifts);
				if (recurse != null)
				{
					return recurse;
				}
			}
		}

		// double check the mutliple game queue to see if the bonus game is queued and is the next one to trigger
		if (outcome.hasQueuedBonuses)
		{
			return getBonusGameOutcome(outcome.peekAtNextQueuedBonusGame(), bonusGame, updateSummaryScreenName, givePriorityToMysteryGifts);
		}

		return null;
	}

	/// Function to check all the free spin variations for pick major and see if the user got one
	public static SlotOutcome getPickMajorFreeSpinOutcome(SlotOutcome outcome, string freeSpinOutcomeName, int numberOfMajorSymbols = 4)
	{
		for (int i = 1; i < numberOfMajorSymbols + 1; ++i)
		{
			string freeSpinGameName = freeSpinOutcomeName + i;
			SlotOutcome freeSpinVariation = outcome.getBonusGameOutcome(freeSpinGameName);
			if (freeSpinVariation != null)
			{
				return freeSpinVariation;
			}
		}

		return null;
	}

	// End of bonus processing methods.

	/// Response type.  Usually only found on the top outcome level.
	public string getType()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_TYPE, "");
	}

	/// Get the string represenation of the outcome type, should perfer the one that returns OutcomeTypeEnum and doesn't have to parse the JSON every time
	private string getOutcomeTypeString()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_OUTCOME_TYPE, "");
	}

	// Get the paytablesetid if one exists
	public string getPaytableSetId()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_BONUS_GAME_PAY_TABLE_SET_ID, "");
	}

	/// Outcome type.
	public OutcomeTypeEnum getOutcomeType()
	{
		if (outcomeType == OutcomeTypeEnum.UNDEFINED)
		{
			string outcomeTypeString = getOutcomeTypeString();
			switch (outcomeTypeString)
			{
				case OUTCOME_TYPE_LINE_WIN:
					outcomeType = OutcomeTypeEnum.LINE_WIN;
					break;
				case OUTCOME_TYPE_CLUSTER_WIN:
					outcomeType = OutcomeTypeEnum.CLUSTER_WIN;
					break;
				case OUTCOME_TYPE_SCATTER_WIN:
					outcomeType = OutcomeTypeEnum.SCATTER_WIN;
					break;
				case OUTCOME_TYPE_BONUS_GAME:
					outcomeType = OutcomeTypeEnum.BONUS_GAME;
					break;
				case OUTCOME_TYPE_SYMBOL_COUNT:
					outcomeType = OutcomeTypeEnum.SYMBOL_COUNT;
					break;
				case OUTCOME_TYPE_BONUS_SYMBOL:
					outcomeType = OutcomeTypeEnum.BONUS_SYMBOL;
					break;
				case OUTCOME_TYPE_WHEEL:
					outcomeType = OutcomeTypeEnum.WHEEL;
					break;
				case OUTCOME_TYPE_PICKEM:
					outcomeType = OutcomeTypeEnum.PICKEM;
					break;
				case OUTCOME_TYPE_REEL_SET:
					outcomeType = OutcomeTypeEnum.REEL_SET;
					break;
				case OUTCOME_TYPE_THRESHOLD_LADDER:
					outcomeType = OutcomeTypeEnum.THRESHOLD_LADDER;
					break;
				case OUTCOME_TYPE_SYMBOL_CREDITS:
					outcomeType = OutcomeTypeEnum.SYMBOL_CREDITS;
					break;
				case "":
					// this outcome apparently doesn't have a type, so we will leave it UNDEFINED
					break;
				default:
					Debug.LogError("SlotOutcome.getOutcomeType() - Unhandled outcomeTypeString = " + outcomeTypeString + ", returning outcomeType as UNDEFINED!");
					break;

			}
		}

		return outcomeType;
	}

	/// Contains information about winning paylines
	public int[] getLandedReels()
	{
		return getOutcomeJsonValue<int[]>(JSON.getIntArrayStatic, FIELD_LANDED_REELS);
	}
	
	// Cache the suboutcomes of this outcome for quicker lookup
	private void cacheSubOutcomes()
	{
		cachedSubOutcomes = new List<SlotOutcome>();

		JSON[] outcomes = getJsonSubOutcomes();
		foreach (JSON outcome in outcomes)
		{
			cachedSubOutcomes.Add(new SlotOutcome(outcome));
		}

		string gameKey = "";
		if (GameState.game != null)
		{
			gameKey = GameState.game.keyName;
		}

		// Reevalution suboutcomes can also contain bonus games that we need to transition to
		List<SlotOutcome> reevaluationOutcomes = getReevaluationSubOutcomes();
		if (reevaluationOutcomes != null && reevaluationOutcomes.Count > 0)
		{
			cachedSubOutcomes.AddRange(reevaluationOutcomes);
		}

		cachedSubOutcomesReadOnly = new ReadOnlyCollection<SlotOutcome>(cachedSubOutcomes);
	}

	// Geta  read only version of the 
	public ReadOnlyCollection<SlotOutcome> getSubOutcomesReadOnly()
	{
		if (cachedSubOutcomesReadOnly == null)
		{
			cacheSubOutcomes();
		}

		return cachedSubOutcomesReadOnly;
	}

	// This gets a list of the sub outcomes for drilling down into bonus game results and the like.
	public List<SlotOutcome> getSubOutcomesCopy()
	{
		if (cachedSubOutcomes == null)
		{
			cacheSubOutcomes();
		}
		
		return new List<SlotOutcome>(cachedSubOutcomes);
	}

	/// Tells if this outcome contains suboutcomes (usually these are bonus game or payline info)
	public bool hasSubOutcomes()
	{
		JSON[] outcomes = getJsonSubOutcomes();
		List<SlotOutcome> reevalOutcomes = getReevaluationSubOutcomes();
		List<SlotOutcome> reevalLayeredOutcomes = getReevaluationSubOutcomesByLayer();

		if (outcomes.Length > 0 || reevalOutcomes.Count > 0 || reevalLayeredOutcomes.Count > 0)
		{
			return true;
		}

		return false;
	}

	public bool hasReevaluations()
	{
		return outcomeJson.hasKey(FIELD_REEVALUATIONS);
	}

	public JSON[] getJsonSubOutcomes()
	{
		JSON[] subOutcomes = getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_OUTCOMES, false);

		// Some of the suboutcomes can come down in reevaluations for bonus_symbol_accumulation because
		// of the way that backend needs to connect the data.
		// Also for games like billions02 we can have a reevaluation with outcome type = bonus_game so add
		// those as well.
		JSON[] reevaluationArray = getArrayReevaluations();

		List<JSON> reevaluationBonusGamesJson = new List<JSON>();
		if (reevaluationArray != null && reevaluationArray.Length > 0)
		{
			for (int i = 0; i < reevaluationArray.Length; i++)
			{
				JSON reevaluationJSON = reevaluationArray[i];
				string type = reevaluationJSON.getString("type", "");
				string outcomeType = reevaluationJSON.getString("outcome_type", "");

				if (outcomeType == FIELD_BONUS_GAME)
				{
					// a reevaluation can be an outcome with bonus_game type
					reevaluationBonusGamesJson.Add(reevaluationJSON);
				}

				if (type == "bonus_symbol_accumulation")
				{
					JSON bonusGameJSON = reevaluationJSON.getJSON("bonus_games");

					if (bonusGameJSON != null)
					{
						JSON[] subOutcomeJSONArray = bonusGameJSON.getJsonArray(FIELD_OUTCOMES);
						if (subOutcomeJSONArray != null && subOutcomeJSONArray.Length > 0)
						{
							reevaluationBonusGamesJson.AddRange(subOutcomeJSONArray);
						}
					}
				}
			}
		}

		if (reevaluationBonusGamesJson.Count > 0)
		{
			int currentIdx = subOutcomes.Length;

			//resize
			Array.Resize<JSON>(ref subOutcomes, reevaluationBonusGamesJson.Count + currentIdx);

			//copy
			for (int i = 0; i < reevaluationBonusGamesJson.Count; i++)
			{
				subOutcomes[currentIdx + i] = reevaluationBonusGamesJson[i];
			}
		}

		return subOutcomes;
	}

	/// Alternative to getSubOutcomes in the case that some manual processing needs to be done.
	public JSON[] getMegaReels()
	{
		return getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_MEGA_REELS, false);
	}

	// Loop through the reevalutaions data sent down for games like gwtw01
	public List<SlotOutcome> getReevaluationSubOutcomesByLayer()
	{
		List<SlotOutcome> allOutcomes = new List<SlotOutcome>();
		JSON[] reevaluations = getArrayReevaluations();
		
		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				JSON[] multiGamesData = reevaluation.getJsonArray("games");
				if (multiGamesData != null)
				{
					for (int j = 0; j < multiGamesData.Length; j++)
					{
						JSON[] outcomesJson;

						// hi03 is terrible and has crappy, deprecated outcome structure. Just let it be it's own case, but never again!!!
						if (GameState.isDeprecatedMultiSlotBaseGame())
						{
							outcomesJson = reevaluation.getJsonArray("games." + j + "." + FIELD_OUTCOMES);
						}
						else
						{
							outcomesJson = multiGamesData[j].getJsonArray(FIELD_OUTCOMES);
						}

						foreach (JSON outcome in outcomesJson)
						{
							SlotOutcome subOutcome = new SlotOutcome(outcome);
							subOutcome.processBonus();
							subOutcome.layer = j;
							allOutcomes.Add(subOutcome);
						}
					}
				}
			}
		}
		
		return allOutcomes;
	}

	/// Get the reevaluation block as a series out SlotOutcomes, allowing for easier parsing
	public List<SlotOutcome> getReevaluationsAsSlotOutcomes()
	{
		List<SlotOutcome> allOutcomes = new List<SlotOutcome>();

		JSON[] reevaluations = getArrayReevaluations();

		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				allOutcomes.Add(new SlotOutcome(reevaluation));
			}
		}

		return allOutcomes;
	}

	/// Some games store outcomes in reevealuations, going to determine the difference between outcomes stored and a re-eval spin via checking "reevaluated_stops"
	/// Also need to avoid reevaluations that are of type symbol_shuffle
	public List<SlotOutcome> getReevaluationSubOutcomes()
	{
		List<SlotOutcome> allOutcomes = new List<SlotOutcome>();

		JSON[] reevaluations = getArrayReevaluations();

		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				int[] reevaluatedStopsIntArray = reevaluation.getIntArray(FIELD_REEVALUATED_REEL_STOPS);
				string reevalType = reevaluation.getString(FIELD_TYPE, "");

				// ensure this reevaluation isn't re-spins and also isn't a shuffle
				if ((reevaluatedStopsIntArray == null || reevaluatedStopsIntArray.Length == 0) && reevalType != REEVALUATION_TYPE_SYMBOL_SHUFFLE)
				{
					JSON[] outcomesJson = reevaluation.getJsonArray(FIELD_OUTCOMES);

					foreach (JSON outcome in outcomesJson)
					{
						allOutcomes.Add(new SlotOutcome(outcome));
					}
				}
			}
		}

		return allOutcomes;
	}

	public List<SlotOutcome> getTumbleOutcomesAsSlotOutcomes()
	{
		List<SlotOutcome> allOutcomes = new List<SlotOutcome>();

		JSON[] tumbleOutcomes = getTumbleOutcomes();

		if (tumbleOutcomes != null && tumbleOutcomes.Length > 0)
		{
			for (int i = 0; i < tumbleOutcomes.Length; i++)
			{
				JSON tumbleOutcome = tumbleOutcomes[i];
				allOutcomes.Add(new SlotOutcome(tumbleOutcome));
			}
		}

		return allOutcomes;
	}

	/// Get a list of reels that aren't going to spin
	public HashSet<int> getStaticReels()
	{
		int[] staticReelsArray = getOutcomeJsonValue<int[]>(JSON.getIntArrayStatic, FIELD_STATIC_REELS);

		HashSet<int> staticReels = new HashSet<int>();

		foreach (int staticReel in staticReelsArray)
		{
			staticReels.Add(staticReel);
		}

		return staticReels;
	}

	// Used to get foreground reel stop info for games which send that down in the outcome itself instead of
	// inside of a mutation
	public int[] getForegroundReelStops()
	{
		int[] reelStops = getOutcomeJsonValue<int[]>(JSON.getIntArrayStatic, FIELD_FOREGROUND_REEL_STOPS);
		int[] reevaluatedReelStops = getOutcomeJsonValue<int[]>(JSON.getIntArrayStatic, FIELD_REEVALUATED_FOREGROUND_REEL_STOPS);
		
		HashSet<int> staticReels = getStaticReels();

		int reevaluationReelStopIndex = 0;

		if (reevaluatedReelStops.Length > 0)
		{
			for (int i = 0; i < reelStops.Length; i++)
			{
				if (!staticReels.Contains(i))
				{
					// reevaluated value
					reelStops[i] = reevaluatedReelStops[reevaluationReelStopIndex];
					reevaluationReelStopIndex++;
				}
			}
		}

		return reelStops;
	}

	// Which reel positions the game should stop on, handles possible reevaluation override and static reels as well
	public int[] getReelStops()
	{
		int[] reelStops = getOutcomeJsonValue<int[]>(JSON.getIntArrayStatic, FIELD_REEL_STOPS);
		int[] reevaluatedReelStops = getOutcomeJsonValue<int[]>(JSON.getIntArrayStatic, FIELD_REEVALUATED_REEL_STOPS);

		HashSet<int> staticReels = getStaticReels();

		int reevaluationReelStopIndex = 0;

		if (reevaluatedReelStops.Length > 0)
		{
			for (int i = 0; i < reelStops.Length; i++)
			{
				if (!staticReels.Contains(i))
				{
					// reevaluated value
					reelStops[i] = reevaluatedReelStops[reevaluationReelStopIndex];
					reevaluationReelStopIndex++;
				}
			}
		}

		JSON[] reevaluations = getArrayReevaluations();
		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluationJson = reevaluations[i];
				string reevalType = reevaluationJson.getString(FIELD_TYPE, "");

				if(reevalType == REEVALUATION_TYPE_SPOTLIGHT && reevaluationJson.hasKey(FIELD_REEL_STOPS))
				{
					reelStops = reevaluationJson.getIntArray(FIELD_REEL_STOPS);
					break;
				}

				if (reevalType == "vip_revamp_mini_game" && reevaluationJson.hasKey(FIELD_REEL_STOPS))
				{
					reelStops = reevaluationJson.getIntArray(FIELD_REEL_STOPS);
					break;
				}
			}
		}

		return reelStops;
	}

	/// Friendly way of displaying reelstops. use in breadcrumbs to find which spins cause issues.
	public string printReelStops()
	{
		int[] reelStops = getReelStops();
		string reelStopsOutput = "[";
		for (int i = 0; i < reelStops.Length; i++)
		{
			reelStopsOutput += reelStops[i] + "";
			if (i != reelStops.Length-1)
			{
				reelStopsOutput += ", ";
			}
		}
		reelStopsOutput += "]";

		return reelStopsOutput;
	}

	/// Matrix of symbols sent form the server which are expected on the reels
	/// NOTE: This matrix assumes sticky symbols have already been processed!
	public List<List<List<string>>> getReevaluatedSymbolMatrix()
	{
		List<List<List<string>>> debugSymbols = new List<List<List<string>>>();
		
		if (!ReelGame.activeGame.isValidatingWithServerDebugSymbols
			|| GameState.game.keyName == "bride01" 
			|| GameState.game.keyName == "harvey01")
		{
			// These games don't get the right data from the server to be able to verify the DEBUG_SYMBOL_MATRIX, so no checks at all :()
			return new List<List<List<string>>>(); // Bride01 would need a data change to get it's symbol stuff to work properly now :(
		}
		List<List<string>> symbolMatrix = getOutcomeJsonValue<List<List<string>>>(JSON.getStringListListStatic, FIELD_REEVALUATED_MATRIX);
		if (symbolMatrix.Count == 0)
		{
			// This might be a layered game, we need to go through each layer of "games" and get the symbol matrix.
			JSON[] reevaluations = getArrayReevaluations();
		
			if (reevaluations != null && reevaluations.Length > 0)
			{
				for (int i = 0; i < reevaluations.Length; i++)
				{
					JSON reevaluation = reevaluations[i];
					JSON[] multiGamesData = reevaluation.getJsonArray("games");
					for (int j = 0; i < multiGamesData.Length; j++)
					{
						symbolMatrix = multiGamesData[j].getStringListList(FIELD_REEVALUATED_MATRIX);
						if (symbolMatrix.Count > 0)
						{
							debugSymbols.Add(symbolMatrix);
						}
					}
				}
			}
		}
		else
		{
			debugSymbols.Add(symbolMatrix);
		}
		return debugSymbols;
	}

	// Gets the symbol matrix from the outcome json. Only available if server debugging/optional logs are toggled on.
	public List<List<List<string>>> getDebugServerSymbols(ReelGame reelGame)
	{
		List<List<List<string>>> debugSymbols = new List<List<List<string>>>();
		
		if (reelGame == null || GameState.game == null)
		{
			Debug.LogError("Cannot get debug server symbols, reelGame is null");
			return debugSymbols;
		}

		if (!reelGame.isValidatingWithServerDebugSymbols
			|| GameState.game.keyName == "bride01" 
			|| GameState.game.keyName == "mm01" 
			|| GameState.game.keyName == "elvira03" 
			|| GameState.game.keyName == "harvey01")
		{
			// These games don't get the right data from the server to be able to verify the DEBUG_SYMBOL_MATRIX, so no checks at all :()
			return debugSymbols; // Bride01 would need a data change to get it's symbol stuff to work properly now :(
		}

		List<List<string>> symbolMatrix = getOutcomeJsonValue<List<List<string>>>(JSON.getStringListListStatic, FIELD_DEBUG_SYMBOL_MATRIX);
		if (symbolMatrix.Count == 0)
		{
			// This might be a layered game, we need to go through each layer of "games" and get the symbol matrix.
			JSON[] reevaluations = getArrayReevaluations();
		
			if (reevaluations != null && reevaluations.Length > 0)
			{
				for (int i = 0; i < reevaluations.Length; i++)
				{
					JSON reevaluation = reevaluations[i];
					JSON[] multiGamesData = reevaluation.getJsonArray("games");
					for (int j = 0; j < multiGamesData.Length; j++)
					{
						symbolMatrix = multiGamesData[j].getStringListList(FIELD_DEBUG_SYMBOL_MATRIX);
						if (symbolMatrix.Count > 0)
						{
							debugSymbols.Add(symbolMatrix);
						}
					}
				}
			}
		}
		else
		{
			debugSymbols.Add(symbolMatrix);
		}
		return debugSymbols;
	}
	
	/// A reel set may be provided for games that do reel strip insertion.
	public bool getPaylineFromRight()
	{
		return getOutcomeJsonValue(JSON.getBoolStatic, FIELD_FROM_RIGHT, false);
	}

	public string getForegroundReelSet()
	{
		// @todo : Consider if we want to add any additional handling like exists for getReelSet() for
		// additional times when we might want to swap the foreground reel set
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_FOREGROUND_REEL_SET, "");
	}

	// A reel set may be provided for games that do reel strip insertion.
	public string getReelSet()
	{
		string reelSetKey = getOutcomeJsonValue(JSON.getStringStatic, FIELD_CHECKPOINT_REEL_SET, "");

		if (string.IsNullOrEmpty(reelSetKey))
		{
			reelSetKey = getOutcomeJsonValue(JSON.getStringStatic, FIELD_RESPIN_REEL_SET, "");
		}

		JSON[] reevaluations = getArrayReevaluations();
		if (reevaluations != null && reevaluations.Length > 0)
		{
			// If we have a reevaulation without spin data and it has a reelset assume it's for the current spin.
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluationSpin = reevaluations[i];
				int[] reevaluatedIntArray = reevaluationSpin.getIntArray(FIELD_REEVALUATED_REEL_STOPS);
				if (reevaluatedIntArray == null || reevaluatedIntArray.Length == 0)
				{
					reelSetKey = reevaluationSpin.getString(FIELD_REEL_SET, reelSetKey, reelSetKey);
				}
			}
		}

		if (!string.IsNullOrEmpty(reelSetKey))
		{
			if (ReelGame.activeGame != null && ReelGame.activeGame.slotGameData.findReelSet(reelSetKey) != null)
			{
				return reelSetKey;
			}
		}

		reelSetKey = getOutcomeJsonValue(JSON.getStringStatic, FIELD_REEL_SET, "");
		return reelSetKey;
	}

	public string getPayLineSet()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_PAYLINE_SET, "");
	}

	/// A reel set may provide reel strips on particular reels when doing reel strip insertion.
	public Dictionary<int,string> getReelStrips()
	{
		return getOutcomeJsonValue<Dictionary<int,string>>(JSON.getIntStringDictStatic, FIELD_REEL_STRIPS);
	}

	// Extract the universal reel strip replacement data from the outcome
	private List<ReelStripReplacementData> getReelStripReplacementData()
	{
		List<ReelStripReplacementData> stripReplacementDataList = new List<ReelStripReplacementData>();
		JSON[] reelStripReplacementJsonArray = getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_REEL_STRIP_REPLACEMENTS, false);
		foreach (JSON reelStripReplacementJson in reelStripReplacementJsonArray)
		{
			string keyName = reelStripReplacementJson.getString("key_name", "");
			int reelIndex = reelStripReplacementJson.getInt("reel", 0);
			// reel comes down 1 based, so to make an index we subtract 1
			if (reelIndex > 0)
			{
				reelIndex -= 1;
			}

			int position = reelStripReplacementJson.getInt("position", 0);
			int layer = reelStripReplacementJson.getInt("layer", 0);
			int visibleSymbols = reelStripReplacementJson.getInt("visible_symbols", 0);
			
			stripReplacementDataList.Add(new ReelStripReplacementData(keyName, reelIndex, position, layer, visibleSymbols));
		}

		return stripReplacementDataList;
	}
	
	// Cache the universal reel strip replacements data of this outcome for quicker lookup
	private void cacheReelStripReplacementData()
	{
		List<ReelStripReplacementData> stripReplacementDataList = getReelStripReplacementData();
		cachedReelStripReplacementDataReadOnly = new ReadOnlyCollection<ReelStripReplacementData>(stripReplacementDataList);
	}

	// Get a read only version of the universal reel strip replacement data
	public ReadOnlyCollection<ReelStripReplacementData> getReelStripReplacementDataReadOnly()
	{
		if (cachedReelStripReplacementDataReadOnly == null)
		{
			cacheReelStripReplacementData();
		}

		return cachedReelStripReplacementDataReadOnly;
	}

	public Dictionary<string,string> getNormalReplacementSymbols()
	{
		// Sets the replacement data for the symbols 
		Dictionary<string, string> normalReplacementSymbolMap = new Dictionary<string, string>();
		JSON replaceData = getOutcomeJsonValue<JSON>(JSON.getJSONStatic, FIELD_REPLACEMENT_SYMBOLS, false);

		if (replaceData != null)
		{
			foreach(KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
			{
				normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
			}
		}
		else
		{
			// check to see if the data is stored in the mutations section
			JSON[] mutationInfo = getMutations();

			for (int i = 0; i < mutationInfo.Length; i++)
			{
				JSON info = mutationInfo[i];
				replaceData = info.getJSON("replace_symbols");

				if (replaceData != null)
				{
					foreach (KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
					{
						// Check and see if mega and normal have the same values.
						normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
					}
				}
			}
		}

		return normalReplacementSymbolMap;

	}

	public Dictionary<string,string> getMegaReplacementSymbols()
	{
		// Sets the replacement data for the symbols 
		Dictionary<string, string> megaReplacementSymbolMap = new Dictionary<string, string>();
		JSON replaceData = getOutcomeJsonValue<JSON>(JSON.getJSONStatic, FIELD_REPLACEMENT_SYMBOLS, false);

		if (replaceData != null)
		{
			foreach(KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
			{
				megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
			}
		}
		else
		{
			// check to see if the data is stored in the mutations section
			JSON[] mutationInfo = getMutations();

			for (int i = 0; i < mutationInfo.Length; i++)
			{
				JSON info = mutationInfo[i];
				replaceData = info.getJSON("replace_symbols");

				if (replaceData != null)
				{
					foreach (KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
					{
						// Check and see if mega and normal have the same values.
						megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
					}
				}
			}
		}

		return megaReplacementSymbolMap;

	}

	// The intial freespin reel set used by 
	//freeSpinInitialReelSet = data.getIntStringDict("freespin_initial_reel_sets");
	// This is a nested dictionary for gifting outcomes.
	public Dictionary<int, string> getFreeSpinInitialReelSet()
	{
		Dictionary<string, JSON> giftingStartingSet = getOutcomeJsonValue<Dictionary<string, JSON>>(JSON.getStringJSONDictStatic, FIELD_FREESPIN_INITIAL_REELSET, false);
		if (giftingStartingSet != null && giftingStartingSet.ContainsKey("reels"))
		{
			// Gifting bonuses for the this is a litle different than the base game.
			Dictionary<int, string> returnVal = new Dictionary<int, string>();
			foreach (string subKey in giftingStartingSet["reels"].getKeyList())
			{
				Debug.LogWarning("free spin reel set subkey: " + subKey);
				returnVal[int.Parse(subKey)] = giftingStartingSet["reels"].getString(subKey, "");
			}

			return returnVal;
		}
		else
		{
			return getOutcomeJsonValue<Dictionary<int,string>>(JSON.getIntStringDictStatic, FIELD_FREESPIN_INITIAL_REELSET, false);
		}
	}

	/// A reel set may provide reel strips on particular reels when doing reel strip insertion.
	public List<KeyValuePair<int,string>> getForegroundReelStrips()
	{
		Dictionary<int, string> dict = getOutcomeJsonValue<Dictionary<int,string>>(JSON.getIntStringDictStatic, FIELD_FOREGROUND_REEL_STRIPS);
		List<KeyValuePair<int,string>> list = new List<KeyValuePair<int,string>>(dict);
		list.Sort(sortByKey);
		return list;
	}

	//same as above, but in a dictionary instead of in a List ... save some allocations.
	public Dictionary<int, string> getForegroundReelStripsDictionary()
	{
		return getOutcomeJsonValue<Dictionary<int,string>>(JSON.getIntStringDictStatic, FIELD_FOREGROUND_REEL_STRIPS);
	}

	private static int sortByKey(KeyValuePair<int,string> a, KeyValuePair<int,string> b)
	{
		return a.Key.CompareTo(b.Key);
	}

	private JSON getAnticipationJSON()
	{
		JSON[] reevaluations = getArrayReevaluations();
		if (reevaluations != null && reevaluations.Length > 0)
		{
			foreach (JSON reeval in reevaluations)
			{
				if (reeval.getString("type", "").Equals(REEVALUATION_TYPE_SPOTLIGHT) ||
					reeval.getString("type", "").Equals(REEVALUATION_TYPE_BONUS_SYMBOL_ACCUMULATION_MULTI))
				{
					return reeval.getJSON(FIELD_ANTICIPATION_INFO);
				}
			}
		}

		return getOutcomeJsonValue<JSON>(JSON.getJSONStatic, FIELD_ANTICIPATION_INFO);
	}

	/// Get the "anticipation_info"'s "symbols" section for games that play anticipations based on symbols.  Returns null if there are no symbol anticipations.
	public Dictionary<int, string> getAnticipationSymbols()
	{
		JSON fieldAnticipationJson = getAnticipationJSON();
		if (fieldAnticipationJson != null)
		{
			if (fieldAnticipationJson.hasKey(FIELD_ANTICIPATION_INFO_SYMBOLS))
			{
				return fieldAnticipationJson.getIntStringDict(FIELD_ANTICIPATION_INFO_SYMBOLS);
			}
		}

		return null;
	}
	
	/// Gets the Anticipation_info's trigger field and returns it as a 2d dictionary. Returns null if there are no anticipations.
	public Dictionary<int,Dictionary<string,int>> getAnticipationTriggers()
	{
		JSON fieldAnticipationJson = getAnticipationJSON();
		return getAnticipationTriggersFromAnticipationJson(fieldAnticipationJson);
	}

	// Utility function for parsing anticipation info into a 2D dictionary from a given JSON
	public Dictionary<int, Dictionary<string, int>> getAnticipationTriggersFromAnticipationJson(JSON fieldAnticipationJson)
	{
		Dictionary<int,Dictionary<string,int>> returnVal = new Dictionary<int,Dictionary<string,int>>();
		if (fieldAnticipationJson != null)
		{
			if (fieldAnticipationJson.hasKey(FIELD_TRIGGERS))
			{
				JSON fieldTriggersJson = fieldAnticipationJson.getJSON(FIELD_TRIGGERS);
				if (fieldTriggersJson != null)
				{
					foreach (string key in fieldTriggersJson.getKeyList())
					{
						int index = 0;
						if (int.TryParse(key, out index))
						{
							//Now each of the values in this entry is it's own Dictionary.
							returnVal[index] = fieldTriggersJson.getStringIntDict(key);
						}
						else
						{
							Debug.LogError("SlotOutcome.getAnticipationTriggers(): key is not an int as expected.");
						}
					}
					
					return returnVal;
				}
			}
		}

		return null;
	}
	
	/// Gets the reevaluation anticipation_info's trigger field and returns it as a 3d dictionary. Used for multi games who have anticipations in the "games" array in the reevaluation.
	public Dictionary<int, Dictionary<int, Dictionary<string, int>>> getReevaluationAnticipationTriggers()
	{
		Dictionary<int, Dictionary<int, Dictionary<string, int>>> returnVal = new Dictionary<int, Dictionary<int, Dictionary<string, int>>>();
		JSON[] reevaluations = getArrayReevaluations();
		
		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				JSON[] multiGamesData = reevaluation.getJsonArray(FIELD_GAMES);
				if (multiGamesData != null)
				{
					for (int j = 0; j < multiGamesData.Length; j++)
					{
						JSON outcomesJson = multiGamesData[j].getJSON(FIELD_ANTICIPATION_INFO);
						if (outcomesJson != null)
						{
							returnVal[j] = new Dictionary<int, Dictionary<string, int>>();
							JSON fieldTriggersJson = outcomesJson.getJSON(FIELD_TRIGGERS);
							if (fieldTriggersJson != null)
							{
								foreach (string key in fieldTriggersJson.getKeyList())
								{
									int index = 0;
									if (int.TryParse(key, out index))
									{
										//Now each of the values in this entry is it's own Dictionary.
										returnVal[j][index] = fieldTriggersJson.getStringIntDict(key);
									}
									else
									{
										Debug.LogError("SlotOutcome.getAnticipationTriggers(): key is not an int as expected.");
									}
								}
		
							}
						}
					}
				}

				JSON multiGameAnticipationInfo = reevaluation.getJSON(FIELD_MULTI_GAME_ANTICIPATION);
				if (multiGameAnticipationInfo != null)
				{
					JSON[] multiGamesAnticipationData = multiGameAnticipationInfo.getJsonArray(FIELD_GAMES);
					if (multiGamesAnticipationData != null)
					{
						for (int j = 0; j < multiGamesAnticipationData.Length; j++)
						{
							JSON fieldTriggersJson = multiGamesAnticipationData[j].getJSON(FIELD_TRIGGERS);
							if (fieldTriggersJson != null)
							{
								if (!returnVal.ContainsKey(j))
								{
									returnVal[j] = new Dictionary<int, Dictionary<string, int>>();
								}
								foreach (string key in fieldTriggersJson.getKeyList())
								{
									int index = 0;
									if (int.TryParse(key, out index))
									{
										//Now each of the values in this entry is it's own Dictionary.
										returnVal[j][index] = fieldTriggersJson.getStringIntDict(key);
									}
									else
									{
										Debug.LogError("SlotOutcome.getAnticipationTriggers(): key is not an int as expected.");
									}
								}
								
							}
						}
					}
				}
			}
		}

		return returnVal;
	}

	// Returns a dictionary keyed by layer (or "game") with values being the arrays of the reels that should play anticipation soudns
	public Dictionary<int, int[]> getReevaluationAnticipationSounds()
	{
		Dictionary<int, int[]> anticipationSoundReels = new Dictionary<int, int[]>();
		JSON[] reevaluations = getArrayReevaluations();
		
		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				JSON[] multiGamesData = reevaluation.getJsonArray(FIELD_GAMES);
				if (multiGamesData != null)
				{
					for (int j = 0; j < multiGamesData.Length; j++)
					{
						JSON outcomesJson = multiGamesData[j].getJSON(FIELD_ANTICIPATION_INFO);
						if (outcomesJson != null)
						{
							int[] reelStops  = outcomesJson.getIntArray(FIELD_REELS_LANDED);
							if (reelStops != null)
							{
								anticipationSoundReels.Add(j, reelStops);
							}
						}
					}
				}

				JSON multiGameAnticipationInfo = reevaluation.getJSON(FIELD_MULTI_GAME_ANTICIPATION);
				if (multiGameAnticipationInfo != null)
				{
					JSON[] multiGamesAnticipationData = multiGameAnticipationInfo.getJsonArray(FIELD_GAMES);
					if (multiGamesAnticipationData != null)
					{
						for (int j = 0; j < multiGamesAnticipationData.Length; j++)
						{
							int[] reelStops  = multiGamesAnticipationData[j].getIntArray(FIELD_REELS_LANDED);
							if (reelStops != null)
							{
								if (!anticipationSoundReels.ContainsKey(j))
								{
									anticipationSoundReels.Add(j, reelStops);
								}
								else
								{
									List<int> reelStopsCopy = new List<int>();
									foreach (int reelIndex in anticipationSoundReels[j])
									{
										reelStopsCopy.Add(reelIndex);
									}
									foreach (int reelIndex in reelStops)
									{
										if (!reelStopsCopy.Contains(reelIndex))
										{
											reelStopsCopy.Add(reelIndex);
										}
									}

									anticipationSoundReels[j] = reelStopsCopy.ToArray();
								}
							}
						}
					}
				}
			}
		}

		return anticipationSoundReels;
	}

	/// The reels that should play the anticipation sounds.
	public int[] getAnticipationSounds()
	{
		JSON fieldAnticipationJson = getAnticipationJSON();
		if (fieldAnticipationJson != null)
		{
			return fieldAnticipationJson.getIntArray(FIELD_REELS_LANDED);
		}

		return new int[0];
	}

	public JSON getLinkedSymbolJSON()
	{
		JSON[] reevaluations = getArrayReevaluations();

		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				if (reevaluation.getString(FIELD_TYPE, "").Equals("linked_symbol"))
				{
					return reevaluation;
				}
			}
		}

		return null;
	}

	public JSON getLinkedSymbolAnticipationInfo()
	{
		JSON linkedSymbolJSON = getLinkedSymbolJSON();
		if (linkedSymbolJSON != null)
		{
			return linkedSymbolJSON.getJSON(FIELD_ANTICIPATION_INFO);
		}

		return null;
	}
	
	// return linked symbol data as a dictionary with kvp: reelId, list of symbol names
	public Dictionary<int, List<string>> getLinkedSymbolsDictionary()
	{
		Dictionary<int, List<string>> outputSymbolsDictionary = new Dictionary<int, List<string>>();
		JSON linkedSymbols = getLinkedSymbolJSON();
		if (linkedSymbols != null)
		{
			JSON[] linkedSymbolInfo = linkedSymbols.getJsonArray("linked_symbols");
			if (linkedSymbolInfo != null)
			{
				for (int i = 0; i < linkedSymbolInfo.Length; i++)
				{
					JSON json = linkedSymbolInfo[i];
					int reelId = json.getInt("reel", -1);
					string symbolName = json.getString("symbol", "");
					if (reelId >= 0 && !string.IsNullOrEmpty(symbolName))
					{
						if (!outputSymbolsDictionary.ContainsKey(reelId))
						{
							outputSymbolsDictionary[reelId] = new List<string>();
						}

						outputSymbolsDictionary[reelId].Add(symbolName);
					}
				}
			}
		}

		return outputSymbolsDictionary;
	}

	// Calculates when the reels should stop, using the timing fields set in SlotGameData (from global data).
	// Checks to see reels are linked by looking at what's assigned to SlotReels
	public int[] getReelTiming(ReelGame reelGame)
	{
		if (reelGame == null)
		{
			Debug.LogError("No reelgame defined, can't get timing info");
			return null;
		}

		SlotGameData gameData = reelGame.slotGameData;
		bool isFreeSpinGame = reelGame is FreeSpinGame; 

		Dictionary<int,Dictionary<string,int>> anticipationTriggers = getAnticipationTriggers();
		Dictionary<int, Dictionary<int, Dictionary<string,int>>> reevaluationAnticipationTriggers = getReevaluationAnticipationTriggers();

		int[] reelTiming = new int[reelGame.stopOrder.Length];

		int landingDelay = 0;
		int landingInterval = 0;
		int anticipationDelay = 0;
		Dictionary<int, string> reelStrips = null;
		int reelStripsLeftReel = 999;	// The leftmost reel that is in the reelStrips data.

		gameData.getSpinTiming(isFreeSpinGame, out landingDelay, out landingInterval, out anticipationDelay);

		reelTiming[0] = landingDelay;

		// We want to see if we can use the linked reeldata sent down from the server.
		bool hasLinkedReels = containsLinkedReels(); 
		foreach (SlotReel reel in reelGame.engine.getReelArray())
		{
			if (reel.reelSyncedTo != -1)
			{
				hasLinkedReels = true;
				break;
			}
		}

		// If the linked reel data isn't already set and we want it to be synced.
		// This uses what reels have changed from the last spin, but that doesn't
		// Work for newer games, this is true for elvira type games.
		if (!hasLinkedReels && reelGame.isGameWithSyncedReels())
		{
			reelStrips = getReelStrips();
			foreach (KeyValuePair<int, string> kvp in reelStrips)
			{
				//Debug.LogWarning(kvp.Key + " - " + kvp.Value);
				reelStripsLeftReel = Mathf.Min(reelStripsLeftReel, kvp.Key);
			}
		}

		HashSet<int> linkedReels = getLinkedReels();

		bool firstLinked = true;
		if (linkedReels.Contains(0))
		{
			firstLinked = false;
		}

		for (int stopIndex = 1; stopIndex < reelGame.stopOrder.Length; stopIndex++)
		{
			if (reelStrips != null &&
				reelStrips.Count > 0 &&
				stopIndex > reelStripsLeftReel - 1 &&
				reelStrips.ContainsKey(stopIndex + 1)
				)
			{
				reelTiming[stopIndex] = 0;
			}
			else if (reelGame.skipReelStopIntervalsForBlankReels && reelGame.engine.isAllBLSymbolsAt(stopIndex))
			{
				// Normally we always want to skip reels that have all BL symbols, but in the case of marilyn02,
				// many independent reels are blank in freespins and we don't want all these reels to stop at the
				// same time. We want all reels to keep stopping normally - so un-check skipReelStopIntervalsForBlankReels 
				reelTiming[stopIndex] = 0;
			}
			else
			{
				// It's not that weird elvira / hol02 / hol03 case, so lets see if it's got a normal linked reel flow.
				if (hasLinkedReels)
				{
					if (reelGame.engine.isReelStopLinkedAt(stopIndex))
					{
						if (reelGame.hasMultipleLinkedReelSets && reelGame.linkedReelStartingReelIndices.Contains(stopIndex)) //Some games might have multiple firstLinked reels now
						{
							firstLinked = true;
						}
						// We calculate time based off when it should stop after the reel next to it.
						if (firstLinked && containsLinkedReels())
						{
							// Linked reels from the outcome contain all the linked reels, using reel.syncedTo doesn't have the first reel.
							reelTiming[stopIndex] = landingInterval;
							firstLinked = false;
						}
						else
						{
							reelTiming[stopIndex] = 0;
						}
					}
					else
					{
						reelTiming[stopIndex] = landingInterval;
					}
				}
				else
				{
					reelTiming[stopIndex] = landingInterval;
				}
			}

			List<SlotReel> reelsAtStopIndex = null;

			if (anticipationTriggers != null)
			{
				foreach (Dictionary<string,int> triggerInfo in anticipationTriggers.Values)
				{
					int reelToAnimate;
					if (triggerInfo.TryGetValue("reel", out reelToAnimate))
					{
						if(reelsAtStopIndex == null)
							reelsAtStopIndex = reelGame.engine.getReelsAtStopIndex (stopIndex);
						foreach (SlotReel reel in reelsAtStopIndex)
						{
							// for now assume all anticipation stuff is tied to reel layer 0, since it isn't sent with layer information
							if (reel != null && reel.layer == 0)
							{
								//Debug.LogWarning("ReelID = " + reel.reelID + " pos = " + reel.reelData.position + " Raw reelID = " + reel.getRawReelID());
								if (reelToAnimate == reel.getRawReelID() + 1)
								{
									//Debug.LogWarning("reelToAnimate = " + reelToAnimate + " stopIndex = " + stopIndex + "\nReelID = " + reel.reelID + " pos = " + reel.reelData.position + " Raw reelID = " + reel.getRawReelID(true) + " layer = " + reel.layer + " anticipationDelay = " + anticipationDelay);
									reelTiming[stopIndex] += anticipationDelay;
									break;
								}
							}
						}
					}
				}
			}
			if (reevaluationAnticipationTriggers != null && reevaluationAnticipationTriggers.Count > 0)
			{
				
				foreach(int i in reevaluationAnticipationTriggers.Keys)
				{
					foreach (Dictionary<string,int> triggerInfo in reevaluationAnticipationTriggers[i].Values)
					{
						int reelToAnimate;
						if (triggerInfo.TryGetValue("reel", out reelToAnimate))
						{
							if(reelsAtStopIndex == null)
								reelsAtStopIndex = reelGame.engine.getReelsAtStopIndex (stopIndex);
							foreach (SlotReel reel in reelsAtStopIndex)
							{
								// for now assume all anticipation stuff is tied to reel layer 0, since it isn't sent with layer information
								if (reel != null && reel.layer == i)
								{
									//Debug.LogWarning("ReelID = " + reel.reelID + " pos = " + reel.reelData.position + " Raw reelID = " + reel.getRawReelID());
									if (reelToAnimate == reel.getRawReelID() + 1)
									{
										//Debug.LogWarning("reelToAnimate = " + reelToAnimate + " stopIndex = " + stopIndex + "\nReelID = " + reel.reelID + " pos = " + reel.reelData.position + " Raw reelID = " + reel.getRawReelID(true) + " layer = " + reel.layer + " anticipationDelay = " + anticipationDelay);
										reelTiming[stopIndex] += anticipationDelay;
										break;
									}
								}
							}
						}
					}
				}
			}
		}
		
		// prints out the timings set for all of the reels.
		/*
		string s = "";
		for (int reelnum = 0; reelnum < reelTiming.Length; reelnum++)
		{
			s += "StopID: " + reelnum + " time = " + reelTiming[reelnum] + "\n";
		}
		Debug.LogWarning(s);
		*/
		
		return reelTiming;
	}

	// Walks the list of all sub-outcomes, examines the win IDs, and looks up the credit value.  Assumes this is referring to a bonus game's paytable.
	public int getTotalCreditAmount()
	{
		int payout = 0;
		ReadOnlyCollection<SlotOutcome> outcomes = getSubOutcomesReadOnly();
		if (outcomes != null && outcomes.Count > 0)
		{
			foreach (SlotOutcome outcome in outcomes)
			{
				if (outcome.getWinId() != -1)
				{
					if (outcome.getOutcomeType() == OutcomeTypeEnum.LINE_WIN || outcome.getOutcomeType() == OutcomeTypeEnum.CLUSTER_WIN)
					{
						payout += BonusGameManager.instance.currentBonusPaytable.lineWins[outcome.getWinId()].credits;
					}
					else if (outcome.getOutcomeType() == OutcomeTypeEnum.SCATTER_WIN)
					{
						payout += BonusGameManager.instance.currentBonusPaytable.scatterWins[outcome.getWinId()].credits;
					}
				}
			}
		}
		return payout;
	}

	public int getFreeSpins()
	{
		return getOutcomeJsonValue(JSON.getIntStatic, FIELD_FREESPINS, -1);
	}
	// Some outcomes include a win ID for comparision with the game's paytable.

	/// Get the mutation information for this spin
	public JSON[] getMutations()
	{
		return getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_MUTATIONS, false);
	}

	// Some outcomes include a win ID for comparision with the game's paytable.
	public int getWinId()
	{
		return getOutcomeJsonValue(JSON.getIntStatic, FIELD_WIN_ID, -1, false);
	}

	public int getFirstRoundStopID()
	{
		return getOutcomeJsonValue(JSON.getIntStatic, FIELD_ROUND_1_STOP_ID, -1, false);
	}

	// So you can get the symbol matched from an outcome.
	public string getSymbol()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_SYMBOL, "", false);
	}

	// The payline for this win.
	public string getPayLine()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_PAY_LINE, "");
	}

	// The bonus_game for this outcome.
	public string getBonusGame()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_BONUS_GAME, "");
	}

	// The bonus_game_pay_table for this outcome.
	public string getBonusGamePayTableName()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_BONUS_GAME_PAY_TABLE, "");
	}

	/// The actual pay table object for this outcome if it is valid, else null.
	public JSON getBonusGamePayTable()
	{
		ReadOnlyCollection<SlotOutcome> subs = getSubOutcomesReadOnly();
		if (subs.Count == 0)
		{
			Debug.LogError("No sub outcome found in getBonusGamePayTable()");
			return null;
		}

		string outcomeType = subs[0].getOutcomeTypeString();
		
		if (outcomeType == "wheel_pickem")
		{
			// There are no paytables of outcome type "wheel_pickem", so we have to use "wheel".
			outcomeType = "wheel";
		}

		return BonusGamePaytable.findPaytable(outcomeType, getBonusGamePayTableName());
	}
	
	public JSON[] getRounds(string rounds)
	{
		ReadOnlyCollection<SlotOutcome> subs = getSubOutcomesReadOnly();
		if (subs.Count == 0)
		{
			Debug.LogError("No sub outcome found in getBonusGamePayTable()");
			return null;
		}

		return subs[0].getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, rounds);
	}

	public JSON getNewBonusGamePayTable()
	{
		return BonusGamePaytable.findPaytable("base_bonus", outcomeJson.getString("bonus_game_pay_table", ""));
	}

	// The multiplier for this win.  Notably used for cluster wins w/ multiple ways.
	public long getCredits()
	{
		return getOutcomeJsonValue<long>(JSON.getLongStatic, FIELD_CREDITS, 0);
	}
	
	public long getOverrideCredits()
	{
		return getOutcomeJsonValue<long>(JSON.getLongStatic, FIELD_OVERRIDE_CREDITS, 0);
	}

	public long getMultiplier()
	{
		return getOutcomeJsonValue(JSON.getIntStatic, FIELD_MULTIPLIER, 1);
	}

	public long getBonusMultiplier()
	{
		long simpleMultiplierReelMultiplier = getMultiplierFromMutation();

		if (simpleMultiplierReelMultiplier > 0)
		{
			return simpleMultiplierReelMultiplier;
		}
		else
		{
			//The bonus multiplier is one less than what is should be 
			//IE: 10x = 9, 5x = 4
			//This is becuase the server assumes a defualt multiplier of 1 for all operations
			//So our bonus mulitplier needs to add in 1 for the default multiplier 
			return getOutcomeJsonValue(JSON.getIntStatic, FIELD_BONUS_MULTIPLIER, 0) + 1;
		}
	}

	// In zynga05 multiplier for the 4th bonus reel comes in the form of a mutation.
	// This looks through the mutations for a multiplier
	private long getMultiplierFromMutation()
	{
		ReelGame reelGame = ReelGame.activeGame;

		if (reelGame != null && reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
			{
				StandardMutation mutation = reelGame.mutationManager.mutations[i] as StandardMutation;

				if (mutation != null && mutation.type == "simple_multiplier_reel" && mutation.simpleMultiplierReelMultiplier > 1)
				{
					return mutation.simpleMultiplierReelMultiplier;
				}
			}
		}

		return -1;
	}

	public long getWager()
	{
		return getOutcomeJsonValue<long>(JSON.getLongStatic, FIELD_WAGER, 0);
	}

	public JSON[] getJsonPicks()
	{
		return getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_PICKS);
	}

	public JSON[] getJsonReveals()
	{
		return getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_REVEALS);
	}

	public JSON getJsonModifiers()
	{
		return getOutcomeJsonValue<JSON>(JSON.getJSONStatic, FIELD_MODIFIERS);
	}

	public string getBonusGameCreditChoiceEventID()
	{
		return getOutcomeJsonValue<string>(JSON.getStringStatic, FIELD_EVENT_ID, "");
	}
	
	/// Returns the picks array for a pickem game.
	public string[] getPicks()
	{
		return getOutcomeJsonValue<string[]>(JSON.getStringArrayStatic, FIELD_PICKS);
	}

	/// Returns the reveal array for a pickem game.
	public string[] getReveals()
	{
		return getOutcomeJsonValue<string[]>(JSON.getStringArrayStatic, FIELD_REVEALS);
	}

	/// Returns the bonus pools for games like Elvira and Lucky Elves.
	public JSON getBonusPools()
	{
		return getOutcomeJsonValue<JSON>(JSON.getJSONStatic, FIELD_BONUS_POOLS);
	}

	/// Get outcomes for tumbling
	public JSON[] getTumbleOutcomes()
	{
		return getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_TUMBLE_OUTCOMES, false);
	}

	/// Returns a list of reevaluation spins, which are a certain type of reevaluations
	public List<SlotOutcome> getReevaluationSpins()
	{
		List<SlotOutcome> reevaluationSpins = new List<SlotOutcome>();

		JSON[] reevaluations = getArrayReevaluations();

		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluationSpin = reevaluations[i];
				int[] reevaluatedIntArray = reevaluationSpin.getIntArray(FIELD_REEVALUATED_REEL_STOPS);
				if (reevaluatedIntArray != null && reevaluatedIntArray.Length > 0)
				{
					SlotOutcome spinOutcome = new SlotOutcome(reevaluationSpin);
					spinOutcome.setParentOutcome(this);
					reevaluationSpins.Add(spinOutcome);
				}
			}
		}

		return reevaluationSpins;
	}

	public JSON[] getBonusGameRounds()
	{
		return outcomeJson.getJsonArray("rounds");;
	}

	/// Returns the feature symbol used for a special game feature if one exists at the root level of the outcome, if you want to get one in reevaluations
	/// use getReevaluationFeatureSymbols() which will return a list of each feature symbol in each reevaluation
	public string getFeatureSymbol()
	{
		return getOutcomeJsonValue<string>(JSON.getStringStatic, FIELD_FEATURE_SYMBOL, "", false);
	}

		public string getBoard()
	{
		return getOutcomeJsonValue<string>(JSON.getStringStatic, FIELD_BOARD, "", false);
	}
	
	public Dictionary<string, long> getWords()
	{
		JSON[] words = getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_WORDS, false);
		
		Dictionary<string, long> dict = new Dictionary<string, long>();
		
		foreach (JSON curWord in words)
		{
			string word = curWord.getString(FIELD_WORD, "");
			long credits = curWord.getLong(FIELD_CREDITS, 0);
			
			if (!dict.ContainsKey(word))
			{
				dict.Add(word, credits);
			}
			else
			{
				Debug.LogError("A word with the key '" + word + "' already exists!'");
			}
		}
		
		return dict;
	}

	/// Grabs a list of every feature symbol entry for each reevaluation
	public List<string> getReevaluationFeatureSymbols()
	{
		List<string> featureSymbolList = new List<string>();

		JSON[] reevaluations = getArrayReevaluations();

		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				string featureSymbol = reevaluation.getString(FIELD_FEATURE_SYMBOL, "");
				if (featureSymbol != null && featureSymbol != "")
				{
					featureSymbolList.Add(featureSymbol);
				}
			}
		}

		return featureSymbolList;
	}

	/// Returns the reevaluations related to bonus pools for games like Elvira and Lucky Elves.
	public JSON getReevaluations()
	{
		return getOutcomeJsonValue<JSON>(JSON.getJSONStatic, FIELD_REEVALUATIONS, false);
	}

	// Returns the reevaluations related to bonus pools for games like Shark01. Yep it's diferent than Elvira and Lucky Elves games.
	public JSON[] getArrayReevaluations()
	{
		return getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_REEVALUATIONS, false);
	}

	// moves the "delayed_reevaluations" data into the "reevaluations" array. First used on orig012 to allow for
	// having SNW and FS data executed out of the traditional order of running reevaluations immediately which 
	// prevented us from running an intermediary picking game and then FS + SNW, etc.
	public bool moveDelayedReevaluationsIntoReevaluations(SlotOutcome slotOutcome, object originalReevals, bool includeBonusGames = true, bool includedMovedReevals = true)
	{
		object reevaluations = null;
		if (originalReevals == null)
		{
			slotOutcome.outcomeJson.jsonDict.TryGetValue("reevaluations", out reevaluations);
		}
		else
		{
			reevaluations = originalReevals;
		}

		if (!(reevaluations is List<object> reevaluationsList) || reevaluationsList.Count <= 0)
		{
			return false;
		}

		List<object> newReevaluationsList = new List<object>();
		object delayedReevaluations = null;
		bool hasBonusGames = false;
		
		//add back in the existing reevaluations, excluding the original "delayed_reevaluations" source block
		foreach(object reevaluationListEntry in reevaluationsList)
		{
			if (!(reevaluationListEntry is JsonDictionary jsonDict))
			{
				continue;
			}
			
			//discard the original "delayed_reevaluations" block
			if (jsonDict.ContainsKey("delayed_reevaluations"))
			{
				delayedReevaluations = reevaluationListEntry;
				continue;
			}

			if (jsonDict.ContainsKey("bonus_games"))
			{
				if (!includeBonusGames)
				{
					continue;
				}
				hasBonusGames = true;
			}

			newReevaluationsList.Add(reevaluationListEntry);
		}


		if (delayedReevaluations != null && includedMovedReevals)
		{
			List<object> delayedReevaluationsList = (delayedReevaluations as JsonDictionary)["delayed_reevaluations"] as List<object>;
			
			for(int i = 0; i < delayedReevaluationsList.Count; ++i)	
			{
				newReevaluationsList.Add(delayedReevaluationsList[i]);	
			}
		}

		slotOutcome.outcomeJson.jsonDict["reevaluations"] = newReevaluationsList;

		return hasBonusGames;
	}

	// Tells if this outcome had reevaluation bonuses, in which case special handling needs to be done when coming back to
	// the base game from the bonus to ensure those are awarded like the suboutcome bonuses are
	public bool isOutcomeWithReevaluationBonusGames()
	{
		JSON[] reevals = getArrayReevaluations();
		for (int i = 0; i < reevals.Length; i++)
		{
			JSON[] bonusGamesJSON = reevals[i].getJsonArray("bonus_games");

			if (bonusGamesJSON.Length > 0)
			{
				return true;
			}
		}

		return false;
	}

	// Getter for the raw JSON object, made available to handle non-general cases.
	public JSON getJsonObject()
	{
		return outcomeJson;
	}

	// Debug logger to make it easier to see what the backend is doing.
	public void printOutcome(bool isPrintingRawJson = true)
	{
#if UNITY_EDITOR
		// This is additionally wrapped in a UNITY_EDITOR so that the debugLogOutcome() never
		// executes on actual builds. This is because it involves lots of large string manipulation.
		if(!UnityEngine.Profiling.Profiler.enabled)
		{
			if (isPrintingRawJson)
			{
				debugLogRawJsonOutcome();
			}
			else
			{
				Glb.editorLog(debugLogOutcome());
			}
		}
#endif
	}
	
	public int getRoundStop(int roundStopNumber)
	{
		return getOutcomeJsonValue(JSON.getIntStatic, "round_" + roundStopNumber + "_stop_id", -1);
	}
	
	public int getParameter()
	{
		return getOutcomeJsonValue(JSON.getIntStatic, FIELD_PARAMETER, 0);
	}

	public int getNumberOfFreespinsOverride()
	{
		return getOutcomeJsonValue(JSON.getIntStatic, FIELD_NUMBER_OF_FREESPINS_OVERRIDE, 0);
	}

	/// Gets a value that allows for a pay table swap for an outcome
	public string getNewPayTable()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_NEW_PAY_TABLE, "");
	}

	/// Gets a value that allows for a pay_table to be looked up.
	public string getPayTable()
	{
		return getOutcomeJsonValue(JSON.getStringStatic, FIELD_PAY_TABLE, "");
	}

	// Gets the largest bet multiplier which is part of this outcome, should probably mostly be used from the root
	// outcome so everything is checked.  Will be used for rollups to ensure that the timing value they use isn't
	// huge due to having a large multiplier override involved in the outcome on a spin where the player bet low.
	public long getLargestBetMultiplierOverrideInOutcome()
	{
		long currentMultiplier = getOutcomeJsonValue<long>(JSON.getLongStatic, FIELD_BET_MULTIPLIER, -1);

		List<SlotOutcome> reevaluationList = getReevaluationsAsSlotOutcomes();
		if (reevaluationList.Count > 0)
		{
			currentMultiplier = getLargestBetMultiplierOverrideFromOutcomeList(new ReadOnlyCollection<SlotOutcome>(reevaluationList), currentMultiplier);
		}

		ReadOnlyCollection<SlotOutcome> subOutcomes = getSubOutcomesReadOnly();
		if (subOutcomes.Count > 0)
		{
			currentMultiplier = getLargestBetMultiplierOverrideFromOutcomeList(subOutcomes, currentMultiplier);
		}

		return currentMultiplier;
	}

	// Help function for use in conjunction with getLargestBetMultiplierOverrideInOutcome() in order to determine
	// what the largest bet multiplier that is applied to an outcome is.
	private long getLargestBetMultiplierOverrideFromOutcomeList(ReadOnlyCollection<SlotOutcome> slotOutcomes, long currentMultiplier)
	{
		for (int i = 0; i < slotOutcomes.Count; i++)
		{
			long multiplierForOutcome = slotOutcomes[i].getLargestBetMultiplierOverrideInOutcome();

			if (multiplierForOutcome > 0 && multiplierForOutcome > currentMultiplier)
			{
				currentMultiplier = multiplierForOutcome;
			}
		}

		return currentMultiplier;
	}

	/// Get the bet multiplier for this outcome which will be used as an override in place of all other multipliers
	public long getBetMultiplierOverride()
	{
		return getOutcomeJsonValue<long>(JSON.getLongStatic, FIELD_BET_MULTIPLIER, -1);
	}

	// Get the award multiplier, this is used to represent the variable multiplier that will apply to a feature like munsters01 tug of war
	public long getAwardMultiplier()
	{
		return getOutcomeJsonValue<long>(JSON.getLongStatic, FIELD_AWARD_MULTIPLIER, -1);
	}
	
	public long getAllLadderRungsLandedMultiplier()
	{
		return getOutcomeJsonValue<long>(JSON.getLongStatic, FIELD_AWARD_MULTIPLIER, -1);
	}
	
	public long getLadderLoopMultiplier()
	{
		return getOutcomeJsonValue<long>(JSON.getLongStatic, FIELD_AWARD_MULTIPLIER, -1);
	}

	// Just log the raw json instead of what debugLogOutcome does,
	// it is easier to read this way
	private void debugLogRawJsonOutcome()
	{
		Debug.Log("SlotOutcome.outcomeJson:\n" + outcomeJson.ToString());
	}

	/// debugLogOutcome - recursive step in the printOutcome process.
	///   depth - number of outcomes "deep" we are.  This is an internal value for recursion processing.
	private string debugLogOutcome(int depth = 0)
	{
		string prefix = "";
		for (int prefixCount = 0; prefixCount < depth; prefixCount++)
		{
			prefix += "  ";
		}

		string debugText = "";

		// Dump out all the outcome keys, to make it easy to tell what's in there.
		bool commaDelimiter = false;
		debugText += prefix + "Raw JSON Keys: ";
		foreach (string key in outcomeJson.getKeyList())
		{
			if (commaDelimiter)
			{
				debugText += ", ";
			}
			else
			{
				commaDelimiter = true;
			}
			debugText += key;
		}
		debugText += "\n";

		// After the raw text has been pumped out, bump up the prefix to make things easier to see grouped visually.
		prefix += "  ";

		// Base type, if present.
		string type = getType();
		if (type != "")
		{
			debugText += prefix + "Type: " + type + "\n";
		}

		// Outcome type, if present.
		string outcomeType = getOutcomeTypeString();
		if (outcomeType != "")
		{
			debugText += prefix + "Outcome Type: " + outcomeType + "\n";
		}

		// Win ID, if present.
		int winId = getWinId();
		if (winId != -1)
		{
			debugText += prefix + "Win ID: " + winId.ToString() + "\n";
		}

		// Pay line, if present.
		string payline = getPayLine();
		if (payline != "")
		{
			debugText += prefix + "Payline: " + payline + "\n";
		}

		int[] landedReels = getLandedReels();
		if (landedReels != null && landedReels.Length > 0)
		{
			commaDelimiter = false;
			debugText += prefix + "landedReels: ";
			foreach (int landedReel in landedReels)
			{
				if (commaDelimiter)
				{
					debugText += ",";
				}
				else
				{
					commaDelimiter = true;
				}
				debugText += landedReel.ToString();
			}
			debugText += "\n";
		}

		long multiplier = getMultiplier();
		if (multiplier != -1)
		{
			debugText += prefix + "Multiplier: " + multiplier.ToString() + "\n";
		}

		long credits = getCredits();
		if (credits != -1)
		{
			debugText += prefix + "Credits: " + credits.ToString() + "\n";
		}

		int freeSpins = getFreeSpins();
		if (freeSpins != -1)
		{
			debugText += prefix + "FreeSpins: " + freeSpins.ToString() + "\n";
		}

		// Bonus Game, if present.
		string bonusGame = getBonusGame();
		if (bonusGame != "")
		{
			debugText += prefix + "Bonus Game: " + bonusGame + "\n";
		}

		// Bonus Game Pay Table, if present.
		string bonusGamePayTable = getBonusGamePayTableName();
		if (bonusGamePayTable != "")
		{
			debugText += prefix + "Bonus Game Pay Table: " + bonusGamePayTable + "\n";
		}

		// Replacement Reel Strips
		Dictionary<int, string> reelStrips = getReelStrips();
		if (reelStrips.Count > 0)
		{
			commaDelimiter = false;
			debugText += prefix + "Reel Strips: ";
			foreach (KeyValuePair<int,string> kvp in reelStrips)
			{
				if (commaDelimiter)
				{
					debugText += ", ";
				}
				else
				{
					commaDelimiter = true;
				}
				debugText += kvp.Key.ToString() + ":" + kvp.Value.ToString();
			}
			debugText += "\n";
		}

		// Reel Stops
		int[] reelStops = getReelStops();
		if (reelStops != null && reelStops.Length > 0)
		{
			commaDelimiter = false;
			debugText += prefix + "Reel Stops: ";
			foreach (int reelStop in reelStops)
			{
				if (commaDelimiter)
				{
					debugText += ",";
				}
				else
				{
					commaDelimiter = true;
				}
				debugText += reelStop.ToString();
			}
			debugText += "\n";
		}

		// Anticipation Info
		Dictionary<int,Dictionary<string,int>> anticipationTriggers = getAnticipationTriggers();
		if (anticipationTriggers != null && anticipationTriggers.Count > 0)
		{
			commaDelimiter = false;
			debugText += prefix + "Anticipation Info: ";
			foreach (KeyValuePair<int, Dictionary<string,int>> kvp in anticipationTriggers)
			{
				if (commaDelimiter)
				{
					debugText += ",";
				}
				else
				{
					commaDelimiter = true;
				}
				int reel,starting_cell,height,width;
				kvp.Value.TryGetValue("reel",out reel);
				kvp.Value.TryGetValue("starting_cell",out starting_cell);
				kvp.Value.TryGetValue("height",out height);
				kvp.Value.TryGetValue("width",out width);
				debugText += string.Format("{0}:{{Reel = {1}, starting_cell = {2}, height = {3}, width = {4}}} ",
					kvp.Key,reel,starting_cell,height,width);
			}
			debugText += "\n";


		}

		ReadOnlyCollection<SlotOutcome> outcomes = getSubOutcomesReadOnly();
		if (outcomes != null && outcomes.Count > 0)
		{
			debugText += prefix + "Outcomes:\n";
			foreach(SlotOutcome outcome in outcomes)
			{
				debugText += outcome.debugLogOutcome(depth + 1);
			}
		}

		return debugText;
	}

	/// Tells if an outcome is using linked reels
	public bool containsLinkedReels()
	{
		HashSet<int> linkedReelSet = getLinkedReels();

		if (linkedReelSet != null)
		{
			return linkedReelSet.Count > 0;
		}
		else
		{
			return false;
		}
	}

	public JSON[] getReelInfo()
	{
		return getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_REEL_INFO);
	}

	/// Gets information about possible linked reels, which means that reels spin together
	public HashSet<int> getLinkedReels()
	{
		HashSet<int> linkedReelSet = new HashSet<int>();
		foreach (int reelIndex in getLinkedReelsAsArray())
		{
			linkedReelSet.Add(reelIndex);
		}
		return linkedReelSet;
	}

	// Gets a JSON which contain extra data that isn't normally part of an outcome,
	// such as progressive jackpot win info like in elvis03
	public JSON getExtrasJson()
	{
		return getOutcomeJsonValue<JSON>(JSON.getJSONStatic, FIELD_EXTRAS);
	}

	// Gets progressive jackpot win information for game features that can award one
	// such as elvis03 freespins. (For elvis03 this data is stored in the base game outcome)
	public JSON getProgressiveJackpotWinJson()
	{
		JSON extrasJSON = getExtrasJson();
		if (extrasJSON != null)
		{
			JSON progJackpotWonJson = extrasJSON.getJSON(FIELD_PROGRESSIVE_JACKPOT_WON);
			if (progJackpotWonJson != null)
			{
				return progJackpotWonJson;
			}
		}

		// no progressive jackpot won data present
		return null;
	}

	public int [] getLinkedReelsAsArray()
	{
		// @todo : checking for the old version of the data, someday hopefully we can remove this and just use the int[] version only
		JSON linkedReelJson = getOutcomeJsonValue<JSON>(JSON.getJSONStatic, FIELD_LINKED_REELS);
		if (linkedReelJson != null)
		{
			List<string> linkedKeyList = linkedReelJson.getKeyList();
			int [] linkedReelSet = new int[linkedKeyList.Count];
			for (int i = 0; i < linkedKeyList.Count; i++)
			{
				int intKey = 0;
				if (int.TryParse(linkedKeyList[i], out intKey))
				{
					linkedReelSet[i] = intKey;
				}
				else
				{
					Debug.LogError("SlotOutCome.getLinkedReels(): key is not an int as expected.");
				}
			}
			return linkedReelSet;
		}

		return getOutcomeJsonValue<int[]>(JSON.getIntArrayStatic, FIELD_LINKED_REELS);
	}

	/// Tells if this outcome contains sticky symbol information
	public bool hasStickySymbols()
	{
		JSON[] stickyLocations = getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_STICKY_SYMBOLS, false);
		
		if (stickyLocations != null && stickyLocations.Length > 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/// Get the sticky symbol information for this outcome.  
	/// Right now only a reevaluated spin can have sticky symbols since it is the next 
	/// reevaluated spin which can win off those stuck symbols.
	public Dictionary<int, Dictionary<int, string>> getStickySymbols()
	{
		JSON[] stickyLocations = getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_STICKY_SYMBOLS, false);
		
		if (stickyLocations != null && stickyLocations.Length > 0)
		{
			Dictionary<int, Dictionary<int, string>> stickySymbols = new Dictionary<int, Dictionary<int, string>>();

			foreach (JSON stickySymbolLoc in stickyLocations)
			{
				int column = System.Convert.ToInt32(stickySymbolLoc.getInt(PROPERTY_STICKY_SYMBOLS_COLUMN, 0));
				int row = System.Convert.ToInt32(stickySymbolLoc.getInt(PROPERTY_STICKY_SYMBOLS_ROW, 0));

				if (!stickySymbols.ContainsKey(column))
				{
					stickySymbols.Add(column, new Dictionary<int, string>());
				}

				stickySymbols[column].Add(row, stickySymbolLoc.getString(PROPERTY_STICK_SYMBOLS_NAME, ""));
			}

			return stickySymbols;
		}
		else
		{
			return new Dictionary<int, Dictionary<int, string>>();
		}
	}

	/// Get the sticky scatter symbol information for this outcome.  
	/// Right now only a reevaluated spin can have sticky symbols since it is the next 
	/// reevaluated spin which can win off those stuck symbols.
	public Dictionary<int, Dictionary<int, string>> getStickySCSymbols()
	{
		JSON[] stickyLocations = getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_SC_STICKY_SYMBOLS, false);
		
		if (stickyLocations != null && stickyLocations.Length > 0)
		{
			Dictionary<int, Dictionary<int, string>> stickySymbols = new Dictionary<int, Dictionary<int, string>>();

			foreach (JSON stickySymbolLoc in stickyLocations)
			{
				int column = System.Convert.ToInt32(stickySymbolLoc.getInt(PROPERTY_STICKY_SYMBOLS_COLUMN, 0));
				int row = System.Convert.ToInt32(stickySymbolLoc.getInt(PROPERTY_STICKY_SYMBOLS_ROW, 0));

				if (!stickySymbols.ContainsKey(column))
				{
					stickySymbols.Add(column, new Dictionary<int, string>());
				}

				stickySymbols[column].Add(row, stickySymbolLoc.getString(PROPERTY_STICK_SYMBOLS_NAME, ""));
			}

			return stickySymbols;
		}
		else
		{
			return new Dictionary<int, Dictionary<int, string>>();
		}
	}

	/// Gets the reel start index of the spotlight (1 based)
	public int getSpotlightReelStartIndex()
	{
		foreach (JSON reevaluationJson in getArrayReevaluations())
		{
			if (reevaluationJson.hasKey(FIELD_ACTIVE_REEL_START_INDEX))
			{
				return reevaluationJson.getInt(FIELD_ACTIVE_REEL_START_INDEX, 1);
			}
		}
		return 1;
	}

	/// Returns whether this outcome has a bonus game.
	public bool hasBonusGame()
	{
		ReadOnlyCollection<SlotOutcome> subOutcomeList = getSubOutcomesReadOnly();
		foreach (SlotOutcome subOutcome in subOutcomeList)
		{
			if (subOutcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.BONUS_GAME)
			{
				return true;
			}
		}

		if (hasQueuedBonuses)
		{
			return true;
		}

		return false;
	}

	public int getNumberOfVisibleReels()
	{
		string paylineSet = getPayLineSet();
		if (paylineSet == null)
		{
			Debug.LogError("No payline set found!");
			return -1;
		}
		if (paylineSet.Contains('x'))
		{
			string[] substrings = paylineSet.Split('x');
			if (substrings.Length >= 2)
			{
				return System.Convert.ToInt32(char.GetNumericValue(substrings[1].ToCharArray()[0]));
			}
			else
			{
				Debug.LogErrorFormat("Unexpected format of payline set: {0}. Does not contain any characters after 'x'", paylineSet);
			}
		}
		else
		{
			Debug.LogErrorFormat("Unexpected format of payline set {0}. Does not contain the character 'x'", paylineSet);
		}


		return -1;
	}

	// get personal jackpot outcome data as a JSON[]
	public JSON[] getPersonalJackpotJSONArray()
	{
		JSON[] jackpots = getOutcomeJsonValue(JSON.getJsonArrayStatic, FIELD_PERSONAL_JACKPOT_OUTCOME_LIST);
		if (jackpots != null && jackpots.Length > 0)
		{
			return jackpots;
		}

		// also support single jackpot outcomes to be compatible with older games
		JSON jackpot = getOutcomeJsonValue(JSON.getJSONStatic, FIELD_PERSONAL_JACKPOT_OUTCOME);
		if (jackpot != null)
		{
			return new JSON[] { jackpot };
		}
		
		// Need to recursively search child outcomes if we don't find it in this one
		// since it might be nested lower down, like in wheel game bonuses where it
		// is nested in the wheel outcome
		if (hasSubOutcomes())
		{
			foreach (SlotOutcome subSlotOutcome in getSubOutcomesReadOnly())
			{
				JSON[] childArray = subSlotOutcome.getPersonalJackpotJSONArray();
				if (childArray != null)
				{
					return childArray;
				}
			}
		}

		return null;
	}

	public long getPersonalJackpotCredits()
	{
		return getValueFromJSONObjectDepthFirst<long>(JSON.getLongStatic, this, FIELD_PERSONAL_JACKPOT_OUTCOME,
			FIELD_CREDITS, 0, true);
	}

	public string getPersonalJackpotKey()
	{
		return getValueFromJSONObjectDepthFirst<string>(JSON.getStringStatic, this, FIELD_PERSONAL_JACKPOT_OUTCOME,
			FIELD_JACKPOT_KEY, "", true);
	}

	public long getProgressiveJackpotCredits()
	{
		return getValueFromJSONObjectDepthFirst<long>(JSON.getLongStatic, this, FIELD_PROGRESSIVE_JACKPOT_OUTCOME,
			FIELD_RUNNING_TOTAL, 0, true);
	}

	// Checks if the data coming from the server for override symbols is in the expected
	// format and logs error if not.  Will return True if the JSON is valid, and false if any
	// part of the JSON was invalid.
	private static bool isOverrideSymbolJsonValid(JSON[] overrideSymbolEntries)
	{
		bool isValid = true;
		for (int i = 0; i < overrideSymbolEntries.Length; i++)
		{
			JSON entryJson = overrideSymbolEntries[i];
			isValid &= entryJson.validateHasKey("reel");
			isValid &= entryJson.validateHasKey("position");
			isValid &= entryJson.validateHasKey("layer");
			isValid &= entryJson.validateHasKey("reel_strip_index");
			isValid &= entryJson.validateHasKey("from_symbol");
			isValid &= entryJson.validateHasKey("to_symbol");
		}

		return isValid;
	}

	// Grab an array of OverrideSymboleData that can be applied to the game reels
	public OverrideSymbolData[] getOverrideSymbols()
	{
		JSON[] overrideSymbolEntries = getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, FIELD_OVERRIDE_SYMBOLS, false);
		
		// Not going to block using the data even if it is invalid for now. Although if the data is busted
		// enough it might be just as bad as just ignoring and not trying to use it.  Neither solution is great
		// but we should have error logs about what fields are missing if something starts acting strange.
		isOverrideSymbolJsonValid(overrideSymbolEntries);
		
		if (overrideSymbolEntries.Length > 0)
		{
			OverrideSymbolData[] output = new OverrideSymbolData[overrideSymbolEntries.Length];

			for (int i = 0; i < overrideSymbolEntries.Length; i++)
			{
				JSON entryJson = overrideSymbolEntries[i];
				OverrideSymbolData entry = new OverrideSymbolData();
				
				entry.reel = entryJson.getInt("reel", 0);
				entry.position = entryJson.getInt("position", 0);
				entry.layer = entryJson.getInt("layer", 0);
				entry.reelStripIndex = entryJson.getInt("reel_strip_index", 0);
				entry.fromSymbol = entryJson.getString("from_symbol", "");
				entry.toSymbol = entryJson.getString("to_symbol", "");

				output[i] = entry;
			}

			return output;
		}
		else
		{
			return null;
		}
	}
	
	// Static version of getBonusGameInOutcomeDepthFirst() intended for classes that don't need
	// to use a SlotOutcome directly, but would like to use that function in order to grab bonus
	// outcomes nested inside a standard outcome structure.
	public static SlotOutcome getBonusGameInOutcomeDepthFirstFromJson(JSON outcomeJson)
	{
		SlotOutcome outcome = new SlotOutcome(outcomeJson);
		return outcome.getBonusGameInOutcomeDepthFirst();
	}

	// Recursively check through subOutcomes depth first to see if there
	// is any addition bonus games that still need to be played
	public SlotOutcome getBonusGameInOutcomeDepthFirst()
	{
		return SlotOutcome.getBonusGameInOutcomeDepthFirst(this, this);
	}

	// Recursively check through subOutcomes depth first to see if there
	// is any addition bonus games that still need to be played
	private static SlotOutcome getBonusGameInOutcomeDepthFirst(SlotOutcome rootOutcome, SlotOutcome currentOutcome)
	{
		if (currentOutcome == null)
		{
			return null;
		}

		// note we don't want to return our self as bonus game, so check it is not this.
		if ((currentOutcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.BONUS_GAME || currentOutcome.isBonusGameChoice()) && currentOutcome != rootOutcome)
		{
			// com games have an extra bonus which isn't used on the client and seems to
			// be included because it was a step that the server took to determine what
			// to use when making the bonus outcome we care about.  So we will just ignore
			// the bonus based on name
			string bonusGameName = currentOutcome.getBonusGame();
			if (bonusGameName != "com_common")
			{
				return currentOutcome;
			}
		}

		if (currentOutcome.hasSubOutcomes())
		{
			foreach (SlotOutcome subOutcome in currentOutcome.getSubOutcomesReadOnly())
			{
				SlotOutcome bonusGameSlotOutcome = getBonusGameInOutcomeDepthFirst(rootOutcome, subOutcome);
				if (bonusGameSlotOutcome != null)
				{
					return bonusGameSlotOutcome;
				}
			}
		}

		return null;
	}

	/// Get data from within the JSON of an Outcome, checks the current JSON and
	/// moves to parent if it can't find it, will default to passed in default value
	/// if the value doesn't exist
	public T getOutcomeJsonValue<T>(Func<JSON, string, T, T> jsonGetterFunc, string valueKey, T defaultVal, bool isRecursive = true)
	{
		if (outcomeJson.hasKey(valueKey))
		{
			return jsonGetterFunc(outcomeJson, valueKey, defaultVal);
		}
		else if(isRecursive && parentOutcome != null)
		{
			return parentOutcome.getOutcomeJsonValue(jsonGetterFunc, valueKey, defaultVal);
		}
		else
		{
			return defaultVal;
		}
	}

	/// Get data from within the JSON of an Outcome, checks the current JSON and
	/// moves to parent if it can't find it, this version used for Types without default values
	public T getOutcomeJsonValue<T>(Func<JSON, string, T> jsonGetterFunc, string valueKey, bool isRecursive = true)
	{		
		if (outcomeJson.hasKey(valueKey))
		{
			return jsonGetterFunc(outcomeJson, valueKey);
		}
		else if(isRecursive && parentOutcome != null)
		{
			return parentOutcome.getOutcomeJsonValue(jsonGetterFunc, valueKey);
		}
		else
		{
			// assuming the function itself will return a good default value
			return jsonGetterFunc(outcomeJson, valueKey);
		}
	}

	// This method recurses into a slotOutcome and its subOutcomes to find a jsonObject with jsonObjectKey and extract
	// a value from that using jsonValueKey.
	public T getValueFromJSONObjectDepthFirst<T>(Func<JSON, string, T, T> jsonGetterFunc, SlotOutcome slotOutcome, string jsonObjectKey, string jsonValueKey, T defaultVal, bool isRecursive = false)
	{
		// This is the json object we are looking for, extract the value from the jsonValueKey
		if (slotOutcome.outcomeJson.hasKey(jsonObjectKey))
		{
			return jsonGetterFunc(slotOutcome.outcomeJson.getJSON(jsonObjectKey), jsonValueKey, defaultVal);
		}

		// Check in the suboutcomes to find the json object we need.
		if (isRecursive && slotOutcome.hasSubOutcomes())
		{
			foreach (SlotOutcome subSlotOutcome in slotOutcome.getSubOutcomesReadOnly())
			{
				T returnValue = getValueFromJSONObjectDepthFirst<T>(jsonGetterFunc, subSlotOutcome, jsonObjectKey, jsonValueKey, defaultVal, isRecursive);
				if (!EqualityComparer<T>.Default.Equals(returnValue , defaultVal))
				{
					return returnValue;
				}
			}
		}

		return defaultVal;
	}

	public JSON[] getPickRewardables()
	{
		return outcomeJson.getJsonArray("rewardables");
	}
}
