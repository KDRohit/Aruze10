using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is a module to handle picking games that can potentially have more than
// one jackpot awarded and each pick can have more than one group revealed.
// games using this module : zynga05
[ExecuteInEditMode]
public class PickingGameMatchMultipleGroupsModule : PickingGameMatchGroupModule
{
	[SerializeField] protected GroupCombinationProperties groupCombinationProperties;

	private bool isJackpotComplete;

#if UNITY_EDITOR
	void Update()
	{
		groupCombinationProperties.initGroupCombinationLookup();
	}
#endif

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		return pickData != null && pickData.newBaseBonusGamePickGroupIds != null;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		// get the outcome for this pick
		ModularChallengeGameOutcomeEntry currentPickOutcome = pickingVariantParent.getCurrentPickOutcome();
		string groupName = groupCombinationProperties.getGroupNameFromCombination(currentPickOutcome.groupCombination);

		// calculate credits and set the labels
		long credits = calculateTotalCredits(currentPickOutcome);
		updateCreditLabels(pickItem, credits);

		// Play the animations and sounds for the item the player picked.
		setPickItemRevealAnimation(pickItem, currentPickOutcome, groupName);
		playRevealSounds(currentPickOutcome, groupName);
		yield return StartCoroutine(executeBasicOnRevealPick(pickItem));

		// update, animate, and play sounds for the jackpots
		yield return StartCoroutine(updateJackpots(pickItem, currentPickOutcome, credits));

