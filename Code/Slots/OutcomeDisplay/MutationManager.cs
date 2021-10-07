using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Holds information about the mutations that belong to a particular game.
*/

public class MutationManager
{
	public bool isLingering = false;	/// Some Free Spins games use lingering mutations, where a mutation from a spin is valid for followup spins as well.
	public bool preSpinMutationsReady = false;   /// Certain mutations need to be shown during the spin, and before the reels stop.

	public const string WORD_MUTATION_JSON_KEY = "word_freespin";
	public const string PAPERFOLD_MUTATION_JSON_KEY = "multi_reel_paper_fold_wild_transfer";
	public const string INDEPENDANT_REEL_EXPANSION_MUTATION_JSON_KEY = "contract_on_checkpoint";
	public const string SYMBOL_TRIGGER_REEL_REPLACEMENT_JSON_KEY = "symbol_trigger_reel_replacement";
	public const string BINGO_MUTATION_JSON_KEY = "bingo";
	public const string MULTI_ROUND_SYMBOLS_TRANSFORM_MUTATION_JSON_KEY = "multi_round_symbols_transform"; 

	public const string TUMBLE_MULTIPLIER_JSON_KEY = "tumble_multiplier";

	public List<MutationBase> mutations = new List<MutationBase>();

	public MutationManager(bool isLingering)
	{
		this.isLingering = isLingering;
	}

	public MutationBase getMutation(string type)
	{
		foreach(MutationBase mutation in mutations)
		{
			if (mutation.type == type)
			{
				return mutation;
			}
		}

		return null;
	}

	public void setMutationsFromOutcome(JSON outcomeJson, bool inReevaluation = false)
	{
		preSpinMutationsReady = false;
		
		if (!isLingering)
		{
			mutations.Clear();
		}

		string mutationKey = "mutations";

		if (inReevaluation)
		{
			mutationKey = "reevaluations";
		}
		
		// Define mutations, if any.
		if (outcomeJson.hasKey(mutationKey))
		{
			addMutationDataToMutationsList(outcomeJson.getJsonArray(mutationKey));
		}
		
		// If this is a Multigame we need to check the games section, because each game
		// might have mutations.  So search to see if we have a Multigame games section
		// and then check that for mutaitons.
		JSON[] reevaluations = outcomeJson.getJsonArray("reevaluations");
		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				JSON[] multiGamesData = reevaluation.getJsonArray("games");
				if (multiGamesData != null)
				{
					for (int k = 0; k < multiGamesData.Length; k++)
					{
						if (multiGamesData[k].hasKey("mutations"))
						{
							addMutationDataToMutationsList(multiGamesData[k].getJsonArray("mutations"));
						}
					}
				}
			}
		}
	}
	
	// Using the passed JSON array of mutations, build and populate the mutations list of MutationsManager
	private void addMutationDataToMutationsList(JSON[] mutationsDataArray)
	{
		MutationBase mutation;
		JSON mutationJson;
		for (int i = 0; i < mutationsDataArray.Length; i++)
		{
			mutationJson = mutationsDataArray[i];
			mutation = createMutation(mutationJson);
			mutations.Add(mutation);
			StandardMutation standardMutation = mutation as StandardMutation;
			if (standardMutation != null)
			{
				if (standardMutation.isPreEndMutation())
				{
					preSpinMutationsReady = true;
				}
			}
		}
	}

	public MutationBase createMutation(JSON mutationJson)
	{
		string type = mutationJson.getString("type", "");
		switch (type)
		{
			case BINGO_MUTATION_JSON_KEY:
				return new BingoBoardMutation(mutationJson);

			case WORD_MUTATION_JSON_KEY:
				return new WordMutation(mutationJson);

			case PAPERFOLD_MUTATION_JSON_KEY:
				return new PaperFoldMutation(mutationJson);
				
			case SYMBOL_TRIGGER_REEL_REPLACEMENT_JSON_KEY:
				return new SymbolTriggerReelReplacementMutation(mutationJson);

			case TUMBLE_MULTIPLIER_JSON_KEY:
				return new MutationTumbleMultiplier(mutationJson);
			
			case MULTI_ROUND_SYMBOLS_TRANSFORM_MUTATION_JSON_KEY:
				return new MultiRoundTransformingSymbolsMutation(mutationJson);

			default:
				return new StandardMutation(mutationJson);
		}
	}
}

/**
Polymorphic base class for all types of mutations, anything using mutations will have to convert to the mutation type they are expecting
*/
public class MutationBase
{
	public string type; // the type id for this mutation

	public MutationBase(JSON mutation)
	{
		type = mutation.getString("type", "");
	}
}

/**
Simple class for data structure.
*/
public class StandardMutation : MutationBase
{
	public int[] reels;
	public string symbol;
	public int topLeftRowIndex = -1;
	public int topLeftColumnIndex = 9999;
	public int totalNumberOfMutations = 0;

	// nudging_wild extra data field
	public string test = "";


	// used in new mutation types
	public int[][] mutatedReels;
	public string[] mutatedSymbols;
	public bool[] isLockingMutatedReel;
	
	public Dictionary<string,string> normalReplacementSymbolMap = null;
	public Dictionary<string,string> megaReplacementSymbolMap = null;
	
	// Below are for single Symbol Replacements, ala T1
	public Dictionary<int, int[]> singleSymbolLocations = null;
	public string replaceSymbol = null;
	
	// Used in win/loss battle games, ala com03 free spins
	public bool didWin = false;
	public List<Reveal> reveals = null;
	
	// nudging_wild extra data field
	public string excludeSymbolKey = "";

	// Used in hi03 and gwtw01
	public List<ReelToReelMutation> fromMutations;
	public List<ReelToReelMutation> toMutations;

	//Used in wonka01
	public int[] featureReels;
	public int[] reelsToMutate;
	public string mutateToSymbol;

	// Used in pawn01
	public string majorRPSymbol;

	public bool isTWmutation = false; //set this to we can easily find the TW mutation in the MutationManager.mutations list 
	public string[,] triggerSymbolNames = new string[10,10];// Max of 10 for now.
	public string triggerSymbolName = null; // single trigger symbol for all reels

	// New data for doing TW, hopefully future games will prefer using this format
	public List<ReplacementCell> twTriggeredSymbolList;
	public List<ReplacementCell> twMutatedSymbolList;

	// New data for doing TW, hopefully future games will prefer using this format
	public List<List<ReplacementCell>> leftRightWildMutateSymbolList;

