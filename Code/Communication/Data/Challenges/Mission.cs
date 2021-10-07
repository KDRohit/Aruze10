using UnityEngine;
using System.Collections.Generic;

/*
Represents a single mission for a challenge.
*/

public class Mission
{
	// =============================
	// PUBLIC GET, PROTECTED SET
	// =============================
	public List<Objective> objectives { get; protected set; }								// A straight list of all objectives.
	public Dictionary<string, List<Objective>> gameObjectives { get; protected set; }	// The objectives organized by game.
	public List<MissionReward> rewards { get; protected set; }
	public Dictionary<string, DialogData> dialogStateData { get; protected set; }

	// =============================
	// PUBLIC
	// =============================
	public bool isComplete { get; private set; }
	public bool hasMadeProgress { get; private set; }
	public bool hasObjectivesWithoutAGame { get; private set; }
	public Mission(JSON data)
	{
		init(data);
	}

	public virtual void init(JSON data)
	{
		objectives = new List<Objective>();
		gameObjectives = new Dictionary<string, List<Objective>>();
		rewards = new List<MissionReward>();
		dialogStateData = new Dictionary<string, DialogData>();

		if (data != null)
		{
			parseDialogs(data.getJSON("dialogs"));
			parseRewards(data.getJsonArray("rewards"));
			parseObjectives(data.getJsonArray("types"));
		}
	}

	public virtual void resetProgress(float replayRewardRatio, float replayGoalRatio)
	{
		if (objectives != null && objectives.Count > 0)
		{
			for(int i=0; i<objectives.Count; ++i)
			{
				if (objectives[i] == null)
				{
					Debug.LogWarning("Invalid mission objective");
					continue;
				}
				objectives[i].resetProgress(replayGoalRatio);
			}
		}
		else
		{
			Debug.LogError("No objectives in challenge campaign");
		}

		if (rewards != null && rewards.Count > 0)
		{
			for(int i = 0; i < rewards.Count; ++i)
			{
				if (rewards[i] == null)
				{
					Debug.LogWarning("Invalid mission reward");
					continue;
				}
				rewards[i].addReplayModifier(replayRewardRatio);
			}
		}
		
		isComplete = false;
		hasMadeProgress = false;
	}

	protected Objective createObjective(JSON data)
	{
		string type = data.getString("definition", string.Empty);
		if (string.IsNullOrEmpty(type.Trim()))
		{
			type = data.getString("type", string.Empty);
		}
		
		switch (type)
		{
			case CollectObjective.SYMBOL_COLLECT:
			case CollectObjective.OF_A_KIND:
				{
					CollectObjective objective = new CollectObjective(data);
					objective.formatSymbol();
					objective.buildLocString();
					return objective;
				}

			case XinYObjective.X_COINS_IN_Y:
				return new XinYObjective(data);
			
			case XDoneYTimesObjective.WIN_X_COINS_Y_TIMES:
				return new XDoneYTimesObjective(data);

			default:
				return new Objective(data);
		}
	}

	protected void parseObjectives(JSON[] objectivesJson)
	{

		foreach (JSON objective in objectivesJson)
		{
			Objective newObjective = createObjective(objective);
			objectives.Add(newObjective);

			// Also add it to the gameObjectives dictionary, keyed on game key,
			// for super-fast lookup of all objectives for a particular game.
			if (!string.IsNullOrEmpty(newObjective.game))
			{
				if (!gameObjectives.ContainsKey(newObjective.game))
				{
					// If the dictionary doesn't yet have a list for this game,
					// create one now and add it to the dictionary.
					gameObjectives.Add(newObjective.game, new List<Objective>());
				}
				gameObjectives[newObjective.game].Add(newObjective);
			}
			else if (!hasObjectivesWithoutAGame)
			{
				hasObjectivesWithoutAGame = true;
			}

			if (newObjective.currentAmount > 0)
			{
				hasMadeProgress = true;
			}
		}
	}

	protected void parseRewards(JSON[] rewardsJson)
	{
		foreach (JSON reward in rewardsJson)
		{
			rewards.Add(new MissionReward(reward));
		}
	}

	protected void parseDialogs(JSON dialogs)
	{
		foreach (string state in dialogs.getKeyList())
		{
			dialogStateData.Add(state, new DialogData(dialogs.getJSON(state)));
		}
	}

	public DialogData getDialogByState(string state)
	{
		DialogData data = null;
		if (dialogStateData.TryGetValue(state, out data))
		{
			return data;
		}
		Debug.LogWarning("Didn't find DialogData for state: " + state);
		return null;
	}
	
	public void updateObjectiveProgress(int index, long amount, List<long> constraintAmounts)
	{
		Objective objective = objectives[index];
		objective.currentAmount = amount;

		if (constraintAmounts != null)
		{
			for (int i = 0; i < constraintAmounts.Count; i++)
			{
				if (constraintAmounts[i] > 0)
				{
					XinYObjective xInYObj = objective as XinYObjective;
					if (xInYObj != null)
					{
						xInYObj.updateConstraintAmounts(constraintAmounts);
						break;
					}
				}	
			}	
		}
		

		if (objective.currentAmount > 0)
		{
			hasMadeProgress = true;
		}
	}

	public bool checkCompletedObjectives()
	{
		if (!isComplete)
		{
			foreach (Objective objective in objectives)
			{
				if (!objective.isComplete)
				{
					isComplete = false;
					return isComplete;
				}
			}
		}

		isComplete = true;
		return isComplete;
	}

	// check if the mission has the specified game key tied to one of the objectives
	public bool containsGame(string gameKey)
	{
		return gameObjectives != null && gameObjectives.ContainsKey(gameKey);
	}
	
	// Returns whether all objectives on this mission for the given game have been completed.
	public bool isGameObjectivesComplete(LobbyGame game)
	{
		if (!gameObjectives.ContainsKey(game.keyName))
		{
			return false;
		}
		
		foreach (Objective objective in gameObjectives[game.keyName])
		{
			if (!objective.isComplete)
			{
				return false;
			}
		}
		return true;
	}

	public bool hasIncompleteObjectiveWithoutAGame()
	{
		if (hasObjectivesWithoutAGame)
		{
			foreach (Objective objective in objectives)
			{
				if (string.IsNullOrEmpty(objective.game) && !objective.isComplete)
				{
					return true;
				}
			}
		}

		return false;
	}

	// finish all objectives
	public void complete()
	{
		for (int i = 0; i < objectives.Count; ++i)
		{
			updateObjectiveProgress(i, objectives[i].amountNeeded, new List<long> {0L});
		}

		isComplete = true;
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	public int numObjectivesCompleted
	{
		get
		{
			int completed = 0;
			foreach (Objective objective in objectives)
			{
				if (objective.isComplete)
				{
					completed++;
				}
			}

			return completed;
		}
	}

	public Objective currentObjective
	{
		get
		{

			for (int i = 0; i < objectives.Count; ++i)
			{
				if (!objectives[i].isComplete)
				{
					return objectives[i];
				}
			}

			return objectives.Count > 0 ? objectives[0] : null;
		}
	}

	public long getCreditsReward
	{
		get
		{
			for (int i = 0; i < rewards.Count; i++)
			{
				if (rewards[i].type == MissionReward.RewardType.CREDITS)
				{
					return rewards[i].amount;
				}
			}

			return 0;
		}
	}
}