		if (isJackpotComplete)
		{
			yield return StartCoroutine(animateCompletedJackpots(pickItem, currentPickOutcome));
		}
	}

	protected virtual IEnumerator updateJackpots(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry pickOutcome, long credits)
	{
		for (int i = 0; i < pickOutcome.groupIds.Length; i++)
		{
			// get information and state about this group and animate the jackpot
			string groupId = pickOutcome.groupIds[i];
			ModularChallengeGameOutcome.PickGroupInfo currentPickGroup = roundVariantParent.outcome.getPickInfoForGroup(groupId);
			PickGroupState currentPickGroupState = getCurrentGroupState(groupId);
			yield return StartCoroutine(animateJackpotProgress(groupId, pickOutcome.newBaseBonusGameGroupHits, currentPickGroupState));

			// update the state of the group and store the match so we can reveal it later when
			// the player wins jackpots
			currentPickGroupState.hitCount += pickOutcome.newBaseBonusGameGroupHits;
			storeMatchedItemForGroup(pickItem, currentPickGroupState, groupCombinationProperties.getGroupNameFromCombination(pickOutcome.groupCombination), pickOutcome.newBaseBonusGameGroupHits);

			// award credits (note this will add to the BonusGamePresenter.instance.currentPayout as well)
			if (AWARD_EACH_CREDIT)
			{
				yield return StartCoroutine(rollupCredits(credits));
			}

			if (currentPickGroupState.hitCount >= currentPickGroup.hitsNeeded)
			{
				isJackpotComplete = true;
			}
		}
	}

	protected virtual IEnumerator animateCompletedJackpots(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry pickOutcome)
	{
		long jackpotCredits = 0;
		for (int i = 0; i < pickOutcome.groupIds.Length; i++)
		{
			string groupId = pickOutcome.groupIds[i];
			ModularChallengeGameOutcome.PickGroupInfo currentPickGroup = roundVariantParent.outcome.getPickInfoForGroup(groupId);
			PickGroupState currentPickGroupState = getCurrentGroupState(groupId);

			if (currentPickGroupState.hitCount >= currentPickGroup.hitsNeeded)
			{
				jackpotCredits += calculateCredits(currentPickGroup);
				StartCoroutine(animateMatchedItems(currentPickGroupState));
				yield return StartCoroutine(playRevealMatchAudioAndWait());
				yield return StartCoroutine(animateJackpotWin(groupId));

				if (!AWARD_EACH_CREDIT && !SHOULD_ROLLUP_JACKPOT_CREDITS_TOGETHER)
				{
					long endRollup = BonusGamePresenter.instance.currentPayout + jackpotCredits * roundVariantParent.gameParent.currentMultiplier;
					yield return StartCoroutine(rollupCredits(BonusGamePresenter.instance.currentPayout, endRollup));
				}

				yield return StartCoroutine(animateMultiplierBoxes());
				yield return StartCoroutine(animateMultipierParticleTrails(pickItem));
				yield return StartCoroutine(animateJackpotWinOutro(groupId));
			}
		}

		
		if (jackpotCredits > 0 && !AWARD_EACH_CREDIT && SHOULD_ROLLUP_JACKPOT_CREDITS_TOGETHER)
		{
			long endRollup = BonusGamePresenter.instance.currentPayout + jackpotCredits * roundVariantParent.gameParent.currentMultiplier; 
			yield return StartCoroutine(rollupCredits(BonusGamePresenter.instance.currentPayout, endRollup));
		}
	}

	// Gets the combined number of credits for each group in the pickOutcome
	protected virtual long calculateTotalCredits(ModularChallengeGameOutcomeEntry pickOutcome)
	{
		long credits = 0;
		for (int i = 0; i < pickOutcome.groupIds.Length; i++)
		{
			ModularChallengeGameOutcome.PickGroupInfo currentPickGroup = roundVariantParent.outcome.getPickInfoForGroup(pickOutcome.groupIds[i]);
			credits += calculateCredits(currentPickGroup);
		}
		return credits;
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		string groupName = groupCombinationProperties.getGroupNameFromCombination(leftoverOutcome.groupCombination);
		yield return StartCoroutine(executeOnRevealLeftoverWithGroupName(leftover, groupName));
	}

	// This is a class that allows us to map group combinations to arbitrary groupNames.
	// A group combination is a concatenation of the possible groups in a round pick.
	// The reason we need this is because there may exist a case, where
	// the order of groups from the server matters or the order of the groups from the
	// server is not guaranteed. Used in zynga05.
	[System.Serializable]
	protected class GroupCombinationProperties
	{
		public bool autoGeneratedGroupCombinationsForMap = false;
		public bool excludeAlreadyOrderedGroupsFromCombinationMap = false;
		public int maxGroupCombinationLength = 0;

		public List<string> groupIds;

		// A list of all the combinations and the groupName they map to.
		public List<GroupCombinationToName> groupCombinationToNameMaps;

		// A lookup dictionary so we can quickly lookup the groupName a combination maps to.
		private Dictionary<string, string> _groupCombinationLookup;

		// Returns the groupName to use for this combination of groups.
		// If no groupName is found it defaults to the groupCombination. 
		public string getGroupNameFromCombination(string groupCombination)
		{
			if (_groupCombinationLookup == null)
			{
				initGroupCombinationLookup();
			}

			if (_groupCombinationLookup.ContainsKey(groupCombination))
			{
				return _groupCombinationLookup[groupCombination];
			}

			return groupCombination;
		}

		private void repopulateGroupCombinationToNameMaps()
		{
			groupCombinationToNameMaps = new List<GroupCombinationToName>();
			foreach (var pair in _groupCombinationLookup)
			{
				GroupCombinationToName newItem = new GroupCombinationToName();
				newItem.groupCombination = pair.Key;
				newItem.groupName = pair.Value;
				groupCombinationToNameMaps.Add(newItem);
			}
		}

		// Populates the _groupCombinationLookup
		public void initGroupCombinationLookup()
		{
			if (autoGeneratedGroupCombinationsForMap)
			{
				generateGroupCombinations();
				repopulateGroupCombinationToNameMaps();
				return;
			}

			_groupCombinationLookup = new Dictionary<string, string>();
			for (int i = 0; i < groupCombinationToNameMaps.Count; i++)
			{
				string groupCombination = groupCombinationToNameMaps[i].groupCombination;
				if (!_groupCombinationLookup.ContainsKey(groupCombination))
				{
					_groupCombinationLookup.Add(groupCombination, groupCombinationToNameMaps[i].groupName);
				}
			}
		}

		// Automatically create a mapping for all combinations of groupIds to groupName
		private void generateGroupCombinations()
		{
			_groupCombinationLookup = new Dictionary<string, string>();

			List<string> sortedGroupIds = new List<string>(groupIds);
			sortedGroupIds.Sort();

			AddSingleGroupsToMap(sortedGroupIds);

			// Create all possible groupNames from the sorted groupId list
			// and add all the permutations for it.
			for (int i = 0; i < maxGroupCombinationLength - 1; i++)
			{
				for (int j = 0; j < sortedGroupIds.Count; j++)
				{
					for (int k = j + 1; k < sortedGroupIds.Count - i; k++)
					{
						string groupName = sortedGroupIds[j] + string.Join("", sortedGroupIds.GetRange(k, i + 1).ToArray());
						List<string> subGroupIds = getSubGroupIds(sortedGroupIds, j, k, i);
						List<string> subGroupPermutations = StringPermutations(subGroupIds);
						AddPerumutations(subGroupPermutations, groupName);
					}
				}
			}
		}

		// Add the perumutations from the list 
		private void AddPerumutations(List<string> subGroupPermutations, string groupName)
		{
			for (int i = 0; i < subGroupPermutations.Count; i++)
			{
				string groupPermutation = subGroupPermutations[i];
				if (!_groupCombinationLookup.ContainsKey(groupPermutation))
				{
					if (groupPermutation != groupName || !excludeAlreadyOrderedGroupsFromCombinationMap)
					{
						_groupCombinationLookup.Add(groupPermutation, groupName);
					}
				}
			}
		}

		private List<string> getSubGroupIds(List<string> sortedGroupIds, int currentGroupIndex, int start, int length)
		{
			List<string> subGroupIds = new List<string>();
			subGroupIds.Add(sortedGroupIds[currentGroupIndex]);
			for (int i = start; i <= start + length; i++)
			{
				subGroupIds.Add(sortedGroupIds[i]);
			}
			return subGroupIds;
		}

		private void AddSingleGroupsToMap(List<string> sortedGroupIds) 
		{
			if (!excludeAlreadyOrderedGroupsFromCombinationMap)
			{
				for (int i = 0; i < sortedGroupIds.Count; i++)
				{
					if (!_groupCombinationLookup.ContainsKey(sortedGroupIds[i]))
					{
						_groupCombinationLookup.Add(sortedGroupIds[i], sortedGroupIds[i]);
					}
				}
			}
		}

		private List<string> StringPermutations(List<string> list)
		{
			if (list.Count == 1)
			{
				return list;
			}

			List<string> perms = new List<string>();

			foreach (string s1 in list)
			{
				string c = s1;

				List<string> subList = new List<string>();

				foreach (string s2 in list)
				{
					if (s2 != s1)
					{
						subList.Add(s2);
					}
				}

				List<string> subPermutations = StringPermutations(subList);
				foreach (string s3 in subPermutations)
				{
					perms.Add(s1 + s3);
				}
			}

			return perms;
		}
	}

	// A container to hold our groupCombination to groupName mappings.
	[System.Serializable]
	protected class GroupCombinationToName : System.Object
	{
		public string groupCombination;
		public string groupName;
	}
}