	// replacement cells that are mapped to a trigger symbol
	public Dictionary<string, List<ReplacementCell>> symbolReplacementCells;

	// Data used for sticky symbols that are moving each spin, see VerticalMovingStickySymbolModule.cs for an example
	public List<ReplacementCell> movingStickySymbolList;
	
	// Used in mutations that need some sort of wheel result
	public int wheelWinAmount = -1;

	// Used by ainsworth
	public int numberOfFreeSpinsAwarded = 0;
	public long creditsAwarded = 0;
	public int creditsMultiplier = 0;
	public int numberOfSymbolsCollected = 0;
	public int accumulatedMulitpler = 1;  //Ainsworth11 FS

	public List<ProgressiveScatterJackpot> progressiveScatterJackpots;

	// Used by zynga05
	public int simpleMultiplierReelMultiplier = -1;

	public string[] reelStrips;

	public string jackpotKey;
	public class ProgressiveScatterJackpot
	{
		public long baseAmount;
		public long amountAdded;
		public long jackpotBalance;
	}
	public class JackpotMeter
	{
		public string keyName;
		public int requiredPips;
		public int currentPips;
		public long baseAmount;
		public bool shouldAward;
	}

	public class Pick
	{
		public long credits = 0;
		public int multiplier = 0;
		public int freespins = 0;

		public Pick(long creditsAwarded, int multiplierAwarded, int freespinsAwarded)
		{
			credits = creditsAwarded;
			multiplier = multiplierAwarded;
			freespins = freespinsAwarded;
		}
	}

	public Pick pickSelected;
	public List<Pick> picksUnselected;

	//Used in games with a running jackpot value
	public long initialJackpotValue = 0;
	public long currentJackpotValue = 0;
	public long reinitializedJackpotValue = 0;

	// Used by Replacement Cell Wild
	public class ReplacementCell
	{
		public int reelIndex;
		public int symbolIndex;
		public string replaceSymbol;

		public ReplacementCell(int reelValue = 0, int posValue = 0, string symbol = "")
		{
			reelIndex = reelValue;
			symbolIndex = posValue;
			replaceSymbol = symbol;
		}
	}
	public List<ReplacementCell> replacementCells;
	public List<ReplacementCell> mutatedReplacementCells;
	public List<ReplacementCell> removedReplacementCells;

	public List<IndependentReelExpansionMutation.IndependentReelExpansion> symbolRandomReelExpanderData;

	public class CreditOrSpinAwardingSymbol
	{
		public string symbolServerName;
		public int reel;
		public int position;
		public long credits;
		public int spinCount;
	}

	public class FreeSpinBattleMultiplier
	{
		public string hit = "neutral";
		public int numberOfStrikes = 0;
		public long currentJackpot = 0;
		public string battleResult = "";

		public FreeSpinBattleMultiplier()
		{

		}
	}

	public FreeSpinBattleMultiplier freeSpinBattleMutliplierData;
	public Dictionary<string, long> scatterPayoutInformation;
	public List<CreditOrSpinAwardingSymbol> symbolLandingAwardCreditSymbols = new List<CreditOrSpinAwardingSymbol>();
	public List<CreditOrSpinAwardingSymbol> symbolLandingAwardSpinsSymbols = new List<CreditOrSpinAwardingSymbol>();
	public Dictionary<string, JackpotMeter> jackpotMeters = new Dictionary<string, JackpotMeter>();

	public bool bonusReplacementActive = false;
	public bool bonusReplacementTriggered = false;

