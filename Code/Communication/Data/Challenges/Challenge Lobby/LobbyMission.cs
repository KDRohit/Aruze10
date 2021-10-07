using UnityEngine;
using System.Collections;

public class LobbyMission : Mission
{
	// =============================
	// CONST
	// =============================
	public const int OBJECTIVES_PER_GAME = 5;

	public LobbyMission(JSON data) : base(data)
	{
		init(data);
	}

	public void refreshLocs()
	{
		foreach (Objective objective in objectives)
		{
			CollectObjective collect = objective as CollectObjective;
			if (collect != null)
			{
				collect.formatSymbol();
			}
			objective.buildLocString();
		}
	}
	
	// checks if the current mission has multiple games (i.e. tier 2, and tier 3 events), check
	// if the passed game key has finished all objectives (static count 5)
	public bool isCompleteByGame(string gameKey)
	{
		// quick evaluation for user on tier 2, or tier 3
		if (!isFirstTier)
		{
			bool gameFound = false;
			int completedObjectives = 0; // this may not be needed, but just incase someone sets up the mission wrong
			
			foreach (Objective objective in objectives)
			{
				if (objective.game == gameKey)
				{
					gameFound = true;
					if (!objective.isComplete)
					{
						return false;
					}
					else
					{
						++completedObjectives;

						if (completedObjectives >= OBJECTIVES_PER_GAME)
						{
							return true;
						}
					}
				}
			}
			return gameFound && completedObjectives >= OBJECTIVES_PER_GAME;
		}
		return isComplete;
	}

	public bool isFirstTier
	{
		get
		{
			return objectives != null && objectives.Count <= OBJECTIVES_PER_GAME;
		}
	}
}
