using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class CollectObjective : Objective
{
	// =============================
	// PRIVATE
	// =============================
	private string[] fillReels;
	private int fullStacks;
	private int symbolMatches;

	// =============================
	// CONST
	// =============================
	public const string SYMBOL_COLLECT = "symbol_collect";
	public const string OF_A_KIND = "of_a_kind";

	public CollectObjective(JSON data) : base(data)
	{
	}

	protected override void parseKey(JSON data, string key)
	{
		switch (key)
		{
			case "full_stack":
				fullStacks = data.getInt("full_stack", 0);
				break;

			case "symbol":
				symbol = data.getString("symbol", "");
				displaySymbol = symbol;
				break;
			
			case "symbol_match_count":
				symbolMatches = data.getInt("symbol_match_count", 2);
				break;

			case "reels":
				{
					string reels = data.getString("reels", null);

					if (!string.IsNullOrEmpty(reels))
					{
						fillReels = reels.Split(',');
					}
				}
				break;

			default:
				base.parseKey(data, key);
				break;
		}

	}

	public override void init(JSON data)
	{
		base.init(data);
		if (type == SYMBOL_COLLECT || type == OF_A_KIND)
		{
			formatSymbol();
		}
	}


	protected override string getLocString(string prefix, bool includeCredits, bool inProgress = false)
	{
		StringBuilder sb = new StringBuilder();

		// old objectives do the default string building
		if (type == OF_A_KIND || type == SYMBOL_COLLECT)
		{
			sb.Append(prefix + type);
		}
		// do the default string building
		else
		{
			return base.getLocString(prefix, includeCredits);
		}

		int parameter = 0;
		List<object> locItems = new List<object>();

		if (symbolMatches > 0)
		{
			sb.Append(Localize.DELIMITER + "{" + parameter.ToString() + "}");
			locItems.Add(symbolMatches);
			++parameter;
		}

		if (!string.IsNullOrEmpty(displaySymbol))
		{
			if (parameter > 0)
			{
				sb.Append(Localize.DELIMITER + "symbol_{" + parameter.ToString() + "}");
			}
			else
			{
				sb.Append(Localize.DELIMITER + "{" + parameter.ToString() + "}");
			}
			locItems.Add(displaySymbol);
			++parameter;
		}

		if (fillReels != null && fillReels.Length > 0)
		{
			locItems.Add(string.Join(" and ", fillReels));
		}
		
		if (fullStacks > 0)
		{
			sb.Append(Localize.DELIMITER + "full_stack");
		}		
		
		if (minWager > 0 && includeCredits)
		{
			sb.Append(Localize.DELIMITER + "min_wager_{" + parameter.ToString() + "}");
			locItems.Add(CreditsEconomy.convertCredits(minWager));
			++parameter;
		}

		if (amountNeeded > 1)
		{
			sb.Append(Localize.DELIMITER + "plural");
		}

		locItems.Add(game);

		return Localize.text(sb.ToString(), locItems.ToArray());

	}

	public override string getShortDescriptionLocalization(string prefix = "robust_challenges_desc_short_", bool abbreviateNumber = false)
	{
		if (type != OF_A_KIND && type != SYMBOL_COLLECT)
		{
			return base.getShortDescriptionLocalization(prefix);
		}

		string shortLocString = prefix + type + "_count_{0}";
		List<object> locItems = new List<object>();

		locItems.Add(amountNeeded);

		if (symbolMatches > 0)
		{
			locItems.Add(symbolMatches);
		}

		if (!string.IsNullOrEmpty(displaySymbol))
		{
			locItems.Add(displaySymbol);
		}

		if (amountNeeded > 1)
		{
			shortLocString += Localize.DELIMITER + "plural";
		}
		return Localize.text(shortLocString, locItems.ToArray());
	}
}