	public StandardMutation(JSON mutation) : base(mutation)
	{
		reels = mutation.getIntArray("reels");
		symbol = mutation.getString("symbol", "");
		replaceSymbol = mutation.getString("replace_symbol", "");
		majorRPSymbol = mutation.getString("major_rp_symbol", "");

		Debug.Log("Mutation of type " + type + " is found");

		if (type == "matrix_cell_replacement" || type == "matrix_block_replacement")
		{
			singleSymbolLocations = new Dictionary<int, int[]>();
			JSON cellPositions = mutation.getJSON("cell_positions");
			if (cellPositions != null)
			{
				foreach (string key in cellPositions.getKeyList())
				{
					int reelID;

					if (int.TryParse(key, out reelID))
					{
						if (reelID < topLeftColumnIndex)
						{
							topLeftColumnIndex = reelID;
						}

						int[] reelsAffected = cellPositions.getIntArray(key);
					
						for (int j = 0; j < reelsAffected.Length; j++)
						{
							if (reelsAffected[j] > topLeftRowIndex)
							{
								topLeftRowIndex = reelsAffected[j];
							}
						}
					
						singleSymbolLocations.Add(reelID, reelsAffected);
						totalNumberOfMutations += reelsAffected.Length;
					}
					else
					{
						Debug.LogError("Mutation key is not an int as expected: " + key);
					}
				}
#if UNITY_EDITOR
				//CommonDataStructures.debugLogDict(singleSymbolLocations);
#endif
			}
		}
		else if(type == "symbol_overlay_replacement")
		{
			JSON[] replacementcellJSONS = mutation.getJsonArray("replacementCells");
			replacementCells = new List<ReplacementCell>();
			foreach (JSON replacementcellJSON in replacementcellJSONS)
			{
				ReplacementCell RPCell = new ReplacementCell();
				RPCell.reelIndex = replacementcellJSON.getInt("reelIndex", 0);
				RPCell.symbolIndex = replacementcellJSON.getInt("symbolIndex", 0);
				RPCell.replaceSymbol = replacementcellJSON.getString("replaceSymbol", "WD1");
				replacementCells.Add(RPCell);
			}

		}
		else if (type == "trigger_replace_multi" || type == "trigger_replace_multi_prob" || type == "symbol_rise_and_fall" || type == "left_right_trigger_replace")
		{
			List<string> keyList = mutation.getKeyList();
			twMutatedSymbolList = new List<ReplacementCell>();
			foreach (string key in keyList)
			{

				if (key.Contains("reel"))
				{
					string[] splitKeyArray = key.Split('_');
					int column = System.Convert.ToInt32(splitKeyArray[1]);
					int row = System.Convert.ToInt32(splitKeyArray[3]);
					string symbolKey = mutation.getString(key, "");
					triggerSymbolNames[column, row] = symbolKey;
					isTWmutation = true;
					// Also add to the mutated symbol list. First used in gen53 freespin battle mode to replace traversing through the triggerSymbolNames array
					twMutatedSymbolList.Add(new ReplacementCell(column, row, symbolKey));

					//Debug.Log("At column " + column + " and row " + row + " is symbol " + mutation.getString(key, ""));
				}
			}
		}
		else if (type == "linking_wilds")
		{
			triggerSymbolName = mutation.getString("trigger_symbol", null, null); // linking wilds (ainsworth10) contain a single trigger_symbol key, versus an array
			twTriggeredSymbolList = populateReplacementCells(mutation.getJsonArray("trigger_symbol"), "reel", "position", "symbol_name");
			twMutatedSymbolList = populateReplacementCells(mutation.getJsonArray("mutated_symbol"), "reel", "position", "to_symbol");
			isTWmutation = true;
		}
		else if (type == "symbol_left_right_wild")
		{
			JSON[] triggerSymbols = mutation.getJsonArray("trigger_symbol");
			twTriggeredSymbolList = populateReplacementCells(triggerSymbols, "reel", "position", "symbol_name");
			leftRightWildMutateSymbolList = new List<List<ReplacementCell>>();
			foreach (JSON trigger in triggerSymbols)
			{
				leftRightWildMutateSymbolList.Add(populateReplacementCells(trigger.getJsonArray("mutated_symbol"), "reel", "position", "to_symbol"));
			}
		}
		else if (type == "free_spin_battle_mode")
		{
			string hitString = mutation.getString("hit", "");
			didWin = (hitString != "bad");

			// Let's see if there was loot!
			if (didWin)
			{
				JSON[] loot = mutation.getJsonArray("loot");
				if (loot != null && loot.Length > 0)
				{
					reveals = new List<Reveal>();
					foreach (JSON chestInfo in loot)
					{
						reveals.Add(new Reveal(chestInfo.getBool("selected", false), chestInfo.getBool("multiplier", false), chestInfo.getInt("value", 0)));
					}
				}
			}
		}
		else if (type == "free_spin_trigger_wild_locking"
			|| type == "symbol_locking_with_freespins"
			|| type == "symbol_locking_with_scatter_wins"
			|| type == "symbol_locking_with_mutating_symbols"
			|| type == "symbol_locking_multi_payout"
			|| type == "symbol_locking"
			|| type == "symbol_locking_multi_payout_jackpot"
			|| type == "symbols_lock_fake_spins_mutator")
		{
			JSON[] wheelJSON = mutation.getJsonArray("wheel");
			JSON[] stickyLocations = mutation.getJsonArray("new_stickies");
			JSON[] triggerWilds = mutation.getJsonArray("trigger_wilds");
			numberOfFreeSpinsAwarded = mutation.getInt("free_spins_count", 0);

			creditsAwarded = mutation.getLong("credits_awarded", 0);
			creditsMultiplier = mutation.getInt("multiply_all", 0);
			numberOfSymbolsCollected = mutation.getInt("sc_collected", 0);

			if (type == "symbol_locking_multi_payout" || type == "symbol_locking_multi_payout_jackpot")
			{
				numberOfFreeSpinsAwarded = mutation.getInt("extra_spins_awarded", 0);
				creditsMultiplier = mutation.getInt("wager_multiplier_awarded", 0);
			}

			if (wheelJSON != null && wheelJSON.Length > 0)
			{
				reveals = new List<Reveal>();
				foreach (JSON wheelInfo in wheelJSON)
				{
					reveals.Add(new Reveal(wheelInfo.getBool("selected", false), wheelInfo.getBool("multiplier", false), wheelInfo.getInt("value", 0)));
				}
			}

			populateTriggerSymbolNames(stickyLocations, "reel", "position", "to_symbol");
			populateTriggerSymbolNames(triggerWilds, "reel", "position", "to_symbol");

			// For sticky symbols being passed down for ainsworth04
			replacementCells = populateReplacementCells(mutation.getJsonArray("new_stickies"), "reel", "position", "to_symbol");
			mutatedReplacementCells = populateReplacementCells(mutation.getJsonArray("mutated_stickies"), "reel", "position", "to_symbol");
			removedReplacementCells = populateReplacementCells(mutation.getJsonArray("removed_stickies"), "reel", "position", "from_symbol");

		}
		else if (type == "simple_multiplier_reel")
		{
			simpleMultiplierReelMultiplier = mutation.getInt("multiplier", -1);
		}
		else if (type == "expanding_sticky")
		{
			populateTriggerSymbolNames(mutation.getJsonArray("new_stickies"), "reel", "position", "to_symbol");
		}
		else if (type == "multi_reel_sparkle_symbol")
		{
			JSON[] sparkleLocations = mutation.getJsonArray("sparkle_trails");
			if (sparkleLocations != null && sparkleLocations.Length > 0)
			{
				Debug.Log("Sparkle trails found!");
				fromMutations = new List<ReelToReelMutation>();
				toMutations = new List<ReelToReelMutation>();
				foreach (JSON sparkleTrail in sparkleLocations)
				{
					JSON fromLocation = sparkleTrail.getJSON("from");
					if (fromLocation != null)
					{
						fromMutations.Add(new ReelToReelMutation(fromLocation.getInt("reelset", 0), fromLocation.getInt("reel", 0), fromLocation.getInt("position", 0), fromLocation.getString("to_symbol", "")));
					}

					JSON toLocation = sparkleTrail.getJSON("to");
					if (toLocation != null)
					{
						toMutations.Add(new ReelToReelMutation(toLocation.getInt("reelset", 0), toLocation.getInt("reel", 0), toLocation.getInt("position", 0), toLocation.getString("to_symbol", "")));
					}
				}
			}
		}
		else if (type == "multi_reel_expanding_wild_transfer")
		{
			JSON[] sparkleLocations = mutation.getJsonArray("sparkle_trails");
			if (sparkleLocations != null && sparkleLocations.Length > 0)
			{
				fromMutations = new List<ReelToReelMutation>();
				toMutations = new List<ReelToReelMutation>();
				foreach (JSON sparkleTrail in sparkleLocations)
				{
					int reelID = sparkleTrail.getInt("reel", -1);
					int fromGame = sparkleTrail.getInt("from_game", -1);
					int[] toGames = sparkleTrail.getIntArray("to_games");

					fromMutations.Add(new ReelToReelMutation(-1, reelID, -1, "", fromGame));

					foreach (int i in toGames)
					{
						toMutations.Add(new ReelToReelMutation(-1, reelID, -1, "", i));
					}
				}
			}
		}
		else if (type == "multi_reel_paper_fold_wild_transfer")
		{

		}
		else if (type == "free_spin_multiplier_accumulator")
		{
			accumulatedMulitpler = mutation.getInt("curr_multiplier", 1);
			creditsAwarded = mutation.getInt("extra_credits", 0);
			numberOfFreeSpinsAwarded = mutation.getInt("extra_spins", 0);
		}
		else if (type == "symbol_replace_multi" || type == "symbol_replace_multi_linked_prob")
		{
			// Sets the replacement data for the symbols 
			normalReplacementSymbolMap = new Dictionary<string, string>();
			megaReplacementSymbolMap = new Dictionary<string, string>();
			JSON replaceData = mutation.getJSON("replace_symbols");

			if (replaceData != null)
			{
				foreach (KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
				{
					megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
				}
				foreach (KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
				{
					normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
				}
			}

			// Set all the replacement symbols for each reel.
			foreach (SlotReel reel in ReelGame.activeGame.engine.getReelArray())
			{
				reel.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: false);
			}
		}
		else if (type == "symbol_replace_by_spin")
		{
			megaReplacementSymbolMap = new Dictionary<string, string>();
			normalReplacementSymbolMap = new Dictionary<string, string>();
			normalReplacementSymbolMap.Add(mutation.getString("from_symbol", ""), mutation.getString("to_symbol", ""));
			// Set all the replacement symbols for each reel.
			foreach (SlotReel reel in ReelGame.activeGame.engine.getReelArray())
			{
				reel.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: false);
			}

		}
		else if (type == "free_spin_wild_picks")
		{
			SlotReel[] reelArray = ReelGame.activeGame.engine.getReelArray();
			int reelLength = reelArray.Length;
			for (int i = 0; i < reelLength; i++)
			{
				List<List<int>> reelMutations = mutation.getIntListList("picks.pick.wild_positions");
				int[] indecies = reelMutations[i].ToArray();
				// ensure we get the name of the symbol in case a game uses different wild types
				string wildSymbolName = mutation.getString("picks.to_symbol", "");
				reelArray[i].setWildMutations(wildSymbolName, indecies);
			}
		}
		else if (type == "multi_reel_advanced_replacement")
		{
			if (mutation.getJsonArray("mutated_reels") != null && mutation.getJsonArray("mutated_reels").Length > 0)
			{
				JSON[] mutatedReelsJson = mutation.getJsonArray("mutated_reels");
				mutatedReels = new int[mutatedReelsJson.Length][];
				mutatedSymbols = new string[mutatedReelsJson.Length];
				isLockingMutatedReel = new bool[mutatedReelsJson.Length];
				for (int i = 0; i < mutatedReelsJson.Length; i++)
				{
					mutatedReels[i] = mutatedReelsJson[i].getIntArray("reels");
					mutatedSymbols[i] = mutatedReelsJson[i].getString("symbol", "");
					isLockingMutatedReel[i] = mutatedReelsJson[i].getBool("is_locking", false);
				}
			}
			else if (mutation.getJsonArray("locked_mutated_reels") != null)
			{
				// NOTE : (Scott Lepthien) For now we don't need to parse this to do anything on the client, but if we ever do need to
				// we can add the code here.  For now adding this block in so we still get warnings if something is configured incorrectly
				// with this mutation.  This is a valid configuration though, where only previously locked reels are sent down.
			}
			else
			{
				Debug.LogWarning("multi_reel_advanced_replacement data is not setup correctly.");
			}
		}
		else if (type == "progressive_vertical_wilds")
		{
			JSON[] triggersFound = mutation.getJsonArray("triggers_found");
			mutateToSymbol = mutation.getString("to_symbol", "");
			reelsToMutate = mutation.getIntArray("vertical_replacements");

			if (triggersFound != null && triggersFound.Length > 0)
			{
				featureReels = new int[triggersFound.Length];
				mutatedSymbols = new string[triggersFound.Length];
				for (int i = 0; i < triggersFound.Length; i++)
				{
					featureReels[i] = triggersFound[i].getInt("reel", 1);
					mutatedSymbols[i] = triggersFound[i].getString("symbol", "");
				}
			}
		}
		else if (type == "replacement_cell_wild")
		{
			replacementCells = new List<ReplacementCell>();

			string[] replaceSymbols = mutation.getStringArray("replace_symbols");
			string replaceSymbolsOverride = mutation.getString("replace_symbol", "");

			List<List<int>> replacementPositions = mutation.getIntListList("cell_positions");
			for (int index = 0; index < replacementPositions.Count; index++)
			{
				List<int> position = replacementPositions[index];

				ReplacementCell replacementCell = new ReplacementCell();
				replacementCell.reelIndex = position[0] - 1;
				replacementCell.symbolIndex = position[1] - 1;
				if (!string.IsNullOrEmpty(replaceSymbolsOverride))
				{
					replacementCell.replaceSymbol = replaceSymbolsOverride;
				}
				else
				{
					replacementCell.replaceSymbol = replaceSymbols[index];
				}
				replacementCells.Add(replacementCell);
			}
		}
		else if (type == "jackpot_multiplier")
		{
			creditsMultiplier = mutation.getInt("jackpot_multiplier", 0);
			creditsAwarded = mutation.getLong("credits_awarded", 0);
			initialJackpotValue = mutation.getLong("jackpot_initialized", 0);
			currentJackpotValue = mutation.getLong("jackpot_value", 0);
			reinitializedJackpotValue = mutation.getLong("jackpot_reinitialized", 0);
		}
		else if (type == SlotOutcome.REEVALUATION_TYPE_SPOTLIGHT)
		{
			if (mutation.getJSON("vertical_wild") != null
				&& mutation.getJSON("vertical_wild").getIntArray("reels") != null
				&& mutation.getJSON("vertical_wild").getIntArray("reels").Length > 0)
			{
				reels = mutation.getJSON("vertical_wild").getIntArray("reels");
				symbol = mutation.getJSON("vertical_wild").getString("symbol", "");
			}

			replacementCells = populateReplacementCells(mutation.getJsonArray("new_stickies"), "reel", "col", "symbol");

			// Sets the replacement data for the symbols. This should ideally be sent down separately as a "symbol_replace_multi" mutation but
			// is instead being sent down with the "spotlight_reel_effect" mutation
			normalReplacementSymbolMap = new Dictionary<string, string>();
			megaReplacementSymbolMap = new Dictionary<string, string>();
			JSON replaceData = mutation.getJSON("replace_symbols");

			if (replaceData != null)
			{
				foreach (KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
				{
					megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
				}
				foreach (KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
				{
					normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
				}
			}

			// Set all the replacement symbols for each reel.
			foreach (SlotReel reel in ReelGame.activeGame.engine.getReelArray())
			{
				reel.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: false);
			}
		}
		else if (type == "contract_on_checkpoint")
		{
			symbolRandomReelExpanderData = new List<IndependentReelExpansionMutation.IndependentReelExpansion>();

			int reelIndex = 0;
			while (mutation.getInt("reel_" + reelIndex, -1) != -1)
			{
				symbolRandomReelExpanderData.Add(new IndependentReelExpansionMutation.IndependentReelExpansion(reelIndex, mutation.getInt("reel_" + reelIndex, -1)));
				reelIndex++;
			}
		}
		else if (type == "symbol_random_reel_expander")
		{
			symbolRandomReelExpanderData = new List<IndependentReelExpansionMutation.IndependentReelExpansion>();

			int reelIndex = 0;
			while (mutation.getInt("reel_" + reelIndex, -1) != -1)
			{
				symbolRandomReelExpanderData.Add(new IndependentReelExpansionMutation.IndependentReelExpansion(reelIndex, mutation.getInt("reel_" + reelIndex, -1)));
				reelIndex++;
			}
		}
		else if (type == "free_spin_battle_multiplier")
		{
			freeSpinBattleMutliplierData = new FreeSpinBattleMultiplier();
			freeSpinBattleMutliplierData.hit = mutation.getString("hit", "neutral");
			freeSpinBattleMutliplierData.numberOfStrikes = mutation.getInt("num_strikes", 0);
			freeSpinBattleMutliplierData.currentJackpot = mutation.getLong("curr_jackpot", 0);
			freeSpinBattleMutliplierData.battleResult = mutation.getString("battle_result", "");
		}
		else if (type == "symbol_expansion")
		{
			reels = mutation.getIntArray("reels");
			symbol = mutation.getString("symbol", "");
		}
		else if (type == "sliding_sticky_symbols")
		{
			// sliding stickies used first by munsters01 freespins
			numberOfFreeSpinsAwarded = mutation.getInt("free_spins_count", 0);

			JSON[] newSlidingWildsJson = mutation.getJsonArray("new_sliding_wilds");
			JSON[] stickyLocations = mutation.getJsonArray("old_sliding_wilds");

			populateTriggerSymbolNames(newSlidingWildsJson, "reel", "position", "symbol");
			movingStickySymbolList = populateReplacementCells(stickyLocations, "reel", "to_position", "symbol");
		}
		else if (type == "free_spins_award_mutator")
		{
			pickSelected = new Pick(
				mutation.getLong("picks.pick.credits", 0),
				mutation.getInt("picks.pick.multiply_rest", 0),
				mutation.getInt("picks.pick.free_spins_count", 0));

			picksUnselected = new List<Pick>();
			JSON[] revealsData = mutation.getJsonArray("picks.reveals");
			foreach (JSON revealsJson in revealsData)
			{
				Pick pick = new Pick(
					revealsJson.getLong("credits", 0),
					revealsJson.getInt("multiply_rest", 0),
					revealsJson.getInt("free_spins_count", 0));
				picksUnselected.Add(pick);
			}
		}
		else if (type == "scatter_payout" || type == "scatter_payout_jackpot")
		{
			JSON[] symbolValues = mutation.getJsonArray("symbols");
			scatterPayoutInformation = new Dictionary<string, long>();

			for (int i = 0; i < symbolValues.Length; i++)
			{
				string symbolKey = symbolValues[i].getString("symbol", "");
				if (!symbolKey.IsNullOrWhiteSpace() && !scatterPayoutInformation.ContainsKey(symbolKey)) //Don't want to double add keys into the dictionary. Also, don't want to try check for a empty string key.
				{
					scatterPayoutInformation.Add(symbolKey, symbolValues[i].getLong("credits", 0));
				}
			}
			creditsAwarded = mutation.getLong("credits_awarded", 0);
		}
		else if (type == "vip_revamp_mini_game")
		{
			reels = mutation.getIntArray("changed_reels");
			reelStrips = mutation.getStringArray("reel_strips");
		}
		else if (type == "bonus_reel_set_replacement")
		{
			bonusReplacementActive = mutation.getBool("bonus_replacement_active", false);
			bonusReplacementTriggered = mutation.getBool("bonus_replacement_triggered", false);
		}
		else if (type == "personal_jackpot_reevaluator")
		{
			progressiveScatterJackpots = new List<ProgressiveScatterJackpot>();
			if (mutation.hasKey("jackpot"))
			{
				JSON wonJackpot = mutation.getJSON("jackpot");
				jackpotKey = wonJackpot.getString("key", "");
				creditsAwarded = wonJackpot.getLong("credits", 0);
			}
			Dictionary<string, JSON> jackpotsDictionary = mutation.getStringJSONDict("jackpots");
			foreach (string key in jackpotsDictionary.Keys)
			{
				JSON jackpotJSON = jackpotsDictionary[key];
				JSON contributionJSON = jackpotJSON.getJSON("contributions");
				ProgressiveScatterJackpot jackpot = new ProgressiveScatterJackpot();
				jackpot.baseAmount = jackpotJSON.getLong("base_payout", 0);
				jackpot.amountAdded = contributionJSON.getLong("amount", 0);
				jackpot.jackpotBalance = contributionJSON.getLong("balance", 0);
				progressiveScatterJackpots.Add(jackpot);
			}
		}
		else if (type == "trigger_pick_replace_multi")
		{
			JSON[] triggerSymbols = mutation.getJsonArray("trigger_symbols");
			twTriggeredSymbolList = populateReplacementCells(triggerSymbols, "reel", "position", "symbol_revealed");
			leftRightWildMutateSymbolList = new List<List<ReplacementCell>>();
			foreach (JSON trigger in triggerSymbols)
			{
				leftRightWildMutateSymbolList.Add(populateReplacementCells(trigger.getJsonArray("transformed_symbols"), "reel", "position", "new_symbol"));
			}
		}
		else if (type == "symbol_landing_award_credits")
		{
			symbolLandingAwardCreditSymbols.Clear();
			creditsAwarded = 0;

			JSON[] symbolValues = mutation.getJsonArray("symbols");
			foreach (JSON symValue in symbolValues)
			{
				string symbolKey = symValue.getString("symbol", "");
				int position = symValue.getInt("pos", 0);
				int reel = symValue.getInt("reel", 0);
				long credits = symValue.getLong("credits", 0);
				symbolLandingAwardCreditSymbols.Add(new CreditOrSpinAwardingSymbol() { symbolServerName = symbolKey, position = position, reel = reel, credits = credits });
				creditsAwarded += credits;
			}
		}
		else if (type == "symbol_landing_award_free_spins")
		{
			symbolLandingAwardSpinsSymbols.Clear();
			numberOfFreeSpinsAwarded = 0;

			JSON[] symbolValues = mutation.getJsonArray("symbols");
			foreach (JSON symValue in symbolValues)
			{
				string symbolKey = symValue.getString("symbol", "");
				int position = symValue.getInt("pos", 0);
				int reel = symValue.getInt("reel", 0);
				int freeSpins = symValue.getInt("free_spins", 0);
				numberOfFreeSpinsAwarded += freeSpins;
				symbolLandingAwardSpinsSymbols.Add(new CreditOrSpinAwardingSymbol() { symbolServerName = symbolKey, position = position, reel = reel, spinCount = freeSpins });
			}
		}
		else if (type == "non_persistent_jackpot_meter")
		{
			JSON[] jackpotMeters = mutation.getJsonArray("jackpot_meters");
			this.jackpotMeters.Clear();

			foreach (JSON jackpotMeterJson in jackpotMeters)
			{
				string typeKey = jackpotMeterJson.getString("type", "");

				if (typeKey.IsNullOrWhiteSpace())
				{
					Debug.LogError("Invalid jackpot key in non_persistent_jackpot_meter.jackpot_meters");
					continue;
				}

				long credits = jackpotMeterJson.getLong("credits", 0);
				bool shouldAward = jackpotMeterJson.getBool("should_award", false);
				int requiredPips = jackpotMeterJson.getInt("required_pips", System.Int32.MaxValue);
				int currentPips = jackpotMeterJson.getInt("current_meter", 0);
				string keyName = jackpotMeterJson.getString("key_name", "");

				JackpotMeter jackpotMeter = new JackpotMeter() { shouldAward = shouldAward, baseAmount = credits, currentPips = currentPips, keyName = keyName, requiredPips = requiredPips };

				this.jackpotMeters.Add(keyName, jackpotMeter);

				if (!shouldAward)
				{
					continue;
				}

				jackpotKey = keyName;
			}
		}
		else if (type == "transform_symbols_from_number_of_symbols_landed")
		{
			List<ReplacementCell> symbolsToTransform = new List<ReplacementCell>();
			symbolReplacementCells = new Dictionary<string, List<ReplacementCell>>();

			JSON[] triggerSymbols = mutation.getJsonArray("trigger_symbols");
			foreach (JSON trigger in triggerSymbols)
			{
				string revealSymbol = trigger.getString("reveal_symbol", "");
				symbolsToTransform = populateReplacementCells(trigger.getJsonArray("transformed_symbols"), "reel", "position", "new_symbol");
				symbolReplacementCells.Add(revealSymbol, symbolsToTransform);
			}
		}
		else if (type == "nudging_wild")
		{
			excludeSymbolKey = mutation.getString("exclude_symbol_key", "");
		}
	}

	public bool isPreEndMutation()
	{
		return singleSymbolLocations != null && singleSymbolLocations.Count > 1;
	}

	public static List<ReplacementCell> populateReplacementCells(JSON[] allMutationData, string rowKey, string colKey, string symbolKey)
	{
		List<ReplacementCell> list = new List<ReplacementCell>();
		if (allMutationData != null && allMutationData.Length > 0)
		{
			foreach (JSON mutationData in allMutationData)
			{
				ReplacementCell replacementCell = new ReplacementCell();

				replacementCell.reelIndex = mutationData.getInt(rowKey, 0);
				replacementCell.symbolIndex = mutationData.getInt(colKey, 0);
				replacementCell.replaceSymbol = mutationData.getString(symbolKey, "");
				list.Add(replacementCell);
			}
		}
		return list;
	}

	private void populateTriggerSymbolNames(JSON[] allMutationData, string rowKey, string colKey, string symbolKey)
	{
		if (allMutationData != null && allMutationData.Length > 0)
		{
			foreach (JSON mutationData in allMutationData)
			{
				int column = mutationData.getInt(rowKey, 0);
				int row = mutationData.getInt(colKey, 0);
				triggerSymbolNames[column, row] = mutationData.getString(symbolKey, "");
			}
		}
	}
}


public class ReelToReelMutation
{
	public int reelSet;
	public int reel;
	public int position;
	public string symbolName;
	public int layer;

