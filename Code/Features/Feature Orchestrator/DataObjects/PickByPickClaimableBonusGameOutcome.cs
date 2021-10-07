using System.Collections.Generic;
using Com.Rewardables;
using UnityEngine;

namespace FeatureOrchestrator
{
	/*
	 * Proton data object used for keeping track of progress data in a feature
	 */
	public class PickByPickClaimableBonusGameOutcome : BaseDataObject
	{
		public List<ModularChallengeGameOutcome> availablePicks { get; private set; }
		public int currentIndex { get; private set; }
		public int availablePickCount { get; private set; }
		public int currentLadderPosition { get; private set; }
		public int[] currentLandedRungs { get; private set; }
		public ModularChallengeGameOutcome picks { get; private set; }
		public long wager { get; private set; }


		public delegate void outcomeUpdatedDelegate(PickByPickClaimableBonusGameOutcome updatedOutcome);
		public event outcomeUpdatedDelegate onOutcomeUpdated;
		
		public PickByPickClaimableBonusGameOutcome(string keyName, JSON json) : base(keyName, json)
		{
			availablePicks = new List<ModularChallengeGameOutcome>();
			currentIndex = 0;
			availablePickCount = 0;
			currentLadderPosition = 0;
		}

		public override void updateValue(JSON json)
		{
			if (json == null)
			{
				return;
			}
			
			currentIndex = json.getInt("current_pick_index", -1);
			availablePickCount = json.getInt("available_pick_count", 0);
			wager = json.getLong("wager", 0);
			JSON outcomeJson = json.getJSON("outcome_with_available_picks");
			SlotOutcome outcome = new SlotOutcome(outcomeJson);
			picks = new ModularChallengeGameOutcome(outcome, false, wager);
			currentLandedRungs = json.getIntArray("current_ladder_landed_rungs");
			currentLadderPosition = json.getInt("current_ladder_position", 0);
			if (Data.debugMode)
			{
				jsonData = json;
			}

			if (onOutcomeUpdated != null)
			{
				onOutcomeUpdated.Invoke(this);
			}
		}

		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new PickByPickClaimableBonusGameOutcome(keyname, json);
		}
	}
}


