using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class XinYObjective : Objective
{

	public const string X_COINS_IN_Y = "win_x_coins_y_spins";
	private const string CONSTRAINT_COUNT = "constraint_count";		//used when there is only one constraint
	private const string CONSTRAINT_DATA = "constraint_data"; 		//used when there are multiple constraints
	

	private List<Constraint> _constraints;
	
	
	public XinYObjective(JSON data) : base(data)
	{
	}

	public override List<Constraint> constraints
	{
		get { return _constraints; }
		protected set { _constraints = value; }
	}

	public override string description
	{
		get
		{
			buildLocString();
			return base.description;
		}
		protected set
		{
			base.description = value;
		}
	}
	
	public ConstraintType constraintToDisplay
	{
		set
		{
			constraintDisplayIndex = 0;
			if (constraints == null)
			{
				return;
			}

			for (int i = 0; i < constraints.Count; i++)
			{
				if (constraints[i] == null)
				{
					continue;
				}

				if (constraints[i].type == value)
				{
					constraintDisplayIndex = i;
					return;
				}
			}
		}
	}
	
	public override void updateConstraintAmounts(List<long> newConstraintAmounts)
	{
		if (constraints == null || newConstraintAmounts == null)
		{
			Debug.LogError("Invalid data");
			return;
		}

		if (constraints.Count != newConstraintAmounts.Count)
		{
			Debug.LogError("Constraint counts don't match");
			return;
		}
		
		for (int i = 0; i < newConstraintAmounts.Count; i++)
		{
			constraints[i].amount = newConstraintAmounts[i];
		}
	}

	protected override void parseKey(JSON data, string key)
	{
		switch (key)
		{
			case CONSTRAINT_COUNT:  //single constraint
				if (constraints == null)
				{
					constraints = new List<Constraint>();
				}
				constraints.Add(new Constraint(ConstraintType.SPINS, 0,data.getLong(key, 0)));
				break;

			case CONSTRAINT_DATA: //multiple constraints
				buildConstraints(data.getJsonArray(CONSTRAINT_DATA));
				break;

			default:
				base.parseKey(data, key);
				break;
		}

	}

	private void buildConstraints(JSON[] data)
	{
		if (data == null)
		{
			Debug.LogError("Invalid constraint json");
			return;
		}
		if (constraints == null)
		{
			constraints = new List<Constraint>(data.Length);
		}
		else
		{
			constraints.Clear();
		}
		
		for (int i = 0; i < data.Length; i++)
		{
			if (data[i] == null)
			{
				Debug.LogWarning("Invalid challenge constraint at index: " + i);
				continue;
			}
			ConstraintType type = getConstraintTypeFromString(data[i].getString("type", ""));
			long count = data[i].getLong("count", 0);
			long limit = data[i].getLong("limit", 0);
			constraints.Add(new Constraint(type, count, limit));
		}
	}

	private void setConstraintAmount(ConstraintType type, long value)
	{
		for (int i = 0; i < constraints.Count; ++i)
		{
			if (constraints[i].type == type)
			{
				constraints[i].amount = value;
				break;
			}
		}
	}

	private void parseProgressJSON(JSON data)
	{
		if (data == null)
		{
			return;
		}

		currentAmount = data.getLong("count", 0);

		JSON constraintJSON = data.getJSON("constraints");
		if (constraintJSON != null)
		{
			List<string> keys = constraintJSON.getKeyList();
			for (int i = 0; i < keys.Count; i++)
			{
				ConstraintType type = getConstraintTypeFromString(keys[i]);
				setConstraintAmount(type, constraintJSON.getLong(keys[i], 0));
			}
		}

	}

	private void resetConstraints()
	{
		if (constraints == null)
			return;

		for (int i = 0; i < constraints.Count; i++)
		{
			constraints[i].amount = 0;
		}
	}
	
	public override string getProgressText()
	{
		long progressAmount = 0;
		if (constraints != null && constraints.Count > 0)
		{
			progressAmount = constraints[constraintDisplayIndex].limit - constraints[constraintDisplayIndex].amount;
		}
		return Localize.text("robust_challenges_desc_progress_win_x_coins_y_spins",CommonText.formatNumber(progressAmount));
	}

	public override string getCompletedProgressText()
	{
		if (constraints == null || constraints.Count <= constraintDisplayIndex)
		{
			return "0";
		}
		
		return CommonText.formatNumber(constraints[constraintDisplayIndex].limit);
	}

	public override void resetProgress(float replayGoalRatio)
	{
		resetConstraints();
		base.resetProgress(replayGoalRatio);
	}

	protected override string getLocString(string prefix, bool includeCredits, bool inProgress = false)
	{
		List<object>locItems = new List<object>();
		StringBuilder sb = new StringBuilder();

		//count
		sb.Append(prefix + type + "_count_{0}");

		long amountNeededToShow = amountNeeded;
		
		if (inProgress)
		{
			amountNeededToShow = amountNeeded - currentAmount;
		}
		if (amountNeededToShow > 1)
		{
			sb.Append(Localize.DELIMITER + "plural");
		}

		//restriction
		sb.Append(Localize.DELIMITER + "restrict_{1}");
		if (constraints != null && 
		    constraints.Count > constraintDisplayIndex && 
		    constraints[constraintDisplayIndex] != null && 
		    constraints[constraintDisplayIndex].limit > 1)
		{
			sb.Append(Localize.DELIMITER + "plural");
		}

		string amount = isComplete ? CreditsEconomy.convertCredits(amountNeeded) : CreditsEconomy.convertCredits(amountNeeded - currentAmount);
		locItems.Add(amount);
		
		long limitNumber = 0;
		if (constraints != null && constraints.Count > constraintDisplayIndex)
		{
			Constraint activeConstraint = constraints[constraintDisplayIndex];
			if (!inProgress)
			{
				limitNumber = activeConstraint.limit;
			}
			else
			{
				limitNumber = activeConstraint.limit - activeConstraint.amount;
			}
		}
		locItems.Add(limitNumber);
		locItems.Add(game);

		return Localize.text(sb.ToString(), locItems.ToArray());
	}

	public string getShortDescriptionWithCurrentAmountAndLimit(string prefix = "robust_challenges_desc_short_", bool abbreviateNumber = false)
	{
		long limitNumber = 0;
		if (constraints != null && constraints.Count > constraintDisplayIndex)
		{
			Constraint activeConstraint = constraints[constraintDisplayIndex];
			limitNumber = activeConstraint.limit - activeConstraint.amount;
		}

		return getShortDescription(prefix, amountNeeded - currentAmount, limitNumber, abbreviateNumber);
	}

	public string getShortDescriptionLocalizationWithCurrentLimit(string prefix = "robust_challenges_desc_short_", bool abbreviateNumber = false)
	{
		long limitNumber = 0;
		if (constraints != null && constraints.Count > constraintDisplayIndex)
		{
			Constraint activeConstraint = constraints[constraintDisplayIndex];
			limitNumber = activeConstraint.limit - activeConstraint.amount;
		}

		return getShortDescription(prefix, amountNeeded, limitNumber, abbreviateNumber);
	}

	public override string getShortDescriptionLocalization(string prefix = "robust_challenges_desc_short_", bool abbreviateNumber = false)
	{
		long limitNumber = 0;
		if (constraints != null && constraints.Count > constraintDisplayIndex)
		{
			limitNumber = constraints[constraintDisplayIndex].limit;
		}

		return getShortDescription(prefix, amountNeeded, limitNumber, abbreviateNumber);
	}

	private string getShortDescription(string prefix, long amount, long limit, bool abbreviateNumber = false)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(prefix + type + "_count_{0}");

		if (amount > 1)
		{
			sb.Append(Localize.DELIMITER + "plural");
		}

		//restriction
		sb.Append(Localize.DELIMITER + "restrict_{1}");
		
		if (limit > 1)
		{
			sb.Append(Localize.DELIMITER + "plural");
		}

		string amountText = abbreviateNumber
			? CreditsEconomy.multiplyAndFormatNumberAbbreviated(amount)
			: CreditsEconomy.convertCredits(amount);
		return Localize.text(sb.ToString(), amountText, limit);
	}

	public override string getInProgressText(string prefix = DEFAULT_LOCALIZATION_PREFIX, bool includeCredits = true)
	{
		return getLocString(prefix, includeCredits, true);
	}
}