	public ReelToReelMutation(int reelSet, int reel, int position, string symbolName = "", int layer = 0)
	{
		this.reelSet = reelSet;
		this.reel = reel;
		this.position = position;
		this.symbolName = symbolName;
		this.layer = layer;
	}
}

// Reveal Info
public class Reveal
{
	public bool selected;
	public bool multiplier;
	public int value;
	
	public Reveal(bool selected, bool multiplier, int value)
	{
		this.selected = selected;
		this.multiplier = multiplier;
		this.value = value;
	}
}

//------------------------------------------------------------------------------------
// Custom Mutations
//------------------------------------------------------------------------------------

/*
 * Mutation for handling Transforming Symbols
 * First use: orig008
 */
public class MultiRoundTransformingSymbolsMutation : MutationBase
{
	public class TransformedSymbol
	{
		public TransformedSymbol(JSON json)
		{
			reel = json.getInt("reel", 0);
			position = json.getInt("position", 0);
			oldSymbol = json.getString("old_symbol", "");
			newSymbol = json.getString("new_symbol", "");
		}
        
		public string oldSymbol;
		public string newSymbol;
		public int reel;
		public int position;
	}

	public readonly List<List<TransformedSymbol>> rounds = new List<List<TransformedSymbol>>();

	public MultiRoundTransformingSymbolsMutation(JSON mutation) : base(mutation)
	{
		JSON[] roundsData = mutation.getJsonArray("rounds");
		foreach (JSON round in roundsData)
		{
			rounds.Add(new List<TransformedSymbol>());
			JSON[] transformedSymbolsJson = round.getJsonArray("transformed_symbols");
			foreach (JSON symbol in transformedSymbolsJson)
			{
				rounds[rounds.Count - 1].Add(new TransformedSymbol(symbol));	
			}
		}
	}
}

/**
Class for handling the mutations used by zynga04 words with friends
*/
public class WordMutation : MutationBase
{
	public Dictionary<string, long> letterScores = new Dictionary<string, long>(); // Stores scoring info that only comes down in the first word mutation
	public string currentWord = "";		// Track the current word being displayed to the player
	public string nextWord = "";		// Only used when first_word and next_word come down at the same time, othersiwe next_word is stored in currentWord
	public List<LetterMultiplier> letterMultipliers = new List<LetterMultiplier>();
	public List<WordMultiplier> wordMultipliers = new List<WordMultiplier>();
	public long creditsAwarded = 0;

	public WordMutation(JSON mutation) : base(mutation)
	{
		letterScores = mutation.getStringLongDict("letter_scores");
		creditsAwarded = mutation.getLong("credits_awarded", 0);

		// if being played from a bonus game then apply the multiplier
		long gameMultiplier;

		// account for a possible multiplier override such as the one used for cumulative bonuses like in zynga04
		if (BonusGameManager.instance.betMultiplierOverride != -1)
		{
			gameMultiplier = BonusGameManager.instance.betMultiplierOverride;
		}
		else
		{
			if (GameState.giftedBonus != null)
			{
				gameMultiplier = GiftedSpinsVipMultiplier.playerMultiplier;
			}
			else
			{
				gameMultiplier = SlotBaseGame.instance.multiplier;
			}
		}

		creditsAwarded *= gameMultiplier;

		currentWord = mutation.getString("first_word", "");
		if (currentWord == "")
		{
			// check for the next_word since we probably already had the first_word
			currentWord = mutation.getString("next_word", "");
		}
		else
		{
			// double check if a next_word came down which means that there will be a win right away and the word will need to swap
			nextWord = mutation.getString("next_word", "");
		}

		JSON[] letterMultiplierJsonData = mutation.getJsonArray("letter_multpliers");
		foreach (JSON letterMultiplierData in letterMultiplierJsonData)
		{
			LetterMultiplier letterMultiplier = new LetterMultiplier(letterMultiplierData);
			letterMultipliers.Add(letterMultiplier);
		}

		JSON[] wordMultiplierJsonData = mutation.getJsonArray("word_multpliers");
		foreach (JSON wordMultiplierData in wordMultiplierJsonData)
		{
			WordMultiplier wordMultiplier = new WordMultiplier(wordMultiplierData);
			wordMultipliers.Add(wordMultiplier);
		}
	}

	/**
	Data for when a letter will have its value multiplied
	*/
	public class LetterMultiplier
	{
		public int letterIndex = -1;
		public long multiplier = 0;
		public int reelID = -1;

		public LetterMultiplier(JSON data)
		{
			letterIndex = data.getInt("letter_index", -1);
			multiplier = data.getLong("multiplier", 0);
			reelID = data.getInt("reel", -1);

			// reelID is 1 based, but the data isn't so just adjust the data
			if (reelID != -1)
			{
				reelID += 1;
			}
		}
	}

	/**
	Data for when the entire word jackpot value will have its multiplier increased
	*/
	public class WordMultiplier
	{
		public long multiplier = 0;
		public int reelID = -1;

		public WordMultiplier(JSON data)
		{
			multiplier = data.getLong("multiplier", 0);
			reelID = data.getInt("reel", -1);

			// reelID is 1 based, but the data isn't so just adjust the data
			if (reelID != -1)
			{
				reelID += 1;
			}
		}
	}
}

public class PaperFoldMutation : MutationBase
{
	public List<BigSymbol> bigSymbols = new List<BigSymbol>();
	public List<PaperFold> paperFolds = new List<PaperFold>();
	public PaperFoldMutation(JSON mutation) : base(mutation)
	{
		JSON[] paperFoldLocations = mutation.getJsonArray("paperfolds");
		if (paperFoldLocations != null && paperFoldLocations.Length > 0)
		{
			paperFolds = new List<PaperFold>();
			foreach (JSON paperFold in paperFoldLocations)
			{	
				int fromReelID = paperFold.getIntArray("from_position")[0];
				int toReelID = paperFold.getIntArray("to_position")[0];
				int rowID = paperFold.getIntArray("from_position")[1];
				int fromGame = paperFold.getInt("from_layer", -1);
				int toGame = paperFold.getInt("to_layer", -1);
				paperFolds.Add(new PaperFold(fromGame, toGame, fromReelID, toReelID, rowID));
			}
		}

		JSON[] bigSymbolLocations = mutation.getJsonArray("pop_in_place_symbols");
		if(bigSymbolLocations != null && bigSymbolLocations.Length > 0)
		{
			bigSymbols = new List<BigSymbol>();
			foreach(JSON bigSymbol in bigSymbolLocations)
			{
				int gameLayer = bigSymbol.getInt("matrix_id", -1);
				string symbolName = bigSymbol.getString("symbol", "");
				int reelID = bigSymbol.getIntArray("position")[0];
				int rowID = bigSymbol.getIntArray("position")[1];
				int size = bigSymbol.getInt("size", -1);
				bigSymbols.Add(new BigSymbol(gameLayer, symbolName, reelID, rowID, size));
			}
		}
	}

	//Data class used for information about what symbols need to be transferred over to different reel set
	public class PaperFold
	{
		public int fromGame;
		public int toGame;
		public int fromReelID;
		public int toReelID;
		public int rowID;

		public PaperFold(int _fromGame, int _toGame, int _fromReelID, int _toReelID, int _rowID)
		{
			this.fromGame = _fromGame;
			this.toGame = _toGame;
			this.fromReelID = _fromReelID;
			this.toReelID = _toReelID;
			this.rowID = _rowID;
		}
	}

	//Data class used for information about the big symbols that pop up and lock onto specific reels
	public class BigSymbol
	{
		public int gameLayer;
		public string symbolName;
		public int reelID;
		public int rowID;
		public int size;

		public BigSymbol(int _gameLayer, string _symbolName, int _reelID, int _rowID, int _size)
		{
			this.gameLayer = _gameLayer;
			this.symbolName = _symbolName;
			this.reelID = _reelID;
			this.rowID = _rowID;
			this.size = _size;
		}
	}
}

// Mutation first used on gen81(freaki3) then monkees01 in next one scheduled
public class MutationTumbleMultiplier : MutationBase
{
	public int tumbleCount; // 0 for base outcome, 1 for 1st tumble, 2 for 2nd tumble, etc.
							// Note: base outcome is called "1st tumble" in math doc, which I find pretty confusing
							// and decided to indicate base outcome as "0" instead of "1" here

	public int baseMultiplier; // The multiplier value defined in math doc based on tumble count and wager tier

	public int finalMultiplier; // If tumble count < 2:  final_multiplier = base_multiplier
								// If tumble count >= 2: final_multiplier = base_multiplier + extra_multiplier.new_value

	public MutationTumbleMultiplier(JSON mutation) : base(mutation)
	{
		tumbleCount = mutation.getInt("tumble_count", 0);
		baseMultiplier = mutation.getInt("base_multiplier", 1);
		finalMultiplier = mutation.getInt("final_multiplier", 1);
	}
}


//This mutation type is used by Batman Begins 01 (bb01) for deciding which reels need to expand and to what height
public class IndependentReelExpansionMutation : MutationBase
{
	public List<IndependentReelExpansion> reelExpansions = new List<IndependentReelExpansion>();

	public IndependentReelExpansionMutation(JSON mutation) : base(mutation)
	{
		reelExpansions = new List<IndependentReelExpansion>();

		//This data currently isn't an array in the JSON
		//JSON EXAMPLE:
		//"mutations" : [{
		//		"type" : "contract_on_checkpoint",
		//		"reel_0" : 3,
		//		"reel_1" : 3,
		//		"reel_2" : 3,
		//		"reel_3" : 3,
		//		"reel_4" : 3
		//}]
		int reelIndex = 0;
		while (mutation.getInt("reel_" + reelIndex, 0) != 0)
		{
			reelExpansions.Add(new IndependentReelExpansion(reelIndex, mutation.getInt("reel_" + reelIndex, -1)));
			++reelIndex;
		}		
	}

	public class IndependentReelExpansion
	{
		public int reelID;
		public int expandHeight;

		public IndependentReelExpansion(int reelID, int expandHeight)
		{
			this.reelID = reelID;
			this.expandHeight = expandHeight;
		}
	}
}

public class BingoBoardMutation : MutationBase
{
	public List<List<int>> bingoBoard = new List<List<int>>(); //Values on the actual bingo board
	public long initialJackpot = 0;
	public List<ColoredCard> cardsBeingColored = new List<ColoredCard>(); //List of any cards being colored in

	public BingoBoardMutation(JSON mutation) : base(mutation)
	{
		bingoBoard = mutation.getIntListList("bingo_grid");
		initialJackpot = mutation.getLong("initial_jackpot", 0);

		JSON[] colorData = mutation.getJsonArray("new_color");
		for (int i = 0; i < colorData.Length; i++)
		{
			int[] coloredCardCoordinate = colorData[i].getIntArray("idx");
			if (coloredCardCoordinate == null || coloredCardCoordinate.Length != 2)
			{
				Debug.LogWarning("Card's coordinate data is set up not as expected. Bailing out of adding in this card: " + colorData[i]);
				return;
			}

			int columnIndex = coloredCardCoordinate[0];
			int rowIndex = coloredCardCoordinate[1];

			int triggerReelId = colorData[i].getInt("trigger_reel", 0);

			long creditsAwarded = colorData[i].getLong("credits", 0);

			List<CompletedLine> completedLineInfo = new List<CompletedLine>();
			JSON[] completedLines = colorData[i].getJsonArray("lines_complete");
			for (int j = 0; j < completedLines.Length; j++)
			{
				long lineCreditsAwarded = completedLines[j].getLong("credits", 0);
				long nextJackpotValue = completedLines[j].getLong("next_jackpot", 0);

				CompletedLine.ELineDirection lineDirection = CompletedLine.ELineDirection.Undefined;
				int lineIndex = -1;
				//Since the line direction can be 3 different possible keys, we need to search for it manually before we can grab it's index
				if (completedLines[j].hasKey("horizontal"))
				{
					lineDirection = CompletedLine.ELineDirection.Horizontal;
				}
				else if (completedLines[j].hasKey("vertical"))
				{
					lineDirection = CompletedLine.ELineDirection.Vertical;
				}
				else if(completedLines[j].hasKey("diag"))
				{
					lineDirection = CompletedLine.ELineDirection.Diag;
				}

				if (lineDirection == CompletedLine.ELineDirection.Undefined)
				{
					Debug.LogWarning(string.Format("No line direction was found for {0}", completedLines[j]));
					return;
				}

				lineIndex = completedLines[j].getInt(lineDirection.ToString().ToLower(), -1); //Now that we have the direction, grab the line index
				if (lineIndex < 0) //Throw a warning if we have an index less than 0. 
				{
					Debug.LogWarning(string.Format("Line index {0} is not in an expected range. Not adding this line to the list", lineIndex));
					return;
				}
				completedLineInfo.Add(new CompletedLine(lineCreditsAwarded, nextJackpotValue, lineDirection, lineIndex));
			}

			cardsBeingColored.Add(new ColoredCard(columnIndex, rowIndex, triggerReelId, creditsAwarded, completedLineInfo));
		}

	}

	public class ColoredCard
	{
		public int columnIndex;
		public int rowIndex;
		public int triggerReelId;
		public long credits;
		public List<CompletedLine> completedLines = new List<CompletedLine>();

		public ColoredCard(int _columnIndex, int _rowIndex, int _triggerReelId, long _credits, List<CompletedLine> _completedLines)
		{
			columnIndex = _columnIndex;
			rowIndex = _rowIndex;
			triggerReelId = _triggerReelId;
			credits = _credits;
			completedLines = _completedLines;
		}
	}
	public class CompletedLine
	{
		public long lineCredits;
		public long nextJackpot;
		public ELineDirection lineDirection;
		public int lineIndex;

		public enum ELineDirection
		{
			Undefined = 0,
			Horizontal,
			Vertical,
			Diag
		}

		public CompletedLine(long _lineCredits, long _nextJackpot, ELineDirection _lineDirection, int _lineIndex)
		{
			lineCredits = _lineCredits;
			nextJackpot = _nextJackpot;
			lineDirection = _lineDirection;
			lineIndex = _lineIndex;
		}
	}
}
